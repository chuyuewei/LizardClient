using System.Text.RegularExpressions;

namespace LizardClient.Core.Configuration;

/// <summary>
/// 验证结果
/// </summary>
public sealed class ValidationResult
{
    /// <summary>
    /// 验证是否通过
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static ValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static ValidationResult Failure(string errorMessage) => new()
    {
        IsValid = false,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// 验证引擎
/// 负责验证配置值是否符合规则
/// </summary>
public sealed class ValidationEngine
{
    /// <summary>
    /// 验证配置属性的值
    /// </summary>
    public ValidationResult Validate(ConfigProperty property, object? value)
    {
        if (property.Validation == null)
            return ValidationResult.Success();

        var validation = property.Validation;

        // 必填项验证
        if (validation.Type == ValidationType.Required)
        {
            if (value == null || (value is string str && string.IsNullOrWhiteSpace(str)))
            {
                return ValidationResult.Failure(
                    validation.ErrorMessage ?? $"{property.DisplayName} is required"
                );
            }
        }

        // 如果值为null且不是必填项，跳过其他验证
        if (value == null)
            return ValidationResult.Success();

        return validation.Type switch
        {
            ValidationType.Range => ValidateRange(property, value, validation),
            ValidationType.Pattern => ValidatePattern(property, value, validation),
            ValidationType.Enum => ValidateEnum(property, value, validation),
            ValidationType.Custom => ValidationResult.Success(), // 自定义验证需要外部处理
            ValidationType.Dependency => ValidationResult.Success(), // 依赖验证需要上下文
            _ => ValidationResult.Success()
        };
    }

    /// <summary>
    /// 验证范围
    /// </summary>
    private ValidationResult ValidateRange(ConfigProperty property, object value, ValidationRule validation)
    {
        try
        {
            double numValue = Convert.ToDouble(value);
            double? min = validation.MinValue != null ? Convert.ToDouble(validation.MinValue) : null;
            double? max = validation.MaxValue != null ? Convert.ToDouble(validation.MaxValue) : null;

            if (min.HasValue && numValue < min.Value)
            {
                return ValidationResult.Failure(
                    validation.ErrorMessage ??
                    $"{property.DisplayName} must be at least {min.Value}"
                );
            }

            if (max.HasValue && numValue > max.Value)
            {
                return ValidationResult.Failure(
                    validation.ErrorMessage ??
                    $"{property.DisplayName} must be at most {max.Value}"
                );
            }

            return ValidationResult.Success();
        }
        catch
        {
            return ValidationResult.Failure($"{property.DisplayName} must be a number");
        }
    }

    /// <summary>
    /// 验证正则表达式模式
    /// </summary>
    private ValidationResult ValidatePattern(ConfigProperty property, object value, ValidationRule validation)
    {
        if (string.IsNullOrEmpty(validation.Pattern))
            return ValidationResult.Success();

        var stringValue = value.ToString() ?? string.Empty;

        try
        {
            if (!Regex.IsMatch(stringValue, validation.Pattern))
            {
                return ValidationResult.Failure(
                    validation.ErrorMessage ??
                    $"{property.DisplayName} does not match the required pattern"
                );
            }

            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            return ValidationResult.Failure($"Pattern validation error: {ex.Message}");
        }
    }

    /// <summary>
    /// 验证枚举值
    /// </summary>
    private ValidationResult ValidateEnum(ConfigProperty property, object value, ValidationRule validation)
    {
        if (validation.AllowedValues == null || validation.AllowedValues.Count == 0)
            return ValidationResult.Success();

        var stringValue = value.ToString();

        foreach (var allowedValue in validation.AllowedValues)
        {
            if (allowedValue.ToString() == stringValue)
                return ValidationResult.Success();
        }

        return ValidationResult.Failure(
            validation.ErrorMessage ??
            $"{property.DisplayName} must be one of: {string.Join(", ", validation.AllowedValues)}"
        );
    }

    /// <summary>
    /// 验证整个配置文件
    /// </summary>
    public Dictionary<string, ValidationResult> ValidateProfile(
        ConfigurationProfile profile,
        ConfigurationSchema schema)
    {
        var results = new Dictionary<string, ValidationResult>();

        foreach (var property in schema.Properties)
        {
            var value = profile.GetValue<object>(property.Key);
            results[property.Key] = Validate(property, value);
        }

        return results;
    }

    /// <summary>
    /// 验证Mod配置
    /// </summary>
    public Dictionary<string, ValidationResult> ValidateModConfiguration(ModConfiguration modConfig)
    {
        var results = new Dictionary<string, ValidationResult>();

        if (modConfig.Schema == null)
            return results;

        foreach (var property in modConfig.Schema.Properties)
        {
            var value = modConfig.GetValue<object>(property.Key);
            results[property.Key] = Validate(property, value);
        }

        return results;
    }

    /// <summary>
    /// 验证依赖关系
    /// </summary>
    public ValidationResult ValidateDependency(
        ConfigProperty property,
        object? value,
        Dictionary<string, object> allValues)
    {
        if (property.Validation?.Type != ValidationType.Dependency)
            return ValidationResult.Success();

        var validation = property.Validation;
        if (string.IsNullOrEmpty(validation.DependsOn))
            return ValidationResult.Success();

        // 检查依赖的配置项是否存在
        if (!allValues.TryGetValue(validation.DependsOn, out var dependencyValue))
        {
            return ValidationResult.Failure(
                $"{property.DisplayName} depends on {validation.DependsOn} which is not set"
            );
        }

        // 如果指定了依赖值，检查是否匹配
        if (validation.DependsOnValue != null)
        {
            if (!dependencyValue.Equals(validation.DependsOnValue))
            {
                return ValidationResult.Failure(
                    validation.ErrorMessage ??
                    $"{property.DisplayName} requires {validation.DependsOn} to be {validation.DependsOnValue}"
                );
            }
        }

        return ValidationResult.Success();
    }
}
