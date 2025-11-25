using LizardClient.Performance.Models;

namespace LizardClient.Performance;

/// <summary>
/// 性能服务接口
/// </summary>
public interface IPerformanceService
{
    /// <summary>
    /// 启动性能优化
    /// </summary>
    void Start();

    /// <summary>
    /// 停止性能优化
    /// </summary>
    void Stop();

    /// <summary>
    /// 设置FPS优化等级 (0-3)
    /// </summary>
    void SetFpsOptimizationLevel(int level);

    /// <summary>
    /// 设置内存优化等级 (0-3)
    /// </summary>
    void SetMemoryOptimizationLevel(int level);

    /// <summary>
    /// 设置优化目标
    /// </summary>
    void SetOptimizationTarget(OptimizationTarget target);

    /// <summary>
    /// 获取当前性能指标
    /// </summary>
    PerformanceMetrics GetMetrics();

    /// <summary>
    /// 开始帧计时
    /// </summary>
    void BeginFrame();

    /// <summary>
    /// 结束帧计时
    /// </summary>
    void EndFrame();

    /// <summary>
    /// 强制垃圾回收
    /// </summary>
    void ForceGarbageCollection();

    /// <summary>
    /// 检查性能是否健康
    /// </summary>
    bool IsPerformanceHealthy();

    /// <summary>
    /// 获取性能摘要
    /// </summary>
    string GetPerformanceSummary();
}
