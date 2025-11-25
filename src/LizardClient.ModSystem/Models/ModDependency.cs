using Newtonsoft.Json;

namespace LizardClient.ModSystem.Models;

/// <summary>
/// 模组依赖定义
/// </summary>
public sealed class ModDependency
{
    /// <summary>
    /// 依赖的模组 ID
    /// </summary>
    [JsonProperty("modId")]
    public string ModId { get; set; } = string.Empty;

    /// <summary>
    /// 版本范围 (例如 "1.0.0", ">=1.0.0", "1.0.0-2.0.0")
    /// </summary>
    [JsonProperty("versionRange")]
    public string VersionRange { get; set; } = "*";

    /// <summary>
    /// 是否为可选依赖
    /// </summary>
    [JsonProperty("optional")]
    public bool IsOptional { get; set; }

    /// <summary>
    /// 依赖类型 (Required, Optional, Incompatible)
    /// </summary>
    [JsonIgnore]
    public DependencyType Type { get; set; } = DependencyType.Required;
}

public enum DependencyType
{
    Required,
    Optional,
    Incompatible
}
