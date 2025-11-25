namespace LizardClient.Injection.Hooks;

/// <summary>
/// Hook 类型
/// </summary>
public enum HookType
{
    /// <summary>
    /// Detour Hook (内联代码重定向)
    /// </summary>
    Detour,

    /// <summary>
    /// VTable Hook (虚函数表 Hook)
    /// </summary>
    VTable,

    /// <summary>
    /// IAT Hook (导入地址表 Hook)
    /// </summary>
    IAT
}

/// <summary>
/// Hook 信息
/// </summary>
public sealed class HookInfo
{
    /// <summary>
    /// Hook 名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Hook 类型
    /// </summary>
    public HookType Type { get; init; }

    /// <summary>
    /// 目标函数地址
    /// </summary>
    public IntPtr TargetAddress { get; init; }

    /// <summary>
    /// Hook 函数地址
    /// </summary>
    public IntPtr HookAddress { get; init; }

    /// <summary>
    /// Trampoline 地址 (用于调用原始函数)
    /// </summary>
    public IntPtr TrampolineAddress { get; set; }

    /// <summary>
    /// 原始字节 (用于恢复)
    /// </summary>
    public byte[] OriginalBytes { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Hook 是否已启用
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 额外元数据
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();

    public override string ToString()
    {
        return $"Hook '{Name}' ({Type}) - Target: 0x{TargetAddress:X}, Enabled: {IsEnabled}";
    }
}
