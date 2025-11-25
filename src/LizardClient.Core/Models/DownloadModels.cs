namespace LizardClient.Core.Models;

/// <summary>
/// 可下载项类型
/// </summary>
public enum DownloadItemType
{
    /// <summary>
    /// Minecraft 版本
    /// </summary>
    MinecraftVersion,

    /// <summary>
    /// Mod 加载器
    /// </summary>
    ModLoader,

    /// <summary>
    /// Mod
    /// </summary>
    Mod
}

/// <summary>
/// Mod 加载器类型
/// </summary>
public enum ModLoaderType
{
    Forge,
    Fabric,
    Quilt,
    NeoForge
}

/// <summary>
/// 可下载项
/// </summary>
public class DownloadItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DownloadItemType Type { get; set; }
    public ModLoaderType? LoaderType { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime ReleaseDate { get; set; }
    public bool IsInstalled { get; set; }
    public string MinecraftVersion { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int Downloads { get; set; }
    public string IconUrl { get; set; } = string.Empty;
}

/// <summary>
/// 下载进度
/// </summary>
public class DownloadProgressInfo
{
    public string ItemName { get; set; } = string.Empty;
    public long BytesDownloaded { get; set; }
    public long TotalBytes { get; set; }
    public double ProgressPercentage { get; set; }
    public double DownloadSpeed { get; set; } // KB/s
    public TimeSpan EstimatedTimeRemaining { get; set; }
    public string Status { get; set; } = string.Empty;
}
