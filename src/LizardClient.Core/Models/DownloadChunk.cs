namespace LizardClient.Core.Models;

/// <summary>
/// 下载分块信息（用于多线程下载）
/// </summary>
public sealed class DownloadChunk
{
    /// <summary>
    /// 分块ID
    /// </summary>
    public int ChunkId { get; set; }

    /// <summary>
    /// 起始字节位置
    /// </summary>
    public long StartByte { get; set; }

    /// <summary>
    /// 结束字节位置
    /// </summary>
    public long EndByte { get; set; }

    /// <summary>
    /// 分块大小
    /// </summary>
    public long ChunkSize => EndByte - StartByte + 1;

    /// <summary>
    /// 已下载字节数
    /// </summary>
    public long DownloadedBytes { get; set; }

    /// <summary>
    /// 是否已完成
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// 临时文件路径
    /// </summary>
    public string? TempFilePath { get; set; }

    /// <summary>
    /// 分配的线程ID
    /// </summary>
    public int ThreadId { get; set; }

    /// <summary>
    /// 重试次数
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// 下载进度百分比 (0-100)
    /// </summary>
    public double ProgressPercentage => ChunkSize > 0
        ? (DownloadedBytes * 100.0 / ChunkSize)
        : 0;

    /// <summary>
    /// 是否可以重试
    /// </summary>
    public bool CanRetry => RetryCount < MaxRetries;

    /// <summary>
    /// 创建分块列表
    /// </summary>
    /// <param name="totalSize">总文件大小</param>
    /// <param name="chunkCount">分块数量</param>
    /// <returns>分块列表</returns>
    public static List<DownloadChunk> CreateChunks(long totalSize, int chunkCount)
    {
        var chunks = new List<DownloadChunk>();
        var chunkSize = totalSize / chunkCount;
        var remainder = totalSize % chunkCount;

        for (int i = 0; i < chunkCount; i++)
        {
            var startByte = i * chunkSize;
            var endByte = (i == chunkCount - 1)
                ? totalSize - 1  // 最后一块包含余数
                : startByte + chunkSize - 1;

            chunks.Add(new DownloadChunk
            {
                ChunkId = i,
                StartByte = startByte,
                EndByte = endByte,
                ThreadId = i
            });
        }

        return chunks;
    }
}

/// <summary>
/// 下载结果
/// </summary>
public sealed class DownloadResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 下载文件路径
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 下载耗时
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// 平均下载速度（字节/秒）
    /// </summary>
    public double AverageSpeed { get; set; }

    /// <summary>
    /// 总字节数
    /// </summary>
    public long TotalBytes { get; set; }

    /// <summary>
    /// 是否验证通过
    /// </summary>
    public bool IsVerified { get; set; }
}
