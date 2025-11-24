using Serilog;
using Serilog.Events;

namespace LizardClient.Core.Services;

/// <summary>
/// 基于 Serilog 的日志服务实现
/// </summary>
public sealed class SerilogLogger : Interfaces.ILogger
{
    private readonly Serilog.ILogger _logger;

    public SerilogLogger()
    {
        // 配置 Serilog
        _logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
            )
            .WriteTo.File(
                path: Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    ".lizardclient",
                    "logs",
                    "lizardclient-.log"
                ),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                retainedFileCountLimit: 7
            )
            .CreateLogger();

        Info("LizardClient 日志服务已启动");
    }

    public void Debug(string message)
    {
        _logger.Debug(message);
    }

    public void Info(string message)
    {
        _logger.Information(message);
    }

    public void Warning(string message)
    {
        _logger.Warning(message);
    }

    public void Error(string message, Exception? exception = null)
    {
        if (exception != null)
        {
            _logger.Error(exception, message);
        }
        else
        {
            _logger.Error(message);
        }
    }

    public void Fatal(string message, Exception? exception = null)
    {
        if (exception != null)
        {
            _logger.Fatal(exception, message);
        }
        else
        {
            _logger.Fatal(message);
        }
    }
}
