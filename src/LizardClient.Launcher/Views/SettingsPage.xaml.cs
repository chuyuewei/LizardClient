using System.Windows;
using System.Windows.Controls;

namespace LizardClient.Launcher.Views;

/// <summary>
/// SettingsPage.xaml 的交互逻辑
/// </summary>
public partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    private void ShowGeneralSettings(object sender, RoutedEventArgs e)
    {
        HideAllPanels();
        GeneralPanel.Visibility = Visibility.Visible;
        UpdateTabSelection(GeneralTab);
    }

    private void ShowPerformanceSettings(object sender, RoutedEventArgs e)
    {
        HideAllPanels();
        PerformancePanel.Visibility = Visibility.Visible;
        UpdateTabSelection(PerformanceTab);
    }

    private void ShowGameSettings(object sender, RoutedEventArgs e)
    {
        HideAllPanels();
        GamePanel.Visibility = Visibility.Visible;
        UpdateTabSelection(GameTab);
    }

    private void ShowPreferencesSettings(object sender, RoutedEventArgs e)
    {
        HideAllPanels();
        PreferencesPanel.Visibility = Visibility.Visible;
        UpdateTabSelection(PreferencesTab);
    }

    private void HideAllPanels()
    {
        GeneralPanel.Visibility = Visibility.Collapsed;
        PerformancePanel.Visibility = Visibility.Collapsed;
        GamePanel.Visibility = Visibility.Collapsed;
        PreferencesPanel.Visibility = Visibility.Collapsed;
    }

    private void UpdateTabSelection(Button selectedTab)
    {
        // 重置所有标签
        GeneralTab.Opacity = 0.6;
        PerformanceTab.Opacity = 0.6;
        GameTab.Opacity = 0.6;
        PreferencesTab.Opacity = 0.6;

        // 高亮选中的标签
        selectedTab.Opacity = 1.0;
    }
}
