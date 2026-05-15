using Com.Scm.Nas;
using Com.Scm.Nas.Download;

namespace Nas.BaiduPan
{
    public class BaiduPanService
    {
        private readonly BaiduPanApi _api;
        private readonly BaiduPanDownloadStrategy _downloadStrategy;

        public BaiduPanService(BaiduPanConfig config)
        {
            _api = new BaiduPanApi(config);
            _downloadStrategy = new BaiduPanDownloadStrategy(_api);
        }

        public BaiduPanApi Api => _api;

        public string GetAuthorizationUrl()
        {
            return _api.GetAuthorizationUrl();
        }

        public async Task<BaiduPanApi.TokenResponse> AuthenticateAsync(string code)
        {
            return await _api.GetTokenByCodeAsync(code);
        }

        public async Task<BaiduPanApi.TokenResponse> RefreshTokenAsync()
        {
            return await _api.RefreshTokenAsync();
        }

        public async Task<BaiduPanApi.QuotaResponse> GetQuotaAsync()
        {
            return await _api.GetQuotaAsync();
        }

        public async Task<List<BaiduPanApi.FileItem>> ListFilesAsync(string path = "/", int limit = 100, string order = "name", int desc = 0)
        {
            var response = await _api.ListFilesAsync(path, limit, order, desc);
            return response.List;
        }

        public async Task<BaiduPanApi.FileItem> GetFileInfoAsync(string path)
        {
            var response = await _api.GetFileInfoAsync(path);
            return response.Info;
        }

        public async Task DownloadFileAsync(string remotePath, string localSavePath, 
            Action<double>? progressCallback = null, CancellationToken cancellationToken = default)
        {
            var fileInfo = await GetFileInfoAsync(remotePath);
            if (fileInfo.IsDir == 1)
            {
                throw new InvalidOperationException("路径指向目录，不是文件");
            }

            var fileName = fileInfo.ServerFilename;
            var fullSavePath = Path.Combine(localSavePath, fileName);

            var task = new NasDownloadTask
            {
                id = DateTime.Now.Ticks,
                Url = remotePath,
                LinkType = NasDownloadLinkType.Nas,
                FilePath = localSavePath,
                FileName = fileName,
                Threads = 4,
                TotalSize = fileInfo.Size
            };

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

        public async Task<BaiduPanApi.UploadResponse> UploadFileAsync(string localFilePath, string remotePath)
        {
            var fileInfo = new FileInfo(localFilePath);
            
            if (fileInfo.Length > 4 * 1024 * 1024)
            {
                return await _api.UploadLargeFileAsync(localFilePath, remotePath);
            }
            
            return await _api.UploadFileAsync(localFilePath, remotePath);
        }

        public async Task<BaiduPanApi.UploadResponse> UploadLargeFileAsync(string localFilePath, string remotePath, 
            int chunkSize = 4 * 1024 * 1024)
        {
            return await _api.UploadLargeFileAsync(localFilePath, remotePath, chunkSize);
        }

        public async Task<bool> CreateDirectoryAsync(string path)
        {
            return await _api.CreateDirectoryAsync(path);
        }

        public async Task<bool> DeleteFileAsync(string path)
        {
            return await _api.DeleteFileAsync(path);
        }

        public async Task DownloadFolderAsync(string remoteFolderPath, string localSavePath,
            Action<string, double>? progressCallback = null, CancellationToken cancellationToken = default)
        {
            var files = await ListFilesAsync(remoteFolderPath);
            
            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fullRemotePath = file.Path;
                var relativePath = fullRemotePath.StartsWith(remoteFolderPath) 
                    ? fullRemotePath.Substring(remoteFolderPath.Length).TrimStart('/') 
                    : file.ServerFilename;
                
                var localPath = Path.Combine(localSavePath, relativePath);

                if (file.IsDir == 1)
                {
                    Directory.CreateDirectory(localPath);
                    await DownloadFolderAsync(fullRemotePath, localPath, progressCallback, cancellationToken);
                }
                else
                {
                    await DownloadFileAsync(fullRemotePath, Path.GetDirectoryName(localPath) ?? localSavePath,
                        progress => progressCallback?.Invoke(file.ServerFilename, progress),
                        cancellationToken);
                }
            }
        }
    }
}