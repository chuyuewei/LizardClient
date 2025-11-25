using LizardClient.Core.Interfaces;
using LizardClient.Performance.Models;
using System.Diagnostics;

namespace LizardClient.Performance.Monitoring;

/// <summary>
/// 性能监控器 - 实时跟踪性能指标
/// </summary>
public sealed class PerformanceMonitor
{
    private readonly ILogger _logger;
    private readonly Stopwatch _frameStopwatch = new();
    private readonly List<int> _fpsHistory = new();
    private readonly List<double> _frameTimeHistory = new();
    private Process? _currentProcess;
    private DateTime _lastGCCheck = DateTime.UtcNow;
    private int _lastGC0Count, _lastGC1Count, _lastGC2Count;

    private const int HistorySize = 60; // 保存60帧的历史数据

    public PerformanceMonitor(ILogger logger)
    {
        _logger = logger;
        _currentProcess = Process.GetCurrentProcess();
        InitializeGCCounters();
    }

    /// <summary>
    /// 开始帧计时
    /// </summary>
    public void BeginFrame()
    {
        _frameStopwatch.Restart();
    }

    /// <summary>
    /// 结束帧计时并记录
    /// </summary>
    public void EndFrame()
    {
        _frameStopwatch.Stop();
        var frameTimeMs = _frameStopwatch.Elapsed.TotalMilliseconds;

        // 计算FPS
        var fps = frameTimeMs > 0 ? (int)(1000.0 / frameTimeMs) : 0;

        // 添加到历史记录
        _fpsHistory.Add(fps);
        _frameTimeHistory.Add(frameTimeMs);

        // 保持历史大小
        if (_fpsHistory.Count > HistorySize)
        {
            _fpsHistory.RemoveAt(0);
            _frameTimeHistory.RemoveAt(0);
        }
    }

    /// <summary>
    /// 获取当前性能指标
    /// </summary>
    public PerformanceMetrics GetMetrics()
    {
        var metrics = new PerformanceMetrics();

        // FPS 统计
        if (_fpsHistory.Count > 0)
        {
            metrics.CurrentFps = _fpsHistory.Last();
            metrics.AverageFps = (int)_fpsHistory.Average();
            metrics.MinFps = _fpsHistory.Min();
            metrics.MaxFps = _fpsHistory.Max();
        }

        // 帧时间
        if (_frameTimeHistory.Count > 0)
        {
            metrics.FrameTimeMs = _frameTimeHistory.Last();
        }

        // 内存统计
        if (_currentProcess != null)
        {
            _currentProcess.Refresh();
            metrics.MemoryUsageMB = _currentProcess.WorkingSet64 / 1024 / 1024;
            metrics.ThreadCount = _currentProcess.Threads.Count;
        }

        // GC 统计
        metrics.GCHeapMB = GC.GetTotalMemory(false) / 1024 / 1024;
        metrics.GCGen0Count = GC.CollectionCount(0) - _lastGC0Count;
        metrics.GCGen1Count = GC.CollectionCount(1) - _lastGC1Count;
        metrics.GCGen2Count = GC.CollectionCount(2) - _lastGC2Count;

        // 每秒更新一次GC计数器
        if ((DateTime.UtcNow - _lastGCCheck).TotalSeconds >= 1.0)
        {
            UpdateGCCounters();
        }

        return metrics;
    }

    /// <summary>
    /// 记录性能指标到日志
    /// </summary>
    public void LogMetrics()
    {
        var metrics = GetMetrics();
        _logger.Debug($"Performance: {metrics}");
    }

    /// <summary>
    /// 重置统计数据
    /// </summary>
    public void Reset()
    {
        _fpsHistory.Clear();
        _frameTimeHistory.Clear();
        InitializeGCCounters();
        _logger.Info("Performance monitor reset");
    }

    /// <summary>
    /// 检查性能是否健康
    /// </summary>
    /// <param name="minAcceptableFps">最低可接受FPS</param>
    /// <returns>性能是否健康</returns>
    public bool IsPerformanceHealthy(int minAcceptableFps = 30)
    {
        var metrics = GetMetrics();
        return metrics.AverageFps >= minAcceptableFps &&
               metrics.MemoryUsageMB < 4096; // 内存小于4GB
    }

    /// <summary>
    /// 获取性能摘要
    /// </summary>
    public string GetPerformanceSummary()
    {
        var metrics = GetMetrics();
        return $"FPS: {metrics.AverageFps} avg ({metrics.MinFps}-{metrics.MaxFps}) | " +
               $"Memory: {metrics.MemoryUsageMB} MB | " +
               $"Frame Time: {metrics.FrameTimeMs:F2} ms | " +
               $"Level: {metrics.Level}";
    }

    private void InitializeGCCounters()
    {
        _lastGC0Count = GC.CollectionCount(0);
        _lastGC1Count = GC.CollectionCount(1);
        _lastGC2Count = GC.CollectionCount(2);
        _lastGCCheck = DateTime.UtcNow;
    }

    private void UpdateGCCounters()
    {
        _lastGC0Count = GC.CollectionCount(0);
        _lastGC1Count = GC.CollectionCount(1);
        _lastGC2Count = GC.CollectionCount(2);
        _lastGCCheck = DateTime.UtcNow;
    }
}
