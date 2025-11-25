using LizardClient.Core.Interfaces;
using LizardClient.Core.Models;
using LizardClient.Game.Java;
using System.Diagnostics;
using System.Text;

namespace LizardClient.Game.Minecraft;

/// <summary>
/// Minecraft 进程启动和管理
/// </summary>
public sealed class MinecraftProcess
{
    private readonly ILogger _logger;
    private readonly JavaDetector _javaDetector;
    private Process? _gameProcess;

    public MinecraftProcess(ILogger logger)
    {
        _logger = logger;
        _javaDetector = new JavaDetector(logger);
    }

    /// <summary>
    /// 启动 Minecraft
    /// </summary>
    public async Task<bool> LaunchAsync(GameProfile profile, string? javaPath = null)
    {
        try
        {
            _logger.Info($"准备启动 Minecraft {profile.MinecraftVersion}...");

            // 1. 检测或使用指定的 Java
            JavaVersionInfo? javaInfo = null;
            if (string.IsNullOrEmpty(javaPath))
            {
                var availableJavas = await _javaDetector.DetectAllJavaInstallationsAsync();
                javaInfo = _javaDetector.RecommendJavaForMinecraft(profile.MinecraftVersion, availableJavas);

                if (javaInfo == null)
                {
                    _logger.Error("未找到合适的 Java 版本！");
                    return false;
                }
            }
            else
            {
                javaInfo = await _javaDetector.GetJavaVersionAsync(javaPath);
                if (javaInfo == null)
                {
                    _logger.Error($"无效的 Java 路径: {javaPath}");
                    return false;
                }
            }

            // 2. 构建启动参数
            var launchArgs = BuildLaunchArguments(profile, javaInfo);
            _logger.Info($"启动参数: {launchArgs}");

            // 3. 启动进程
            _gameProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = javaInfo.JavaPath,
                    Arguments = launchArgs,
                    WorkingDirectory = profile.GameDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = false
                }
            };

            // 输出日志
            _gameProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    _logger.Info($"[Game] {e.Data}");
            };

            _gameProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    _logger.Warning($"[Game] {e.Data}");
            };

            _gameProcess.Start();
            _gameProcess.BeginOutputReadLine();
            _gameProcess.BeginErrorReadLine();

            _logger.Info($"Minecraft 进程已启动 (PID: {_gameProcess.Id})");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"启动 Minecraft 失败: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// 构建启动参数
    /// </summary>
    private string BuildLaunchArguments(GameProfile profile, JavaVersionInfo javaInfo)
    {
        var args = new StringBuilder();

        // JVM 参数
        args.Append(profile.JvmArguments);
        args.Append(' ');

        // 内存设置
        var memoryMB = profile.MaxMemoryMB > 0 ? profile.MaxMemoryMB : 2048;
        args.Append($"-Xmx{memoryMB}M -Xms{memoryMB / 2}M ");

        // 优化参数
        if (javaInfo.MajorVersion >= 11)
        {
            args.Append("-XX:+UnlockExperimentalVMOptions -XX:+UseG1GC ");
            args.Append("-XX:G1NewSizePercent=20 -XX:G1ReservePercent=20 ");
            args.Append("-XX:MaxGCPauseMillis=50 -XX:G1HeapRegionSize=32M ");
        }

        // 游戏参数
        args.Append($"-Djava.library.path=\"{Path.Combine(profile.GameDirectory, "natives")}\" ");
        args.Append($"-cp \"{GetClasspath(profile)}\" ");

        // 主类
        args.Append("net.minecraft.client.main.Main ");

        // Minecraft 参数
        args.Append($"--username {profile.PlayerName} ");
        args.Append($"--version {profile.MinecraftVersion} ");
        args.Append($"--gameDir \"{profile.GameDirectory}\" ");
        args.Append($"--assetsDir \"{Path.Combine(profile.GameDirectory, "assets")}\" ");
        args.Append($"--assetIndex {GetAssetIndex(profile.MinecraftVersion)} ");

        if (profile.FullScreen)
        {
            args.Append("--fullscreen ");
        }
        else
        {
            args.Append($"--width {profile.WindowWidth} --height {profile.WindowHeight} ");
        }

        // UUID (如果有)
        if (!string.IsNullOrEmpty(profile.PlayerUUID))
        {
            args.Append($"--uuid {profile.PlayerUUID} ");
        }

        // 访问令牌 (离线模式使用占位符)
        args.Append("--accessToken 0 ");
        args.Append("--userType legacy ");

        return args.ToString().Trim();
    }

    /// <summary>
    /// 获取 Classpath
    /// </summary>
    private string GetClasspath(GameProfile profile)
    {
        var libraries = new List<string>();
        var librariesDir = Path.Combine(profile.GameDirectory, "libraries");
        var versionsDir = Path.Combine(profile.GameDirectory, "versions", profile.MinecraftVersion);

        // 添加主 JAR
        var mainJar = Path.Combine(versionsDir, $"{profile.MinecraftVersion}.jar");
        if (File.Exists(mainJar))
        {
            libraries.Add(mainJar);
        }

        // 添加库文件 (简化版，实际需要解析 version.json)
        if (Directory.Exists(librariesDir))
        {
            libraries.AddRange(Directory.GetFiles(librariesDir, "*.jar", SearchOption.AllDirectories));
        }

        return string.Join(Path.PathSeparator, libraries);
    }

    /// <summary>
    /// 获取资产索引
    /// </summary>
    private string GetAssetIndex(string minecraftVersion)
    {
        // 简化版本映射
        if (Version.TryParse(minecraftVersion.TrimStart('1', '.'), out var ver))
        {
            if (ver >= new Version("16.0")) return "1.16";
            if (ver >= new Version("13.0")) return "1.13";
            if (ver >= new Version("12.0")) return "1.12";
            if (ver >= new Version("8.0")) return "1.8";
        }
        return "legacy";
    }

    /// <summary>
    /// 等待游戏退出
    /// </summary>
    public async Task WaitForExitAsync()
    {
        if (_gameProcess != null && !_gameProcess.HasExited)
        {
            await _gameProcess.WaitForExitAsync();
            _logger.Info($"Minecraft 已退出 (退出代码: {_gameProcess.ExitCode})");
        }
    }

    /// <summary>
    /// 强制终止游戏
    /// </summary>
    public void Kill()
    {
        if (_gameProcess != null && !_gameProcess.HasExited)
        {
            _gameProcess.Kill();
            _logger.Info("Minecraft 进程已被终止");
        }
    }

    /// <summary>
    /// 游戏是否正在运行
    /// </summary>
    public bool IsRunning => _gameProcess != null && !_gameProcess.HasExited;

    /// <summary>
    /// 游戏进程 ID
    /// </summary>
    public int ProcessId => _gameProcess?.Id ?? 0;

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _gameProcess?.Dispose();
    }
}
