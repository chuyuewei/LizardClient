using LizardClient.Core.Interfaces;
using Newtonsoft.Json;

namespace LizardClient.Core.Models;

/// <summary>
/// 更新清单，从服务器获取的更新信息
/// </summary>
public sealed class UpdateManifest
{
    /// <summary>
    /// 清单版本
    /// </summary>
    [JsonProperty("manifestVersion")]
    public string ManifestVersion { get; set; } = "1.0.0";

    /// <summary>
    /// 最新版本号
    /// </summary>
    [JsonProperty("latestVersion")]
    public string LatestVersion { get; set; } = string.Empty;

    /// <summary>
    /// 最小支持版本
    /// </summary>
    [JsonProperty("minimumVersion")]
    public string? MinimumVersion { get; set; }

    /// <summary>
    /// 可用更新列表
    /// </summary>
    [JsonProperty("updates")]
    public List<UpdateInfo> Updates { get; set; } = new();

    /// <summary>
    /// 更新服务器 URL
    /// </summary>
    [JsonProperty("updateServerUrl")]
    public string UpdateServerUrl { get; set; } = string.Empty;

    /// <summary>
    /// 更新频道 (stable, beta, dev)
    /// </summary>
    [JsonProperty("channel")]
    public string Channel { get; set; } = "stable";

    /// <summary>
    /// 最后更新时间
    /// </summary>
    [JsonProperty("lastUpdated")]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 检查是否有更新
    /// </summary>
    /// <param name="currentVersion">当前版本</param>
    /// <returns>如果有更新则返回 UpdateInfo</returns>
    public UpdateInfo? GetLatestUpdate(string currentVersion)
    {
        return Updates
            .Where(u => IsNewerVersion(u.Version, currentVersion))
            .OrderByDescending(u => u.ReleaseDate)
            .FirstOrDefault();
    }

    /// <summary>
    /// 简单版本比较 (将由 VersionComparer 替代)
    /// </summary>
    private static bool IsNewerVersion(string newVersion, string currentVersion)
    {
        return string.Compare(newVersion, currentVersion, StringComparison.Ordinal) > 0;
    }
}

/// <summary>
/// 更新频道枚举
/// </summary>
public enum UpdateChannel
{
    /// <summary>
    /// 稳定版
    /// </summary>
    Stable,

    /// <summary>
    /// 测试版
    /// </summary>
    Beta,

    /// <summary>
    /// 开发版
    /// </summary>
    Dev
}
