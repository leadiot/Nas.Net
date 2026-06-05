using Com.Scm.Nas;
using Com.Scm.Nas.Download;

namespace Nas.Alipan
{
    /// <summary>
    /// 阿里云盘文件管理服务
    /// 封装 API 调用和下载策略，提供高级文件管理功能
    /// </summary>
    public class AlipanService
    {
        private readonly AlipanApi _api;
        private readonly AlipanDownloadStrategy _downloadStrategy;

        public AlipanService(AlipanConfig config)
        {
            _api = new AlipanApi(config);
            _downloadStrategy = new AlipanDownloadStrategy(_api);
        }

        /// <summary>
        /// API 实例（用于直接调用底层接口）
        /// </summary>
        public AlipanApi Api => _api;

        #region 认证

        /// <summary>
        /// 获取 OAuth2 授权URL
        /// </summary>
        public string GetAuthorizationUrl()
        {
            return _api.GetAuthorizationUrl();
        }

        /// <summary>
        /// 通过授权码认证
        /// </summary>
        public async Task<AlipanApi.TokenResponse> AuthenticateAsync(string code)
        {
            return await _api.GetTokenByCodeAsync(code);
        }

        /// <summary>
        /// 刷新访问令牌
        /// </summary>
        public async Task<AlipanApi.TokenResponse> RefreshTokenAsync()
        {
            return await _api.RefreshTokenAsync();
        }

        #endregion

        #region 空间管理

        /// <summary>
        /// 获取我的空间列表
        /// </summary>
        public async Task<List<AlipanApi.DriveInfo>> ListMyDrivesAsync()
        {
            var response = await _api.ListMyDrivesAsync();
            return response.Items;
        }

        /// <summary>
        /// 获取默认空间信息
        /// </summary>
        public async Task<AlipanApi.DriveInfo> GetDefaultDriveAsync()
        {
            return await _api.GetDefaultDriveAsync();
        }

        /// <summary>
        /// 获取空间容量信息
        /// </summary>
        public async Task<AlipanApi.DriveInfo> GetDriveInfoAsync(string driveId)
        {
            return await _api.GetDriveAsync(driveId);
        }

        #endregion

        #region 文件管理

        /// <summary>
        /// 列举文件夹下的文件和子文件夹
        /// </summary>
        public async Task<List<AlipanApi.FileItem>> ListFilesAsync(string driveId, string parentFileId = "root",
            int limit = 100, string orderBy = "name", string orderDirection = "ASC",
            string? type = null, string? category = null)
        {
            var response = await _api.ListFilesAsync(driveId, parentFileId, limit, orderBy, orderDirection, null, type, category);
            return response.Items;
        }

        /// <summary>
        /// 分页列举文件（返回包含分页标记的完整响应）
        /// </summary>
        public async Task<AlipanApi.ListFileResponse> ListFilesPagedAsync(string driveId, string parentFileId = "root",
            int limit = 100, string? marker = null, string orderBy = "name", string orderDirection = "ASC",
            string? type = null, string? category = null)
        {
            return await _api.ListFilesAsync(driveId, parentFileId, limit, orderBy, orderDirection, marker, type, category);
        }

        /// <summary>
        /// 获取文件详情
        /// </summary>
        public async Task<AlipanApi.FileItem> GetFileAsync(string driveId, string fileId)
        {
            return await _api.GetFileAsync(driveId, fileId);
        }

        /// <summary>
        /// 搜索文件
        /// </summary>
        public async Task<List<AlipanApi.FileItem>> SearchFilesAsync(string driveId, string query,
            int limit = 100, string? category = null, string? type = null)
        {
            var response = await _api.SearchFileAsync(driveId, query, limit, null, category, type);
            return response.Items;
        }

        /// <summary>
        /// 创建文件夹
        /// </summary>
        public async Task<AlipanApi.FileItem> CreateFolderAsync(string driveId, string parentFileId, string name)
        {
            return await _api.CreateFolderAsync(driveId, parentFileId, name);
        }

        /// <summary>
        /// 删除文件或文件夹（移至回收站）
        /// </summary>
        public async Task<bool> TrashFileAsync(string driveId, string fileId)
        {
            return await _api.TrashFileAsync(driveId, fileId);
        }

        /// <summary>
        /// 重命名文件或文件夹
        /// </summary>
        public async Task<AlipanApi.FileItem> RenameFileAsync(string driveId, string fileId, string newName)
        {
            return await _api.UpdateFileAsync(driveId, fileId, name: newName);
        }

        /// <summary>
        /// 设置/取消收藏
        /// </summary>
        public async Task<AlipanApi.FileItem> SetStarredAsync(string driveId, string fileId, bool starred)
        {
            return await _api.UpdateFileAsync(driveId, fileId, starred: starred);
        }

        /// <summary>
        /// 移动文件或文件夹
        /// </summary>
        public async Task<bool> MoveFileAsync(string driveId, string fileId, string toParentFileId)
        {
            return await _api.MoveFileAsync(driveId, fileId, toParentFileId);
        }

        /// <summary>
        /// 复制文件或文件夹
        /// </summary>
        public async Task<AlipanApi.CopyFileResponse> CopyFileAsync(string driveId, string fileId, string toParentFileId,
            string? toDriveId = null, string? newName = null)
        {
            return await _api.CopyFileAsync(driveId, fileId, toParentFileId, toDriveId, newName);
        }

        #endregion

        #region 下载

        /// <summary>
        /// 下载单个文件
        /// </summary>
        /// <param name="driveId">空间ID</param>
        /// <param name="fileId">文件ID</param>
        /// <param name="localSavePath">本地保存目录</param>
        /// <param name="progressCallback">进度回调（0~100）</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async Task DownloadFileAsync(string driveId, string fileId, string localSavePath,
            Action<double>? progressCallback = null, CancellationToken cancellationToken = default)
        {
            var fileInfo = await GetFileAsync(driveId, fileId);
            if (fileInfo.IsFolder)
            {
                throw new InvalidOperationException("路径指向文件夹，不是文件");
            }

            var fileName = fileInfo.Name;
            var fullSavePath = Path.Combine(localSavePath, fileName);

            var task = new NasDownloadTask
            {
                id = DateTime.Now.Ticks,
                Url = fileId,
                LinkType = NasDownloadLinkType.Nas,
                FilePath = localSavePath,
                FileName = fileName,
                Threads = 4,
                TotalSize = fileInfo.Size
            };

            // 传递 driveId 和 fileId 给下载策略
            task.FtpUser = driveId;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var progressTask = Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    progressCallback?.Invoke(task.Progress);
                    await Task.Delay(500);
                }
            }, cts.Token);

            try
            {
                await _downloadStrategy.DownloadAsync(task, cts.Token);
                progressCallback?.Invoke(100);
            }
            finally
            {
                cts.Cancel();
                try { await progressTask; } catch { }
            }
        }

        /// <summary>
        /// 下载整个文件夹（递归下载所有子文件）
        /// </summary>
        public async Task DownloadFolderAsync(string driveId, string folderId, string localSavePath,
            Action<string, double>? progressCallback = null, CancellationToken cancellationToken = default)
        {
            var files = await ListFilesAsync(driveId, folderId);

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var localPath = Path.Combine(localSavePath, file.Name);

                if (file.IsFolder)
                {
                    Directory.CreateDirectory(localPath);
                    await DownloadFolderAsync(driveId, file.FileId, localPath, progressCallback, cancellationToken);
                }
                else
                {
                    await DownloadFileAsync(driveId, file.FileId, Path.GetDirectoryName(localPath) ?? localSavePath,
                        progress => progressCallback?.Invoke(file.Name, progress),
                        cancellationToken);
                }
            }
        }

        #endregion

        #region 上传

        /// <summary>
        /// 上传文件（自动判断大小选择上传方式）
        /// </summary>
        public async Task<AlipanApi.FileItem> UploadFileAsync(string driveId, string parentFileId, string localFilePath)
        {
            var fileInfo = new FileInfo(localFilePath);

            if (fileInfo.Length > 4 * 1024 * 1024)
            {
                return await _api.UploadLargeFileAsync(driveId, parentFileId, localFilePath);
            }

            return await _api.UploadSmallFileAsync(driveId, parentFileId, localFilePath);
        }

        /// <summary>
        /// 上传大文件（分片上传）
        /// </summary>
        public async Task<AlipanApi.FileItem> UploadLargeFileAsync(string driveId, string parentFileId,
            string localFilePath, int chunkSize = 4 * 1024 * 1024)
        {
            return await _api.UploadLargeFileAsync(driveId, parentFileId, localFilePath, chunkSize);
        }

        #endregion

        #region 回收站

        /// <summary>
        /// 列举回收站文件
        /// </summary>
        public async Task<List<AlipanApi.FileItem>> ListRecyclebinAsync(string driveId, int limit = 100)
        {
            var response = await _api.ListRecyclebinAsync(driveId, limit);
            return response.Items;
        }

        /// <summary>
        /// 从回收站恢复文件
        /// </summary>
        public async Task<bool> RestoreFileAsync(string driveId, string fileId)
        {
            return await _api.RestoreFileAsync(driveId, fileId);
        }

        /// <summary>
        /// 清空回收站
        /// </summary>
        public async Task<bool> ClearRecyclebinAsync(string driveId)
        {
            return await _api.ClearRecyclebinAsync(driveId);
        }

        #endregion

        #region 增量同步

        /// <summary>
        /// 获取增量操作游标
        /// </summary>
        public async Task<string> GetDeltaLastCursorAsync(string driveId)
        {
            return await _api.GetDeltaLastCursorAsync(driveId);
        }

        /// <summary>
        /// 获取增量变化列表
        /// </summary>
        public async Task<AlipanApi.ListDeltaResponse> ListDeltaAsync(string driveId, string? cursor = null, int limit = 100)
        {
            return await _api.ListDeltaAsync(driveId, cursor, limit);
        }

        #endregion
    }
}