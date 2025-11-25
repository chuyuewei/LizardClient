using LizardClient.Core.Interfaces;
using LizardClient.Core.Models;
using LizardClient.Core.Utilities;
using Newtonsoft.Json;

namespace LizardClient.Core.Services;

/// <summary>
/// 更新检查器，负责检查客户端更新
/// </summary>
public sealed class UpdateChecker
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    private UpdateManifest? _cachedManifest;
    private DateTime _lastCheckTime;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(1);

    public UpdateChecker(ILogger logger, HttpClient? httpClient = null)
    {
        _logger = logger;
        _httpClient = httpClient ?? new HttpClient();
        _lastCheckTime = DateTime.MinValue;
    }

    /// <summary>
    /// 检查客户端更新
    /// </summary>
    /// <param name="currentVersion">当前版本</param>
    /// <param name="updateServerUrl">更新服务器URL</param>
    /// <param name="channel">更新频道</param>
    /// <param name="forceRefresh">强制刷新（忽略缓存）</param>
    /// <returns>如果有更新返回 UpdateInfo，否则返回 null</returns>
    public async Task<UpdateInfo?> CheckForUpdatesAsync(
        string currentVersion,
        string updateServerUrl,
        UpdateChannel channel = UpdateChannel.Stable,
        bool forceRefresh = false)
    {
        try
        {
            // 检查缓存
            if (!forceRefresh && _cachedManifest != null &&
                DateTime.UtcNow - _lastCheckTime < _cacheExpiration)
            {
                _logger.Info("使用缓存的更新清单");
                return GetUpdateFromManifest(_cachedManifest, currentVersion, channel);
            }

            // 从服务器获取更新清单
            var manifest = await FetchManifestAsync(updateServerUrl, channel);
            if (manifest == null)
            {
                _logger.Warning("无法获取更新清单");
                return null;
            }

            // 缓存清单
            _cachedManifest = manifest;
            _lastCheckTime = DateTime.UtcNow;

            return GetUpdateFromManifest(manifest, currentVersion, channel);
        }
        catch (Exception ex)
        {
            _logger.Error($"检查更新失败: {ex.Message}", ex);
            return null;
        }
    }

    /// <summary>
    /// 从服务器获取更新清单
    /// </summary>
    private async Task<UpdateManifest?> FetchManifestAsync(string baseUrl, UpdateChannel channel)
    {
        try
        {
            var manifestUrl = $"{baseUrl.TrimEnd('/')}/manifest-{channel.ToString().ToLower()}.json";
            _logger.Info($"正在从服务器获取更新清单: {manifestUrl}");

            var response = await _httpClient.GetAsync(manifestUrl);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var manifest = JsonConvert.DeserializeObject<UpdateManifest>(json);

            if (manifest != null)
            {
                _logger.Info($"成功获取更新清单 (最新版本: {manifest.LatestVersion})");
                return manifest;
            }

            _logger.Warning("更新清单解析失败");
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.Error($"获取更新清单失败 (网络错误): {ex.Message}", ex);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.Error($"更新清单格式错误: {ex.Message}", ex);
            return null;
        }
        catch (Exception ex)
        {
            _logger.Error($"获取更新清单时发生未知错误: {ex.Message}", ex);
            return null;
        }
    }

    /// <summary>
    /// 从清单中获取适用的更新
    /// </summary>
    private UpdateInfo? GetUpdateFromManifest(
        UpdateManifest manifest,
        string currentVersion,
        UpdateChannel channel)
    {
        // 检查当前版本是否有效
        if (!VersionComparer.IsValidVersion(currentVersion))
        {
            _logger.Warning($"当前版本格式无效: {currentVersion}");
            return null;
        }

        // 检查是否低于最小支持版本
        if (!string.IsNullOrEmpty(manifest.MinimumVersion) &&
            VersionComparer.IsLessThan(currentVersion, manifest.MinimumVersion))
        {
            _logger.Warning($"当前版本 {currentVersion} 低于最小支持版本 {manifest.MinimumVersion}");
            // 返回强制更新
            var forcedUpdate = manifest.Updates
                .Where(u => u.Version == manifest.MinimumVersion)
                .FirstOrDefault();

            if (forcedUpdate != null)
            {
                forcedUpdate.IsMandatory = true;
                return forcedUpdate;
            }
        }

        // 查找可用的更新
        var availableUpdates = manifest.Updates
            .Where(u => VersionComparer.IsGreaterThan(u.Version, currentVersion))
            .OrderByDescending(u => u.ReleaseDate)
            .ToList();

        if (!availableUpdates.Any())
        {
            _logger.Info($"当前版本 {currentVersion} 已是最新版本");
            return null;
        }

        // 获取最新更新
        var latestUpdate = availableUpdates.First();
        _logger.Info($"发现新版本: {latestUpdate.Version} (当前: {currentVersion})");

        return latestUpdate;
    }

    /// <summary>
    /// 验证更新信息是否完整
    /// </summary>
    public bool ValidateUpdateInfo(UpdateInfo updateInfo)
    {
        if (string.IsNullOrEmpty(updateInfo.Version))
        {
            _logger.Error("更新版本号为空");
            return false;
        }

        if (!VersionComparer.IsValidVersion(updateInfo.Version))
        {
            _logger.Error($"更新版本号格式无效: {updateInfo.Version}");
            return false;
        }

        if (string.IsNullOrEmpty(updateInfo.DownloadUrl))
        {
            _logger.Error("下载URL为空");
            return false;
        }

        if (string.IsNullOrEmpty(updateInfo.FileHash))
        {
            _logger.Warning("更新文件缺少哈希值，无法验证完整性");
        }

        if (updateInfo.FileSize <= 0)
        {
            _logger.Warning("更新文件大小无效");
        }

        return true;
    }

    /// <summary>
    /// 清除缓存
    /// </summary>
    public void ClearCache()
    {
        _cachedManifest = null;
        _lastCheckTime = DateTime.MinValue;
        _logger.Info("更新检查器缓存已清除");
    }
}
