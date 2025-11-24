using System.Windows;
using LizardClient.Core.Interfaces;
using LizardClient.Core.Services;
using LizardClient.ModSystem.Loader;

namespace LizardClient.Launcher;

/// <summary>
/// MainWindow 的交互逻辑
/// </summary>
public partial class MainWindow : Window
{
    private readonly ILogger _logger;
    private readonly ModLoader _modLoader;

    public MainWindow()
    {
        InitializeComponent();

        // 初始化服务
        _logger = new SerilogLogger();
        _modLoader = new ModLoader(_logger);

        // 加载模组
        _modLoader.LoadModsFromAssembly();

        _logger.Info("LizardClient 启动器已初始化");
        _logger.Info($"已加载 {_modLoader.LoadedMods.Count} 个模组");
    }

    private void NavigateHome(object sender, RoutedEventArgs e)
    {
        _logger.Info("导航到主页");
        MessageBox.Show("主页功能开发中...", "LizardClient", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void NavigateMods(object sender, RoutedEventArgs e)
    {
        _logger.Info("导航到模组页面");
        
        var modsInfo = string.Join("\n", _modLoader.LoadedMods.Values.Select(m => 
            $"• {m.Info.Name} v{m.Info.Version} - {(m.IsEnabled ? "✓ 已启用" : "✗ 已禁用")}"));
        
        MessageBox.Show($"已加载的模组:\n\n{modsInfo}", 
            "模组管理", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void NavigateSettings(object sender, RoutedEventArgs e)
    {
        _logger.Info("导航到设置页面");
        MessageBox.Show("设置功能开发中...", "LizardClient", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void LaunchGame(object sender, RoutedEventArgs e)
    {
        _logger.Info("准备启动游戏");

        var selectedVersion = ((System.Windows.Controls.ComboBoxItem)VersionComboBox.SelectedItem).Content.ToString();
        
        MessageBox.Show($"正在启动 {selectedVersion}...\n\n" +
            $"功能说明:\n" +
            $"• 已加载 {_modLoader.LoadedMods.Count} 个模组\n" +
            $"• FPS 提升已启用\n" +
            $"• 内存优化已启用\n\n" +
            $"实际的游戏启动功能需要进一步实现 MinecraftProcess 和注入系统。",
            "启动游戏", MessageBoxButton.OK, MessageBoxImage.Information);

        _logger.Info($"用户请求启动 {selectedVersion}");
    }

    protected override void OnClosed(EventArgs e)
    {
        _logger.Info("LizardClient 启动器正在关闭");
        _modLoader.UnloadAllMods();
        base.OnClosed(e);
    }
}