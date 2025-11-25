# MainWindow.xaml.cs 完整实现指南

由于自动编辑工具出现问题，请手动复制以下代码到 `MainWindow.xaml.cs`：

## 完整代码：

```csharp
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LizardClient.Core.Interfaces;
using LizardClient.Core.Models;
using LizardClient.Core.Services;
using LizardClient.Game.Minecraft;
using LizardClient.Injection;
using LizardClient.ModSystem.Loader;
using LizardClient.Launcher.Views;
using LizardClient.Launcher.ViewModels;

namespace LizardClient.Launcher;

/// <summary>
/// MainWindow 的交互逻辑
/// </summary>
public partial class MainWindow : Window
{
    private readonly ILogger _logger;
    private readonly ModLoader _modLoader;
    private readonly GameLauncher _gameLauncher;
    private InjectionManager? _injectionManager;
    private GameProfile? _currentProfile;
    private ModsPage? _modsPage;
    private SettingsPage? _settingsPage;
    private DownloadPage? _downloadPage;
    private AccountPage? _accountPage;

    public MainWindow()
    {
        InitializeComponent();

        // 初始化服务
        _logger = new SerilogLogger();
        _modLoader = new ModLoader(_logger);
        _gameLauncher = new GameLauncher(_logger, GetDefaultMinecraftDirectory());

        // 加载模组
        _modLoader.LoadModsFromAssembly();

        // 初始化默认配置
        InitializeDefaultProfile();

        _logger.Info("LizardClient 启动器已初始化");
        _logger.Info($"已加载 {_modLoader.LoadedMods.Count} 个模组");
    }

    /// <summary>
    /// 初始化默认配置
    /// </summary>
    private void InitializeDefaultProfile()
    {
        _currentProfile = new GameProfile
        {
            Name = "Default Profile",
            PlayerName = "Player123",
            MinecraftVersion = "1.8.9",
            MaxMemoryMB = 4096,
            GameDirectory = GetDefaultMinecraftDirectory(),
            IsDefault = true
        };
    }

    /// <summary>
    /// 获取默认 Minecraft 目录
    /// </summary>
    private string GetDefaultMinecraftDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, ".minecraft");
    }

    private void NavigateHome(object sender, RoutedEventArgs e)
    {
        _logger.Info("导航到主页");
        if (HomeView != null) HomeView.Visibility = Visibility.Visible;
        if (ModsContainer != null) ModsContainer.Visibility = Visibility.Collapsed;
        if (SettingsContainer != null) SettingsContainer.Visibility = Visibility.Collapsed;
        if (DownloadContainer != null) DownloadContainer.Visibility = Visibility.Collapsed;
        if (AccountContainer != null) AccountContainer.Visibility = Visibility.Collapsed;
    }

    private void NavigateMods(object sender, RoutedEventArgs e)
    {
        _logger.Info("导航到模组页面");

        if (_modsPage == null)
        {
            var modViewModels = new ObservableCollection<ModViewModel>(
                _modLoader.LoadedMods.Values.Select(m => new ModViewModel(m.Info, m.IsEnabled))
            );
            _modsPage = new ModsPage(modViewModels);
            ModsContainer.Content = _modsPage;
        }

        if (HomeView != null) HomeView.Visibility = Visibility.Collapsed;
        if (ModsContainer != null) ModsContainer.Visibility = Visibility.Visible;
        if (SettingsContainer != null) SettingsContainer.Visibility = Visibility.Collapsed;
        if (DownloadContainer != null) DownloadContainer.Visibility = Visibility.Collapsed;
        if (AccountContainer != null) AccountContainer.Visibility = Visibility.Collapsed;
    }

    private void NavigateSettings(object sender, RoutedEventArgs e)
    {
        _logger.Info("导航到设置页面");

        if (_settingsPage == null)
        {
            var configService = new JsonConfigurationService(_logger);
            var viewModel = new SettingsViewModel(configService, _logger);
            _settingsPage = new SettingsPage
            {
                DataContext = viewModel
            };
            SettingsContainer.Content = _settingsPage;
        }

        if (HomeView != null) HomeView.Visibility = Visibility.Collapsed;
        if (ModsContainer != null) ModsContainer.Visibility = Visibility.Collapsed;
        if (SettingsContainer != null) SettingsContainer.Visibility = Visibility.Visible;
        if (DownloadContainer != null) DownloadContainer.Visibility = Visibility.Collapsed;
        if (AccountContainer != null) AccountContainer.Visibility = Visibility.Collapsed;
    }

    private void NavigateDownload(object sender, RoutedEventArgs e)
    {
        _logger.Info("导航到下载页面");

        if (_downloadPage == null)
        {
            var downloadService = new MockDownloadService(_logger);
            var viewModel = new DownloadViewModel(downloadService, _logger);
            _downloadPage = new DownloadPage
            {
                DataContext = viewModel
            };
            DownloadContainer.Content = _downloadPage;
        }

        if (HomeView != null) HomeView.Visibility = Visibility.Collapsed;
        if (ModsContainer != null) ModsContainer.Visibility = Visibility.Collapsed;
        if (SettingsContainer != null) SettingsContainer.Visibility = Visibility.Collapsed;
        if (DownloadContainer != null) DownloadContainer.Visibility = Visibility.Visible;
        if (AccountContainer != null) AccountContainer.Visibility = Visibility.Collapsed;
    }

    private void NavigateAccount(object sender, RoutedEventArgs e)
    {
        _logger.Info("导航到账户页面");

        if (_accountPage == null)
        {
            var accountService = new AccountService(_logger);
            var viewModel = new AccountViewModel(accountService, _logger);
            _accountPage = new AccountPage
            {
                DataContext = viewModel
            };
            AccountContainer.Content = _accountPage;
        }

        if (HomeView != null) HomeView.Visibility = Visibility.Collapsed;
        if (ModsContainer != null) ModsContainer.Visibility = Visibility.Collapsed;
        if (SettingsContainer != null) SettingsContainer.Visibility = Visibility.Collapsed;
        if (DownloadContainer != null) DownloadContainer.Visibility = Visibility.Collapsed;
        if (AccountContainer != null) AccountContainer.Visibility = Visibility.Visible;
    }

    // 保留原有的 LaunchGame, InjectDllsAsync, MonitorGameProcessAsync, OnClosed 方法
    // （从原文件复制过来）
}
```

## 说明：

1. **已添加字段**：`_settingsPage`, `_downloadPage`, `_accountPage`
2. **已更新方法**：所有导航方法现在都会隐藏/显示所有容器
3. **新增方法**：`NavigateDownload` 和 `NavigateAccount`

请手动将此代码复制到 MainWindow.xaml.cs，并保留文件中原有的 `LaunchGame`、`InjectDllsAsync`、`MonitorGameProcessAsync` 和 `OnClosed` 等方法。
