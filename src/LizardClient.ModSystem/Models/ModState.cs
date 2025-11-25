namespace LizardClient.ModSystem.Models;

/// <summary>
/// 模组状态枚举
/// </summary>
public enum ModState
{
    /// <summary>
    /// 未加载 - 初始状态
    /// </summary>
    Unloaded,

    /// <summary>
    /// 加载中 - 正在从磁盘加载
    /// </summary>
    Loading,

    /// <summary>
    /// 已加载 - 加载完成但未初始化
    /// </summary>
    Loaded,

    /// <summary>
    /// 初始化中 - 正在执行OnLoad
    /// </summary>
    Initializing,

    /// <summary>
    /// 已初始化 - OnLoad完成但未启用
    /// </summary>
    Initialized,

    /// <summary>
    /// 启用中 - 正在执行OnEnable
    /// </summary>
    Enabling,

    /// <summary>
    /// 已启用 - 模组完全激活
    /// </summary>
    Enabled,

    /// <summary>
    /// 禁用中 - 正在执行OnDisable
    /// </summary>
    Disabling,

    /// <summary>
    /// 已禁用 - 已初始化但未启用
    /// </summary>
    Disabled,

    /// <summary>
    /// 卸载中 - 正在执行OnUnload
    /// </summary>
    Unloading,

    /// <summary>
    /// 失败 - 加载或初始化失败
    /// </summary>
    Failed,

    /// <summary>
    /// 热重载中 - 正在热重载
    /// </summary>
    Reloading
}

/// <summary>
/// 模组状态改变事件参数
/// </summary>
public sealed class ModStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// 模组ID
    /// </summary>
    public string ModId { get; set; } = string.Empty;

    /// <summary>
    /// 模组名称
    /// </summary>
    public string ModName { get; set; } = string.Empty;

    /// <summary>
    /// 之前的状态
    /// </summary>
    public ModState OldState { get; set; }

    /// <summary>
    /// 新状态
    /// </summary>
    public ModState NewState { get; set; }

    /// <summary>
    /// 状态改变时间
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 错误信息（如果状态为Failed）
    /// </summary>
    public string? ErrorMessage { get; set; }
}
