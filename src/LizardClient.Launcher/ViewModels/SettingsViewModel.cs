using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using LizardClient.Core.Interfaces;
using LizardClient.Core.Models;
using Microsoft.Win32;

namespace LizardClient.Launcher.ViewModels;

/// <summary>
/// 设置页面 ViewModel
/// </summary>
public class SettingsViewModel : INotifyPropertyChanged
{
    private readonly IConfigurationService _configService;
    private readonly ILogger _logger;
    private ClientConfiguration _config;

    public event PropertyChangedEventHandler? PropertyChanged;

    public SettingsViewModel(IConfigurationService configService, ILogger logger)
    {
        _configService = configService;
        _logger = logger;
        _config = new ClientConfiguration();

        // 初始化命令
        SaveCommand = new RelayCommand(async () => await SaveSettingsAsync());
        ResetCommand = new RelayCommand(async () => await ResetSettingsAsync());
        BrowseJavaPathCommand = new RelayCommand(BrowseJavaPath);
        BrowseGameDirectoryCommand = new RelayCommand(BrowseGameDirectory);

        // 加载配置
        _ = LoadSettingsAsync();
    }

    #region Properties

    // 语言设置
    public string Language
    {
        get => _config.PreferredLanguage;
        set
        {
            _config.PreferredLanguage = value;
            OnPropertyChanged();
        }
    }

    // 主题设置
    public string Theme
    {
        get => _config.Theme;
        set
        {
            _config.Theme = value;
            OnPropertyChanged();
        }
    }

    // 自动更新
    public bool EnableAutoUpdate
    {
        get => _config.EnableAutoUpdate;
        set
        {
            _config.EnableAutoUpdate = value;
            OnPropertyChanged();
        }
    }

    // 硬件加速
    public bool EnableHardwareAcceleration
    {
        get => _config.EnableHardwareAcceleration;
        set
        {
            _config.EnableHardwareAcceleration = value;
            OnPropertyChanged();
        }
    }

    // FPS 提升等级
    public int FpsBoostLevel
    {
        get => _config.Performance.FpsBoostLevel;
        set
        {
            _config.Performance.FpsBoostLevel = value;
            OnPropertyChanged();
        }
    }

    // 内存优化等级
    public int MemoryOptimizationLevel
    {
        get => _config.Performance.MemoryOptimizationLevel;
        set
        {
            _config.Performance.MemoryOptimizationLevel = value;
            OnPropertyChanged();
        }
    }

    // 快速加载
    public bool EnableFastLoading
    {
        get => _config.Performance.EnableFastLoading;
        set
        {
            _config.Performance.EnableFastLoading = value;
            OnPropertyChanged();
        }
    }

    // 最大内存
    public int MaxMemoryMB
    {
        get => _config.Performance.MaxMemoryMB;
        set
        {
            _config.Performance.MaxMemoryMB = value;
            OnPropertyChanged();
        }
    }

    // Java 路径
    public string? JavaPath
    {
        get => _config.JavaPath;
        set
        {
            _config.JavaPath = value;
            OnPropertyChanged();
        }
    }

    // 游戏目录
    public string GameRootDirectory
    {
        get => _config.GameRootDirectory;
        set
        {
            _config.GameRootDirectory = value;
            OnPropertyChanged();
        }
    }

    // 最小化到托盘
    public bool MinimizeToTray
    {
        get => _config.Preferences.MinimizeToTray;
        set
        {
            _config.Preferences.MinimizeToTray = value;
            OnPropertyChanged();
        }
    }

    // 随系统启动
    public bool StartWithSystem
    {
        get => _config.Preferences.StartWithSystem;
        set
        {
            _config.Preferences.StartWithSystem = value;
            OnPropertyChanged();
        }
    }

    // 游戏启动后关闭启动器
    public bool CloseAfterGameLaunch
    {
        get => _config.Preferences.CloseAfterGameLaunch;
        set
        {
            _config.Preferences.CloseAfterGameLaunch = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Commands

    public ICommand SaveCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand BrowseJavaPathCommand { get; }
    public ICommand BrowseGameDirectoryCommand { get; }

    #endregion

    #region Methods

    private async Task LoadSettingsAsync()
    {
        try
        {
            _config = await _configService.LoadConfigurationAsync();
            OnPropertyChanged(string.Empty); // 刷新所有属性
            _logger.Info("设置已加载");
        }
        catch (Exception ex)
        {
            _logger.Error($"加载设置失败: {ex.Message}", ex);
            MessageBox.Show("加载设置失败，请检查日志。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task SaveSettingsAsync()
    {
        try
        {
            if (!_configService.ValidateConfiguration(_config))
            {
                MessageBox.Show("配置验证失败，请检查输入值。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            await _configService.SaveConfigurationAsync(_config);
            _logger.Info("设置已保存");
            MessageBox.Show("设置已保存！\n\n某些设置可能需要重启应用才能生效。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _logger.Error($"保存设置失败: {ex.Message}", ex);
            MessageBox.Show("保存设置失败，请检查日志。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ResetSettingsAsync()
    {
        try
        {
            var result = MessageBox.Show(
                "确定要重置所有设置为默认值吗？",
                "确认",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await _configService.ResetToDefaultAsync();
                await LoadSettingsAsync();
                MessageBox.Show("设置已重置为默认值。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"重置设置失败: {ex.Message}", ex);
            MessageBox.Show("重置设置失败，请检查日志。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BrowseJavaPath()
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择 Java 可执行文件",
            Filter = "Java 可执行文件|java.exe;javaw.exe|所有文件|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            JavaPath = dialog.FileName;
        }
    }

    private void BrowseGameDirectory()
    {
        // 使用 OpenFileDialog 作为替代方案
        var dialog = new OpenFileDialog
        {
            Title = "选择游戏目录（选择目录中的任意文件）",
            CheckFileExists = false,
            CheckPathExists = true
        };

        if (dialog.ShowDialog() == true)
        {
            var directory = Path.GetDirectoryName(dialog.FileName);
            if (!string.IsNullOrEmpty(directory))
            {
                GameRootDirectory = directory;
            }
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}
