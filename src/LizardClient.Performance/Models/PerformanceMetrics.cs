namespace LizardClient.Performance.Models;

/// <summary>
/// 性能指标数据模型
/// </summary>
public sealed class PerformanceMetrics
{
    /// <summary>
    /// 当前 FPS
    /// </summary>
    public int CurrentFps { get; set; }

    /// <summary>
    /// 平均 FPS (过去1秒)
    /// </summary>
    public int AverageFps { get; set; }

    /// <summary>
    /// 最小 FPS (过去1秒)
    /// </summary>
    public int MinFps { get; set; }

    /// <summary>
    /// 最大 FPS (过去1秒)
    /// </summary>
    public int MaxFps { get; set; }

    /// <summary>
    /// 帧时间 (毫秒)
    /// </summary>
    public double FrameTimeMs { get; set; }

    /// <summary>
    /// 内存使用 (MB)
    /// </summary>
    public long MemoryUsageMB { get; set; }

    /// <summary>
    /// GC 堆内存 (MB)
    /// </summary>
    public long GCHeapMB { get; set; }

    /// <summary>
    /// GC 第0代回收次数
    /// </summary>
    public int GCGen0Count { get; set; }

    /// <summary>
    /// GC 第1代回收次数
    /// </summary>
    public int GCGen1Count { get; set; }

    /// <summary>
    /// GC 第2代回收次数
    /// </summary>
    public int GCGen2Count { get; set; }

    /// <summary>
    /// CPU 使用率 (%)
    /// </summary>
    public double CpuUsagePercent { get; set; }

    /// <summary>
    /// 活跃线程数
    /// </summary>
    public int ThreadCount { get; set; }

    /// <summary>
    /// 渲染时间 (毫秒)
    /// </summary>
    public double RenderTimeMs { get; set; }

    /// <summary>
    /// 更新时间 (毫秒)
    /// </summary>
    public double UpdateTimeMs { get; set; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 性能等级 (Low/Medium/High/VeryHigh)
    /// </summary>
    public PerformanceLevel Level => CurrentFps switch
    {
        < 30 => PerformanceLevel.Low,
        < 60 => PerformanceLevel.Medium,
        < 120 => PerformanceLevel.High,
        _ => PerformanceLevel.VeryHigh
    };

    public override string ToString()
    {
        return $"FPS: {CurrentFps} ({AverageFps} avg) | Memory: {MemoryUsageMB} MB | Frame: {FrameTimeMs:F2}ms";
    }
}

/// <summary>
/// 性能等级
/// </summary>
public enum PerformanceLevel
{
    Low,
    Medium,
    High,
    VeryHigh
}

/// <summary>
/// 优化目标
/// </summary>
public enum OptimizationTarget
{
    /// <summary>
    /// 平衡模式
    /// </summary>
    Balanced,

    /// <summary>
    /// 最大FPS
    /// </summary>
    MaxFps,

    /// <summary>
    /// 最小内存占用
    /// </summary>
    MinMemory,

    /// <summary>
    /// 最佳画质
    /// </summary>
    MaxQuality
}
