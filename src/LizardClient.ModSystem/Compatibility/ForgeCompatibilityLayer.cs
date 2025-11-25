using LizardClient.Core.Interfaces;
using LizardClient.ModSystem.API;
using LizardClient.ModSystem.Models;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace LizardClient.ModSystem.Compatibility;

public sealed class ForgeCompatibilityLayer : IModCompatibilityLayer
{
    private readonly ILogger _logger;
    public string Name => "Forge";

    public ForgeCompatibilityLayer(ILogger logger)
    {
        _logger = logger;
    }

    public bool CanHandle(string modPath)
    {
        try
        {
            if (File.Exists(modPath) && (modPath.EndsWith(".jar", StringComparison.OrdinalIgnoreCase) || modPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)))
            {
                using var archive = ZipFile.OpenRead(modPath);
                return archive.GetEntry("META-INF/mods.toml") != null;
            }
        }
        catch (Exception ex)
        {
            _logger.Warning($"Failed to check Forge compatibility for {modPath}: {ex.Message}");
        }
        return false;
    }

    public ModMetadata? ParseMetadata(string modPath)
    {
        try
        {
            using var archive = ZipFile.OpenRead(modPath);
            var tomlEntry = archive.GetEntry("META-INF/mods.toml");
            if (tomlEntry == null) return null;

            using var stream = tomlEntry.Open();
            using var reader = new StreamReader(stream);
            var tomlContent = reader.ReadToEnd();
            return ParseToml(tomlContent, Path.GetFileNameWithoutExtension(modPath));
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to parse Forge metadata from {modPath}: {ex.Message}", ex);
            return null;
        }
    }

    public IMod? LoadMod(string modPath, ModMetadata metadata)
    {
        _logger.Info($"Loading Forge mod: {metadata.Name} (wrapper mode)");
        return new ForgeModWrapper(metadata, modPath, _logger);
    }

    private ModMetadata ParseToml(string tomlContent, string fallbackId)
    {
        var metadata = new ModMetadata { Id = fallbackId, Name = fallbackId, Version = "1.0.0" };
        var modsSection = Regex.Match(tomlContent, @"\[\[mods\]\](.*?)(?=\[\[|$)", RegexOptions.Singleline);
        if (modsSection.Success)
        {
            var modContent = modsSection.Groups[1].Value;
            var modIdMatch = Regex.Match(modContent, @"modId\s*=\s*""([^""]+)""");
            if (modIdMatch.Success) metadata.Id = modIdMatch.Groups[1].Value;
            var versionMatch = Regex.Match(modContent, @"version\s*=\s*""([^""]+)""");
            if (versionMatch.Success) metadata.Version = versionMatch.Groups[1].Value;
            var nameMatch = Regex.Match(modContent, @"displayName\s*=\s*""([^""]+)""");
            if (nameMatch.Success) metadata.Name = nameMatch.Groups[1].Value;
            var descMatch = Regex.Match(modContent, @"description\s*=\s*""([^""]+)""");
            if (descMatch.Success) metadata.Description = descMatch.Groups[1].Value;
            var authorsMatch = Regex.Match(modContent, @"authors\s*=\s*""([^""]+)""");
            if (authorsMatch.Success) metadata.Author = authorsMatch.Groups[1].Value;
        }
        return metadata;
    }
}

internal class ForgeModWrapper : ModBase
{
    private readonly string _modPath;
    private readonly ILogger _logger;
    public override LizardClient.Core.Models.ModInfo Info { get; }

    public ForgeModWrapper(ModMetadata metadata, string modPath, ILogger logger)
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
        _logger.Info($"Forge mod wrapper loaded: {Info.Name}");
    }

    public override void OnEnable()
    {
        _logger.Info($"Forge mod wrapper enabled: {Info.Name}");
    }

    public override void OnDisable()
    {
        _logger.Info($"Forge mod wrapper disabled: {Info.Name}");
    }

    public override void OnUnload()
    {
        _logger.Info($"Forge mod wrapper unloaded: {Info.Name}");
    }
}
