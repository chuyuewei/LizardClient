using System;
using System.IO;
using System.Linq;
using System.Windows;
using LizardClient.Core.Interfaces;
using LizardClient.Core.Models;
using LizardClient.Core.Services;
using LizardClient.Game.Minecraft;
using LizardClient.Injection;
using LizardClient.ModSystem.Loader;

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
    }

    private void NavigateMods(object sender, RoutedEventArgs e)
    {
        _logger.Info("导航到模组页面");

        var modsInfo = string.Join("\n\n", _modLoader.LoadedMods.Values.Select(m =>
        {
            var status = m.IsEnabled ? "✓ 已启用" : "✗ 已禁用";
            var deps = m.Info.Dependencies.Any()
                ? $"\n   依赖: {string.Join(", ", m.Info.Dependencies)}"
                : "";
            return $"• {m.Info.Name} v{m.Info.Version} ({status}){deps}\n   {m.Info.Description}";
        }));

        MessageBox.Show($"已加载 {_modLoader.LoadedMods.Count} 个模组:\n\n{modsInfo}",
            "模组管理", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void NavigateSettings(object sender, RoutedEventArgs e)
    {
        _logger.Info("导航到设置页面");
        MessageBox.Show("设置功能开发中...", "LizardClient", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void LaunchGame(object sender, RoutedEventArgs e)
    {
        if (_currentProfile == null)
        {
            MessageBox.Show("配置文件未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            _logger.Info("准备启动游戏");

            // 获取选中的版本
            var selectedItem = VersionComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem;
            var selectedVersion = selectedItem?.Content?.ToString() ?? "1.8.9";

            // 解析版本号（去掉描述）
            if (selectedVersion.Contains("("))
            {
                selectedVersion = selectedVersion.Substring(0, selectedVersion.IndexOf("(")).Trim();
            }

            _currentProfile.MinecraftVersion = selectedVersion;
            _logger.Info($"选择版本: {selectedVersion}");

            // 禁用启动按钮
            var launchButton = sender as System.Windows.Controls.Button;
            if (launchButton != null)
            {
                launchButton.IsEnabled = false;
                launchButton.Content = "启动中...";
            }

            // 创建进度处理器
            var progress = new Progress<LaunchProgress>(p =>
            {
                Dispatcher.Invoke(() =>
                {
                    _logger.Info($"[启动进度] {p.Status}: {p.Message} ({p.Percentage}%)");

                    // 更新状态显示
                    if (launchButton != null)
                    {
                        launchButton.Content = $"{p.Message} ({p.Percentage}%)";
                    }
                });
            });

            // 启动游戏
            var success = await _gameLauncher.LaunchGameAsync(_currentProfile, progress);

            if (success)
            {
                _logger.Info("游戏启动成功！");

                // 如果需要注入
                if (MessageBoxResult.Yes == MessageBox.Show(
                    "游戏已启动！是否要注入 DLL？\n\n注意：这仅用于测试目的。",
                    "LizardClient",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question))
                {
                    await InjectDllsAsync();
                }

                if (launchButton != null)
                {
                    launchButton.Content = "游戏运行中";
                }

                // 监控游戏进程
                _ = MonitorGameProcessAsync(launchButton);
            }
            else
            {
                MessageBox.Show(
                    "游戏启动失败！\n\n请检查:\n" +
                    $"1. Minecraft {selectedVersion} 是否已安装\n" +
                    "2. Java 是否正确安装\n" +
                    "3. 查看日志获取详细错误信息",
                    "启动失败",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                if (launchButton != null)
                {
                    launchButton.IsEnabled = true;
                    launchButton.Content = "启动游戏";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"启动游戏时发生异常: {ex.Message}", ex);
            MessageBox.Show($"启动失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);

            var launchButton = sender as System.Windows.Controls.Button;
            if (launchButton != null)
            {
                launchButton.IsEnabled = true;
                launchButton.Content = "启动游戏";
            }
        }
    }

    /// <summary>
    /// 注入 DLL
    /// </summary>
    private async Task InjectDllsAsync()
    {
        try
        {
            if (_gameLauncher.CurrentProcess == null || !_gameLauncher.IsGameRunning)
            {
                MessageBox.Show("游戏未运行", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 创建注入配置
            var injectionConfig = InjectionConfig.CreateDefault();

            // 注意：这里需要指定实际的 DLL 路径
            if (injectionConfig.DllPaths.Count == 0)
            {
                MessageBox.Show(
                    "未配置要注入的 DLL 文件。\n\n" +
                    "这是一个框架演示，实际使用时需要指定 DLL 路径。",
                    "注入",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            _injectionManager = new InjectionManager(_logger, injectionConfig);

            var progress = new Progress<InjectionProgress>(p =>
            {
                Dispatcher.Invoke(() =>
                {
                    _logger.Info($"[注入进度] {p.Status}: {p.Message}");
                });
            });

            var processId = _gameLauncher.CurrentProcess.ProcessId;
            var success = await _injectionManager.InjectAsync(processId, progress);

            if (success)
            {
                MessageBox.Show("DLL 注入成功！", "LizardClient", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("DLL 注入失败，请查看日志了解详情。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"注入 DLL 时发生异常: {ex.Message}", ex);
            MessageBox.Show($"注入失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 监控游戏进程
    /// </summary>
    private async Task MonitorGameProcessAsync(System.Windows.Controls.Button? launchButton)
    {
        try
        {
            if (_gameLauncher.CurrentProcess != null)
            {
                await _gameLauncher.WaitForGameExitAsync();

                Dispatcher.Invoke(() =>
                {
                    _logger.Info("游戏已退出");
                    if (launchButton != null)
                    {
                        launchButton.IsEnabled = true;
                        launchButton.Content = "启动游戏";
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"监控游戏进程时发生异常: {ex.Message}", ex);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _logger.Info("LizardClient 启动器正在关闭");

        // 停止游戏（如果正在运行）
        if (_gameLauncher.IsGameRunning)
        {
            _gameLauncher.StopGame();
        }

        // 清理资源
        _modLoader.UnloadAllMods();
        _gameLauncher.Dispose();
        _injectionManager?.Dispose();

        base.OnClosed(e);
    }
}