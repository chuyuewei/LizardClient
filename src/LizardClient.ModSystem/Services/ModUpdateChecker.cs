using LizardClient.Core.Interfaces;
using LizardClient.Core.Models;
using LizardClient.Core.Utilities;
using LizardClient.ModSystem.Loader;
using LizardClient.ModSystem.Models;
using Newtonsoft.Json;

namespace LizardClient.ModSystem.Services;

/// <summary>
/// 模组更新检查器
/// </summary>
public sealed class ModUpdateChecker
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    private readonly ModLoader _modLoader;

    public ModUpdateChecker(ILogger logger, ModLoader modLoader, HttpClient? httpClient = null)
    {
        _logger = logger;
        _modLoader = modLoader;
        _httpClient = httpClient ?? new HttpClient();
    }

    /// <summary>
    /// 检查所有已安装模组的更新
    /// </summary>
    /// <param name="modRepositoryUrl">模组仓库URL</param>
    /// <returns>有可用更新的模组列表</returns>
    public async Task<List<ModUpdateInfo>> CheckAllModUpdatesAsync(string modRepositoryUrl)
    {
        var updatesAvailable = new List<ModUpdateInfo>();

        try
        {
            _logger.Info("开始检查模组更新...");

            // 获取所有已加载的模组
            var loadedMods = _modLoader.LoadedMods.Values;

            if (!loadedMods.Any())
            {
                _logger.Info("没有已安装的模组");
                return updatesAvailable;
            }

            // 批量检查所有模组
            var checkTasks = loadedMods.Select(mod =>
                CheckSingleModUpdateAsync(mod.Info.Id, mod.Info.Version, modRepositoryUrl)
            );

            var results = await Task.WhenAll(checkTasks);
            updatesAvailable = results.Where(u => u != null && u.HasUpdate).ToList()!;

            _logger.Info($"检查完成，发现 {updatesAvailable.Count} 个模组有可用更新");
        }
        catch (Exception ex)
        {
            _logger.Error($"检查模组更新失败: {ex.Message}", ex);
        }

        return updatesAvailable;
    }

    /// <summary>
    /// 检查单个模组的更新
    /// </summary>
    /// <param name="modId">模组ID</param>
    /// <param name="currentVersion">当前版本</param>
    /// <param name="repositoryUrl">仓库URL</param>
    /// <returns>模组更新信息，如果无更新则返回 null</returns>
    public async Task<ModUpdateInfo?> CheckSingleModUpdateAsync(
        string modId,
        string currentVersion,
        string repositoryUrl)
    {
        try
        {
            // 构建模组信息URL
            var modInfoUrl = $"{repositoryUrl.TrimEnd('/')}/mods/{modId}/latest.json";
            _logger.Info($"检查模组 {modId} 的更新...");

            var response = await _httpClient.GetAsync(modInfoUrl);

            // 如果模组不在仓库中，返回 null
            if (!response.IsSuccessStatusCode)
            {
                _logger.Info($"模组 {modId} 在仓库中未找到");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var latestModInfo = JsonConvert.DeserializeObject<ModUpdateInfo>(json);

            if (latestModInfo == null)
            {
                _logger.Warning($"无法解析模组 {modId} 的更新信息");
                return null;
            }

            // 设置当前版本
            latestModInfo.CurrentVersion = currentVersion;

            // 比较版本
            if (VersionComparer.IsGreaterThan(latestModInfo.LatestVersion, currentVersion))
            {
                _logger.Info($"模组 {modId} 发现新版本: {latestModInfo.LatestVersion} (当前: {currentVersion})");
                return latestModInfo;
            }

            _logger.Info($"模组 {modId} 已是最新版本");
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.Warning($"检查模组 {modId} 更新失败 (网络错误): {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.Error($"检查模组 {modId} 更新时发生错误: {ex.Message}", ex);
            return null;
        }
    }

    /// <summary>
    /// 检查指定模组列表的更新
    /// </summary>
    /// <param name="modIds">要检查的模组ID列表</param>
    /// <param name="repositoryUrl">仓库URL</param>
    /// <returns>更新信息列表</returns>
    public async Task<List<ModUpdateInfo>> CheckSpecificModsAsync(
        IEnumerable<string> modIds,
        string repositoryUrl)
    {
        var updates = new List<ModUpdateInfo>();

        foreach (var modId in modIds)
        {
            var mod = _modLoader.GetMod(modId);
            if (mod == null)
            {
                _logger.Warning($"模组 {modId} 未安装，跳过检查");
                continue;
            }

            var updateInfo = await CheckSingleModUpdateAsync(modId, mod.Info.Version, repositoryUrl);
            if (updateInfo != null && updateInfo.HasUpdate)
            {
                updates.Add(updateInfo);
            }
        }

        return updates;
    }

    /// <summary>
    /// 获取模组的更新历史
    /// </summary>
    /// <param name="modId">模组ID</param>
    /// <param name="repositoryUrl">仓库URL</param>
    /// <returns>更新历史列表</returns>
    public async Task<List<ModUpdateInfo>> GetModUpdateHistoryAsync(
        string modId,
        string repositoryUrl)
    {
        try
        {
            var historyUrl = $"{repositoryUrl.TrimEnd('/')}/mods/{modId}/history.json";
            _logger.Info($"获取模组 {modId} 的更新历史...");

            var response = await _httpClient.GetAsync(historyUrl);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var history = JsonConvert.DeserializeObject<List<ModUpdateInfo>>(json);

            return history ?? new List<ModUpdateInfo>();
        }
        catch (Exception ex)
        {
            _logger.Error($"获取模组 {modId} 更新历史失败: {ex.Message}", ex);
            return new List<ModUpdateInfo>();
        }
    }

    /// <summary>
    /// 验证模组更新信息
    /// </summary>
    public bool ValidateModUpdateInfo(ModUpdateInfo updateInfo)
    {
        if (string.IsNullOrEmpty(updateInfo.ModId))
        {
            _logger.Error("模组ID为空");
            return false;
        }

        if (string.IsNullOrEmpty(updateInfo.LatestVersion))
        {
            _logger.Error($"模组 {updateInfo.ModId} 最新版本号为空");
            return false;
        }

        if (!VersionComparer.IsValidVersion(updateInfo.LatestVersion))
        {
            _logger.Error($"模组 {updateInfo.ModId} 版本号格式无效: {updateInfo.LatestVersion}");
            return false;
        }

        if (string.IsNullOrEmpty(updateInfo.DownloadUrl))
        {
            _logger.Error($"模组 {updateInfo.ModId} 下载URL为空");
            return false;
        }

        if (string.IsNullOrEmpty(updateInfo.FileHash))
        {
            _logger.Warning($"模组 {updateInfo.ModId} 缺少文件哈希值");
        }

        return true;
    }
}
