using LizardClient.Core.Interfaces;
using LizardClient.Core.Models;
using LizardClient.Core.Utilities;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;

namespace LizardClient.Core.Services;

/// <summary>
/// 多线程下载管理器，支持断点续传和进度跟踪
/// </summary>
public sealed class DownloadManager
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, DownloadTask> _activeDownloads;
    private readonly int _defaultChunkCount;
    private readonly long _minChunkSize = 1024 * 1024; // 1MB 最小分块大小

    public DownloadManager(ILogger logger, int threadCount = 4, HttpClient? httpClient = null)
    {
        _logger = logger;
        _defaultChunkCount = threadCount;
        _httpClient = httpClient ?? new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(30) // 增加超时时间
        };
        _activeDownloads = new ConcurrentDictionary<string, DownloadTask>();
    }

    /// <summary>
    /// 下载文件（支持多线程和断点续传）
    /// </summary>
    /// <param name="url">下载URL</param>
    /// <param name="destinationPath">目标路径</param>
    /// <param name="progress">进度报告</param>
    /// <param name="expectedHash">期望的文件哈希值（SHA256）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>下载结果</returns>
    public async Task<DownloadResult> DownloadAsync(
        string url,
        string destinationPath,
        IProgress<DownloadProgress>? progress = null,
        string? expectedHash = null,
        CancellationToken cancellationToken = default)
    {
        var downloadId = Guid.NewGuid().ToString();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.Info($"开始下载: {url}");

            // 创建目标目录
            var directory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 获取文件信息
            var (fileSize, supportsRange) = await GetFileInfoAsync(url, cancellationToken);

            if (fileSize <= 0)
            {
                return new DownloadResult
                {
                    IsSuccess = false,
                    ErrorMessage = "无法获取文件大小"
                };
            }

            _logger.Info($"文件大小: {fileSize} 字节，支持断点续传: {supportsRange}");

            // 创建下载任务
            var downloadTask = new DownloadTask
            {
                Id = downloadId,
                Url = url,
                DestinationPath = destinationPath,
                TotalSize = fileSize,
                Progress = new DownloadProgress
                {
                    TotalBytes = fileSize,
                    Status = DownloadStatus.Preparing
                }
            };

            _activeDownloads[downloadId] = downloadTask;

            // 报告准备状态
            progress?.Report(downloadTask.Progress.Clone());

            // 确定是否使用多线程
            var useMultiThread = supportsRange && fileSize > _minChunkSize;
            var chunkCount = useMultiThread
                ? Math.Min(_defaultChunkCount, (int)(fileSize / _minChunkSize))
                : 1;

            _logger.Info($"使用 {chunkCount} 个线程下载");

            // 开始下载
            downloadTask.Progress.Status = DownloadStatus.Downloading;
            downloadTask.Progress.StartTime = DateTime.UtcNow;
            downloadTask.Progress.TotalChunks = chunkCount;

            bool success;
            if (chunkCount > 1)
            {
                success = await DownloadMultiThreadAsync(
                    downloadTask,
                    chunkCount,
                    progress,
                    cancellationToken
                );
            }
            else
            {
                success = await DownloadSingleThreadAsync(
                    downloadTask,
                    progress,
                    cancellationToken
                );
            }

            if (!success)
            {
                return new DownloadResult
                {
                    IsSuccess = false,
                    ErrorMessage = downloadTask.Progress.ErrorMessage ?? "下载失败"
                };
            }

            // 验证文件完整性
            downloadTask.Progress.Status = DownloadStatus.Verifying;
            progress?.Report(downloadTask.Progress.Clone());

            bool isVerified = true;
            if (!string.IsNullOrEmpty(expectedHash))
            {
                _logger.Info("正在验证文件完整性...");
                isVerified = await IntegrityVerifier.VerifySHA256Async(destinationPath, expectedHash);

                if (!isVerified)
                {
                    _logger.Error("文件哈希值验证失败");
                    File.Delete(destinationPath);

                    return new DownloadResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "文件完整性验证失败",
                        IsVerified = false
                    };
                }

                _logger.Info("文件完整性验证通过");
            }

            stopwatch.Stop();

            // 下载完成
            downloadTask.Progress.Status = DownloadStatus.Completed;
            downloadTask.Progress.DownloadedBytes = fileSize;
            progress?.Report(downloadTask.Progress.Clone());

            _logger.Info($"下载完成: {destinationPath} ({stopwatch.Elapsed.TotalSeconds:F2}秒)");

            return new DownloadResult
            {
                IsSuccess = true,
                FilePath = destinationPath,
                Duration = stopwatch.Elapsed,
                AverageSpeed = fileSize / stopwatch.Elapsed.TotalSeconds,
                TotalBytes = fileSize,
                IsVerified = isVerified
            };
        }
        catch (OperationCanceledException)
        {
            _logger.Info("下载已取消");
            return new DownloadResult
            {
                IsSuccess = false,
                ErrorMessage = "下载已取消"
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"下载失败: {ex.Message}", ex);
            return new DownloadResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
        finally
        {
            _activeDownloads.TryRemove(downloadId, out _);
        }
    }

    /// <summary>
    /// 获取文件信息（大小和是否支持断点续传）
    /// </summary>
    private async Task<(long fileSize, bool supportsRange)> GetFileInfoAsync(
        string url,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Head, url);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var fileSize = response.Content.Headers.ContentLength ?? 0;
            var supportsRange = response.Headers.AcceptRanges.Contains("bytes");

            return (fileSize, supportsRange);
        }
        catch (Exception ex)
        {
            _logger.Warning($"HEAD 请求失败，尝试 GET 请求: {ex.Message}");

            // 如果 HEAD 请求失败，尝试 GET 请求
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 0);

                var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                var fileSize = response.Content.Headers.ContentRange?.Length ?? response.Content.Headers.ContentLength ?? 0;
                var supportsRange = response.StatusCode == HttpStatusCode.PartialContent;

                return (fileSize, supportsRange);
            }
            catch
            {
                return (0, false);
            }
        }
    }

    /// <summary>
    /// 单线程下载
    /// </summary>
    private async Task<bool> DownloadSingleThreadAsync(
        DownloadTask task,
        IProgress<DownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync(task.Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = new FileStream(task.DestinationPath, FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[8192];
            int bytesRead;
            var lastReportTime = DateTime.UtcNow;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                task.Progress.DownloadedBytes += bytesRead;

                // 每 100ms 报告一次进度
                if ((DateTime.UtcNow - lastReportTime).TotalMilliseconds >= 100)
                {
                    UpdateProgress(task);
                    progress?.Report(task.Progress.Clone());
                    lastReportTime = DateTime.UtcNow;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"单线程下载失败: {ex.Message}", ex);
            task.Progress.Status = DownloadStatus.Failed;
            task.Progress.ErrorMessage = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// 多线程下载
    /// </summary>
    private async Task<bool> DownloadMultiThreadAsync(
        DownloadTask task,
        int chunkCount,
        IProgress<DownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        try
        {
            // 创建分块
            var chunks = DownloadChunk.CreateChunks(task.TotalSize, chunkCount);
            task.Chunks = chunks;
            task.Progress.ActiveChunks = chunks.Count;

            // 创建临时目录
            var tempDir = Path.Combine(Path.GetDirectoryName(task.DestinationPath) ?? "", ".download_temp");
            Directory.CreateDirectory(tempDir);

            // 为每个分块创建临时文件
            foreach (var chunk in chunks)
            {
                chunk.TempFilePath = Path.Combine(tempDir, $"chunk_{chunk.ChunkId}.tmp");
            }

            // 并行下载分块
            var downloadTasks = chunks.Select(chunk =>
                DownloadChunkAsync(task.Url, chunk, task.Progress, progress, cancellationToken)
            ).ToArray();

            await Task.WhenAll(downloadTasks);

            // 检查是否所有分块都下载成功
            if (!chunks.All(c => c.IsCompleted))
            {
                _logger.Error("部分分块下载失败");
                task.Progress.Status = DownloadStatus.Failed;
                task.Progress.ErrorMessage = "部分分块下载失败";
                CleanupTempFiles(chunks);
                return false;
            }

            // 合并分块
            _logger.Info("正在合并分块...");
            await MergeChunksAsync(chunks, task.DestinationPath, cancellationToken);

            // 清理临时文件
            CleanupTempFiles(chunks);
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"多线程下载失败: {ex.Message}", ex);
            task.Progress.Status = DownloadStatus.Failed;
            task.Progress.ErrorMessage = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// 下载单个分块（支持重试和断点续传）
    /// </summary>
    private async Task DownloadChunkAsync(
        string url,
        DownloadChunk chunk,
        DownloadProgress overallProgress,
        IProgress<DownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        while (!chunk.IsCompleted && chunk.CanRetry)
        {
            try
            {
                // 检查是否有部分已下载（断点续传）
                long startByte = chunk.StartByte + chunk.DownloadedBytes;

                if (startByte > chunk.EndByte)
                {
                    chunk.IsCompleted = true;
                    return;
                }

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(startByte, chunk.EndByte);

                var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();

                using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var fileStream = new FileStream(chunk.TempFilePath!, FileMode.Append, FileAccess.Write, FileShare.None);

                var buffer = new byte[8192];
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);

                    chunk.DownloadedBytes += bytesRead;
                    lock (overallProgress)
                    {
                        overallProgress.DownloadedBytes += bytesRead;
                    }
                }

                chunk.IsCompleted = true;
                lock (overallProgress)
                {
                    overallProgress.ActiveChunks--;
                }
                return;
            }
            catch (Exception ex)
            {
                chunk.RetryCount++;
                _logger.Warning($"分块 {chunk.ChunkId} 下载失败 (重试 {chunk.RetryCount}/{chunk.MaxRetries}): {ex.Message}");

                if (!chunk.CanRetry)
                {
                    _logger.Error($"分块 {chunk.ChunkId} 下载失败，已达最大重试次数");
                    throw;
                }

                // 指数退避
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, chunk.RetryCount)), cancellationToken);
            }
        }
    }

    /// <summary>
    /// 合并分块
    /// </summary>
    private async Task MergeChunksAsync(
        List<DownloadChunk> chunks,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        using var outputStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);

        foreach (var chunk in chunks.OrderBy(c => c.ChunkId))
        {
            if (chunk.TempFilePath == null || !File.Exists(chunk.TempFilePath))
            {
                throw new FileNotFoundException($"分块文件不存在: {chunk.TempFilePath}");
            }

            using var chunkStream = File.OpenRead(chunk.TempFilePath);
            await chunkStream.CopyToAsync(outputStream, cancellationToken);
        }
    }

    /// <summary>
    /// 清理临时文件
    /// </summary>
    private void CleanupTempFiles(List<DownloadChunk> chunks)
    {
        foreach (var chunk in chunks)
        {
            try
            {
                if (chunk.TempFilePath != null && File.Exists(chunk.TempFilePath))
                {
                    File.Delete(chunk.TempFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.Warning($"清理临时文件失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 更新进度信息
    /// </summary>
    private void UpdateProgress(DownloadTask task)
    {
        var elapsed = DateTime.UtcNow - task.Progress.StartTime;
        if (elapsed.TotalSeconds > 0)
        {
            task.Progress.DownloadSpeed = task.Progress.DownloadedBytes / elapsed.TotalSeconds;

            var remaining = task.TotalSize - task.Progress.DownloadedBytes;
            if (task.Progress.DownloadSpeed > 0)
            {
                task.Progress.EstimatedTimeRemaining = TimeSpan.FromSeconds(remaining / task.Progress.DownloadSpeed);
            }
        }
    }

    /// <summary>
    /// 下载任务
    /// </summary>
    private class DownloadTask
    {
        public string Id { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string DestinationPath { get; set; } = string.Empty;
        public long TotalSize { get; set; }
        public DownloadProgress Progress { get; set; } = new();
        public List<DownloadChunk> Chunks { get; set; } = new();
    }
}
