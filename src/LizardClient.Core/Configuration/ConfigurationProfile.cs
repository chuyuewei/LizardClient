using Newtonsoft.Json;

namespace LizardClient.Core.Configuration;

/// <summary>
/// 配置文件
/// 用户可以创建多个配置文件，在不同场景下切换
/// </summary>
public sealed class ConfigurationProfile
{
    /// <summary>
    /// 配置文件ID
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 配置文件名称
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = "Default";

    /// <summary>
    /// 是否为默认配置文件
    /// </summary>
    [JsonProperty("isDefault")]
    public bool IsDefault { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 最后修改时间
    /// </summary>
    [JsonProperty("lastModified")]
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 配置值字典 (key -> value)
    /// </summary>
    [JsonProperty("values")]
    public Dictionary<string, object> Values { get; set; } = new();

    /// <summary>
    /// 每个Mod的配置
    /// </summary>
    [JsonProperty("modConfigs")]
    public Dictionary<string, Dictionary<string, object>> ModConfigs { get; set; } = new();

    /// <summary>
    /// 描述
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>
    /// 标签
    /// </summary>
    [JsonProperty("tags")]
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// 获取配置值
    /// </summary>
    public T? GetValue<T>(string key, T? defaultValue = default)
    {
        if (Values.TryGetValue(key, out var value))
        {
            try
            {
                if (value is T typedValue)
                    return typedValue;

                // 尝试转换
                return (T?)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        return defaultValue;
    }

    /// <summary>
    /// 设置配置值
    /// </summary>
    public void SetValue(string key, object value)
    {
        Values[key] = value;
        LastModified = DateTime.UtcNow;
    }

    /// <summary>
    /// 克隆配置文件
    /// </summary>
    public ConfigurationProfile Clone()
    {
        return new ConfigurationProfile
        {
            Id = Guid.NewGuid().ToString(),
            Name = $"{Name} (Copy)",
            IsDefault = false,
            CreatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow,
            Values = new Dictionary<string, object>(Values),
            ModConfigs = ModConfigs.ToDictionary(
                kvp => kvp.Key,
                kvp => new Dictionary<string, object>(kvp.Value)
            ),
            Description = Description,
            Tags = new List<string>(Tags)
        };
    }
}

/// <summary>
/// Mod配置
/// 每个Mod可以有自己的配置架构和值
/// </summary>
public sealed class ModConfiguration
{
    /// <summary>
    /// Mod ID
    /// </summary>
    [JsonProperty("modId")]
    public string ModId { get; set; } = string.Empty;

    /// <summary>
    /// 配置架构
    /// </summary>
    [JsonProperty("schema")]
    public ConfigurationSchema? Schema { get; set; }

    /// <summary>
    /// 配置值
    /// </summary>
    [JsonProperty("values")]
    public Dictionary<string, object> Values { get; set; } = new();

    /// <summary>
    /// 是否启用
    /// </summary>
    [JsonProperty("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 最后修改时间
    /// </summary>
    [JsonProperty("lastModified")]
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 获取配置值
    /// </summary>
    public T? GetValue<T>(string key, T? defaultValue = default)
    {
        if (Values.TryGetValue(key, out var value))
        {
            try
            {
                if (value is T typedValue)
                    return typedValue;

                return (T?)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        // 如果没有值，尝试从Schema获取默认值
        if (Schema != null)
        {
            var property = Schema.GetProperty(key);
            if (property?.DefaultValue != null)
            {
                try
                {
                    if (property.DefaultValue is T defaultTyped)
                        return defaultTyped;

                    return (T?)Convert.ChangeType(property.DefaultValue, typeof(T));
                }
                catch
                {
                    // Ignore conversion errors
                }
            }
        }

        return defaultValue;
    }

    /// <summary>
    /// 设置配置值
    /// </summary>
    public void SetValue(string key, object value)
    {
        Values[key] = value;
        LastModified = DateTime.UtcNow;
    }

    /// <summary>
    /// 重置为默认值
    /// </summary>
    public void ResetToDefaults()
    {
        Values.Clear();
        if (Schema != null)
        {
            foreach (var property in Schema.Properties)
            {
                if (property.DefaultValue != null)
                {
                    Values[property.Key] = property.DefaultValue;
                }
            }
        }
        LastModified = DateTime.UtcNow;
    }
}
