namespace LizardClient.Core.Models;

/// <summary>
/// 模组元数据信息
/// </summary>
public sealed class ModInfo
{
    /// <summary>
    /// 模组唯一标识符
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 模组显示名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 模组描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 模组版本
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// 模组作者
    /// </summary>
    public string Author { get; set; } = "LizardClient Team";

    /// <summary>
    /// 模组图标路径（可选）
    /// </summary>
    public string? IconPath { get; set; }

    /// <summary>
    /// 模组类别（PvP、性能、视觉、实用等）
    /// </summary>
    public ModCategory Category { get; set; } = ModCategory.Utility;

    /// <summary>
    /// 依赖的其他模组 ID 列表
    /// </summary>
    public List<string> Dependencies { get; set; } = new();

    /// <summary>
    /// 支持的 Minecraft 版本列表
    /// </summary>
    public List<string> SupportedVersions { get; set; } = new();

    /// <summary>
    /// 是否默认启用
    /// </summary>
    public bool EnabledByDefault { get; set; }

    /// <summary>
    /// 是否为核心模组（不可禁用）
    /// </summary>
    public bool IsCore { get; set; }

    /// <summary>
    /// 模组配置数据（JSON 格式）
    /// </summary>
    public string? ConfigData { get; set; }
}

/// <summary>
/// 模组类别枚举
/// </summary>
public enum ModCategory
{
    /// <summary>
    /// 性能优化
    /// </summary>
    Performance,

    /// <summary>
    /// PvP 功能
    /// </summary>
    PvP,

    /// <summary>
    /// 视觉增强
    /// </summary>
    Visual,

    /// <summary>
    /// 实用工具
    /// </summary>
    Utility,

    /// <summary>
    /// 信息显示
    /// </summary>
    Information,

    /// <summary>
    /// 其他
    /// </summary>
    Other
}
