using LizardClient.Core.Interfaces;
using LizardClient.ModSystem.API;
using LizardClient.ModSystem.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO.Compression;

namespace LizardClient.ModSystem.Compatibility;

/// <summary>
/// Fabric模组兼容层
/// 支持解析Fabric的fabric.mod.json格式
/// </summary>
public sealed class FabricCompatibilityLayer : IModCompatibilityLayer
{
    private readonly ILogger _logger;

    public string Name => "Fabric";

    public FabricCompatibilityLayer(ILogger logger)
    {
        _logger = logger;
    }

    public bool CanHandle(string modPath)
    {
        try
        {
            if (File.Exists(modPath) && (modPath.EndsWith(".jar", StringComparison.OrdinalIgnoreCase) ||
                                          modPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)))
            {
                using var archive = ZipFile.OpenRead(modPath);
                return archive.GetEntry("fabric.mod.json") != null;
            }
        }
        catch (Exception ex)
        {
            _logger.Warning($"Failed to check Fabric compatibility for {modPath}: {ex.Message}");
        }

        return false;
    }

    public ModMetadata? ParseMetadata(string modPath)
    {
        try
        {
            using var archive = ZipFile.OpenRead(modPath);
            var jsonEntry = archive.GetEntry("fabric.mod.json");

            if (jsonEntry == null)
                return null;

            using var stream = jsonEntry.Open();
            using var reader = new StreamReader(stream);
            var jsonContent = reader.ReadToEnd();

            return ParseFabricJson(jsonContent);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to parse Fabric metadata from {modPath}: {ex.Message}", ex);
            return null;
        }
    }

    public IMod? LoadMod(string modPath, ModMetadata metadata)
    {
        _logger.Info($"Loading Fabric mod: {metadata.Name} (wrapper mode)");

        // 创建Fabric mod包装器
        return new FabricModWrapper(metadata, modPath, _logger);
    }

    /// <summary>
    /// 解析Fabric的fabric.mod.json
    /// </summary>
    private ModMetadata ParseFabricJson(string jsonContent)
    {
        var json = JObject.Parse(jsonContent);

        var metadata = new ModMetadata
        {
            Id = json["id"]?.ToString() ?? "unknown",
            Name = json["name"]?.ToString() ?? "Unknown Mod",
            Version = json["version"]?.ToString() ?? "1.0.0",
            Description = json["description"]?.ToString() ?? "",
            Author = json["authors"]?.FirstOrDefault()?.ToString() ??
                     json["contact"]?["email"]?.ToString() ?? "Unknown"
        };

        // 解析依赖
        var depends = json["depends"] as JObject;
        if (depends != null)
        {
            foreach (var dep in depends.Properties())
            {
                var modId = dep.Name;
                var versionRange = dep.Value?.ToString() ?? "*";

                // 跳过 Minecraft 和 Fabric API 的硬依赖
                if (modId == "minecraft" || modId == "fabricloader" || modId == "java")
                    continue;

                metadata.Dependencies.Add(new ModDependency
                {
                    ModId = modId,
                    VersionRange = versionRange,
                    IsOptional = false
                });
            }
        }

        // 解析推荐依赖（可选）
        var recommends = json["recommends"] as JObject;
        if (recommends != null)
        {
            foreach (var dep in recommends.Properties())
            {
                var modId = dep.Name;
                var versionRange = dep.Value?.ToString() ?? "*";

                metadata.Dependencies.Add(new ModDependency
                {
                    ModId = modId,
                    VersionRange = versionRange,
                    IsOptional = true
                });
            }
        }

        // 解析 breaks (不兼容)
        var breaks = json["breaks"] as JObject;
        if (breaks != null)
        {
            foreach (var brk in breaks.Properties())
            {
                metadata.Incompatibilities.Add(brk.Name);
            }
        }

        // 解析 entrypoints (入口点)
        var entrypoints = json["entrypoints"];
        if (entrypoints != null)
        {
            var mainEntry = entrypoints["main"]?.FirstOrDefault()?.ToString();
            if (!string.IsNullOrEmpty(mainEntry))
            {
                metadata.EntryPoint = mainEntry;
            }
        }

        return metadata;
    }
}

/// <summary>
/// Fabric模组包装器
/// </summary>
internal class FabricModWrapper : ModBase
{
    private readonly string _modPath;
    private readonly ILogger _logger;

    public override LizardClient.Core.Models.ModInfo Info { get; }

    public FabricModWrapper(ModMetadata metadata, string modPath, ILogger logger)
    {
        _modPath = modPath;
        _logger = logger;

        Info = new LizardClient.Core.Models.ModInfo
        {
            Id = metadata.Id,
            Name = metadata.Name,
            Description = metadata.Description,
            Version = metadata.Version,
            Author = metadata.Author ?? "Unknown",
            Category = LizardClient.Core.Models.ModCategory.Utility
        };
    }

    public override void OnLoad()
    {
        _logger.Info($"Fabric mod wrapper loaded: {Info.Name}");
        // 实际实现需要Java互操作或者使用IKVM等工具
    }

    public override void OnEnable()
    {
        _logger.Info($"Fabric mod wrapper enabled: {Info.Name}");
    }

    public override void OnDisable()
    {
        _logger.Info($"Fabric mod wrapper disabled: {Info.Name}");
    }

    public override void OnUnload()
    {
        _logger.Info($"Fabric mod wrapper unloaded: {Info.Name}");
    }
}
