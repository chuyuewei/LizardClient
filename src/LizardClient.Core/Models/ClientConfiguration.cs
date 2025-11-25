namespace LizardClient.Core.Models;

/// <summary>
/// 客户端全局配置
/// </summary>
public sealed class ClientConfiguration
{
    /// <summary>
    /// 配置版本号
    /// </summary>
    public string ConfigVersion { get; set; } = "1.0.0";

    /// <summary>
    /// 首选语言（zh-CN, en-US等）
    /// </summary>
    public string PreferredLanguage { get; set; } = "zh-CN";


    /// <summary>
    /// UI 主题（Light, Dark）
    /// </summary>
    public string Theme { get; set; } = "Dark";

    /// <summary>
    /// 是否启用硬件加速
    /// </summary>
    public bool EnableHardwareAcceleration { get; set; } = true;

    /// <summary>
    /// 是否启用自动更新
    /// </summary>
    public bool EnableAutoUpdate { get; set; } = true;

    /// <summary>
    /// 是否启用遥测数据
    /// </summary>
    public bool EnableTelemetry { get; set; } = false;

    /// <summary>
    /// 下载线程数
    /// </summary>
    public int DownloadThreads { get; set; } = 4;

    /// <summary>
    /// 更新频道 (Stable, Beta, Dev)
    /// </summary>
    public string UpdateChannel { get; set; } = "Stable";

    /// <summary>
    /// 自动下载更新
    /// </summary>
    public bool AutoDownloadUpdates { get; set; } = false;

    /// <summary>
    /// 更新检查间隔（小时）
    /// </summary>
    public int UpdateCheckInterval { get; set; } = 24;

    /// <summary>
    /// 更新服务器 URL
    /// </summary>
    public string UpdateServerUrl { get; set; } = "https://updates.lizardclient.com";

    /// <summary>
    /// 最大下载速度限制 (KB/s, 0 = 无限制)
    /// </summary>
    public int MaxDownloadSpeed { get; set; } = 0;

    /// <summary>
    /// 默认游戏配置 ID
    /// </summary>
    public Guid? DefaultProfileId { get; set; }

    /// <summary>
    /// 游戏根目录
    /// </summary>
    public string GameRootDirectory { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        ".lizardclient"
    );

    /// <summary>
    /// Java 路径（自动检测或自定义）
    /// </summary>
    public string? JavaPath { get; set; }

    /// <summary>
    /// 性能设置
    /// </summary>
    public PerformanceSettings Performance { get; set; } = new();

    /// <summary>
    /// 用户偏好设置
    /// </summary>
    public UserPreferences Preferences { get; set; } = new();
}

/// <summary>
/// 性能设置
/// </summary>
public sealed class PerformanceSettings
{
    /// <summary>
    /// FPS 提升等级（0-3）
    /// </summary>
    public int FpsBoostLevel { get; set; } = 2;

    /// <summary>
    /// 内存优化等级（0-3）
    /// </summary>
    public int MemoryOptimizationLevel { get; set; } = 2;

    /// <summary>
    /// 是否启用快速加载
    /// </summary>
    public bool EnableFastLoading { get; set; } = true;

    /// <summary>
    /// 最大内存分配（MB）
    /// </summary>
    public int MaxMemoryMB { get; set; } = 2048;
}

/// <summary>
/// 用户偏好设置
/// </summary>
public sealed class UserPreferences
{
    /// <summary>
    /// 是否最小化到托盘
    /// </summary>
    public bool MinimizeToTray { get; set; } = true;

    /// <summary>
    /// 是否随系统启动
    /// </summary>
    public bool StartWithSystem { get; set; } = false;

    /// <summary>
    /// 是否在游戏启动后关闭启动器
    /// </summary>
    public bool CloseAfterGameLaunch { get; set; } = false;

    /// <summary>
    /// 是否显示新闻
    /// </summary>
    public bool ShowNews { get; set; } = true;

    /// <summary>
    /// 热键配置
    /// </summary>
    public Dictionary<string, string> Hotkeys { get; set; } = new()
    {
        { "OpenModMenu", "F6" },
        { "ToggleZoom", "C" },
        { "Freelook", "LeftAlt" }
    };
}
