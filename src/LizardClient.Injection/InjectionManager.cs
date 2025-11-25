using LizardClient.Core.Interfaces;
using LizardClient.Core.Models;
using LizardClient.Injection.Injectors;
using LizardClient.Injection.Memory;
using System.Diagnostics;

namespace LizardClient.Injection;

/// <summary>
/// 注入状态
/// </summary>
public enum InjectionStatus
{
    /// <summary>
    /// 未开始
    /// </summary>
    NotStarted,

    /// <summary>
    /// 等待目标进程
    /// </summary>
    WaitingForProcess,

    /// <summary>
    /// 准备注入
    /// </summary>
    Preparing,

    /// <summary>
    /// 注入中
    /// </summary>
    Injecting,

    /// <summary>
    /// 注入成功
    /// </summary>
    Success,

    /// <summary>
    /// 注入失败
    /// </summary>
    Failed
}

/// <summary>
/// 注入进度信息
/// </summary>
public sealed class InjectionProgress
{
    public InjectionStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public int CurrentDll { get; set; }
    public int TotalDlls { get; set; }
}

/// <summary>
/// 注入管理器 - 简化 DLL 注入流程
/// </summary>
public sealed class InjectionManager : IDisposable
{
    private readonly ILogger _logger;
    private readonly InjectionConfig _config;
    private ProcessInjector? _injector;
    private bool _disposed;

    public InjectionManager(ILogger logger, InjectionConfig config)
    {
        _logger = logger;
        _config = config;

        // 验证配置
        if (!config.IsValid(out var errorMessage))
        {
            throw new ArgumentException($"注入配置无效: {errorMessage}");
        }

        _logger.Info("注入管理器已初始化");
    }

    /// <summary>
    /// 注入到指定进程
    /// </summary>
    public async Task<bool> InjectAsync(
        int processId,
        IProgress<InjectionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.Info($"开始注入流程 - 目标进程 ID: {processId}");

            // 验证进程存在
            if (!ProcessExists(processId))
            {
                _logger.Error($"进程 {processId} 不存在");
                ReportProgress(progress, InjectionStatus.Failed, "目标进程不存在");
                return false;
            }

            // 准备阶段
            ReportProgress(progress, InjectionStatus.Preparing, "正在准备注入...");

            // 延迟等待游戏初始化
            if (_config.DelayMs > 0)
            {
                _logger.Info($"等待 {_config.DelayMs}ms 以确保游戏初始化完成...");
                await Task.Delay(_config.DelayMs, cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();

            // 创建注入器
            _injector = new ProcessInjector(_logger, processId);

            // 检查反调试
            if (_config.CheckAntiDebug && _injector.IsTargetBeingDebugged())
            {
                _logger.Warning("检测到目标进程正在被调试，可能影响注入");
            }

            // 注入所有 DLL
            for (int i = 0; i < _config.DllPaths.Count; i++)
            {
                var dllPath = _config.DllPaths[i];
                var dllName = Path.GetFileName(dllPath);

                ReportProgress(progress, InjectionStatus.Injecting,
                    $"注入 {dllName}... ({i + 1}/{_config.DllPaths.Count})",
                    currentDll: i + 1,
                    totalDlls: _config.DllPaths.Count);

                var success = await InjectDllWithRetryAsync(dllPath, cancellationToken);

                if (!success)
                {
                    var errorMsg = $"注入 {dllName} 失败";
                    _logger.Error(errorMsg);
                    ReportProgress(progress, InjectionStatus.Failed, errorMsg);
                    return false;
                }

                _logger.Info($"成功注入 {dllName}");
                cancellationToken.ThrowIfCancellationRequested();
            }

            // 注入完成
            ReportProgress(progress, InjectionStatus.Success, "所有 DLL 注入成功！");
            _logger.Info("注入流程完成");
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.Warning("注入被取消");
            ReportProgress(progress, InjectionStatus.Failed, "注入已取消");
            return false;
        }
        catch (Exception ex)
        {
            _logger.Error($"注入失败: {ex.Message}", ex);
            ReportProgress(progress, InjectionStatus.Failed, $"注入失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 等待进程启动并注入
    /// </summary>
    public async Task<bool> WaitForProcessAndInjectAsync(
        string processName,
        TimeSpan timeout,
        IProgress<InjectionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.Info($"等待进程 {processName} 启动...");
            ReportProgress(progress, InjectionStatus.WaitingForProcess, $"等待 {processName} 启动...");

            var startTime = DateTime.Now;
            Process? targetProcess = null;

            // 轮询等待进程
            while (DateTime.Now - startTime < timeout)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var processes = Process.GetProcessesByName(processName);
                if (processes.Length > 0)
                {
                    targetProcess = processes[0];
                    _logger.Info($"找到目标进程 (PID: {targetProcess.Id})");
                    break;
                }

                await Task.Delay(500, cancellationToken);
            }

            if (targetProcess == null)
            {
                _logger.Error($"超时: 未找到进程 {processName}");
                ReportProgress(progress, InjectionStatus.Failed, $"未找到进程 {processName}");
                return false;
            }

            // 执行注入
            return await InjectAsync(targetProcess.Id, progress, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.Warning("等待进程被取消");
            ReportProgress(progress, InjectionStatus.Failed, "等待进程已取消");
            return false;
        }
        catch (Exception ex)
        {
            _logger.Error($"等待进程并注入失败: {ex.Message}", ex);
            ReportProgress(progress, InjectionStatus.Failed, $"失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 注入 DLL（带重试）
    /// </summary>
    private async Task<bool> InjectDllWithRetryAsync(string dllPath, CancellationToken cancellationToken)
    {
        if (_injector == null)
        {
            _logger.Error("注入器未初始化");
            return false;
        }

        int attemptCount = 0;
        int maxAttempts = _config.RetryOnFailure ? _config.MaxRetryCount + 1 : 1;

        while (attemptCount < maxAttempts)
        {
            attemptCount++;

            if (attemptCount > 1)
            {
                _logger.Info($"重试注入 ({attemptCount}/{maxAttempts})...");
                await Task.Delay(_config.RetryIntervalMs, cancellationToken);
            }

            try
            {
                var success = _injector.InjectDll(dllPath, _config.Method);
                if (success)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.Warning($"注入尝试 {attemptCount} 失败: {ex.Message}");
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        return false;
    }

    /// <summary>
    /// 检查进程是否存在
    /// </summary>
    private bool ProcessExists(int processId)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 报告进度
    /// </summary>
    private void ReportProgress(
        IProgress<InjectionProgress>? progress,
        InjectionStatus status,
        string message,
        string? errorMessage = null,
        int currentDll = 0,
        int totalDlls = 0)
    {
        progress?.Report(new InjectionProgress
        {
            Status = status,
            Message = message,
            ErrorMessage = errorMessage,
            CurrentDll = currentDll,
            TotalDlls = totalDlls
        });
    }

    public void Dispose()
    {
        if (_disposed) return;

        _injector?.Dispose();
        _logger.Info("注入管理器已释放");

        _disposed = true;
    }
}
