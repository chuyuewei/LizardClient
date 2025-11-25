using LizardClient.Core.Interfaces;

namespace LizardClient.Core.Models;

/// <summary>
/// 更新包信息
/// </summary>
public sealed class UpdatePackage
{
    /// <summary>
    /// 目标版本
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 更新包文件路径
    /// </summary>
    public string PackagePath { get; set; } = string.Empty;

    /// <summary>
    /// 需要更新的文件列表
    /// </summary>
    public List<UpdateFile> Files { get; set; } = new();

    /// <summary>
    /// 需要删除的文件列表（废弃的文件）
    /// </summary>
    public List<string> FilesToDelete { get; set; } = new();

    /// <summary>
    /// 更新后执行的脚本（可选）
    /// </summary>
    public List<string> PostUpdateScripts { get; set; } = new();

    /// <summary>
    /// 更新类型
    /// </summary>
    public UpdateType Type { get; set; } = UpdateType.Client;

    /// <summary>
    /// 是否需要重启应用程序
    /// </summary>
    public bool RequiresRestart { get; set; } = true;

    /// <summary>
    /// 最小兼容版本
    /// </summary>
    public string? MinimumCompatibleVersion { get; set; }

    /// <summary>
    /// 包的哈希值
    /// </summary>
    public string PackageHash { get; set; } = string.Empty;
}

/// <summary>
/// 更新文件信息
/// </summary>
public sealed class UpdateFile
{
    /// <summary>
    /// 文件相对路径
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// 文件哈希值
    /// </summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// 是否为可执行文件
    /// </summary>
    public bool IsExecutable { get; set; }

    /// <summary>
    /// 更新操作类型
    /// </summary>
    public UpdateFileAction Action { get; set; } = UpdateFileAction.Add;
}

/// <summary>
/// 更新文件操作类型
/// </summary>
public enum UpdateFileAction
{
    /// <summary>
    /// 添加新文件
    /// </summary>
    Add,

    /// <summary>
    /// 替换现有文件
    /// </summary>
    Replace,

    /// <summary>
    /// 删除文件
    /// </summary>
    Delete,

    /// <summary>
    /// 修补文件（差分更新）
    /// </summary>
    Patch
}

/// <summary>
/// 更新状态
/// </summary>
public enum UpdateState
{
    /// <summary>
    /// 空闲
    /// </summary>
    Idle,

    /// <summary>
    /// 检查更新中
    /// </summary>
    CheckingForUpdates,

    /// <summary>
    /// 发现可用更新
    /// </summary>
    UpdateAvailable,

    /// <summary>
    /// 正在下载
    /// </summary>
    Downloading,

    /// <summary>
    /// 下载完成
    /// </summary>
    Downloaded,

    /// <summary>
    /// 安装中
    /// </summary>
    Installing,

    /// <summary>
    /// 需要重启
    /// </summary>
    RestartRequired,

    /// <summary>
    /// 失败
    /// </summary>
    Failed,

    /// <summary>
    /// 回滚中
    /// </summary>
    RollingBack,

    /// <summary>
    /// 已取消
    /// </summary>
    Cancelled
}
