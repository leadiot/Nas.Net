using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nas.Alipan
{
    /// <summary>
    /// 阿里云盘开放平台 API 封装
    /// 基于 PDS 开放 API 实现 OAuth2 认证和文件管理
    /// </summary>
    public class AlipanApi
    {
        private readonly AlipanConfig _config;
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public AlipanApi(AlipanConfig config)
        {
            _config = config;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(config.ApiBaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        #region OAuth2 认证

        /// <summary>
        /// 获取 OAuth2 授权URL，用于用户扫码登录
        /// </summary>
        public string GetAuthorizationUrl()
        {
            return $"{_config.AuthorizeUrl}?client_id={_config.ClientId}&redirect_uri={Uri.EscapeDataString(_config.RedirectUri)}&scope={Uri.EscapeDataString(_config.Scope)}&response_type=code";
        }

        /// <summary>
        /// 通过授权码获取访问令牌
        /// </summary>
        public async Task<TokenResponse> GetTokenByCodeAsync(string code)
        {
            var parameters = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "client_id", _config.ClientId },
                { "client_secret", _config.ClientSecret },
                { "redirect_uri", _config.RedirectUri }
            };

            using var content = new FormUrlEncodedContent(parameters);
            using var response = await _httpClient.PostAsync(_config.TokenUrl, content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TokenResponse>(json, _jsonOptions);

            if (result != null)
            {
                UpdateToken(result);
            }

            return result!;
        }

        /// <summary>
        /// 通过刷新令牌获取新的访问令牌
        /// </summary>
        public async Task<TokenResponse> RefreshTokenAsync()
        {
            if (string.IsNullOrEmpty(_config.RefreshToken))
                throw new InvalidOperationException("刷新令牌为空");

            var parameters = new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", _config.RefreshToken },
                { "client_id", _config.ClientId },
                { "client_secret", _config.ClientSecret }
            };

            using var content = new FormUrlEncodedContent(parameters);
            using var response = await _httpClient.PostAsync(_config.TokenUrl, content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TokenResponse>(json, _jsonOptions);

            if (result != null)
            {
                UpdateToken(result);
            }

            return result!;
        }

        /// <summary>
        /// 更新令牌信息到配置
        /// </summary>
        private void UpdateToken(TokenResponse token)
        {
            _config.AccessToken = token.AccessToken;
            _config.RefreshToken = token.RefreshToken;
            _config.ExpireTime = DateTimeOffset.Now.AddSeconds(token.ExpiresIn).ToUnixTimeSeconds();

            if (!string.IsNullOrEmpty(token.DefaultDriveId))
            {
                _config.DefaultDriveId = token.DefaultDriveId;
            }

            if (!string.IsNullOrEmpty(token.UserId))
            {
                _config.UserId = token.UserId;
            }
        }

        /// <summary>
        /// 确保访问令牌有效，过期则自动刷新
        /// </summary>
        private async Task EnsureTokenValidAsync()
        {
            if (string.IsNullOrEmpty(_config.AccessToken) ||
                DateTimeOffset.Now.ToUnixTimeSeconds() >= _config.ExpireTime - 60)
            {
                await RefreshTokenAsync();
            }
        }

        /// <summary>
        /// 设置请求Authorization头
        /// </summary>
        private void SetAuthorizationHeader()
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _config.AccessToken);
        }

        #endregion

        #region 空间管理

        /// <summary>
        /// 获取我的空间列表
        /// </summary>
        public async Task<ListMyDrivesResponse> ListMyDrivesAsync()
        {
            await EnsureTokenValidAsync();
            SetAuthorizationHeader();

            var url = "/v2/drive/list_my_drives";
            using var response = await _httpClient.PostAsync(url, null);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ListMyDrivesResponse>(json, _jsonOptions)!;
        }

        /// <summary>
        /// 获取默认空间信息
        /// </summary>
        public async Task<DriveInfo> GetDefaultDriveAsync()
        {
            await EnsureTokenValidAsync();
            SetAuthorizationHeader();

            var url = "/v2/drive/get_default_drive";
            using var response = await _httpClient.PostAsync(url, null);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<GetDefaultDriveResponse>(json, _jsonOptions)!;
            return result.Drive;
        }

        /// <summary>
        /// 获取空间容量信息
        /// </summary>
        public async Task<DriveInfo> GetDriveAsync(string driveId)
        {
            await EnsureTokenValidAsync();
            SetAuthorizationHeader();

            var body = new { drive_id = driveId };
            var jsonBody = JsonSerializer.Serialize(body, _jsonOptions);
            using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var url = "/v2/drive/get";
            using var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<GetDriveResponse>(json, _jsonOptions)!;
            return result.Drive;
        }

        #endregion

        #region 文件管理

        /// <summary>
        /// 列举文件夹下的文件和子文件夹
        /// </summary>
        /// <param name="driveId">空间ID</param>
        /// <param name="parentFileId">父文件夹ID，根目录使用 "root"</param>
        /// <param name="limit">返回数量限制 [1,100]</param>
        /// <param name="orderBy">排序字段: created_at, updated_at, size, name</param>
        /// <param name="orderDirection">排序方向: ASC, DESC</param>
        /// <param name="marker">分页标记</param>
        /// <param name="type">文件类型过滤: file, folder（空则返回全部）</param>
        /// <param name="category">文件分类: image, video, doc, audio, zip, app, others</param>
        public async Task<ListFileResponse> ListFilesAsync(string driveId, string parentFileId = "root",
            int limit = 100, string orderBy = "name", string orderDirection = "ASC",
            string? marker = null, string? type = null, string? category = null)
        {
            await EnsureTokenValidAsync();
            SetAuthorizationHeader();

            var bodyDict = new Dictionary<string, object?>
            {
                { "drive_id", driveId },
                { "parent_file_id", parentFileId },
                { "limit", limit },
                { "order_by", orderBy },
                { "order_direction", orderDirection }
            };

            if (!string.IsNullOrEmpty(marker))
                bodyDict["marker"] = marker;
            if (!string.IsNullOrEmpty(type))
                bodyDict["type"] = type;
            if (!string.IsNullOrEmpty(category))
                bodyDict["category"] = category;

            var jsonBody = JsonSerializer.Serialize(bodyDict, _jsonOptions);
            using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var url = "/v2/file/list";
            using var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ListFileResponse>(json, _jsonOptions)!;
        }

        /// <summary>
        /// 获取文件详情
        /// </summary>
        public async Task<FileItem> GetFileAsync(string driveId, string fileId)
        {
            await EnsureTokenValidAsync();
            SetAuthorizationHeader();

            var body = new { drive_id = driveId, file_id = fileId };
            var jsonBody = JsonSerializer.Serialize(body, _jsonOptions);
            using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var url = "/v2/file/get";
            using var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<FileItem>(json, _jsonOptions)!;
        }

        /// <summary>
        /// 搜索文件
        /// </summary>
        public async Task<SearchFileResponse> SearchFileAsync(string driveId, string query,
            int limit = 100, string? marker = null, string? category = null, string? type = null)
        {
            await EnsureTokenValidAsync();
            SetAuthorizationHeader();

            var bodyDict = new Dictionary<string, object?>
            {
                { "drive_id", driveId },
                { "query", query },
                { "limit", limit }
            };

            if (!string.IsNullOrEmpty(marker))
                bodyDict["marker"] = marker;
            if (!string.IsNullOrEmpty(category))
                bodyDict["category"] = category;
            if (!string.IsNullOrEmpty(type))
                bodyDict["type"] = type;

            var jsonBody = JsonSerializer.Serialize(bodyDict, _jsonOptions);
            using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var url = "/v2/file/search";
            using var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SearchFileResponse>(json, _jsonOptions)!;
        }

        /// <summary>
        /// 创建文件夹
        /// </summary>
        public async Task<FileItem> CreateFolderAsync(string driveId, string parentFileId, string name)
        {
            await EnsureTokenValidAsync();
            SetAuthorizationHeader();

            var body = new
            {
                drive_id = driveId,
                parent_file_id = parentFileId,
                name = name,
                type = "folder"
            };
            var jsonBody = JsonSerializer.Serialize(body, _jsonOptions);
            using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var url = "/v2/file/create";
            using var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<FileItem>(json, _jsonOptions)!;
        }

        /// <summary>
        /// 删除文件或文件夹（移至回收站）
        /// </summary>
        public async Task<bool> TrashFileAsync(string driveId, string fileId)
        {
            await EnsureTokenValidAsync();
            SetAuthorizationHeader();

            var body = new { drive_id = driveId, file_id = fileId };
            var jsonBody = JsonSerializer.Serialize(body, _jsonOptions);
            using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var url = "/v2/file/trash";
            using var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            return true;
        }

        /// <summary>
        /// 更新文件信息（重命名等）
        /// </summary>
        public async Task<FileItem> UpdateFileAsync(string driveId, string fileId, string? name = null,
            string? description = null, bool? starred = null, bool? hidden = null)
        {
            await EnsureTokenValidAsync();
            SetAuthorizationHeader();

            var bodyDict = new Dictionary<string, object?>
            {
                { "drive_id", driveId },
                { "file_id", fileId }
            };

            if (!string.IsNullOrEmpty(name))
                bodyDict["name"] = name;
            if (!string.IsNullOrEmpty(description))
                bodyDict["description"] = description;
            if (starred.HasValue)
                bodyDict["starred"] = starred.Value;
            if (hidden.HasValue)
                bodyDict["hidden"] = hidden.Value;

            var jsonBody = JsonSerializer.Serialize(bodyDict, _jsonOptions);
            using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var url = "/v2/file/update";
            using var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<FileItem>(json, _jsonOptions)!;
        }

        /// <summary>
        /// 移动文件或文件夹
        /// </summary>
        public async Task<bool> MoveFileAsync(string driveId, string fileId, string toParentFileId)
        {
            await EnsureTokenValidAsync();
            SetAuthorizationHeader();

            var body = new
            {
                drive_id = driveId,
                file_id = fileId,
                to_parent_file_id = toParentFileId
            };
            var jsonBody = JsonSerializer.Serialize(body, _jsonOptions);
            using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var url = "/v2/file/move";
            using var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            return true;
        }

        /// <summary>
        /// 复制文件或文件夹
        /// </summary>
        public async Task<CopyFileResponse> CopyFileAsync(string driveId, string fileId, string toParentFileId,
            string? toDriveId = null, string? newName = null)
        {
            await EnsureTokenValidAsync();
            SetAuthorizationHeader();

            var bodyDict = new Dictionary<string, object?>
            {
                { "drive_id", driveId },
                { "file_id", fileId },
                { "to_parent_file_id", toParentFileId }
            };

            if (!string.IsNullOrEmpty(toDriveId))
                bodyDict["to_drive_id"] = toDriveId;
            if (!string.IsNullOrEmpty(newName))
                bodyDict["new_name"] = newName;

            var jsonBody = JsonSerializer.Serialize(bodyDict, _jsonOptions);
            using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var url = "/v2/file/copy";
            using var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<CopyFileResponse>(json, _jsonOptions)!;
        }

        #endregion

        #region 下载

        /// <summary>
        /// 获取文件下载地址
        /// </summary>
        public async Task<GetDownloadUrlResponse> GetDownloadUrlAsync(string driveId, string fileId)
        {
            await EnsureTokenValidAsync();
            SetAuthorizationHeader();

            var body = new { drive_id = driveId, file_id = fileId };
            var jsonBody = JsonSerializer.Serialize(body, _jsonOptions);
            using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var url = "/v2/file/get_download_url";
            using var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<GetDownloadUrlResponse>(json, _jsonOptions)!;
        }

        #endregion

        #region 上传

        /// <summary>
        /// 获取文件上传地址（小文件直接上传）
        /// </summary>
        public async Task<GetUploadUrlResponse> GetUploadUrlAsync(string driveId, string parentFileId,
            string fileName, long fileSize, string? contentHash = null, string? contentHashName = null)
        {
            await EnsureTokenValidAsync();
            SetAuthorizationHeader();

            var bodyDict = new Dictionary<string, object?>
            {
                { "drive_id", driveId },
                { "parent_file_id", parentFileId },
                { "name", fileName },
                { "type", "file" },
                { "size", fileSize }
            };

            if (!string.IsNullOrEmpty(contentHash))
                bodyDict["content_hash"] = contentHash;
            if (!string.IsNullOrEmpty(contentHashName))
                bodyDict["content_hash_name"] = contentHashName;

            var jsonBody = JsonSerializer.Serialize(bodyDict, _jsonOptions);
            using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var url = "/v2/file/get_upload_url";
            using var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<GetUploadUrlResponse>(json, _jsonOptions)!;
        }

        /// <summary>
        /// 创建文件（用于分片上传的第一步）
        /// </summary>
        public async Task<FileItem> CreateFileAsync(string driveId, string parentFileId, string fileName,
            long fileSize, string? contentHash = null, string contentHashName = "sha1")
        {
            await EnsureTokenValidAsync();
            SetAuthorizationHeader();

            var bodyDict = new Dictionary<string, object?>
            {
                { "drive_id", driveId },
                { "parent_file_id", parentFileId },
                { "name", fileName },
                { "type", "file" },
                { "size", fileSize },
                { "content_hash_name", contentHashName }
            };

            if (!string.IsNullOrEmpty(contentHash))
                bodyDict["content_hash"] = contentHash;

            var jsonBody = JsonSerializer.Serialize(bodyDict, _jsonOptions);
            using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var url = "/v2/file/create";
            using var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<FileItem>(json, _jsonOptions)!;
        }

        /// <summary>
        /// 获取分片上传地址（大文件分片上传）
        /// </summary>
        public async Task<GetUploadUrlResponse> GetUploadUrlWithPartInfoAsync(string driveId, string fileId,
            string uploadId, int partNumber)
        {
            await EnsureTokenValidAsync();
            SetAuthorizationHeader();

            var body = new
            {
                drive_id = driveId,
                file_id = fileId,
                upload_id = uploadId,
                part_info_list = new[]
                {
                    new { part_number = partNumber }
                }
            };
            var jsonBody = JsonSerializer.Serialize(body, _jsonOptions);
            using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var url = "/v2/file/get_upload_url";
            using var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<GetUploadUrlResponse>(json, _jsonOptions)!;
        }

        /// <summary>
        /// 完成文件上传
        /// </summary>
        public async Task<FileItem> CompleteFileAsync(string driveId, string fileId, string uploadId)
        {
            await EnsureTokenValidAsync();
            SetAuthorizationHeader();

            var body = new { drive_id = driveId, file_id = fileId, upload_id = uploadId };
            var jsonBody = JsonSerializer.Serialize(body, _jsonOptions);
            using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var url = "/v2/file/complete";
            using var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<FileItem>(json, _jsonOptions)!;
        }

        /// <summary>
        /// 上传小文件（直接上传，适用于小于4MB的文件）
        /// </summary>
        public async Task<FileItem> UploadSmallFileAsync(string driveId, string parentFileId,
            string localFilePath)
        {
            await EnsureTokenValidAsync();

            var fileInfo = new FileInfo(localFilePath);
            var fileName = fileInfo.Name;
            var fileSize = fileInfo.Length;

            // 获取上传地址
            var uploadUrlResponse = await GetUploadUrlAsync(driveId, parentFileId, fileName, fileSize);

            if (uploadUrlResponse.PartInfoList != null && uploadUrlResponse.PartInfoList.Length > 0)
            {
                // 上传到获取的URL
                using var fileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read);
                using var streamContent = new StreamContent(fileStream);
                streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");

                using var uploadClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
                using var uploadResponse = await uploadClient.PutAsync(
                    uploadUrlResponse.PartInfoList[0].UploadUrl, streamContent);
                uploadResponse.EnsureSuccessStatusCode();
            }

            // 完成上传
            if (!string.IsNullOrEmpty(uploadUrlResponse.UploadId))
            {
                return await CompleteFileAsync(driveId, uploadUrlResponse.FileId, uploadUrlResponse.UploadId);
            }

            return await GetFileAsync(driveId, uploadUrlResponse.FileId);
        }

        /// <summary>
        /// 上传大文件（分片上传，适用于大于4MB的文件）
        /// </summary>
        public async Task<FileItem> UploadLargeFileAsync(string driveId, string parentFileId,
            string localFilePath, int chunkSize = 4 * 1024 * 1024)
        {
            await EnsureTokenValidAsync();

            var fileInfo = new FileInfo(localFilePath);
            var fileName = fileInfo.Name;
            var fileSize = fileInfo.Length;
            var sha1 = CalculateSha1(localFilePath);

            // 1. 创建文件
            var createdFile = await CreateFileAsync(driveId, parentFileId, fileName, fileSize, sha1, "sha1");

            if (string.IsNullOrEmpty(createdFile.UploadId))
            {
                // 秒传成功（文件已存在）
                return createdFile;
            }

            // 2. 分片上传
            var partCount = (int)Math.Ceiling((double)fileSize / chunkSize);
            var partInfoRequests = new List<object>();
            for (int i = 1; i <= partCount; i++)
            {
                partInfoRequests.Add(new { part_number = i });
            }

            var body = new
            {
                drive_id = driveId,
                file_id = createdFile.FileId,
                upload_id = createdFile.UploadId,
                part_info_list = partInfoRequests
            };
            var jsonBody = JsonSerializer.Serialize(body, _jsonOptions);
            SetAuthorizationHeader();
            using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync("/v2/file/get_upload_url", content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var uploadUrlResponse = JsonSerializer.Deserialize<GetUploadUrlResponse>(json, _jsonOptions)!;

            // 上传每个分片
            using var uploadClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            for (int i = 0; i < partCount; i++)
            {
                var start = (long)i * chunkSize;
                var end = Math.Min(start + chunkSize, fileSize);
                var partSize = end - start;

                using var fileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read);
                fileStream.Seek(start, SeekOrigin.Begin);

                var chunkData = new byte[partSize];
                int bytesRead = 0;
                while (bytesRead < partSize)
                {
                    int read = await fileStream.ReadAsync(chunkData.AsMemory(bytesRead, (int)(partSize - bytesRead)));
                    if (read == 0) break;
                    bytesRead += read;
                }

                using var chunkContent = new ByteArrayContent(chunkData);
                chunkContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");

                if (uploadUrlResponse.PartInfoList != null && i < uploadUrlResponse.PartInfoList.Length)
                {
                    using var uploadResponse = await uploadClient.PutAsync(
                        uploadUrlResponse.PartInfoList[i].UploadUrl, chunkContent);
                    uploadResponse.EnsureSuccessStatusCode();
                }
            }

            // 3. 完成上传
            return await CompleteFileAsync(driveId, createdFile.FileId, createdFile.UploadId);
        }

        #endregion

        #region 回收站

        /// <summary>
        /// 列举回收站文件
        /// </summary>
        public async Task<ListFileResponse> ListRecyclebinAsync(string driveId, int limit = 100, string? marker = null)
        {
            await EnsureTokenValidAsync();
            SetAuthorizationHeader();

            var bodyDict = new Dictionary<string, object?>
            {
                { "drive_id", driveId },
                { "limit", limit }
            };

            if (!string.IsNullOrEmpty(marker))
                bodyDict["marker"] = marker;

            var jsonBody = JsonSerializer.Serialize(bodyDict, _jsonOptions);
            using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var url = "/v2/recyclebin/list";
            using var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ListFileResponse>(json, _jsonOptions)!;
        }

        /// <summary>
        /// 从回收站恢复文件
        /// </summary>
        public async Task<bool> RestoreFileAsync(string driveId, string fileId)
        {
            await EnsureTokenValidAsync();
            SetAuthorizationHeader();

            var body = new { drive_id = driveId, file_id = fileId };
            var jsonBody = JsonSerializer.Serialize(body, _jsonOptions);
            using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var url = "/v2/recyclebin/restore";
            using var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            return true;
        }

        /// <summary>
        /// 清空回收站
        /// </summary>
        public async Task<bool> ClearRecyclebinAsync(string driveId)
        {
            await EnsureTokenValidAsync();
            SetAuthorizationHeader();

            var body = new { drive_id = driveId };
            var jsonBody = JsonSerializer.Serialize(body, _jsonOptions);
            using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var url = "/v2/recyclebin/clear";
            using var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            return true;
        }

        #endregion

        #region 增量同步

        /// <summary>
        /// 获取增量操作游标
        /// </summary>
        public async Task<string> GetDeltaLastCursorAsync(string driveId)
        {
            await EnsureTokenValidAsync();
            SetAuthorizationHeader();

            var body = new { drive_id = driveId };
            var jsonBody = JsonSerializer.Serialize(body, _jsonOptions);
            using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var url = "/v2/delta/get_last_cursor";
            using var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<DeltaCursorResponse>(json, _jsonOptions)!;
            return result.Cursor;
        }

        /// <summary>
        /// 列举增量变化
        /// </summary>
        public async Task<ListDeltaResponse> ListDeltaAsync(string driveId, string? cursor = null, int limit = 100)
        {
            await EnsureTokenValidAsync();
            SetAuthorizationHeader();

            var bodyDict = new Dictionary<string, object?>
            {
                { "drive_id", driveId },
                { "limit", limit }
            };

            if (!string.IsNullOrEmpty(cursor))
                bodyDict["cursor"] = cursor;

            var jsonBody = JsonSerializer.Serialize(bodyDict, _jsonOptions);
            using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var url = "/v2/delta/list";
            using var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ListDeltaResponse>(json, _jsonOptions)!;
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 计算 SHA1 值
        /// </summary>
        private string CalculateSha1(string filePath)
        {
            using var sha1 = System.Security.Cryptography.SHA1.Create();
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var hash = sha1.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToUpper();
        }

        #endregion

        #region DTO 定义

        /// <summary>
        /// OAuth2 令牌响应
        /// </summary>
        public class TokenResponse
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; } = string.Empty;

            [JsonPropertyName("refresh_token")]
            public string RefreshToken { get; set; } = string.Empty;

            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }

            [JsonPropertyName("token_type")]
            public string TokenType { get; set; } = "Bearer";

            [JsonPropertyName("user_id")]
            public string UserId { get; set; } = string.Empty;

            [JsonPropertyName("user_name")]
            public string UserName { get; set; } = string.Empty;

            [JsonPropertyName("nick_name")]
            public string NickName { get; set; } = string.Empty;

            [JsonPropertyName("avatar")]
            public string Avatar { get; set; } = string.Empty;

            [JsonPropertyName("default_drive_id")]
            public string DefaultDriveId { get; set; } = string.Empty;

            [JsonPropertyName("default_sbox_drive_id")]
            public string DefaultSboxDriveId { get; set; } = string.Empty;

            [JsonPropertyName("role")]
            public string Role { get; set; } = string.Empty;

            [JsonPropertyName("status")]
            public string Status { get; set; } = string.Empty;

            [JsonPropertyName("expire_time")]
            public string ExpireTime { get; set; } = string.Empty;

            [JsonPropertyName("state")]
            public string State { get; set; } = string.Empty;

            [JsonPropertyName("pin_setup")]
            public bool PinSetup { get; set; }

            [JsonPropertyName("is_first_login")]
            public bool IsFirstLogin { get; set; }

            [JsonPropertyName("device_id")]
            public string DeviceId { get; set; } = string.Empty;

            [JsonPropertyName("device_name")]
            public string DeviceName { get; set; } = string.Empty;

            [JsonPropertyName("domain_id")]
            public string DomainId { get; set; } = string.Empty;
        }

        /// <summary>
        /// 空间列表响应
        /// </summary>
        public class ListMyDrivesResponse
        {
            [JsonPropertyName("items")]
            public List<DriveInfo> Items { get; set; } = new();
        }

        /// <summary>
        /// 空间信息
        /// </summary>
        public class DriveInfo
        {
            [JsonPropertyName("drive_id")]
            public string DriveId { get; set; } = string.Empty;

            [JsonPropertyName("drive_name")]
            public string DriveName { get; set; } = string.Empty;

            [JsonPropertyName("drive_type")]
            public string DriveType { get; set; } = string.Empty;

            [JsonPropertyName("owner_type")]
            public string OwnerType { get; set; } = string.Empty;

            [JsonPropertyName("owner_user_id")]
            public string OwnerUserId { get; set; } = string.Empty;

            [JsonPropertyName("total_size")]
            public long TotalSize { get; set; }

            [JsonPropertyName("used_size")]
            public long UsedSize { get; set; }

            [JsonPropertyName("available_size")]
            public long AvailableSize { get; set; }
        }

        /// <summary>
        /// 获取默认空间响应
        /// </summary>
        public class GetDefaultDriveResponse
        {
            [JsonPropertyName("drive")]
            public DriveInfo Drive { get; set; } = new();
        }

        /// <summary>
        /// 获取空间信息响应
        /// </summary>
        public class GetDriveResponse
        {
            [JsonPropertyName("drive")]
            public DriveInfo Drive { get; set; } = new();
        }

        /// <summary>
        /// 文件列表响应
        /// </summary>
        public class ListFileResponse
        {
            [JsonPropertyName("items")]
            public List<FileItem> Items { get; set; } = new();

            [JsonPropertyName("next_marker")]
            public string NextMarker { get; set; } = string.Empty;
        }

        /// <summary>
        /// 搜索文件响应
        /// </summary>
        public class SearchFileResponse
        {
            [JsonPropertyName("items")]
            public List<FileItem> Items { get; set; } = new();

            [JsonPropertyName("next_marker")]
            public string NextMarker { get; set; } = string.Empty;
        }

        /// <summary>
        /// 文件信息
        /// </summary>
        public class FileItem
        {
            [JsonPropertyName("drive_id")]
            public string DriveId { get; set; } = string.Empty;

            [JsonPropertyName("file_id")]
            public string FileId { get; set; } = string.Empty;

            [JsonPropertyName("parent_file_id")]
            public string ParentFileId { get; set; } = string.Empty;

            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("type")]
            public string Type { get; set; } = string.Empty;

            [JsonPropertyName("size")]
            public long Size { get; set; }

            [JsonPropertyName("file_extension")]
            public string FileExtension { get; set; } = string.Empty;

            [JsonPropertyName("content_type")]
            public string ContentType { get; set; } = string.Empty;

            [JsonPropertyName("category")]
            public string Category { get; set; } = string.Empty;

            [JsonPropertyName("created_at")]
            public DateTime CreatedAt { get; set; }

            [JsonPropertyName("updated_at")]
            public DateTime UpdatedAt { get; set; }

            [JsonPropertyName("status")]
            public string Status { get; set; } = string.Empty;

            [JsonPropertyName("starred")]
            public bool Starred { get; set; }

            [JsonPropertyName("hidden")]
            public bool Hidden { get; set; }

            [JsonPropertyName("description")]
            public string Description { get; set; } = string.Empty;

            [JsonPropertyName("upload_id")]
            public string UploadId { get; set; } = string.Empty;

            [JsonPropertyName("crc64_hash")]
            public string Crc64Hash { get; set; } = string.Empty;

            [JsonPropertyName("content_hash")]
            public string ContentHash { get; set; } = string.Empty;

            [JsonPropertyName("content_hash_name")]
            public string ContentHashName { get; set; } = string.Empty;

            [JsonPropertyName("download_url")]
            public string DownloadUrl { get; set; } = string.Empty;

            [JsonPropertyName("thumbnail")]
            public string Thumbnail { get; set; } = string.Empty;

            /// <summary>
            /// 是否为文件夹
            /// </summary>
            public bool IsFolder => Type == "folder";
        }

        /// <summary>
        /// 获取下载地址响应
        /// </summary>
        public class GetDownloadUrlResponse
        {
            [JsonPropertyName("drive_id")]
            public string DriveId { get; set; } = string.Empty;

            [JsonPropertyName("file_id")]
            public string FileId { get; set; } = string.Empty;

            [JsonPropertyName("download_url")]
            public string DownloadUrl { get; set; } = string.Empty;

            [JsonPropertyName("expire_time")]
            public string ExpireTime { get; set; } = string.Empty;

            [JsonPropertyName("crc64_hash")]
            public string Crc64Hash { get; set; } = string.Empty;

            [JsonPropertyName("content_hash")]
            public string ContentHash { get; set; } = string.Empty;

            [JsonPropertyName("content_hash_name")]
            public string ContentHashName { get; set; } = string.Empty;

            [JsonPropertyName("size")]
            public long Size { get; set; }
        }

        /// <summary>
        /// 获取上传地址响应
        /// </summary>
        public class GetUploadUrlResponse
        {
            [JsonPropertyName("drive_id")]
            public string DriveId { get; set; } = string.Empty;

            [JsonPropertyName("file_id")]
            public string FileId { get; set; } = string.Empty;

            [JsonPropertyName("upload_id")]
            public string UploadId { get; set; } = string.Empty;

            [JsonPropertyName("part_info_list")]
            public PartInfo[]? PartInfoList { get; set; }

            [JsonPropertyName("created_at")]
            public DateTime CreatedAt { get; set; }

            [JsonPropertyName("status")]
            public string Status { get; set; } = string.Empty;

            [JsonPropertyName("parent_file_id")]
            public string ParentFileId { get; set; } = string.Empty;

            [JsonPropertyName("rapid_upload")]
            public bool RapidUpload { get; set; }
        }

        /// <summary>
        /// 分片信息
        /// </summary>
        public class PartInfo
        {
            [JsonPropertyName("part_number")]
            public int PartNumber { get; set; }

            [JsonPropertyName("upload_url")]
            public string UploadUrl { get; set; } = string.Empty;

            [JsonPropertyName("etag")]
            public string Etag { get; set; } = string.Empty;
        }

        /// <summary>
        /// 复制文件响应
        /// </summary>
        public class CopyFileResponse
        {
            [JsonPropertyName("drive_id")]
            public string DriveId { get; set; } = string.Empty;

            [JsonPropertyName("file_id")]
            public string FileId { get; set; } = string.Empty;

            [JsonPropertyName("async_task_id")]
            public string AsyncTaskId { get; set; } = string.Empty;
        }

        /// <summary>
        /// 增量游标响应
        /// </summary>
        public class DeltaCursorResponse
        {
            [JsonPropertyName("cursor")]
            public string Cursor { get; set; } = string.Empty;
        }

        /// <summary>
        /// 增量变化响应
        /// </summary>
        public class ListDeltaResponse
        {
            [JsonPropertyName("items")]
            public List<DeltaItem> Items { get; set; } = new();

            [JsonPropertyName("next_marker")]
            public string NextMarker { get; set; } = string.Empty;

            [JsonPropertyName("has_more")]
            public bool HasMore { get; set; }
        }

        /// <summary>
        /// 增量变化项
        /// </summary>
        public class DeltaItem
        {
            [JsonPropertyName("drive_id")]
            public string DriveId { get; set; } = string.Empty;

            [JsonPropertyName("file_id")]
            public string FileId { get; set; } = string.Empty;

            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("type")]
            public string Type { get; set; } = string.Empty;

            [JsonPropertyName("status")]
            public string Status { get; set; } = string.Empty;

            [JsonPropertyName("parent_file_id")]
            public string ParentFileId { get; set; } = string.Empty;

            [JsonPropertyName("size")]
            public long Size { get; set; }

            [JsonPropertyName("updated_at")]
            public DateTime UpdatedAt { get; set; }
        }

        #endregion
    }
}