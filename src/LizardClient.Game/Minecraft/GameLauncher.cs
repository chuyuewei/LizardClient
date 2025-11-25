using LizardClient.Core.Interfaces;
using LizardClient.Core.Models;
using LizardClient.Game.Java;

namespace LizardClient.Game.Minecraft;

/// <summary>
/// 游戏启动器 - 统一管理游戏启动流程
/// </summary>
public sealed class GameLauncher : IDisposable
{
    private readonly ILogger _logger;
    private readonly VersionManager _versionManager;
    private readonly JavaDetector _javaDetector;
    private MinecraftProcess? _minecraftProcess;
    private bool _disposed;

    public GameLauncher(ILogger logger, string minecraftDirectory)
    {
        _logger = logger;
        _versionManager = new VersionManager(logger, minecraftDirectory);
        _javaDetector = new JavaDetector(logger);
    }

    /// <summary>
    /// 当前运行的游戏进程
    /// </summary>
    public MinecraftProcess? CurrentProcess => _minecraftProcess;

    /// <summary>
    /// 游戏是否正在运行
    /// </summary>
    public bool IsGameRunning => _minecraftProcess?.IsRunning ?? false;

    /// <summary>
    /// 启动游戏（带进度报告）
    /// </summary>
    public async Task<bool> LaunchGameAsync(
        GameProfile profile,
        IProgress<LaunchProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.Info($"开始启动游戏 - 配置: {profile.Name}, 版本: {profile.MinecraftVersion}");

            // 1. 准备阶段
            ReportProgress(progress, LaunchStatus.Preparing, 0, "正在准备启动...");

            // 2. 验证文件完整性
            ReportProgress(progress, LaunchStatus.ValidatingFiles, 10, "验证游戏文件...");
            if (!await ValidateGameFilesAsync(profile))
            {
                var errorMsg = $"游戏文件验证失败: 版本 {profile.MinecraftVersion} 未正确安装";
                _logger.Error(errorMsg);
                ReportProgress(progress, LaunchStatus.Failed, 0, errorMsg);
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // 3. 检测 Java 环境
            ReportProgress(progress, LaunchStatus.DetectingJava, 25, "检测 Java 环境...");
            var javaInfo = await DetectJavaAsync(profile);
            if (javaInfo == null)
            {
                var errorMsg = "未找到合适的 Java 版本，请确保已安装 Java";
                _logger.Error(errorMsg);
                ReportProgress(progress, LaunchStatus.Failed, 0, errorMsg);
                return false;
            }

            _logger.Info($"使用 Java: {javaInfo.JavaPath} (版本 {javaInfo.MajorVersion})");

            cancellationToken.ThrowIfCancellationRequested();

            // 4. 构建启动参数
            ReportProgress(progress, LaunchStatus.BuildingArguments, 40, "构建启动参数...");

            // 确保游戏目录设置正确
            if (string.IsNullOrEmpty(profile.GameDirectory))
            {
                profile.GameDirectory = GetDefaultMinecraftDirectory();
                _logger.Info($"使用默认游戏目录: {profile.GameDirectory}");
            }

            cancellationToken.ThrowIfCancellationRequested();

            // 5. 启动进程
            ReportProgress(progress, LaunchStatus.LaunchingProcess, 60, "启动 Minecraft 进程...");
            _minecraftProcess = new MinecraftProcess(_logger);

            var launchSuccess = await _minecraftProcess.LaunchAsync(profile, javaInfo.JavaPath);
            if (!launchSuccess)
            {
                var errorMsg = "启动 Minecraft 进程失败";
                _logger.Error(errorMsg);
                ReportProgress(progress, LaunchStatus.Failed, 0, errorMsg);
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // 6. 等待游戏初始化
            ReportProgress(progress, LaunchStatus.WaitingForGameInit, 80, "等待游戏初始化...");
            await Task.Delay(2000, cancellationToken); // 等待游戏窗口创建

            // 7. 完成
            ReportProgress(progress, LaunchStatus.Completed, 100, "游戏启动成功！");
            _logger.Info($"游戏启动成功 (PID: {_minecraftProcess.ProcessId})");

            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.Warning("游戏启动被取消");
            ReportProgress(progress, LaunchStatus.Failed, 0, "启动已取消");
            return false;
        }
        catch (Exception ex)
        {
            _logger.Error($"游戏启动失败: {ex.Message}", ex);
            ReportProgress(progress, LaunchStatus.Failed, 0, $"启动失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 验证游戏文件完整性
    /// </summary>
    private async Task<bool> ValidateGameFilesAsync(GameProfile profile)
    {
        return await Task.Run(() =>
        {
            try
            {
                // 检查版本是否已安装
                var installedVersions = _versionManager.GetInstalledVersions();
                var targetVersion = installedVersions.FirstOrDefault(v => v.Id == profile.MinecraftVersion);

                if (targetVersion == null)
                {
                    _logger.Warning($"版本 {profile.MinecraftVersion} 未安装");
                    return false;
                }

                // 验证版本完整性
                var isValid = _versionManager.ValidateVersion(profile.MinecraftVersion);
                if (!isValid)
                {
                    _logger.Warning($"版本 {profile.MinecraftVersion} 文件不完整");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"验证游戏文件时出错: {ex.Message}", ex);
                return false;
            }
        });
    }

    /// <summary>
    /// 检测合适的 Java 版本
    /// </summary>
    private async Task<JavaVersionInfo?> DetectJavaAsync(GameProfile profile)
    {
        try
        {
            var availableJavas = await _javaDetector.DetectAllJavaInstallationsAsync();

            if (availableJavas.Count == 0)
            {
                _logger.Error("系统中未找到 Java 安装");
                return null;
            }

            var recommendedJava = _javaDetector.RecommendJavaForMinecraft(profile.MinecraftVersion, availableJavas);

            if (recommendedJava == null)
            {
                _logger.Warning($"未找到适合 Minecraft {profile.MinecraftVersion} 的 Java 版本");
                // 使用找到的第一个 Java
                recommendedJava = availableJavas.First();
                _logger.Info($"将使用 Java {recommendedJava.MajorVersion}");
            }

            return recommendedJava;
        }
        catch (Exception ex)
        {
            _logger.Error($"检测 Java 时出错: {ex.Message}", ex);
            return null;
        }
    }

    /// <summary>
    /// 获取默认 Minecraft 目录
    /// </summary>
    private string GetDefaultMinecraftDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, ".minecraft");
    }

    /// <summary>
    /// 报告进度
    /// </summary>
    private void ReportProgress(IProgress<LaunchProgress>? progress, LaunchStatus status, int percentage, string message)
    {
        progress?.Report(LaunchProgress.Create(status, percentage, message));
    }

    /// <summary>
    /// 获取已安装的版本列表
    /// </summary>
    public List<VersionInfo> GetInstalledVersions()
    {
        return _versionManager.GetInstalledVersions();
    }

    /// <summary>
    /// 获取可用的版本列表（在线）
    /// </summary>
    public async Task<List<VersionInfo>> GetAvailableVersionsAsync()
    {
        return await _versionManager.GetAvailableVersionsAsync();
    }

    /// <summary>
    /// 安装指定版本
    /// </summary>
    public async Task<bool> InstallVersionAsync(string versionId, IProgress<int>? progress = null)
    {
        return await _versionManager.InstallVersionAsync(versionId, progress);
    }

    /// <summary>
    /// 停止游戏
    /// </summary>
    public void StopGame()
    {
        if (_minecraftProcess != null && _minecraftProcess.IsRunning)
        {
            _logger.Info("正在停止游戏...");
            _minecraftProcess.Kill();
        }
    }

    /// <summary>
    /// 等待游戏退出
    /// </summary>
    public async Task WaitForGameExitAsync()
    {
        if (_minecraftProcess != null)
        {
            await _minecraftProcess.WaitForExitAsync();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _minecraftProcess?.Dispose();
        _logger.Info("游戏启动器已释放");

        _disposed = true;
    }
}
