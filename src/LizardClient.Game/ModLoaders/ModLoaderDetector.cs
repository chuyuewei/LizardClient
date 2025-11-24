using LizardClient.Core.Interfaces;

namespace LizardClient.Game.ModLoaders;

/// <summary>
/// Mod 加载器检测器，自动识别已安装的 mod 加载器
/// </summary>
public sealed class ModLoaderDetector
{
    private readonly ILogger _logger;
    private readonly List<IModLoaderAdapter> _adapters;

    public ModLoaderDetector(ILogger logger)
    {
        _logger = logger;
        _adapters = new List<IModLoaderAdapter>();
    }

    /// <summary>
    /// 注册 mod 加载器适配器
    /// </summary>
    public void RegisterAdapter(IModLoaderAdapter adapter)
    {
        _adapters.Add(adapter);
        _logger.Info($"注册 mod 加载器适配器: {adapter.LoaderType}");
    }

    /// <summary>
    /// 检测指定 Minecraft 版本安装的 mod 加载器
    /// </summary>
    /// <param name="minecraftPath">Minecraft 安装路径</param>
    /// <param name="version">Minecraft 版本</param>
    /// <returns>检测到的 mod 加载器信息列表</returns>
    public async Task<List<ModLoaderInfo>> DetectAllAsync(string minecraftPath, string version)
    {
        _logger.Info($"开始检测 mod 加载器: {version}");
        var detectedLoaders = new List<ModLoaderInfo>();

        foreach (var adapter in _adapters)
        {
            try
            {
                var loaderInfo = await adapter.DetectAsync(minecraftPath, version);
                if (loaderInfo != null && loaderInfo.IsInstalled)
                {
                    detectedLoaders.Add(loaderInfo);
                    _logger.Info($"检测到 {loaderInfo.Type} v{loaderInfo.Version}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"检测 {adapter.LoaderType} 失败", ex);
            }
        }

        if (detectedLoaders.Count == 0)
        {
            _logger.Info("未检测到任何 mod 加载器，使用原版 Minecraft");
            detectedLoaders.Add(new ModLoaderInfo
            {
                Type = ModLoaderType.Vanilla,
                MinecraftVersion = version,
                IsInstalled = true,
                IsCompatible = true
            });
        }

        return detectedLoaders;
    }

    /// <summary>
    /// 检测主要的 mod 加载器（如果安装了多个，返回优先级最高的）
    /// </summary>
    public async Task<ModLoaderInfo?> DetectPrimaryAsync(string minecraftPath, string version)
    {
        var allLoaders = await DetectAllAsync(minecraftPath, version);

        // 优先级：NeoForge > Fabric > Quilt > Forge > OptiFine > Vanilla
        var priority = new Dictionary<ModLoaderType, int>
        {
            { ModLoaderType.NeoForge, 5 },
            { ModLoaderType.Fabric, 4 },
            { ModLoaderType.Quilt, 3 },
            { ModLoaderType.Forge, 2 },
            { ModLoaderType.OptiFine, 1 },
            { ModLoaderType.Vanilla, 0 }
        };

        return allLoaders
            .OrderByDescending(l => priority.GetValueOrDefault(l.Type, -1))
            .FirstOrDefault();
    }

    /// <summary>
    /// 检查是否可以同时运行多个 mod 加载器
    /// </summary>
    public bool CanCoexist(ModLoaderType type1, ModLoaderType type2)
    {
        // Fabric 和 Quilt 可以共存（Quilt 是 Fabric 的超集）
        if ((type1 == ModLoaderType.Fabric && type2 == ModLoaderType.Quilt) ||
            (type1 == ModLoaderType.Quilt && type2 == ModLoaderType.Fabric))
        {
            return true;
        }

        // OptiFine 可以与大多数加载器共存
        if (type1 == ModLoaderType.OptiFine || type2 == ModLoaderType.OptiFine)
        {
            return true;
        }

        // 其他情况不能共存
        return false;
    }
}
