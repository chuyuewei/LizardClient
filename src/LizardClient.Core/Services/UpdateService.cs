using LizardClient.Core.Interfaces;
using LizardClient.Core.Models;
using LizardClient.Core.Utilities;
using System.Diagnostics;

namespace LizardClient.Core.Services;

/// <summary>
/// 更新服务实现，整合所有更新功能
/// </summary>
public sealed class UpdateService : IUpdateService
{
    private readonly ILogger _logger;
    private readonly UpdateChecker _updateChecker;
    private readonly DownloadManager _downloadManager;
    private readonly UpdateInstaller _updateInstaller;
    private readonly BackupManager _backupManager;

    private string _updateServerUrl;
    private UpdateChannel _currentChannel;
    private UpdateState _currentState;
    private UpdateInfo? _pendingUpdate;
    private string? _downloadedUpdatePath;

    public UpdateService(
        ILogger logger,
        string updateServerUrl,
        UpdateChannel channel = UpdateChannel.Stable,
        int downloadThreads = 4)
    {
        _logger = logger;
        _updateServerUrl = updateServerUrl;
        _currentChannel = channel;
        _currentState = UpdateState.Idle;

        var httpClient = new HttpClient();
        _updateChecker = new UpdateChecker(logger, httpClient);
        _downloadManager = new DownloadManager(logger, downloadThreads, httpClient);
        _backupManager = new BackupManager(logger);
        _updateInstaller = new UpdateInstaller(logger, _backupManager);
    }

    /// <summary>
    /// 当前更新状态
    /// </summary>
    public UpdateState CurrentState => _currentState;

    /// <summary>
    /// 检查客户端更新
    /// </summary>
    public async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        try
        {
            _currentState = UpdateState.CheckingForUpdates;
            _logger.Info("正在检查客户端更新...");

            var currentVersion = GetCurrentVersion();
            var updateInfo = await _updateChecker.CheckForUpdatesAsync(
                currentVersion,
                _updateServerUrl,
                _currentChannel,
                forceRefresh: false
            );

            if (updateInfo != null)
            {
                _currentState = UpdateState.UpdateAvailable;
                _pendingUpdate = updateInfo;
                _logger.Info($"发现新版本: {updateInfo.Version}");
            }
            else
            {
                _currentState = UpdateState.Idle;
                _logger.Info("已是最新版本");
            }

            return updateInfo;
        }
        catch (Exception ex)
        {
            _currentState = UpdateState.Failed;
            _logger.Error($"检查更新失败: {ex.Message}", ex);
            return null;
        }
    }

    /// <summary>
    /// 下载更新
    /// </summary>
    public async Task DownloadUpdateAsync(
        UpdateInfo updateInfo,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _currentState = UpdateState.Downloading;
            _logger.Info($"开始下载更新: {updateInfo.Version}");

            // 创建临时目录
            var tempDir = Path.Combine(Path.GetTempPath(), "LizardClientUpdate");
            Directory.CreateDirectory(tempDir);

            var fileName = $"LizardClient_v{updateInfo.Version}.zip";
            var downloadPath = Path.Combine(tempDir, fileName);

            // 创建下载进度包装器
            var downloadProgress = new Progress<DownloadProgress>(p =>
            {
                progress?.Report(p.ProgressPercentage);
            });

            // 下载更新
            var result = await _downloadManager.DownloadAsync(
                updateInfo.DownloadUrl,
                downloadPath,
                downloadProgress,
                updateInfo.FileHash,
                cancellationToken
            );

            if (!result.IsSuccess)
            {
                _currentState = UpdateState.Failed;
                throw new Exception(result.ErrorMessage ?? "下载失败");
            }

            if (!result.IsVerified && !string.IsNullOrEmpty(updateInfo.FileHash))
            {
                _currentState = UpdateState.Failed;
                throw new Exception("文件完整性验证失败");
            }

            _downloadedUpdatePath = downloadPath;
            _currentState = UpdateState.Downloaded;
            _pendingUpdate = updateInfo;

            _logger.Info($"更新下载完成: {downloadPath}");
        }
        catch (OperationCanceledException)
        {
            _currentState = UpdateState.Cancelled;
            _logger.Info("更新下载已取消");
            throw;
        }
        catch (Exception ex)
        {
            _currentState = UpdateState.Failed;
            _logger.Error($"下载更新失败: {ex.Message}", ex);
            throw;
        }
    }

    /// <summary>
    /// 应用更新（需要重启）
    /// </summary>
    public async Task ApplyUpdateAsync(UpdateInfo updateInfo)
    {
        try
        {
            if (string.IsNullOrEmpty(_downloadedUpdatePath) || !File.Exists(_downloadedUpdatePath))
            {
                throw new InvalidOperationException("没有可用的更新文件，请先下载更新");
            }

            _currentState = UpdateState.Installing;
            _logger.Info("正在安装更新...");

            var targetDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var currentVersion = GetCurrentVersion();

            var (success, errorMessage) = await _updateInstaller.InstallUpdateAsync(
                _downloadedUpdatePath,
                targetDirectory,
                currentVersion,
                verifyIntegrity: true
            );

            if (!success)
            {
                _currentState = UpdateState.Failed;
                throw new Exception(errorMessage ?? "安装更新失败");
            }

            _currentState = UpdateState.RestartRequired;
            _logger.Info("更新安装成功，需要重启应用程序");

            // 清理下载的更新文件
            try
            {
                File.Delete(_downloadedUpdatePath);
                _downloadedUpdatePath = null;
            }
            catch (Exception ex)
            {
                _logger.Warning($"清理更新文件失败: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            _currentState = UpdateState.Failed;
            _logger.Error($"应用更新失败: {ex.Message}", ex);
            throw;
        }
    }

    /// <summary>
    /// 获取更新日志
    /// </summary>
    public async Task<string> GetChangelogAsync(string version)
    {
        try
        {
            var changelogUrl = $"{_updateServerUrl.TrimEnd('/')}/changelog/{version}.md";

            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(changelogUrl);

            if (!response.IsSuccessStatusCode)
            {
                return $"无法获取版本 {version} 的更新日志";
            }

            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            _logger.Error($"获取更新日志失败: {ex.Message}", ex);
            return $"获取更新日志失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 回滚到上一个版本
    /// </summary>
    public async Task RollbackAsync()
    {
        try
        {
            _currentState = UpdateState.RollingBack;
            _logger.Info("正在回滚更新...");

            var targetDirectory = AppDomain.CurrentDomain.BaseDirectory;
            await _updateInstaller.RollbackAsync(targetDirectory);

            _currentState = UpdateState.RestartRequired;
            _logger.Info("回滚完成，需要重启应用程序");
        }
        catch (Exception ex)
        {
            _currentState = UpdateState.Failed;
            _logger.Error($"回滚失败: {ex.Message}", ex);
            throw;
        }
    }

    /// <summary>
    /// 设置更新频道
    /// </summary>
    public void SetUpdateChannel(UpdateChannel channel)
    {
        _currentChannel = channel;
        _updateChecker.ClearCache();
        _logger.Info($"更新频道已设置为: {channel}");
    }

    /// <summary>
    /// 设置更新服务器URL
    /// </summary>
    public void SetUpdateServerUrl(string url)
    {
        _updateServerUrl = url;
        _updateChecker.ClearCache();
        _logger.Info($"更新服务器URL已设置为: {url}");
    }

    /// <summary>
    /// 调度应用程序重启
    /// </summary>
    public void ScheduleRestart(int delaySeconds = 2)
    {
        var executablePath = Process.GetCurrentProcess().MainModule?.FileName;
        if (string.IsNullOrEmpty(executablePath))
        {
            throw new InvalidOperationException("无法确定应用程序路径");
        }

        _updateInstaller.ScheduleRestart(executablePath, delaySeconds);
    }

    /// <summary>
    /// 获取当前版本
    /// </summary>
    private string GetCurrentVersion()
    {
        // 从程序集版本获取
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;

        return version != null
            ? $"{version.Major}.{version.Minor}.{version.Build}"
            : "1.0.0";
    }

    /// <summary>
    /// 清理临时文件
    /// </summary>
    public void Cleanup()
    {
        try
        {
            if (!string.IsNullOrEmpty(_downloadedUpdatePath) && File.Exists(_downloadedUpdatePath))
            {
                File.Delete(_downloadedUpdatePath);
                _downloadedUpdatePath = null;
            }

            var tempDir = Path.Combine(Path.GetTempPath(), "LizardClientUpdate");
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
        catch (Exception ex)
        {
            _logger.Warning($"清理临时文件失败: {ex.Message}");
        }
    }
}
