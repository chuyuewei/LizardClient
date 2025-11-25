namespace LizardClient.Core.Configuration;

/// <summary>
/// 属性类型枚举
/// </summary>
public enum PropertyType
{
    /// <summary>
    /// 字符串
    /// </summary>
    String,

    /// <summary>
    /// 整数
    /// </summary>
    Integer,

    /// <summary>
    /// 浮点数
    /// </summary>
    Float,

    /// <summary>
    /// 布尔值
    /// </summary>
    Boolean,

    /// <summary>
    /// 枚举（下拉选择）
    /// </summary>
    Enum,

    /// <summary>
    /// 颜色选择器
    /// </summary>
    Color,

    /// <summary>
    /// 滑块（范围选择）
    /// </summary>
    Slider,

    /// <summary>
    /// 按键绑定
    /// </summary>
    KeyBinding,

    /// <summary>
    /// 文件路径
    /// </summary>
    FilePath,

    /// <summary>
    /// 目录路径
    /// </summary>
    DirectoryPath
}

/// <summary>
/// 验证类型
/// </summary>
public enum ValidationType
{
    /// <summary>
    /// 无验证
    /// </summary>
    None,

    /// <summary>
    /// 范围验证（最小/最大值）
    /// </summary>
    Range,

    /// <summary>
    /// 正则表达式模式匹配
    /// </summary>
    Pattern,

    /// <summary>
    /// 枚举值验证
    /// </summary>
    Enum,

    /// <summary>
    /// 自定义验证函数
    /// </summary>
    Custom,

    /// <summary>
    /// 必填项
    /// </summary>
    Required,

    /// <summary>
    /// 依赖验证（依赖其他配置项）
    /// </summary>
    Dependency
}

/// <summary>
/// 验证规则
/// </summary>
public sealed class ValidationRule
{
    /// <summary>
    /// 验证类型
    /// </summary>
    public ValidationType Type { get; set; } = ValidationType.None;

    /// <summary>
    /// 最小值（用于Range验证）
    /// </summary>
    public object? MinValue { get; set; }

    /// <summary>
    /// 最大值（用于Range验证）
    /// </summary>
    public object? MaxValue { get; set; }

    /// <summary>
    /// 正则表达式模式（用于Pattern验证）
    /// </summary>
    public string? Pattern { get; set; }

    /// <summary>
    /// 允许的枚举值（用于Enum验证）
    /// </summary>
    public List<object>? AllowedValues { get; set; }

    /// <summary>
    /// 错误消息模板
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 依赖的配置项键（用于Dependency验证）
    /// </summary>
    public string? DependsOn { get; set; }

    /// <summary>
    /// 依赖条件（当依赖项等于此值时才验证）
    /// </summary>
    public object? DependsOnValue { get; set; }
}

/// <summary>
/// UI显示提示
/// </summary>
public sealed class UIHints
{
    /// <summary>
    /// 分组/类别
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// 显示顺序
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 是否只读
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// 是否隐藏
    /// </summary>
    public bool IsHidden { get; set; }

    /// <summary>
    /// 单位标签（如 "px", "%", "ms"）
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// 步进值（用于数字输入）
    /// </summary>
    public double? Step { get; set; }

    /// <summary>
    /// 占位符文本
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// 图标名称
    /// </summary>
    public string? Icon { get; set; }
}

/// <summary>
/// 配置属性定义
/// </summary>
public sealed class ConfigProperty
{
    /// <summary>
    /// 属性键（唯一标识）
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 属性类型
    /// </summary>
    public PropertyType Type { get; set; } = PropertyType.String;

    /// <summary>
    /// 默认值
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// 描述/说明
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 验证规则
    /// </summary>
    public ValidationRule? Validation { get; set; }

    /// <summary>
    /// UI显示提示
    /// </summary>
    public UIHints? UIHints { get; set; }

    /// <summary>
    /// 是否需要重启才能生效
    /// </summary>
    public bool RequiresRestart { get; set; }
}

/// <summary>
/// 配置架构
/// </summary>
public sealed class ConfigurationSchema
{
    /// <summary>
    /// 架构ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 架构名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 架构描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 版本号
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// 属性列表
    /// </summary>
    public List<ConfigProperty> Properties { get; set; } = new();

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 获取指定键的属性
    /// </summary>
    public ConfigProperty? GetProperty(string key)
    {
        return Properties.FirstOrDefault(p => p.Key == key);
    }

    /// <summary>
    /// 按类别分组属性
    /// </summary>
    public Dictionary<string, List<ConfigProperty>> GetPropertiesByCategory()
    {
        return Properties
            .GroupBy(p => p.UIHints?.Category ?? "General")
            .ToDictionary(g => g.Key, g => g.OrderBy(p => p.UIHints?.Order ?? 0).ToList());
    }
}
