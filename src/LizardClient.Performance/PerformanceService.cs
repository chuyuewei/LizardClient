using LizardClient.Core.Interfaces;
using LizardClient.Performance.Models;
using LizardClient.Performance.Monitoring;
using LizardClient.Performance.Optimizers;

namespace LizardClient.Performance;

/// <summary>
/// 性能服务实现
/// </summary>
public sealed class PerformanceService : IPerformanceService
{
    private readonly ILogger _logger;
    private readonly PerformanceMonitor _monitor;
    private readonly FpsOptimizer _fpsOptimizer;
    private readonly MemoryOptimizer _memoryOptimizer;
    private bool _isRunning;

    public PerformanceService(ILogger logger)
    {
        _logger = logger;
        _monitor = new PerformanceMonitor(logger);
        _fpsOptimizer = new FpsOptimizer(logger);
        _memoryOptimizer = new MemoryOptimizer(logger);
    }

    /// <summary>
    /// 启动性能优化
    /// </summary>
    public void Start()
    {
        if (_isRunning)
        {
            _logger.Warning("性能服务已在运行");
            return;
        }

        _isRunning = true;
        _memoryOptimizer.Start();
        _fpsOptimizer.ApplyOptimizations();

        _logger.Info("性能服务已启动");
    }

    /// <summary>
    /// 停止性能优化
    /// </summary>
    public void Stop()
    {
        if (!_isRunning)
        {
            return;
        }

        _memoryOptimizer.Stop();
        _isRunning = false;

        _logger.Info("性能服务已停止");
    }

    /// <summary>
    /// 设置FPS优化等级 (0-3)
    /// </summary>
    public void SetFpsOptimizationLevel(int level)
    {
        _fpsOptimizer.SetOptimizationLevel(level);
    }

    /// <summary>
    /// 设置内存优化等级 (0-3)
    /// </summary>
    public void SetMemoryOptimizationLevel(int level)
    {
        _memoryOptimizer.SetOptimizationLevel(level);
    }

    /// <summary>
    /// 设置优化目标
    /// </summary>
    public void SetOptimizationTarget(OptimizationTarget target)
    {
        _fpsOptimizer.SetOptimizationTarget(target);
    }

    /// <summary>
    /// 获取当前性能指标
    /// </summary>
    public PerformanceMetrics GetMetrics()
    {
        return _monitor.GetMetrics();
    }

    /// <summary>
    /// 开始帧计时
    /// </summary>
    public void BeginFrame()
    {
        _monitor.BeginFrame();
    }

    /// <summary>
    /// 结束帧计时
    /// </summary>
    public void EndFrame()
    {
        _monitor.EndFrame();
    }

    /// <summary>
    /// 强制垃圾回收
    /// </summary>
    public void ForceGarbageCollection()
    {
        _memoryOptimizer.ForceFullGC();
    }

    /// <summary>
    /// 检查性能是否健康
    /// </summary>
    public bool IsPerformanceHealthy()
    {
        return _monitor.IsPerformanceHealthy();
    }

    /// <summary>
    /// 获取性能摘要
    /// </summary>
    public string GetPerformanceSummary()
    {
        return _monitor.GetPerformanceSummary();
    }
}
