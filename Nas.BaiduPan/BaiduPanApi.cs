using System.Net.Http.Headers;
using System.Text.Json;

namespace Nas.BaiduPan
{
    public class BaiduPanApi
    {
        private readonly BaiduPanConfig _config;
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public BaiduPanApi(BaiduPanConfig config)
        {
            _config = config;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(config.ApiBaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public async Task<TokenResponse> GetTokenByCodeAsync(string code)
        {
            var parameters = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "client_id", _config.AppKey },
                { "client_secret", _config.AppSecret },
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

        public async Task<TokenResponse> RefreshTokenAsync()
        {
            if (string.IsNullOrEmpty(_config.RefreshToken))
                throw new InvalidOperationException("Refresh token is empty");

            var parameters = new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", _config.RefreshToken },
                { "client_id", _config.AppKey },
                { "client_secret", _config.AppSecret }
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

        private void UpdateToken(TokenResponse token)
        {
            _config.AccessToken = token.AccessToken;
            _config.RefreshToken = token.RefreshToken;
            _config.ExpireTime = DateTimeOffset.Now.AddSeconds(token.ExpiresIn).ToUnixTimeSeconds();
        }

        public async Task<QuotaResponse> GetQuotaAsync()
        {
            await EnsureTokenValidAsync();

            var url = $"/pan/nas/quota?access_token={_config.AccessToken}";
            using var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<QuotaResponse>(json, _jsonOptions)!;
        }

        public async Task<ListResponse> ListFilesAsync(string path = "/", int limit = 100, string order = "name", int desc = 0)
        {
            await EnsureTokenValidAsync();

            var url = $"/pan/nas/file/list?access_token={_config.AccessToken}&path={Uri.EscapeDataString(path)}&limit={limit}&order={order}&desc={desc}";
            using var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ListResponse>(json, _jsonOptions)!;
        }

        public async Task<FileInfoResponse> GetFileInfoAsync(string path)
        {
            await EnsureTokenValidAsync();

            var url = $"/pan/nas/file/info?access_token={_config.AccessToken}&path={Uri.EscapeDataString(path)}";
            using var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<FileInfoResponse>(json, _jsonOptions)!;
        }

        public async Task<DownloadResponse> GetDownloadUrlAsync(string path)
        {
            await EnsureTokenValidAsync();

            var url = $"/pan/nas/file/download?access_token={_config.AccessToken}&path={Uri.EscapeDataString(path)}";
            using var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<DownloadResponse>(json, _jsonOptions)!;
        }

        public async Task<UploadResponse> UploadFileAsync(string localFilePath, string remotePath, 
            long? size = null, string? md5 = null)
        {
            await EnsureTokenValidAsync();

            var fileInfo = new FileInfo(localFilePath);
            var fileName = fileInfo.Name;
            var targetPath = remotePath.EndsWith("/") ? remotePath + fileName : remotePath + "/" + fileName;

            if (!size.HasValue)
                size = fileInfo.Length;

            if (string.IsNullOrEmpty(md5))
                md5 = CalculateMd5(localFilePath);

            var url = $"/pan/nas/file/upload?access_token={_config.AccessToken}";

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(targetPath), "path");
            content.Add(new StringContent(size.Value.ToString()), "size");
            content.Add(new StringContent(md5), "md5");

            using var fileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read);
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
            content.Add(fileContent, "file", fileName);

            using var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UploadResponse>(json, _jsonOptions)!;
        }

        public async Task<UploadResponse> UploadLargeFileAsync(string localFilePath, string remotePath, 
            int chunkSize = 4 * 1024 * 1024)
        {
            await EnsureTokenValidAsync();

            var fileInfo = new FileInfo(localFilePath);
            var fileName = fileInfo.Name;
            var targetPath = remotePath.EndsWith("/") ? remotePath + fileName : remotePath + "/" + fileName;
            var fileSize = fileInfo.Length;
            var md5 = CalculateMd5(localFilePath);
            var uploadId = Guid.NewGuid().ToString("N");

            var partCount = (int)Math.Ceiling((double)fileSize / chunkSize);

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

                var chunkMd5 = CalculateMd5(chunkData);

                var url = $"/pan/nas/file/upload?access_token={_config.AccessToken}";
                using var content = new MultipartFormDataContent();
                content.Add(new StringContent(targetPath), "path");
                content.Add(new StringContent(fileSize.ToString()), "size");
                content.Add(new StringContent(md5), "md5");
                content.Add(new StringContent(uploadId), "uploadid");
                content.Add(new StringContent(i.ToString()), "partseq");

                var chunkContent = new ByteArrayContent(chunkData);
                chunkContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                content.Add(chunkContent, "file", $"part_{i}");

                using var response = await _httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();
            }

            return await CompleteUploadAsync(targetPath, uploadId, md5, fileSize);
        }

        private async Task<UploadResponse> CompleteUploadAsync(string path, string uploadId, string md5, long size)
        {
            var url = $"/pan/nas/file/upload/complete?access_token={_config.AccessToken}";

            var parameters = new Dictionary<string, string>
            {
                { "path", path },
                { "uploadid", uploadId },
                { "md5", md5 },
                { "size", size.ToString() }
            };

            using var content = new FormUrlEncodedContent(parameters);
            using var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UploadResponse>(json, _jsonOptions)!;
        }

        public async Task<bool> CreateDirectoryAsync(string path)
        {
            await EnsureTokenValidAsync();

            var url = $"/pan/nas/file/mkdir?access_token={_config.AccessToken}&path={Uri.EscapeDataString(path)}";
            using var response = await _httpClient.PostAsync(url, null);
            response.EnsureSuccessStatusCode();
            return true;
        }

        public async Task<bool> DeleteFileAsync(string path)
        {
            await EnsureTokenValidAsync();

            var url = $"/pan/nas/file/delete?access_token={_config.AccessToken}&path={Uri.EscapeDataString(path)}";
            using var response = await _httpClient.PostAsync(url, null);
            response.EnsureSuccessStatusCode();
            return true;
        }

        private async Task EnsureTokenValidAsync()
        {
            if (string.IsNullOrEmpty(_config.AccessToken) || 
                DateTimeOffset.Now.ToUnixTimeSeconds() >= _config.ExpireTime - 60)
            {
                await RefreshTokenAsync();
            }
        }

        private string CalculateMd5(string filePath)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        private string CalculateMd5(byte[] data)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            var hash = md5.ComputeHash(data);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        public string GetAuthorizationUrl()
        {
            return $"{_config.AuthorizeUrl}?response_type=code&client_id={_config.AppKey}&redirect_uri={Uri.EscapeDataString(_config.RedirectUri)}&scope={_config.Scope}";
        }

        public class TokenResponse
        {
            public string AccessToken { get; set; } = string.Empty;
            public string RefreshToken { get; set; } = string.Empty;
            public int ExpiresIn { get; set; }
            public string Scope { get; set; } = string.Empty;
            public string SessionKey { get; set; } = string.Empty;
            public string SessionSecret { get; set; } = string.Empty;
        }

        public class QuotaResponse
        {
            public int Errno { get; set; }
            public long Used { get; set; }
            public long Total { get; set; }
        }

        public class ListResponse
        {
            public int Errno { get; set; }
            public List<FileItem> List { get; set; } = new();
            public string Path { get; set; } = string.Empty;
        }

        public class FileItem
        {
            public string Path { get; set; } = string.Empty;
            public string ServerFilename { get; set; } = string.Empty;
            public long Size { get; set; }
            public long Mtime { get; set; }
            public long Ctime { get; set; }
            public string Md5 { get; set; } = string.Empty;
            public int IsDir { get; set; }
        }

        public class FileInfoResponse
        {
            public int Errno { get; set; }
            public FileItem Info { get; set; } = new();
        }

        public class DownloadResponse
        {
            public int Errno { get; set; }
            public string Url { get; set; } = string.Empty;
            public long ExpireTime { get; set; }
        }

        public class UploadResponse
        {
            public int Errno { get; set; }
            public string Path { get; set; } = string.Empty;
        }
    }
}