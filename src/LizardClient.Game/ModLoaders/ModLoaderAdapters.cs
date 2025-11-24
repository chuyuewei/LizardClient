namespace LizardClient.Game.ModLoaders;

/// <summary>
/// Forge mod 加载器适配器
/// </summary>
public sealed class ForgeAdapter : IModLoaderAdapter
{
    public ModLoaderType LoaderType => ModLoaderType.Forge;

    public async Task<ModLoaderInfo?> DetectAsync(string minecraftPath, string version)
    {
        // 检测 Forge 安装
        // 1. 检查 versions 目录中是否有 forge 版本
        var versionsPath = Path.Combine(minecraftPath, "versions");
        if (!Directory.Exists(versionsPath))
        {
            return null;
        }

        var versionDirs = Directory.GetDirectories(versionsPath);
        foreach (var versionDir in versionDirs)
        {
            var dirName = Path.GetFileName(versionDir);
            if (dirName.Contains("forge", StringComparison.OrdinalIgnoreCase) &&
                dirName.Contains(version, StringComparison.OrdinalIgnoreCase))
            {
                // 提取 Forge 版本号
                var forgeVersion = ExtractForgeVersion(dirName);

                return new ModLoaderInfo
                {
                    Type = ModLoaderType.Forge,
                    Version = forgeVersion,
                    MinecraftVersion = version,
                    InstallPath = versionDir,
                    IsInstalled = true,
                    IsCompatible = true
                };
            }
        }

        return null;
    }

    public Task InitializeAsync(ModLoaderInfo loaderInfo)
    {
        // 初始化 Forge 适配器
        return Task.CompletedTask;
    }

    public async Task<List<string>> GetInstalledModsAsync()
    {
        // 返回 mods 文件夹中的所有 .jar 文件
        var modsList = new List<string>();
        // 实现从 mods 文件夹读取
        return await Task.FromResult(modsList);
    }

    public bool IsLizardClientCompatible()
    {
        // LizardClient 与 Forge 兼容
        return true;
    }

    public List<string> GetLaunchArguments()
    {
        // Forge 特定的启动参数
        return new List<string>
        {
            "-Dfml.ignoreInvalidMinecraftCertificates=true",
            "-Dfml.ignorePatchDiscrepancies=true"
        };
    }

    private string ExtractForgeVersion(string dirName)
    {
        // 从目录名提取 Forge 版本，例如 "1.8.9-forge-11.15.1.2318"
        var parts = dirName.Split('-');
        if (parts.Length >= 3 && parts[1] == "forge")
        {
            return parts[2];
        }
        return "unknown";
    }
}

/// <summary>
/// Fabric mod 加载器适配器
/// </summary>
public sealed class FabricAdapter : IModLoaderAdapter
{
    public ModLoaderType LoaderType => ModLoaderType.Fabric;

    public async Task<ModLoaderInfo?> DetectAsync(string minecraftPath, string version)
    {
        // 检测 Fabric 安装
        // 1. 检查 versions 目录中是否有 fabric-loader 版本
        var versionsPath = Path.Combine(minecraftPath, "versions");
        if (!Directory.Exists(versionsPath))
        {
            return null;
        }

        var versionDirs = Directory.GetDirectories(versionsPath);
        foreach (var versionDir in versionDirs)
        {
            var dirName = Path.GetFileName(versionDir);
            if (dirName.Contains("fabric", StringComparison.OrdinalIgnoreCase) &&
                dirName.Contains(version, StringComparison.OrdinalIgnoreCase))
            {
                var fabricVersion = ExtractFabricVersion(dirName);

                return new ModLoaderInfo
                {
                    Type = ModLoaderType.Fabric,
                    Version = fabricVersion,
                    MinecraftVersion = version,
                    InstallPath = versionDir,
                    IsInstalled = true,
                    IsCompatible = true
                };
            }
        }

        return null;
    }

    public Task InitializeAsync(ModLoaderInfo loaderInfo)
    {
        return Task.CompletedTask;
    }

    public async Task<List<string>> GetInstalledModsAsync()
    {
        var modsList = new List<string>();
        return await Task.FromResult(modsList);
    }

    public bool IsLizardClientCompatible()
    {
        return true;
    }

    public List<string> GetLaunchArguments()
    {
        return new List<string>
        {
            "-Dfabric.development=false"
        };
    }

    private string ExtractFabricVersion(string dirName)
    {
        // 从目录名提取 Fabric 版本
        var parts = dirName.Split('-');
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] == "fabric" && i + 1 < parts.Length)
            {
                return parts[i + 1];
            }
        }
        return "unknown";
    }
}

/// <summary>
/// NeoForge mod 加载器适配器
/// </summary>
public sealed class NeoForgeAdapter : IModLoaderAdapter
{
    public ModLoaderType LoaderType => ModLoaderType.NeoForge;

    public async Task<ModLoaderInfo?> DetectAsync(string minecraftPath, string version)
    {
        // NeoForge 检测逻辑（类似 Forge）
        var versionsPath = Path.Combine(minecraftPath, "versions");
        if (!Directory.Exists(versionsPath))
        {
            return null;
        }

        var versionDirs = Directory.GetDirectories(versionsPath);
        foreach (var versionDir in versionDirs)
        {
            var dirName = Path.GetFileName(versionDir);
            if (dirName.Contains("neoforge", StringComparison.OrdinalIgnoreCase) &&
                dirName.Contains(version, StringComparison.OrdinalIgnoreCase))
            {
                return new ModLoaderInfo
                {
                    Type = ModLoaderType.NeoForge,
                    Version = "detected",
                    MinecraftVersion = version,
                    InstallPath = versionDir,
                    IsInstalled = true,
                    IsCompatible = true
                };
            }
        }

        return null;
    }

    public Task InitializeAsync(ModLoaderInfo loaderInfo)
    {
        return Task.CompletedTask;
    }

    public async Task<List<string>> GetInstalledModsAsync()
    {
        return await Task.FromResult(new List<string>());
    }

    public bool IsLizardClientCompatible()
    {
        return true;
    }

    public List<string> GetLaunchArguments()
    {
        return new List<string>
        {
            "-Dneoforge.enabledGameTestNamespaces=lizardclient"
        };
    }
}
