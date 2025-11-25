namespace LizardClient.Core.Models;

/// <summary>
/// 表示游戏配置文件，包含玩家信息和游戏设置
/// </summary>
public sealed class GameProfile
{
    /// <summary>
    /// 配置文件唯一标识符
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 配置文件名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 玩家名称
    /// </summary>
    public string PlayerName { get; set; } = "Player";

    /// <summary>
    /// 玩家 UUID
    /// </summary>
    public string PlayerUUID { get; set; } = string.Empty;

    /// <summary>
    /// 最大内存分配 (MB)
    /// </summary>
    public int MaxMemoryMB { get; set; } = 2048;

    /// <summary>
    /// Minecraft 版本
    /// </summary>
    public string MinecraftVersion { get; set; } = "1.8.9";

    /// <summary>
    /// 启用的模组 ID 列表
    /// </summary>
    public List<string> EnabledMods { get; set; } = new();

    /// <summary>
    /// 游戏目录路径
    /// </summary>
    public string GameDirectory { get; set; } = string.Empty;

    /// <summary>
    /// JVM 启动参数
    /// </summary>
    public string JvmArguments { get; set; } = "-Xmx2G -XX:+UnlockExperimentalVMOptions -XX:+UseG1GC";

    /// <summary>
    /// 游戏分辨率宽度
    /// </summary>
    public int WindowWidth { get; set; } = 854;

    /// <summary>
    /// 游戏分辨率高度
    /// </summary>
    public int WindowHeight { get; set; } = 480;

    /// <summary>
    /// 是否全屏启动
    /// </summary>
    public bool IsFullscreen { get; set; }

    /// <summary>
    /// 是否全屏启动（别名属性用于兼容）
    /// </summary>
    public bool FullScreen
    {
        get => IsFullscreen;
        set => IsFullscreen = value;
    }

    /// <summary>
    /// 最后启动时间
    /// </summary>
    public DateTime? LastLaunchTime { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 是否为默认配置
    /// </summary>
    public bool IsDefault { get; set; }
}
