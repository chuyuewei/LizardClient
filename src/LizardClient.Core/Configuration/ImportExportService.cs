using LizardClient.Core.Interfaces;
using Newtonsoft.Json;

namespace LizardClient.Core.Configuration;

/// <summary>
/// 导入/导出服务
/// 负责配置文件的导入导出功能
/// </summary>
public sealed class ImportExportService
{
    private readonly ILogger _logger;

    public ImportExportService(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 导出配置文件
    /// </summary>
    public void ExportProfile(ConfigurationProfile profile, string filePath)
    {
        try
        {
            var json = JsonConvert.SerializeObject(profile, Formatting.Indented);
            File.WriteAllText(filePath, json);
            _logger.Info($"Exported profile {profile.Name} to {filePath}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to export profile: {ex.Message}", ex);
            throw;
        }
    }

    /// <summary>
    /// 导入配置文件
    /// </summary>
    public ConfigurationProfile ImportProfile(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            var profile = JsonConvert.DeserializeObject<ConfigurationProfile>(json);

            if (profile == null)
            {
                throw new InvalidOperationException("Failed to deserialize profile");
            }

            // 生成新ID和时间戳
            profile.Id = Guid.NewGuid().ToString();
            profile.CreatedAt = DateTime.UtcNow;
            profile.LastModified = DateTime.UtcNow;
            profile.IsDefault = false;

            _logger.Info($"Imported profile {profile.Name} from {filePath}");
            return profile;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to import profile: {ex.Message}", ex);
            throw;
        }
    }

    /// <summary>
    /// 合并配置文件
    /// </summary>
    public void MergeConfigurations(ConfigurationProfile source, ConfigurationProfile target, bool overwriteExisting = true)
    {
        foreach (var kvp in source.Values)
        {
            if (overwriteExisting || !target.Values.ContainsKey(kvp.Key))
            {
                target.Values[kvp.Key] = kvp.Value;
            }
        }

        // 合并Mod配置
        foreach (var modConfig in source.ModConfigs)
        {
            if (!target.ModConfigs.ContainsKey(modConfig.Key))
            {
                target.ModConfigs[modConfig.Key] = new Dictionary<string, object>(modConfig.Value);
            }
            else if (overwriteExisting)
            {
                foreach (var kvp in modConfig.Value)
                {
                    target.ModConfigs[modConfig.Key][kvp.Key] = kvp.Value;
                }
            }
        }

        target.LastModified = DateTime.UtcNow;
        _logger.Info($"Merged configuration from {source.Name} to {target.Name}");
    }

    /// <summary>
    /// 导出所有配置文件到目录
    /// </summary>
    public void ExportAllProfiles(List<ConfigurationProfile> profiles, string directory)
    {
        Directory.CreateDirectory(directory);

        foreach (var profile in profiles)
        {
            var fileName = $"{SanitizeFileName(profile.Name)}_{profile.Id}.json";
            var filePath = Path.Combine(directory, fileName);
            ExportProfile(profile, filePath);
        }

        _logger.Info($"Exported {profiles.Count} profiles to {directory}");
    }

    /// <summary>
    /// 从目录导入所有配置文件
    /// </summary>
    public List<ConfigurationProfile> ImportAllProfiles(string directory)
    {
        var profiles = new List<ConfigurationProfile>();

        if (!Directory.Exists(directory))
        {
            return profiles;
        }

        foreach (var file in Directory.GetFiles(directory, "*.json"))
        {
            try
            {
                var profile = ImportProfile(file);
                profiles.Add(profile);
            }
            catch (Exception ex)
            {
                _logger.Warning($"Failed to import profile from {file}: {ex.Message}");
            }
        }

        _logger.Info($"Imported {profiles.Count} profiles from {directory}");
        return profiles;
    }

    /// <summary>
    /// 导出Mod配置
    /// </summary>
    public void ExportModConfiguration(ModConfiguration modConfig, string filePath)
    {
        try
        {
            var json = JsonConvert.SerializeObject(modConfig, Formatting.Indented);
            File.WriteAllText(filePath, json);
            _logger.Info($"Exported mod configuration {modConfig.ModId} to {filePath}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to export mod configuration: {ex.Message}", ex);
            throw;
        }
    }

    /// <summary>
    /// 导入Mod配置
    /// </summary>
    public ModConfiguration ImportModConfiguration(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            var modConfig = JsonConvert.DeserializeObject<ModConfiguration>(json);

            if (modConfig == null)
            {
                throw new InvalidOperationException("Failed to deserialize mod configuration");
            }

            modConfig.LastModified = DateTime.UtcNow;

            _logger.Info($"Imported mod configuration {modConfig.ModId} from {filePath}");
            return modConfig;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to import mod configuration: {ex.Message}", ex);
            throw;
        }
    }

    /// <summary>
    /// 创建配置预设
    /// </summary>
    public void CreatePreset(ConfigurationProfile profile, string presetName, string presetDirectory)
    {
        Directory.CreateDirectory(presetDirectory);

        var preset = profile.Clone();
        preset.Name = presetName;
        preset.Tags.Add("preset");

        var fileName = $"{SanitizeFileName(presetName)}.json";
        var filePath = Path.Combine(presetDirectory, fileName);
        ExportProfile(preset, filePath);

        _logger.Info($"Created configuration preset: {presetName}");
    }

    /// <summary>
    /// 加载配置预设
    /// </summary>
    public List<ConfigurationProfile> LoadPresets(string presetDirectory)
    {
        if (!Directory.Exists(presetDirectory))
        {
            return new List<ConfigurationProfile>();
        }

        return ImportAllProfiles(presetDirectory)
            .Where(p => p.Tags.Contains("preset"))
            .ToList();
    }

    /// <summary>
    /// 清理文件名
    /// </summary>
    private string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
    }
}
