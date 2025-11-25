using Newtonsoft.Json;

namespace LizardClient.Core.Models;

/// <summary>
/// 模组更新信息
/// </summary>
public sealed class ModUpdateInfo
{
    /// <summary>
    /// 模组唯一标识符
    /// </summary>
    [JsonProperty("modId")]
    public string ModId { get; set; } = string.Empty;

    /// <summary>
    /// 模组名称
    /// </summary>
    [JsonProperty("modName")]
    public string ModName { get; set; } = string.Empty;

    /// <summary>
    /// 当前已安装版本
    /// </summary>
    [JsonProperty("currentVersion")]
    public string CurrentVersion { get; set; } = string.Empty;

    /// <summary>
    /// 最新可用版本
    /// </summary>
    [JsonProperty("latestVersion")]
    public string LatestVersion { get; set; } = string.Empty;

    /// <summary>
    /// 下载 URL
    /// </summary>
    [JsonProperty("downloadUrl")]
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    [JsonProperty("fileSize")]
    public long FileSize { get; set; }

    /// <summary>
    /// 文件哈希值（SHA256）
    /// </summary>
    [JsonProperty("fileHash")]
    public string FileHash { get; set; } = string.Empty;

    /// <summary>
    /// 更新日志
    /// </summary>
    [JsonProperty("changelog")]
    public string Changelog { get; set; } = string.Empty;

    /// <summary>
    /// 发布日期
    /// </summary>
    [JsonProperty("releaseDate")]
    public DateTime ReleaseDate { get; set; }

    /// <summary>
    /// 是否为关键更新（安全补丁等）
    /// </summary>
    [JsonProperty("isCritical")]
    public bool IsCritical { get; set; }

    /// <summary>
    /// 依赖项更新（如果此模组依赖其他模组）
    /// </summary>
    [JsonProperty("dependencies")]
    public List<string> Dependencies { get; set; } = new();

    /// <summary>
    /// 模组作者
    /// </summary>
    [JsonProperty("author")]
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// 是否有可用更新
    /// </summary>
    [JsonIgnore]
    public bool HasUpdate => !string.IsNullOrEmpty(LatestVersion) &&
                              LatestVersion != CurrentVersion;
}
