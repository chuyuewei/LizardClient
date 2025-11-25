namespace LizardClient.Core.Models;

/// <summary>
/// 下载进度信息
/// </summary>
public sealed class DownloadProgress
{
    /// <summary>
    /// 总字节数
    /// </summary>
    public long TotalBytes { get; set; }

    /// <summary>
    /// 已下载字节数
    /// </summary>
    public long DownloadedBytes { get; set; }

    /// <summary>
    /// 进度百分比 (0-100)
    /// </summary>
    public double ProgressPercentage => TotalBytes > 0
        ? (DownloadedBytes * 100.0 / TotalBytes)
        : 0;

    /// <summary>
    /// 下载速度（字节/秒）
    /// </summary>
    public double DownloadSpeed { get; set; }

    /// <summary>
    /// 预计剩余时间
    /// </summary>
    public TimeSpan EstimatedTimeRemaining { get; set; }

    /// <summary>
    /// 下载状态
    /// </summary>
    public DownloadStatus Status { get; set; } = DownloadStatus.Idle;

    /// <summary>
    /// 错误消息（如果失败）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 当前分块信息（用于多线程下载）
    /// </summary>
    public int ActiveChunks { get; set; }

    /// <summary>
    /// 总分块数
    /// </summary>
    public int TotalChunks { get; set; }

    /// <summary>
    /// 下载开始时间
    /// </summary>
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 获取格式化的下载速度字符串
    /// </summary>
    public string FormattedSpeed => FormatBytes(DownloadSpeed) + "/s";

    /// <summary>
    /// 获取格式化的文件大小
    /// </summary>
    public string FormattedSize => $"{FormatBytes(DownloadedBytes)} / {FormatBytes(TotalBytes)}";

    /// <summary>
    /// 格式化字节大小
    /// </summary>
    private static string FormatBytes(double bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }

    /// <summary>
    /// 克隆当前进度（用于快照）
    /// </summary>
    public DownloadProgress Clone()
    {
        return new DownloadProgress
        {
            TotalBytes = TotalBytes,
            DownloadedBytes = DownloadedBytes,
            DownloadSpeed = DownloadSpeed,
            EstimatedTimeRemaining = EstimatedTimeRemaining,
            Status = Status,
            ErrorMessage = ErrorMessage,
            ActiveChunks = ActiveChunks,
            TotalChunks = TotalChunks,
            StartTime = StartTime
        };
    }
}

/// <summary>
/// 下载状态枚举
/// </summary>
public enum DownloadStatus
{
    /// <summary>
    /// 空闲
    /// </summary>
    Idle,

    /// <summary>
    /// 准备中
    /// </summary>
    Preparing,

    /// <summary>
    /// 下载中
    /// </summary>
    Downloading,

    /// <summary>
    /// 已暂停
    /// </summary>
    Paused,

    /// <summary>
    /// 验证中
    /// </summary>
    Verifying,

    /// <summary>
    /// 已完成
    /// </summary>
    Completed,

    /// <summary>
    /// 失败
    /// </summary>
    Failed,

    /// <summary>
    /// 已取消
    /// </summary>
    Cancelled
}
