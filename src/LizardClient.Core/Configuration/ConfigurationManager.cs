using LizardClient.Core.Interfaces;
using Newtonsoft.Json;

namespace LizardClient.Core.Configuration;

/// <summary>
/// 配置改变事件参数
/// </summary>
public sealed class ConfigChangedEventArgs : EventArgs
{
    /// <summary>
    /// 配置键
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 旧值
    /// </summary>
    public object? OldValue { get; set; }

    /// <summary>
    /// 新值
    /// </summary>
    public object? NewValue { get; set; }

    /// <summary>
    /// ModID（如果是Mod配置）
    /// </summary>
    public string? ModId { get; set; }

    /// <summary>
    /// 配置文件ID
    /// </summary>
    public string ProfileId { get; set; } = string.Empty;
}

/// <summary>
/// 配置管理器
/// 负责管理所有配置文件、Mod配置和配置值
/// </summary>
public sealed class ConfigurationManager
{
    private readonly ILogger _logger;
    private readonly string _configDirectory;
    private readonly ValidationEngine _validationEngine;
    private readonly Dictionary<string, ConfigurationSchema> _schemas;
    private readonly Dictionary<string, ConfigurationProfile> _profiles;
    private readonly Dictionary<string, ModConfiguration> _modConfigs;
    private ConfigurationProfile? _currentProfile;
    private readonly object _lock = new();

    /// <summary>
    /// 配置改变事件
    /// </summary>
    public event EventHandler<ConfigChangedEventArgs>? ConfigChanged;

    /// <summary>
    /// 配置文件改变事件
    /// </summary>
    public event EventHandler<string>? ProfileChanged;

    public ConfigurationManager(ILogger logger, string configDirectory = "./config")
    {
        _logger = logger;
        _configDirectory = configDirectory;
        _validationEngine = new ValidationEngine();
        _schemas = new Dictionary<string, ConfigurationSchema>();
        _profiles = new Dictionary<string, ConfigurationProfile>();
        _modConfigs = new Dictionary<string, ModConfiguration>();

        // 确保配置目录存在
        if (!Directory.Exists(_configDirectory))
        {
            Directory.CreateDirectory(_configDirectory);
        }

        // 加载配置
        LoadProfiles();
        LoadModConfigurations();
    }

    #region Schema Management

    /// <summary>
    /// 注册配置架构
    /// </summary>
    public void RegisterSchema(ConfigurationSchema schema)
    {
        lock (_lock)
        {
            _schemas[schema.Id] = schema;
            _logger.Info($"Registered configuration schema: {schema.Name} ({schema.Id})");
        }
    }

    /// <summary>
    /// 获取配置架构
    /// </summary>
    public ConfigurationSchema? GetSchema(string schemaId)
    {
        lock (_lock)
        {
            return _schemas.TryGetValue(schemaId, out var schema) ? schema : null;
        }
    }

    /// <summary>
    /// 获取所有配置架构
    /// </summary>
    public List<ConfigurationSchema> GetAllSchemas()
    {
        lock (_lock)
        {
            return new List<ConfigurationSchema>(_schemas.Values);
        }
    }

    #endregion

    #region Profile Management

    /// <summary>
    /// 创建新配置文件
    /// </summary>
    public ConfigurationProfile CreateProfile(string name, bool setAsDefault = false)
    {
        lock (_lock)
        {
            var profile = new ConfigurationProfile
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                IsDefault = setAsDefault,
                CreatedAt = DateTime.UtcNow
            };

            // 从所有注册的Schema初始化默认值
            foreach (var schema in _schemas.Values)
            {
                foreach (var property in schema.Properties)
                {
                    if (property.DefaultValue != null)
                    {
                        profile.Values[property.Key] = property.DefaultValue;
                    }
                }
            }

            _profiles[profile.Id] = profile;
            SaveProfile(profile);

            _logger.Info($"Created configuration profile: {name} ({profile.Id})");

            return profile;
        }
    }

    /// <summary>
    /// 切换配置文件
    /// </summary>
    public void SwitchProfile(string profileId)
    {
        lock (_lock)
        {
            if (!_profiles.TryGetValue(profileId, out var profile))
            {
                throw new InvalidOperationException($"Profile {profileId} not found");
            }

            _currentProfile = profile;
            _logger.Info($"Switched to profile: {profile.Name} ({profileId})");

            ProfileChanged?.Invoke(this, profileId);
        }
    }

    /// <summary>
    /// 删除配置文件
    /// </summary>
    public void DeleteProfile(string profileId)
    {
        lock (_lock)
        {
            if (!_profiles.TryGetValue(profileId, out var profile))
            {
                return;
            }

            if (profile.IsDefault)
            {
                throw new InvalidOperationException("Cannot delete default profile");
            }

            if (_currentProfile?.Id == profileId)
            {
                // 切换到默认配置文件
                var defaultProfile = _profiles.Values.FirstOrDefault(p => p.IsDefault);
                if (defaultProfile != null)
                {
                    _currentProfile = defaultProfile;
                }
            }

            _profiles.Remove(profileId);

            // 删除文件
            var filePath = Path.Combine(_configDirectory, "profiles", $"{profileId}.json");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            _logger.Info($"Deleted profile: {profile.Name} ({profileId})");
        }
    }

    /// <summary>
    /// 获取当前配置文件
    /// </summary>
    public ConfigurationProfile GetCurrentProfile()
    {
        lock (_lock)
        {
            if (_currentProfile == null)
            {
                // 尝试加载默认配置文件
                _currentProfile = _profiles.Values.FirstOrDefault(p => p.IsDefault);

                // 如果没有默认配置文件，创建一个
                if (_currentProfile == null)
                {
                    _currentProfile = CreateProfile("Default", true);
                }
            }

            return _currentProfile;
        }
    }

    /// <summary>
    /// 获取所有配置文件
    /// </summary>
    public List<ConfigurationProfile> GetAllProfiles()
    {
        lock (_lock)
        {
            return new List<ConfigurationProfile>(_profiles.Values);
        }
    }

    #endregion

    #region Configuration Values

    /// <summary>
    /// 获取配置值
    /// </summary>
    public T? GetValue<T>(string key, T? defaultValue = default)
    {
        var profile = GetCurrentProfile();
        return profile.GetValue(key, defaultValue);
    }

    /// <summary>
    /// 设置配置值
    /// </summary>
    public void SetValue(string key, object value, bool validate = true)
    {
        var profile = GetCurrentProfile();

        // 验证
        if (validate)
        {
            var property = _schemas.Values
                .SelectMany(s => s.Properties)
                .FirstOrDefault(p => p.Key == key);

            if (property != null)
            {
                var validationResult = _validationEngine.Validate(property, value);
                if (!validationResult.IsValid)
                {
                    throw new InvalidOperationException(validationResult.ErrorMessage);
                }
            }
        }

        var oldValue = profile.GetValue<object>(key);
        profile.SetValue(key, value);
        SaveProfile(profile);

        // 触发事件
        ConfigChanged?.Invoke(this, new ConfigChangedEventArgs
        {
            Key = key,
            OldValue = oldValue,
            NewValue = value,
            ProfileId = profile.Id
        });

        _logger.Info($"Config value changed: {key} = {value}");
    }

    #endregion

    #region Mod Configuration

    /// <summary>
    /// 获取Mod配置
    /// </summary>
    public ModConfiguration GetModConfig(string modId)
    {
        lock (_lock)
        {
            if (!_modConfigs.TryGetValue(modId, out var modConfig))
            {
                modConfig = new ModConfiguration { ModId = modId };
                _modConfigs[modId] = modConfig;
            }

            return modConfig;
        }
    }

    /// <summary>
    /// 保存Mod配置
    /// </summary>
    public void SaveModConfig(string modId, ModConfiguration config)
    {
        lock (_lock)
        {
            _modConfigs[modId] = config;
            config.LastModified = DateTime.UtcNow;

            var filePath = Path.Combine(_configDirectory, "mods", $"{modId}.json");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(filePath, json);

            _logger.Info($"Saved mod configuration: {modId}");
        }
    }

    /// <summary>
    /// 获取Mod配置值
    /// </summary>
    public T? GetModValue<T>(string modId, string key, T? defaultValue = default)
    {
        var modConfig = GetModConfig(modId);
        return modConfig.GetValue(key, defaultValue);
    }

    /// <summary>
    /// 设置Mod配置值
    /// </summary>
    public void SetModValue(string modId, string key, object value)
    {
        var modConfig = GetModConfig(modId);

        // 验证
        if (modConfig.Schema != null)
        {
            var property = modConfig.Schema.GetProperty(key);
            if (property != null)
            {
                var validationResult = _validationEngine.Validate(property, value);
                if (!validationResult.IsValid)
                {
                    throw new InvalidOperationException(validationResult.ErrorMessage);
                }
            }
        }

        var oldValue = modConfig.GetValue<object>(key);
        modConfig.SetValue(key, value);
        SaveModConfig(modId, modConfig);

        // 触发事件
        ConfigChanged?.Invoke(this, new ConfigChangedEventArgs
        {
            Key = key,
            OldValue = oldValue,
            NewValue = value,
            ModId = modId,
            ProfileId = GetCurrentProfile().Id
        });
    }

    #endregion

    #region Persistence

    /// <summary>
    /// 保存配置文件
    /// </summary>
    private void SaveProfile(ConfigurationProfile profile)
    {
        try
        {
            var profilesDir = Path.Combine(_configDirectory, "profiles");
            Directory.CreateDirectory(profilesDir);

            var filePath = Path.Combine(profilesDir, $"{profile.Id}.json");

            // 备份现有文件
            if (File.Exists(filePath))
            {
                var backupPath = filePath + ".bak";
                File.Copy(filePath, backupPath, true);
            }

            var json = JsonConvert.SerializeObject(profile, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to save profile {profile.Id}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 加载所有配置文件
    /// </summary>
    private void LoadProfiles()
    {
        try
        {
            var profilesDir = Path.Combine(_configDirectory, "profiles");
            if (!Directory.Exists(profilesDir))
            {
                return;
            }

            foreach (var file in Directory.GetFiles(profilesDir, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var profile = JsonConvert.DeserializeObject<ConfigurationProfile>(json);
                    if (profile != null)
                    {
                        _profiles[profile.Id] = profile;
                        _logger.Info($"Loaded profile: {profile.Name}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed to load profile from {file}: {ex.Message}", ex);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load profiles: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 加载所有Mod配置
    /// </summary>
    private void LoadModConfigurations()
    {
        try
        {
            var modsDir = Path.Combine(_configDirectory, "mods");
            if (!Directory.Exists(modsDir))
            {
                return;
            }

            foreach (var file in Directory.GetFiles(modsDir, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var modConfig = JsonConvert.DeserializeObject<ModConfiguration>(json);
                    if (modConfig != null)
                    {
                        _modConfigs[modConfig.ModId] = modConfig;
                        _logger.Info($"Loaded mod configuration: {modConfig.ModId}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed to load mod config from {file}: {ex.Message}", ex);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load mod configurations: {ex.Message}", ex);
        }
    }

    #endregion
}
