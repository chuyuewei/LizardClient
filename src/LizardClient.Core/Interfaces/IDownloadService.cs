using LizardClient.Core.Models;

namespace LizardClient.Core.Interfaces;

/// <summary>
/// 下载服务接口
/// </summary>
public interface IDownloadService
{
    /// <summary>
    /// 获取可用的 Minecraft 版本列表
    /// </summary>
    Task<List<DownloadItem>> GetMinecraftVersionsAsync();

    /// <summary>
    /// 获取指定 Minecraft 版本的 Mod 加载器列表
    /// </summary>
    Task<List<DownloadItem>> GetModLoadersAsync(string minecraftVersion);

    /// <summary>
    /// 搜索 Mod
    /// </summary>
    Task<List<DownloadItem>> SearchModsAsync(string query, string minecraftVersion, ModLoaderType? loaderType = null);

    /// <summary>
    /// 下载项目
    /// </summary>
    Task<bool> DownloadItemAsync(DownloadItem item, IProgress<DownloadProgressInfo> progress, CancellationToken cancellationToken = default);

    /// <summary>
    /// 安装已下载的项目
    /// </summary>
    Task<bool> InstallItemAsync(DownloadItem item);

    /// <summary>
    /// 获取已安装的项目列表
    /// </summary>
    Task<List<DownloadItem>> GetInstalledItemsAsync();

    /// <summary>
    /// 卸载项目
    /// </summary>
    Task<bool> UninstallItemAsync(DownloadItem item);
}
