namespace LizardClient.Core.Models;

/// <summary>
/// 启动状态枚举
/// </summary>
public enum LaunchStatus
{
    /// <summary>
    /// 准备中
    /// </summary>
    Preparing,

    /// <summary>
    /// 检查文件完整性
    /// </summary>
    ValidatingFiles,

    /// <summary>
    /// 检测 Java 环境
    /// </summary>
    DetectingJava,

    /// <summary>
    /// 构建启动参数
    /// </summary>
    BuildingArguments,

    /// <summary>
    /// 启动进程
    /// </summary>
    LaunchingProcess,

    /// <summary>
    /// 等待游戏初始化
    /// </summary>
    WaitingForGameInit,

    /// <summary>
    /// 注入中
    /// </summary>
    Injecting,

    /// <summary>
    /// 完成
    /// </summary>
    Completed,

    /// <summary>
    /// 失败
    /// </summary>
    Failed
}

/// <summary>
/// 启动进度信息
/// </summary>
public sealed class LaunchProgress
{
    /// <summary>
    /// 当前状态
    /// </summary>
    public LaunchStatus Status { get; set; }

    /// <summary>
    /// 进度百分比 (0-100)
    /// </summary>
    public int Percentage { get; set; }

    /// <summary>
    /// 当前步骤描述
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 详细日志信息
    /// </summary>
    public string DetailLog { get; set; } = string.Empty;

    /// <summary>
    /// 错误信息（如果状态为 Failed）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 创建启动进度实例
    /// </summary>
    public static LaunchProgress Create(LaunchStatus status, int percentage, string message, string detailLog = "")
    {
        return new LaunchProgress
        {
            Status = status,
            Percentage = percentage,
            Message = message,
            DetailLog = detailLog
        };
    }

    /// <summary>
    /// 创建失败状态的启动进度
    /// </summary>
    public static LaunchProgress CreateFailed(string errorMessage)
    {
        return new LaunchProgress
        {
            Status = LaunchStatus.Failed,
            Percentage = 0,
            Message = "启动失败",
            ErrorMessage = errorMessage
        };
    }
}
