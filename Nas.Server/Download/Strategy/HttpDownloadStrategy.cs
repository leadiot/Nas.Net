namespace Com.Scm.Nas.Download.Strategy
{
    /// <summary>
    /// HTTP/HTTPS 多线程分片下载策略
    /// 原理：通过 Range 请求头将文件拆分为 N 个分片并发下载，合并后得到完整文件。
    /// 若服务端不支持 Range，则自动退化为单线程下载。
    /// </summary>
    public class HttpDownloadStrategy : IDownloadStrategy
    {
        private static readonly HttpClient _httpClient = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5
        })
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        public NasDownloadLinkType LinkType => NasDownloadLinkType.Http;

        public async Task DownloadAsync(NasDownloadTask task, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(task.FilePath);

            // 探测文件大小及是否支持 Range
            long fileSize = -1;
            bool supportsRange = false;

            using (var headReq = new HttpRequestMessage(HttpMethod.Head, task.Url))
            {
                try
                {
                    using var headResp = await _httpClient.SendAsync(headReq, cancellationToken);
                    if (headResp.IsSuccessStatusCode)
                    {
                        fileSize = headResp.Content.Headers.ContentLength ?? -1;
                        supportsRange = headResp.Headers.AcceptRanges.Contains("bytes") || headResp.StatusCode == System.Net.HttpStatusCode.PartialContent;
                    }
                }
                catch
                {
                    // HEAD 请求不支持时忽略，退化为单线程
                }
            }

            task.TotalSize = fileSize;

            if (supportsRange && fileSize > 0 && task.Threads > 1)
            {
                await DownloadMultiThreadAsync(task, fileSize, cancellationToken);
            }
            else
            {
                await DownloadSingleThreadAsync(task, cancellationToken);
            }
        }

        /// <summary>
        /// 多线程分片下载（支持断点续传）
        /// </summary>
        private async Task DownloadMultiThreadAsync(NasDownloadTask task, long fileSize, CancellationToken cancellationToken)
        {
            int threads = Math.Clamp(task.Threads, 1, 16);
            long chunkSize = fileSize / threads;

            var tempFiles = new string[threads];
            var downloadTasks = new Task[threads];
            var chunkBytes = new long[threads];
            var chunkStarts = new long[threads];

            long existingTotal = 0L;

            for (int i = 0; i < threads; i++)
            {
                int idx = i;
                tempFiles[idx] = task.FullPath + $".part{idx}";

                long existingSize = 0;
                if (File.Exists(tempFiles[idx]))
                {
                    try
                    {
                        existingSize = new FileInfo(tempFiles[idx]).Length;
                    }
                    catch { }
                }

                chunkStarts[idx] = existingSize;
                existingTotal += existingSize;
                long from = idx * chunkSize + existingSize;
                long to = (idx == threads - 1) ? fileSize - 1 : from + chunkSize - 1;

                if (from <= to)
                {
                    downloadTasks[idx] = DownloadChunkAsync(task.Url, from, to, tempFiles[idx],
                        bytes =>
                        {
                            Interlocked.Add(ref chunkBytes[idx], bytes);
                            task.DownloadedSize = existingTotal + chunkBytes.Sum();
                            task.UpdateSpeed();
                        }, cancellationToken);
                }
                else
                {
                    downloadTasks[idx] = Task.CompletedTask;
                }
            }

            task.DownloadedSize = existingTotal + chunkBytes.Sum();
            await Task.WhenAll(downloadTasks);

            await MergeChunksAsync(tempFiles, task.FullPath);
        }

        /// <summary>
        /// 下载单个分片（支持断点续传）
        /// </summary>
        private async Task DownloadChunkAsync(string url, long from, long to, string savePath,
            Action<long> onProgress, CancellationToken cancellationToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(from, to);

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            bool fileExists = File.Exists(savePath);
            var fileMode = fileExists ? FileMode.Append : FileMode.Create;

            using (var readStream = await response.Content.ReadAsStreamAsync(cancellationToken))
            {
                using (var fileStream = new FileStream(savePath, fileMode, FileAccess.Write, FileShare.None, NasEnv.BUFFER_SIZE, true))
                {
                    var buffer = new byte[NasEnv.BUFFER_SIZE];
                    int bytesRead;
                    while ((bytesRead = await readStream.ReadAsync(buffer, cancellationToken)) > 0)
                    {
                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                        onProgress(bytesRead);
                    }
                }
            }
        }

        /// <summary>
        /// 合并分片文件
        /// </summary>
        private async Task MergeChunksAsync(string[] tempFiles, string outputPath)
        {
            using (var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, NasEnv.BUFFER_SIZE, true))
            {
                foreach (var part in tempFiles)
                {
                    using (var input = new FileStream(part, FileMode.Open, FileAccess.Read, FileShare.Read, NasEnv.BUFFER_SIZE, true))
                    {
                        await input.CopyToAsync(output);
                    }
                }
            }

            // 清理分片
            foreach (var part in tempFiles)
            {
                try { File.Delete(part); } catch { /* ignore */ }
            }
        }

        /// <summary>
        /// 单线程流式下载（支持断点续传）
        /// </summary>
        private async Task DownloadSingleThreadAsync(NasDownloadTask task, CancellationToken cancellationToken)
        {
            long existingSize = 0;
            if (File.Exists(task.FullPath))
            {
                try
                {
                    existingSize = new FileInfo(task.FullPath).Length;
                }
                catch { }
            }

            task.DownloadedSize = existingSize;

            if (task.DownloadedSize >= task.TotalSize && task.TotalSize > 0)
            {
                return;
            }

            var request = new HttpRequestMessage(HttpMethod.Get, task.Url);
            if (existingSize > 0 && task.TotalSize > 0)
            {
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(existingSize, task.TotalSize - 1);
            }

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            if (task.TotalSize <= 0)
            {
                task.TotalSize = response.Content.Headers.ContentLength ?? -1;
                if (task.TotalSize > 0)
                {
                    task.TotalSize += existingSize;
                }
            }

            var fileMode = existingSize > 0 ? FileMode.Append : FileMode.Create;
            using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken))
            {
                using (var fileStream = new FileStream(task.FullPath, fileMode, FileAccess.Write, FileShare.None, NasEnv.BUFFER_SIZE, true))
                {
                    var buffer = new byte[NasEnv.BUFFER_SIZE];
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
