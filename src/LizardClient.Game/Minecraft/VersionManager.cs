using LizardClient.Core.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LizardClient.Game.Minecraft;

/// <summary>
/// Minecraft 版本信息
/// </summary>
public sealed class VersionInfo
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "release";  // release, snapshot, old_alpha, old_beta
    public DateTime ReleaseTime { get; set; }
    public string Url { get; set; } = string.Empty;
    public bool IsInstalled { get; set; }
    public string InstallPath { get; set; } = string.Empty;
}

/// <summary>
/// Minecraft 版本管理器
/// </summary>
public sealed class VersionManager
{
    private readonly ILogger _logger;
    private readonly string _minecraftDir;
    private const string VERSION_MANIFEST_URL = "https://piston-meta.mojang.com/mc/game/version_manifest_v2.json";

    public VersionManager(ILogger logger, string minecraftDirectory)
    {
        _logger = logger;
        _minecraftDir = minecraftDirectory;
    }

    /// <summary>
    /// 获取所有可用的 Minecraft 版本
    /// </summary>
    public async Task<List<VersionInfo>> GetAvailableVersionsAsync()
    {
        try
        {
            _logger.Info("获取 Minecraft 版本列表...");
            
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            var response = await httpClient.GetStringAsync(VERSION_MANIFEST_URL);
            var manifest = JObject.Parse(response);
            
            var versions = new List<VersionInfo>();
            var versionsArray = manifest["versions"] as JArray;
            
            if (versionsArray == null) return versions;

            foreach (var versionToken in versionsArray)
            {
                var version = new VersionInfo
                {
                    Id = versionToken["id"]?.ToString() ?? string.Empty,
                    Type = versionToken["type"]?.ToString() ?? "release",
                    ReleaseTime = versionToken["releaseTime"]?.ToObject<DateTime>() ?? DateTime.MinValue,
                    Url = versionToken["url"]?.ToString() ?? string.Empty
                };

                // 检查是否已安装
                var versionPath = Path.Combine(_minecraftDir, "versions", version.Id);
                version.IsInstalled = Directory.Exists(versionPath);
                version.InstallPath = versionPath;

                versions.Add(version);
            }

            _logger.Info($"获取到 {versions.Count} 个版本");
            return versions;
        }
        catch (Exception ex)
        {
            _logger.Error($"获取版本列表失败: {ex.Message}", ex);
            return new List<VersionInfo>();
        }
    }

    /// <summary>
    /// 获取已安装的版本
    /// </summary>
    public List<VersionInfo> GetInstalledVersions()
    {
        var versions = new List<VersionInfo>();
        var versionsDir = Path.Combine(_minecraftDir, "versions");

        if (!Directory.Exists(versionsDir))
        {
            Directory.CreateDirectory(versionsDir);
            return versions;
        }

        try
        {
            foreach (var versionDir in Directory.GetDirectories(versionsDir))
            {
                var versionId = Path.GetFileName(versionDir);
                var jsonFile = Path.Combine(versionDir, $"{versionId}.json");
                var jarFile = Path.Combine(versionDir, $"{versionId}.jar");

                if (File.Exists(jsonFile) && File.Exists(jarFile))
                {
                    try
                    {
                        var jsonContent = File.ReadAllText(jsonFile);
                        var versionJson = JObject.Parse(jsonContent);

                        versions.Add(new VersionInfo
                        {
                            Id = versionId,
                            Type = versionJson["type"]?.ToString() ?? "release",
                            ReleaseTime = versionJson["releaseTime"]?.ToObject<DateTime>() ?? DateTime.MinValue,
                            IsInstalled = true,
                            InstallPath = versionDir
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning($"读取版本 {versionId} 失败: {ex.Message}");
                    }
                }
            }

            _logger.Info($"找到 {versions.Count} 个已安装版本");
        }
        catch (Exception ex)
        {
            _logger.Error($"扫描已安装版本失败: {ex.Message}", ex);
        }

        return versions.OrderByDescending(v => v.ReleaseTime).ToList();
    }

    /// <summary>
    /// 下载并安装指定版本
    /// </summary>
    public async Task<bool> InstallVersionAsync(string versionId, IProgress<int>? progress = null)
    {
        try
        {
            _logger.Info($"开始安装 Minecraft {versionId}...");

            // 1. 获取版本信息
            var availableVersions = await GetAvailableVersionsAsync();
            var version = availableVersions.FirstOrDefault(v => v.Id == versionId);
            
            if (version == null)
            {
                _logger.Error($"未找到版本 {versionId}");
                return false;
            }

            // 2. 创建版本目录
            var versionDir = Path.Combine(_minecraftDir, "versions", versionId);
            Directory.CreateDirectory(versionDir);

            // 3. 下载版本 JSON
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(10);

            progress?.Report(10);
            var versionJson = await httpClient.GetStringAsync(version.Url);
            var versionJsonPath = Path.Combine(versionDir, $"{versionId}.json");
            await File.WriteAllTextAsync(versionJsonPath, versionJson);
            _logger.Info("版本 JSON 下载完成");

            // 4. 解析 JSON 获取下载 URL
            var jsonObject = JObject.Parse(versionJson);
            var downloads = jsonObject["downloads"];
            var clientUrl = downloads?["client"]?["url"]?.ToString();

            if (string.IsNullOrEmpty(clientUrl))
            {
                _logger.Error("未找到客户端下载链接");
                return false;
            }

            // 5. 下载客户端 JAR
            progress?.Report(30);
            _logger.Info("下载客户端 JAR...");
            var jarPath = Path.Combine(versionDir, $"{versionId}.jar");
            
            using (var response = await httpClient.GetAsync(clientUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                
                using var fileStream = new FileStream(jarPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await response.Content.CopyToAsync(fileStream);
            }

            progress?.Report(100);
            _logger.Info($"Minecraft {versionId} 安装完成");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"安装版本 {versionId} 失败: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// 删除指定版本
    /// </summary>
    public bool UninstallVersion(string versionId)
    {
        try
        {
            var versionDir = Path.Combine(_minecraftDir, "versions", versionId);
            
            if (!Directory.Exists(versionDir))
            {
                _logger.Warning($"版本 {versionId} 不存在");
                return false;
            }

            Directory.Delete(versionDir, recursive: true);
            _logger.Info($"已删除版本 {versionId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"删除版本 {versionId} 失败: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// 验证版本完整性
    /// </summary>
    public bool ValidateVersion(string versionId)
    {
        var versionDir = Path.Combine(_minecraftDir, "versions", versionId);
        var jsonFile = Path.Combine(versionDir, $"{versionId}.json");
        var jarFile = Path.Combine(versionDir, $"{versionId}.jar");

        var isValid = File.Exists(jsonFile) && File.Exists(jarFile);
        
        if (isValid)
        {
            _logger.Info($"版本 {versionId} 验证通过");
        }
        else
        {
            _logger.Warning($"版本 {versionId} 验证失败");
        }

        return isValid;
    }
}
