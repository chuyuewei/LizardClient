namespace LizardClient.Core.Interfaces;

/// <summary>
/// 日志服务接口
/// </summary>
public interface ILogger
{
    /// <summary>
    /// 记录调试信息
    /// </summary>
    /// <param name="message">日志消息</param>
    void Debug(string message);

    /// <summary>
    /// 记录普通信息
    /// </summary>
    /// <param name="message">日志消息</param>
    void Info(string message);

    /// <summary>
    /// 记录警告信息
    /// </summary>
    /// <param name="message">日志消息</param>
    void Warning(string message);

    /// <summary>
    /// 记录错误信息
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="exception">异常对象（可选）</param>
    void Error(string message, Exception? exception = null);

    /// <summary>
    /// 记录严重错误信息
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="exception">异常对象（可选）</param>
    void Fatal(string message, Exception? exception = null);
}
