using LizardClient.Core.Interfaces;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace LizardClient.Game.Java;

/// <summary>
/// Java 版本信息
/// </summary>
public sealed class JavaVersionInfo
{
    /// <summary>
    /// Java 安装路径
    /// </summary>
    public string JavaPath { get; set; } = string.Empty;

    /// <summary>
    /// Java 版本号 (如 "17.0.5")
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 主版本号 (如 17)
    /// </summary>
    public int MajorVersion { get; set; }

    /// <summary>
    /// 架构 (x64, x86)
    /// </summary>
    public string Architecture { get; set; } = string.Empty;

    /// <summary>
    /// 是否为 64 位
    /// </summary>
    public bool Is64Bit => Architecture.Contains("64");

    /// <summary>
    /// 供应商 (Oracle, Adoptium, Microsoft, etc.)
    /// </summary>
    public string Vendor { get; set; } = string.Empty;

    public override string ToString() => $"Java {Version} ({Architecture}) - {JavaPath}";
}

/// <summary>
/// Java 版本检测器
/// </summary>
public sealed class JavaDetector
{
    private readonly ILogger _logger;

    public JavaDetector(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 检测系统中所有可用的 Java 安装
    /// </summary>
    public async Task<List<JavaVersionInfo>> DetectAllJavaInstallationsAsync()
    {
        _logger.Info("开始检测 Java 安装...");
        var javaInstalls = new List<JavaVersionInfo>();

        // 1. 检查环境变量 JAVA_HOME
        var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
        if (!string.IsNullOrEmpty(javaHome))
        {
            var javaExe = Path.Combine(javaHome, "bin", "java.exe");
            if (File.Exists(javaExe))
            {
                var info = await GetJavaVersionAsync(javaExe);
                if (info != null)
                {
                    javaInstalls.Add(info);
                    _logger.Info($"找到 JAVA_HOME: {info}");
                }
            }
        }

        // 2. 检查 PATH 环境变量
        var pathJava = await FindJavaInPathAsync();
        if (pathJava != null && !javaInstalls.Any(j => j.JavaPath == pathJava.JavaPath))
        {
            javaInstalls.Add(pathJava);
            _logger.Info($"找到 PATH: {pathJava}");
        }

        // 3. 检查常见安装位置
        var commonPaths = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            @"C:\Program Files\Java",
            @"C:\Program Files\Eclipse Adoptium",
            @"C:\Program Files\Microsoft",
            @"C:\Program Files\Zulu"
        };

        foreach (var basePath in commonPaths)
        {
            if (!Directory.Exists(basePath)) continue;

            try
            {
                var javaDirs = Directory.GetDirectories(basePath, "*jdk*", SearchOption.TopDirectoryOnly)
                    .Concat(Directory.GetDirectories(basePath, "*jre*", SearchOption.TopDirectoryOnly));

                foreach (var javaDir in javaDirs)
                {
                    var javaExe = Path.Combine(javaDir, "bin", "java.exe");
                    if (File.Exists(javaExe))
                    {
                        var info = await GetJavaVersionAsync(javaExe);
                        if (info != null && !javaInstalls.Any(j => j.JavaPath == info.JavaPath))
                        {
                            javaInstalls.Add(info);
                            _logger.Info($"找到安装: {info}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warning($"扫描目录 {basePath} 失败: {ex.Message}");
            }
        }

        _logger.Info($"共检测到 {javaInstalls.Count} 个 Java 安装");
        return javaInstalls;
    }

    /// <summary>
    /// 查找 PATH 中的 Java
    /// </summary>
    private async Task<JavaVersionInfo?> FindJavaInPathAsync()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "where",
                    Arguments = "java",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                var javaPath = output.Split('\n')[0].Trim();
                return await GetJavaVersionAsync(javaPath);
            }
        }
        catch (Exception ex)
        {
            _logger.Warning($"查找 PATH 中的 Java 失败: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// 获取指定 Java 可执行文件的版本信息
    /// </summary>
    public async Task<JavaVersionInfo?> GetJavaVersionAsync(string javaPath)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = javaPath,
                    Arguments = "-version",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardError.ReadToEndAsync();
            output += await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0) return null;

            return ParseJavaVersion(javaPath, output);
        }
        catch (Exception ex)
        {
            _logger.Warning($"获取 Java 版本失败 ({javaPath}): {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 解析 Java 版本输出
    /// </summary>
    private JavaVersionInfo? ParseJavaVersion(string javaPath, string output)
    {
        try
        {
            // 示例输出:
            // java version "17.0.5" 2022-10-18 LTS
            // Java(TM) SE Runtime Environment (build 17.0.5+9-LTS-191)
            // Java HotSpot(TM) 64-Bit Server VM (build 17.0.5+9-LTS-191, mixed mode, sharing)

            var versionMatch = Regex.Match(output, @"version ""([^""]+)""");
            if (!versionMatch.Success) return null;

            var versionString = versionMatch.Groups[1].Value;
            
            // 解析主版本号
            int majorVersion = 0;
            if (versionString.StartsWith("1."))
            {
                // 旧版本格式 (1.8.0_xxx)
                var parts = versionString.Split('.');
                if (parts.Length >= 2 && int.TryParse(parts[1], out var minor))
                {
                    majorVersion = minor;
                }
            }
            else
            {
                // 新版本格式 (17.0.5)
                var parts = versionString.Split('.');
                if (parts.Length >= 1 && int.TryParse(parts[0], out var major))
                {
                    majorVersion = major;
                }
            }

            // 检测架构
            var arch = output.Contains("64-Bit") ? "x64" : "x86";

            // 检测供应商
            string vendor = "Unknown";
            if (output.Contains("Oracle")) vendor = "Oracle";
            else if (output.Contains("Adoptium") || output.Contains("Eclipse Temurin")) vendor = "Adoptium";
            else if (output.Contains("Microsoft")) vendor = "Microsoft";
            else if (output.Contains("Azul")) vendor = "Azul Zulu";
            else if (output.Contains("OpenJDK")) vendor = "OpenJDK";

            return new JavaVersionInfo
            {
                JavaPath = javaPath,
                Version = versionString,
                MajorVersion = majorVersion,
                Architecture = arch,
                Vendor = vendor
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"解析 Java 版本失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 为指定的 Minecraft 版本推荐最佳 Java 版本
    /// </summary>
    public JavaVersionInfo? RecommendJavaForMinecraft(string minecraftVersion, List<JavaVersionInfo> availableJavas)
    {
        // Minecraft 版本对 Java 的要求:
        // 1.17+: 需要 Java 16+
        // 1.18+: 需要 Java 17+
        // 1.20.5+: 需要 Java 21+
        // 其他: Java 8+

        int requiredJavaVersion = 8;

        if (Version.TryParse(minecraftVersion.TrimStart('1', '.'), out var mcVer))
        {
            if (mcVer >= new Version("20.5")) requiredJavaVersion = 21;
            else if (mcVer >= new Version("18.0")) requiredJavaVersion = 17;
            else if (mcVer >= new Version("17.0")) requiredJavaVersion = 16;
        }

        // 选择满足要求的最合适的 Java 版本（优先选择推荐版本，然后选择更高版本）
        var suitable = availableJavas
            .Where(j => j.MajorVersion >= requiredJavaVersion && j.Is64Bit)
            .OrderBy(j => Math.Abs(j.MajorVersion - requiredJavaVersion))
            .ThenByDescending(j => j.MajorVersion)
            .FirstOrDefault();

        if (suitable != null)
        {
            _logger.Info($"为 Minecraft {minecraftVersion} 推荐 Java {suitable.MajorVersion} ({suitable.JavaPath})");
        }
        else
        {
            _logger.Warning($"未找到适合 Minecraft {minecraftVersion} 的 Java 版本 (需要 Java {requiredJavaVersion}+)");
        }

        return suitable;
    }
}
