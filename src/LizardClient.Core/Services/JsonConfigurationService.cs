using LizardClient.Core.Interfaces;
using LizardClient.Core.Models;
using Newtonsoft.Json;
using System.IO;

namespace LizardClient.Core.Services;

/// <summary>
/// 基于 JSON 文件的配置服务实现
/// </summary>
public sealed class JsonConfigurationService : IConfigurationService
{
    private readonly string _configDirectory;
    private readonly string _configFilePath;
    private readonly string _profilesDirectory;
    private readonly ILogger _logger;

    public JsonConfigurationService(ILogger logger)
    {
        _logger = logger;
        _configDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            ".lizardclient"
        );
        _configFilePath = Path.Combine(_configDirectory, "config.json");
        _profilesDirectory = Path.Combine(_configDirectory, "profiles");

        // 确保目录存在
        Directory.CreateDirectory(_configDirectory);
        Directory.CreateDirectory(_profilesDirectory);
    }

    public async Task<ClientConfiguration> LoadConfigurationAsync()
    {
        try
        {
            if (!File.Exists(_configFilePath))
            {
                _logger.Info("配置文件不存在，创建默认配置");
                var defaultConfig = new ClientConfiguration();
                await SaveConfigurationAsync(defaultConfig);
                return defaultConfig;
            }

            var json = await File.ReadAllTextAsync(_configFilePath);
            var config = JsonConvert.DeserializeObject<ClientConfiguration>(json);

            if (config == null)
            {
                _logger.Warning("配置文件解析失败，使用默认配置");
                return new ClientConfiguration();
            }

            _logger.Info($"成功加载配置，版本: {config.ConfigVersion}");
            return config;
        }
        catch (Exception ex)
        {
            _logger.Error($"加载配置失败: {ex.Message}", ex);
            return new ClientConfiguration();
        }
    }

    public async Task SaveConfigurationAsync(ClientConfiguration configuration)
    {
        try
        {
            if (!ValidateConfiguration(configuration))
            {
                throw new InvalidOperationException("配置验证失败");
            }

            var json = JsonConvert.SerializeObject(configuration, Formatting.Indented);
            await File.WriteAllTextAsync(_configFilePath, json);
            _logger.Info("配置保存成功");
        }
        catch (Exception ex)
        {
            _logger.Error($"保存配置失败: {ex.Message}", ex);
            throw;
        }
    }

    public async Task<List<GameProfile>> GetGameProfilesAsync()
    {
        try
        {
            var profiles = new List<GameProfile>();
            var profileFiles = Directory.GetFiles(_profilesDirectory, "*.json");

            foreach (var file in profileFiles)
            {
                var json = await File.ReadAllTextAsync(file);
                var profile = JsonConvert.DeserializeObject<GameProfile>(json);
                if (profile != null)
                {
                    profiles.Add(profile);
                }
            }

            _logger.Info($"加载了 {profiles.Count} 个游戏配置文件");
            return profiles.OrderByDescending(p => p.LastLaunchTime ?? p.CreatedAt).ToList();
        }
        catch (Exception ex)
        {
            _logger.Error($"加载游戏配置文件失败: {ex.Message}", ex);
            return new List<GameProfile>();
        }
    }

    public async Task<GameProfile?> GetGameProfileAsync(Guid profileId)
    {
        try
        {
            var filePath = Path.Combine(_profilesDirectory, $"{profileId}.json");
            if (!File.Exists(filePath))
            {
                return null;
            }

            var json = await File.ReadAllTextAsync(filePath);
            return JsonConvert.DeserializeObject<GameProfile>(json);
        }
        catch (Exception ex)
        {
            _logger.Error($"加载游戏配置失败: {ex.Message}", ex);
            return null;
        }
    }

    public async Task SaveGameProfileAsync(GameProfile profile)
    {
        try
        {
            var filePath = Path.Combine(_profilesDirectory, $"{profile.Id}.json");
            var json = JsonConvert.SerializeObject(profile, Formatting.Indented);
            await File.WriteAllTextAsync(filePath, json);
            _logger.Info($"保存游戏配置: {profile.Name}");
        }
        catch (Exception ex)
        {
            _logger.Error($"保存游戏配置失败: {ex.Message}", ex);
            throw;
        }
    }

    public async Task DeleteGameProfileAsync(Guid profileId)
    {
        try
        {
            var filePath = Path.Combine(_profilesDirectory, $"{profileId}.json");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.Info($"删除游戏配置: {profileId}");
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.Error($"删除游戏配置失败: {ex.Message}", ex);
            throw;
        }
    }

    public bool ValidateConfiguration(ClientConfiguration configuration)
    {
        if (configuration == null)
        {
            _logger.Warning("配置对象为 null");
            return false;
        }

        if (string.IsNullOrWhiteSpace(configuration.GameRootDirectory))
        {
            _logger.Warning("游戏根目录未设置");
            return false;
        }

        if (configuration.Performance.MaxMemoryMB < 512)
        {
            _logger.Warning("内存设置过低，最小值为 512MB");
            return false;
        }

        return true;
    }

    public async Task ResetToDefaultAsync()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                File.Delete(_configFilePath);
            }

            var defaultConfig = new ClientConfiguration();
            await SaveConfigurationAsync(defaultConfig);
            _logger.Info("配置已重置为默认值");
        }
        catch (Exception ex)
        {
            _logger.Error($"重置配置失败: {ex.Message}", ex);
            throw;
        }
    }
}
