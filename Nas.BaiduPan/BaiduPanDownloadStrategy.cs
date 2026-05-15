using Com.Scm.Nas;
using Com.Scm.Nas.Download;

namespace Nas.BaiduPan
{
    public class BaiduPanDownloadStrategy : IDownloadStrategy
    {
        private readonly BaiduPanApi _baiduPanApi;
        private static readonly HttpClient _httpClient = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5
        })
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        public BaiduPanDownloadStrategy(BaiduPanApi baiduPanApi)
        {
            _baiduPanApi = baiduPanApi;
        }

        public NasDownloadLinkType LinkType => NasDownloadLinkType.Nas;

        public async Task DownloadAsync(NasDownloadTask task, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(task.FilePath);

            var downloadResponse = await _baiduPanApi.GetDownloadUrlAsync(task.Url);
            if (downloadResponse.Errno != 0 || string.IsNullOrEmpty(downloadResponse.Url))
            {
                throw new Exception("获取下载链接失败");
            }

            string downloadUrl = downloadResponse.Url;
            bool supportsRange = false;
            long fileSize = -1;

            using (var headReq = new HttpRequestMessage(HttpMethod.Head, downloadUrl))
            {
                try
                {
                    using var headResp = await _httpClient.SendAsync(headReq, cancellationToken);
                    if (headResp.IsSuccessStatusCode)
                    {
                        fileSize = headResp.Content.Headers.ContentLength ?? -1;
                        supportsRange = headResp.Headers.AcceptRanges.Contains("bytes") || 
                            headResp.StatusCode == System.Net.HttpStatusCode.PartialContent;
                    }
                }
                catch
                {
                    // HEAD 请求不支持时忽略
                }
            }

            task.TotalSize = fileSize;

            if (supportsRange && fileSize > 0 && task.Threads > 1)
            {
                await DownloadMultiThreadAsync(downloadUrl, task, fileSize, cancellationToken);
            }
            else
            {
                await DownloadSingleThreadAsync(downloadUrl, task, cancellationToken);
            }
        }

        private async Task DownloadMultiThreadAsync(string url, NasDownloadTask task, long fileSize, CancellationToken cancellationToken)
        {
            int threads = Math.Clamp(task.Threads, 1, 16);
            long chunkSize = fileSize / threads;

            var tempFiles = new string[threads];
            var downloadTasks = new Task[threads];
            var chunkBytes = new long[threads];

            for (int i = 0; i < threads; i++)
            {
                int idx = i;
                long from = idx * chunkSize;
                long to = (idx == threads - 1) ? fileSize - 1 : from + chunkSize - 1;
                tempFiles[idx] = task.FullPath + $".part{idx}";

                downloadTasks[idx] = DownloadChunkAsync(url, from, to, tempFiles[idx],
                    bytes =>
                    {
                        Interlocked.Add(ref chunkBytes[idx], bytes);
                        task.DownloadedSize = chunkBytes.Sum();
                        task.UpdateSpeed();
                    }, cancellationToken);
            }

            await Task.WhenAll(downloadTasks);

            await MergeChunksAsync(tempFiles, task.FullPath);
        }

        private async Task DownloadChunkAsync(string url, long from, long to, string savePath,
            Action<long> onProgress, CancellationToken cancellationToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(from, to);

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            using (var readStream = await response.Content.ReadAsStreamAsync(cancellationToken))
            {
                using (var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
                {
                    var buffer = new byte[81920];
                    int bytesRead;
                    while ((bytesRead = await readStream.ReadAsync(buffer, cancellationToken)) > 0)
                    {
                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                        onProgress(bytesRead);
                    }
                }
            }
        }

        private async Task MergeChunksAsync(string[] tempFiles, string outputPath)
        {
            using (var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
            {
                foreach (var part in tempFiles)
                {
                    using (var input = new FileStream(part, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true))
                    {
                        await input.CopyToAsync(output);
                    }
                }
            }

            foreach (var part in tempFiles)
            {
                try { File.Delete(part); } catch { }
            }
        }

        private async Task DownloadSingleThreadAsync(string url, NasDownloadTask task, CancellationToken cancellationToken)
        {
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            if (task.TotalSize <= 0)
            {
                task.TotalSize = response.Content.Headers.ContentLength ?? -1;
            }

            using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken))
            {
                using (var fileStream = new FileStream(task.FullPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
                {
                    var buffer = new byte[81920];
                    int bytesRead;
                    while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken)) > 0)
                    {
                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                        task.DownloadedSize += bytesRead;
                        task.UpdateSpeed();
                    }
                }
            }
        }
    }
}