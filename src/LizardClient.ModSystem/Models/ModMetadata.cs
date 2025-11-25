using Newtonsoft.Json;

namespace LizardClient.ModSystem.Models;

/// <summary>
/// 模组元数据 (对应 mod.json)
/// </summary>
public sealed class ModMetadata
{
    /// <summary>
    /// 模组唯一标识符
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 模组名称
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 模组版本
    /// </summary>
    [JsonProperty("version")]
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// 模组描述
    /// </summary>
    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 作者
    /// </summary>
    [JsonProperty("author")]
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// 许可证
    /// </summary>
    [JsonProperty("license")]
    public string License { get; set; } = string.Empty;

    /// <summary>
    /// 依赖列表
    /// </summary>
    [JsonProperty("dependencies")]
    public List<ModDependency> Dependencies { get; set; } = new();

    /// <summary>
    /// 不兼容列表
    /// </summary>
    [JsonProperty("incompatibilities")]
    public List<string> Incompatibilities { get; set; } = new();

    /// <summary>
    /// 建议在此模组之前加载的模组 ID 列表
    /// </summary>
    [JsonProperty("loadBefore")]
    public List<string> LoadBefore { get; set; } = new();

    /// <summary>
    /// 建议在此模组之后加载的模组 ID 列表
    /// </summary>
    [JsonProperty("loadAfter")]
    public List<string> LoadAfter { get; set; } = new();

    /// <summary>
    /// 入口点类名 (可选)
    /// </summary>
    [JsonProperty("entryPoint")]
    public string? EntryPoint { get; set; }

    /// <summary>
    /// 验证元数据是否有效
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Id) &&
               !string.IsNullOrWhiteSpace(Name) &&
               !string.IsNullOrWhiteSpace(Version);
    }
}
