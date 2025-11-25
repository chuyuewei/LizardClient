namespace LizardClient.Core.Models;

/// <summary>
/// 注入方法类型
/// </summary>
public enum InjectionMethod
{
    /// <summary>
    /// 标准 DLL 注入 (CreateRemoteThread + LoadLibrary)
    /// </summary>
    StandardDll,

    /// <summary>
    /// Manual Mapping (更隐蔽)
    /// </summary>
    ManualMap,

    /// <summary>
    /// Shellcode 注入
    /// </summary>
    Shellcode
}

/// <summary>
/// 注入配置
/// </summary>
public sealed class InjectionConfig
{
    /// <summary>
    /// 是否启用自动注入
    /// </summary>
    public bool AutoInject { get; set; } = true;

    /// <summary>
    /// 注入方法
    /// </summary>
    public InjectionMethod Method { get; set; } = InjectionMethod.StandardDll;

    /// <summary>
    /// 要注入的 DLL 文件路径列表
    /// </summary>
    public List<string> DllPaths { get; set; } = new();

    /// <summary>
    /// 启动后延迟注入时间（毫秒）
    /// </summary>
    public int DelayMs { get; set; } = 3000;

    /// <summary>
    /// 注入失败时是否重试
    /// </summary>
    public bool RetryOnFailure { get; set; } = true;

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// 重试间隔（毫秒）
    /// </summary>
    public int RetryIntervalMs { get; set; } = 1000;

    /// <summary>
    /// 是否在注入前检查反调试
    /// </summary>
    public bool CheckAntiDebug { get; set; } = true;

    /// <summary>
    /// 创建默认配置
    /// </summary>
    public static InjectionConfig CreateDefault()
    {
        return new InjectionConfig
        {
            AutoInject = true,
            Method = InjectionMethod.StandardDll,
            DllPaths = new List<string>(),
            DelayMs = 3000,
            RetryOnFailure = true,
            MaxRetryCount = 3,
            RetryIntervalMs = 1000,
            CheckAntiDebug = true
        };
    }

    /// <summary>
    /// 验证配置是否有效
    /// </summary>
    public bool IsValid(out string errorMessage)
    {
        if (DllPaths == null || DllPaths.Count == 0)
        {
            errorMessage = "未配置要注入的 DLL 文件";
            return false;
        }

        foreach (var dllPath in DllPaths)
        {
            if (!File.Exists(dllPath))
            {
                errorMessage = $"DLL 文件不存在: {dllPath}";
                return false;
            }
        }

        if (DelayMs < 0)
        {
            errorMessage = "延迟时间不能为负数";
            return false;
        }

        if (MaxRetryCount < 0)
        {
            errorMessage = "最大重试次数不能为负数";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}
