namespace LizardClient.Game.ModLoaders;

/// <summary>
/// Mod 加载器类型枚举
/// </summary>
public enum ModLoaderType
{
    /// <summary>
    /// 无 mod 加载器（原版）
    /// </summary>
    Vanilla,

    /// <summary>
    /// Forge mod 加载器
    /// </summary>
    Forge,

    /// <summary>
    /// Fabric mod 加载器
    /// </summary>
    Fabric,

    /// <summary>
    /// Quilt mod 加载器
    /// </summary>
    Quilt,

    /// <summary>
    /// NeoForge mod 加载器
    /// </summary>
    NeoForge,

    /// <summary>
    /// OptiFine（技术上不是加载器，但需要特殊处理）
    /// </summary>
    OptiFine,

    /// <summary>
    /// 未知的 mod 加载器
    /// </summary>
    Unknown
}

/// <summary>
/// Mod 加载器信息
/// </summary>
public sealed class ModLoaderInfo
{
    /// <summary>
    /// Mod 加载器类型
    /// </summary>
    public ModLoaderType Type { get; set; } = ModLoaderType.Vanilla;

    /// <summary>
    /// Mod 加载器版本
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Minecraft 版本
    /// </summary>
    public string MinecraftVersion { get; set; } = string.Empty;

    /// <summary>
    /// 安装路径
    /// </summary>
    public string InstallPath { get; set; } = string.Empty;

    /// <summary>
    /// 是否已安装
    /// </summary>
    public bool IsInstalled { get; set; }

    /// <summary>
    /// 是否与 LizardClient 兼容
    /// </summary>
    public bool IsCompatible { get; set; } = true;
}

/// <summary>
/// Mod 加载器适配器接口
/// </summary>
public interface IModLoaderAdapter
{
    /// <summary>
    /// Mod 加载器类型
    /// </summary>
    ModLoaderType LoaderType { get; }

    /// <summary>
    /// 检测 mod 加载器是否已安装
    /// </summary>
    /// <param name="minecraftPath">Minecraft 安装路径</param>
    /// <param name="version">Minecraft 版本</param>
    /// <returns>Mod 加载器信息，如果未安装则返回 null</returns>
    Task<ModLoaderInfo?> DetectAsync(string minecraftPath, string version);

    /// <summary>
    /// 初始化适配器
    /// </summary>
    /// <param name="loaderInfo">Mod 加载器信息</param>
    Task InitializeAsync(ModLoaderInfo loaderInfo);

    /// <summary>
    /// 获取已安装的 mods 列表
    /// </summary>
    /// <returns>Mod 文件路径列表</returns>
    Task<List<string>> GetInstalledModsAsync();

    /// <summary>
    /// 验证 LizardClient 内置模组是否与此加载器兼容
    /// </summary>
    /// <returns>true 如果兼容</returns>
    bool IsLizardClientCompatible();

    /// <summary>
    /// 获取启动参数
    /// </summary>
    /// <returns>需要添加到 JVM 参数的额外参数</returns>
    List<string> GetLaunchArguments();
}
