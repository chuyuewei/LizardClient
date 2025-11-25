namespace LizardClient.Core.Interfaces;

/// <summary>
/// 更新服务接口，处理客户端和模组的更新
/// </summary>
public interface IUpdateService
{
    /// <summary>
    /// 检查客户端更新
    /// </summary>
    /// <returns>更新信息，如果没有更新则返回 null</returns>
    Task<UpdateInfo?> CheckForUpdatesAsync();

    /// <summary>
    /// 下载更新
    /// </summary>
    /// <param name="updateInfo">更新信息</param>
    /// <param name="progress">下载进度回调（百分比 0-100）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DownloadUpdateAsync(
        UpdateInfo updateInfo,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 应用更新（需要重启）
    /// </summary>
    /// <param name="updateInfo">更新信息</param>
    Task ApplyUpdateAsync(UpdateInfo updateInfo);

    /// <summary>
    /// 获取更新日志
    /// </summary>
    /// <param name="version">版本号</param>
    /// <returns>更新日志内容</returns>
    Task<string> GetChangelogAsync(string version);

    /// <summary>
    /// 回滚到上一个版本
    /// </summary>
    Task RollbackAsync();
}

/// <summary>
/// 更新信息
/// </summary>
public sealed class UpdateInfo
{
    /// <summary>
    /// 新版本号
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 发布日期
    /// </summary>
    public DateTime ReleaseDate { get; set; }

    /// <summary>
    /// 下载 URL
    /// </summary>
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 文件哈希值（SHA256）
    /// </summary>
    public string FileHash { get; set; } = string.Empty;

    /// <summary>
    /// 是否为强制更新
    /// </summary>
    public bool IsMandatory { get; set; }

    /// <summary>
    /// 更新日志
    /// </summary>
    public string Changelog { get; set; } = string.Empty;

    /// <summary>
    /// 最小兼容版本
    /// </summary>
    public string? MinimumCompatibleVersion { get; set; }

    /// <summary>
    /// 更新类型
    /// </summary>
    public UpdateType Type { get; set; } = UpdateType.Client;

    /// <summary>
    /// 是否为关键更新（安全补丁等）
    /// </summary>
    public bool IsCritical { get; set; }

    /// <summary>
    /// 依赖的更新列表
    /// </summary>
    public List<string> Dependencies { get; set; } = new();

    /// <summary>
    /// 发布说明 URL
    /// </summary>
    public string? ReleaseNotesUrl { get; set; }
}

/// <summary>
/// 更新类型枚举
/// </summary>
public enum UpdateType
{
    /// <summary>
    /// 客户端更新
    /// </summary>
    Client,

    /// <summary>
    /// 模组更新
    /// </summary>
    Mod,

    /// <summary>
    /// 资源更新
    /// </summary>
    Asset,

    /// <summary>
    /// 配置更新
    /// </summary>
    Configuration
}

