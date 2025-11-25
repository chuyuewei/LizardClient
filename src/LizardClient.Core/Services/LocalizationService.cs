using LizardClient.Core.Interfaces;
using Newtonsoft.Json;
using System.Globalization;

namespace LizardClient.Core.Services;

/// <summary>
/// 本地化服务实现
/// </summary>
public sealed class LocalizationService : ILocalizationService
{
    private readonly ILogger _logger;
    private readonly IConfigurationService _configurationService;
    private readonly string _resourcesDirectory;
    private Dictionary<string, string> _currentResources = new();
    private string _currentLanguage = "en-US";
    private readonly object _lockObject = new();

    public event EventHandler<LanguageChangedEventArgs>? LanguageChanged;

    public LocalizationService(ILogger logger, IConfigurationService configurationService, string? resourcesDirectory = null)
    {
        _logger = logger;
        _configurationService = configurationService;
        _resourcesDirectory = resourcesDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Localization");

        // 确保资源目录存在
        if (!Directory.Exists(_resourcesDirectory))
        {
            Directory.CreateDirectory(_resourcesDirectory);
            _logger.Info($"创建本地化资源目录: {_resourcesDirectory}");
        }

        // 从配置加载语言偏好
        InitializeLanguage();
    }

    /// <summary>
    /// 当前语言代码
    /// </summary>
    public string CurrentLanguage
    {
        get
        {
            lock (_lockObject)
            {
                return _currentLanguage;
            }
        }
    }

    /// <summary>
    /// 获取本地化字符串
    /// </summary>
    public string GetString(string key, params object[] args)
    {
        lock (_lockObject)
        {
            if (_currentResources.TryGetValue(key, out var value))
            {
                try
                {
                    // 如果有参数，进行格式化
                    if (args.Length > 0)
                    {
                        return string.Format(value, args);
                    }
                    return value;
                }
                catch (FormatException ex)
                {
                    _logger.Warning($"字符串格式化失败 [Key: {key}]: {ex.Message}");
                    return value; // 返回未格式化的字符串
                }
            }

            // 如果找不到，返回键本身，并记录警告
            _logger.Warning($"本地化键未找到: {key}");
            return $"[{key}]";
        }
    }

    /// <summary>
    /// 尝试获取本地化字符串
    /// </summary>
    public bool TryGetString(string key, out string value)
    {
        lock (_lockObject)
        {
            return _currentResources.TryGetValue(key, out value!);
        }
    }

    /// <summary>
    /// 设置语言
    /// </summary>
    public void SetLanguage(string languageCode)
    {
        lock (_lockObject)
        {
            if (_currentLanguage.Equals(languageCode, StringComparison.OrdinalIgnoreCase))
            {
                return; // 语言没有变化
            }

            var oldLanguage = _currentLanguage;

            // 加载新语言资源
            if (LoadLanguageResources(languageCode))
            {
                _currentLanguage = languageCode;

                // 保存到配置
                SaveLanguagePreference(languageCode);

                _logger.Info($"语言已切换: {oldLanguage} -> {languageCode}");

                // 触发事件
                LanguageChanged?.Invoke(this, new LanguageChangedEventArgs
                {
                    OldLanguage = oldLanguage,
                    NewLanguage = languageCode
                });
            }
            else
            {
                _logger.Error($"加载语言资源失败: {languageCode}");
            }
        }
    }

    /// <summary>
    /// 获取可用的语言列表
    /// </summary>
    public IEnumerable<string> GetAvailableLanguages()
    {
        try
        {
            var languages = new List<string>();

            if (Directory.Exists(_resourcesDirectory))
            {
                var files = Directory.GetFiles(_resourcesDirectory, "*.json");
                foreach (var file in files)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    languages.Add(fileName);
                }
            }

            // 如果没有找到任何语言文件，返回默认语言
            if (languages.Count == 0)
            {
                languages.Add("en-US");
                languages.Add("zh-CN");
            }

            return languages;
        }
        catch (Exception ex)
        {
            _logger.Error($"获取可用语言列表失败: {ex.Message}", ex);
            return new[] { "en-US" };
        }
    }

    /// <summary>
    /// 重新加载资源文件
    /// </summary>
    public void ReloadResources()
    {
        lock (_lockObject)
        {
            LoadLanguageResources(_currentLanguage);
            _logger.Info("资源文件已重新加载");
        }
    }

    /// <summary>
    /// 初始化语言
    /// </summary>
    private async Task InitializeLanguageAsync()
    {
        try
        {
            // 1. 尝试从配置读取用户偏好
            var config = await _configurationService.LoadConfigurationAsync();
            var preferredLanguage = config.PreferredLanguage;

            if (!string.IsNullOrEmpty(preferredLanguage))
            {
                _logger.Info($"从配置加载语言偏好: {preferredLanguage}");
                if (LoadLanguageResources(preferredLanguage))
                {
                    _currentLanguage = preferredLanguage;
                    return;
                }
            }

            // 2. 尝试使用系统语言
            var systemLanguage = CultureInfo.CurrentUICulture.Name;
            _logger.Info($"尝试使用系统语言: {systemLanguage}");

            if (LoadLanguageResources(systemLanguage))
            {
                _currentLanguage = systemLanguage;
                SaveLanguagePreference(systemLanguage);
                return;
            }

            // 3. 尝试使用简化的语言代码 (例如: zh-CN -> zh)
            var simplifiedLanguage = systemLanguage.Split('-')[0];
            var availableLanguages = GetAvailableLanguages().ToList();
            var matchedLanguage = availableLanguages.FirstOrDefault(l => l.StartsWith(simplifiedLanguage, StringComparison.OrdinalIgnoreCase));

            if (matchedLanguage != null && LoadLanguageResources(matchedLanguage))
            {
                _currentLanguage = matchedLanguage;
                SaveLanguagePreference(matchedLanguage);
                return;
            }

            // 4. 使用默认语言 (英语)
            _logger.Info("使用默认语言: en-US");
            if (LoadLanguageResources("en-US"))
            {
                _currentLanguage = "en-US";
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"初始化语言失败: {ex.Message}", ex);
            _currentLanguage = "en-US";
        }
    }

    /// <summary>
    /// 初始化语言（同步包装器）
    /// </summary>
    private void InitializeLanguage()
    {
        try
        {
            InitializeLanguageAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.Error($"初始化语言失败: {ex.Message}", ex);
            _currentLanguage = "en-US";
        }
    }

    /// <summary>
    /// 加载语言资源
    /// </summary>
    private bool LoadLanguageResources(string languageCode)
    {
        try
        {
            var resourceFile = Path.Combine(_resourcesDirectory, $"{languageCode}.json");

            if (!File.Exists(resourceFile))
            {
                _logger.Warning($"语言资源文件不存在: {resourceFile}");
                return false;
            }

            var json = File.ReadAllText(resourceFile);
            var resources = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            if (resources == null)
            {
                _logger.Error($"解析语言资源文件失败: {resourceFile}");
                return false;
            }

            _currentResources = resources;
            _logger.Info($"语言资源已加载: {languageCode} ({resources.Count} 条)");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"加载语言资源失败 [{languageCode}]: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// 保存语言偏好到配置
    /// </summary>
    private async void SaveLanguagePreference(string languageCode)
    {
        try
        {
            var config = await _configurationService.LoadConfigurationAsync();
            config.PreferredLanguage = languageCode;
            await _configurationService.SaveConfigurationAsync(config);
            _logger.Debug($"语言偏好已保存: {languageCode}");
        }
        catch (Exception ex)
        {
            _logger.Error($"保存语言偏好失败: {ex.Message}", ex);
        }
    }
}
