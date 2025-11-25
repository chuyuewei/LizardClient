using LizardClient.Core.Interfaces;
using System.Runtime;

namespace LizardClient.Performance.Optimizers;

/// <summary>
/// 内存优化器 - 优化内存使用和GC性能
/// </summary>
public sealed class MemoryOptimizer
{
    private readonly ILogger _logger;
    private int _optimizationLevel = 2; // 0-3
    private Timer? _cleanupTimer;
    private bool _isEnabled;

    public MemoryOptimizer(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 启动内存优化
    /// </summary>
    public void Start()
    {
        if (_isEnabled)
        {
            _logger.Warning("内存优化器已在运行");
            return;
        }

        _isEnabled = true;
        ApplyOptimizations();

        // 启动定期清理任务
        _cleanupTimer = new Timer(PeriodicCleanup, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

        _logger.Info("内存优化器已启动");
    }

    /// <summary>
    /// 停止内存优化
    /// </summary>
    public void Stop()
    {
        _cleanupTimer?.Dispose();
        _cleanupTimer = null;
        _isEnabled = false;
        _logger.Info("内存优化器已停止");
    }

    /// <summary>
    /// 设置优化等级 (0=关闭, 1=低, 2=中, 3=高)
    /// </summary>
    public void SetOptimizationLevel(int level)
    {
        if (level < 0 || level > 3)
        {
            _logger.Warning($"无效的优化等级: {level}，使用默认值2");
            level = 2;
        }

        _optimizationLevel = level;
        _logger.Info($"内存优化等级设置为: {level}");

        if (_isEnabled)
        {
            ApplyOptimizations();
        }
    }

    /// <summary>
    /// 应用内存优化
    /// </summary>
    private void ApplyOptimizations()
    {
        if (_optimizationLevel == 0)
        {
            _logger.Info("内存优化已关闭");
            return;
        }

        _logger.Info($"应用内存优化 (等级: {_optimizationLevel})");

        // GC优化
        ApplyGCOptimizations();

        // 内存池配置
        ConfigureMemoryPools();

        // 大对象堆优化
        OptimizeLargeObjectHeap();

        _logger.Info("内存优化已应用");
    }

    /// <summary>
    /// GC优化
    /// </summary>
    private void ApplyGCOptimizations()
    {
        switch (_optimizationLevel)
        {
            case 3: // 激进优化
                // 使用服务器GC模式（更高吞吐量，但占用更多内存）
                if (GCSettings.IsServerGC)
                {
                    _logger.Debug("服务器GC模式已启用");
                }

                // 设置延迟模式为交互式（优化响应时间）
                GCSettings.LatencyMode = GCLatencyMode.Interactive;
                _logger.Debug("GC延迟模式: Interactive");

                // 配置大对象堆紧缩
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                _logger.Debug("大对象堆紧缩: CompactOnce");
                break;

            case 2: // 平衡优化
                GCSettings.LatencyMode = GCLatencyMode.Interactive;
                _logger.Debug("GC延迟模式: Interactive");
                break;

            case 1: // 轻度优化
                GCSettings.LatencyMode = GCLatencyMode.Batch;
                _logger.Debug("GC延迟模式: Batch");
                break;
        }
    }

    /// <summary>
    /// 配置内存池
    /// </summary>
    private void ConfigureMemoryPools()
    {
        switch (_optimizationLevel)
        {
            case 3:
                // 大内存池，减少分配频率
                _logger.Debug("配置大型内存池");
                // TODO: 初始化对象池
                break;

            case 2:
                // 中等内存池
                _logger.Debug("配置中型内存池");
                break;

            case 1:
                // 小内存池
                _logger.Debug("配置小型内存池");
                break;
        }
    }

    /// <summary>
    /// 大对象堆优化
    /// </summary>
    private void OptimizeLargeObjectHeap()
    {
        if (_optimizationLevel >= 2)
        {
            // 触发一次大对象堆紧缩
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(2, GCCollectionMode.Optimized, true, true);
            _logger.Debug("大对象堆紧缩已触发");
        }
    }

    /// <summary>
    /// 定期清理
    /// </summary>
    private void PeriodicCleanup(object? state)
    {
        try
        {
            _logger.Debug("执行定期内存清理...");

            // 获取当前内存使用
            var beforeMemory = GC.GetTotalMemory(false) / 1024 / 1024;

            // 清理未使用的内存
            CleanupUnusedMemory();

            // 获取清理后内存
            var afterMemory = GC.GetTotalMemory(false) / 1024 / 1024;
            var freed = beforeMemory - afterMemory;

            _logger.Debug($"内存清理完成，释放了 {freed} MB");
        }
        catch (Exception ex)
        {
            _logger.Error($"定期清理失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 清理未使用的内存
    /// </summary>
    public void CleanupUnusedMemory()
    {
        if (!_isEnabled)
        {
            _logger.Warning("内存优化器未启动");
            return;
        }

        _logger.Debug("开始清理未使用的内存...");

        // 触发垃圾回收
        switch (_optimizationLevel)
        {
            case 3:
                // 激进清理：强制完整GC
                GC.Collect(2, GCCollectionMode.Forced, true, true);
                GC.WaitForPendingFinalizers();
                GC.Collect(2, GCCollectionMode.Forced, true, true);
                break;

            case 2:
                // 正常清理
                GC.Collect(2, GCCollectionMode.Optimized, false, true);
                break;

            case 1:
                // 轻度清理：仅第0代
                GC.Collect(0, GCCollectionMode.Optimized, false, false);
                break;
        }

        // 紧缩大对象堆（如果启用）
        if (_optimizationLevel >= 2)
        {
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        }
    }

    /// <summary>
    /// 获取当前内存使用情况
    /// </summary>
    public MemoryUsageInfo GetMemoryUsage()
    {
        return new MemoryUsageInfo
        {
            TotalMemoryMB = GC.GetTotalMemory(false) / 1024 / 1024,
            Gen0Collections = GC.CollectionCount(0),
            Gen1Collections = GC.CollectionCount(1),
            Gen2Collections = GC.CollectionCount(2),
            IsServerGC = GCSettings.IsServerGC,
            LatencyMode = GCSettings.LatencyMode.ToString()
        };
    }

    /// <summary>
    /// 强制执行完整垃圾回收
    /// </summary>
    public void ForceFullGC()
    {
        _logger.Info("强制执行完整垃圾回收...");
        var beforeMemory = GC.GetTotalMemory(false) / 1024 / 1024;

        GC.Collect(2, GCCollectionMode.Forced, true, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true, true);

        var afterMemory = GC.GetTotalMemory(true) / 1024 / 1024;
        var freed = beforeMemory - afterMemory;

        _logger.Info($"垃圾回收完成，释放了 {freed} MB 内存");
    }
}

/// <summary>
/// 内存使用信息
/// </summary>
public sealed class MemoryUsageInfo
{
    public long TotalMemoryMB { get; init; }
    public int Gen0Collections { get; init; }
    public int Gen1Collections { get; init; }
    public int Gen2Collections { get; init; }
    public bool IsServerGC { get; init; }
    public string LatencyMode { get; init; } = string.Empty;

    public override string ToString()
    {
        return $"Memory: {TotalMemoryMB} MB | GC: Gen0={Gen0Collections}, Gen1={Gen1Collections}, Gen2={Gen2Collections} | Mode: {LatencyMode}";
    }
}
