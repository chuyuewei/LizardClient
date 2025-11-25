using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using LizardClient.Core.Interfaces;
using LizardClient.Core.Models;

namespace LizardClient.Launcher.ViewModels;

/// <summary>
/// 下载页面 ViewModel
/// </summary>
public class DownloadViewModel : INotifyPropertyChanged
{
    private readonly IDownloadService _downloadService;
    private readonly ILogger _logger;
    private CancellationTokenSource? _downloadCancellation;

    private string _searchQuery = string.Empty;
    private string _selectedMinecraftVersion = string.Empty;
    private DownloadItemType _selectedCategory = DownloadItemType.MinecraftVersion;
    private DownloadItem? _selectedItem;
    private DownloadProgressInfo? _currentProgress;
    private bool _isDownloading;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<DownloadItem> AvailableItems { get; } = new();
    public ObservableCollection<string> MinecraftVersions { get; } = new();

    public DownloadViewModel(IDownloadService downloadService, ILogger logger)
    {
        _downloadService = downloadService;
        _logger = logger;

        // 初始化命令
        SearchCommand = new RelayCommand(async () => await SearchItemsAsync());
        DownloadCommand = new RelayCommand(async () => await DownloadSelectedItemAsync(), () => SelectedItem != null && !IsDownloading);
        CancelDownloadCommand = new RelayCommand(CancelDownload, () => IsDownloading);
        CategoryChangedCommand = new RelayCommand<string>(async (category) => await OnCategoryChanged(category!));

        // 加载初始数据
        _ = LoadInitialDataAsync();
    }

    #region Properties

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            _searchQuery = value;
            OnPropertyChanged();
        }
    }

    public string SelectedMinecraftVersion
    {
        get => _selectedMinecraftVersion;
        set
        {
            _selectedMinecraftVersion = value;
            OnPropertyChanged();
            _ = LoadItemsForVersionAsync();
        }
    }

    public DownloadItemType SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            _selectedCategory = value;
            OnPropertyChanged();
        }
    }

    public DownloadItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            _selectedItem = value;
            OnPropertyChanged();
            ((RelayCommand)DownloadCommand).RaiseCanExecuteChanged();
        }
    }

    public DownloadProgressInfo? CurrentProgress
    {
        get => _currentProgress;
        set
        {
            _currentProgress = value;
            OnPropertyChanged();
        }
    }

    public bool IsDownloading
    {
        get => _isDownloading;
        set
        {
            _isDownloading = value;
            OnPropertyChanged();
            ((RelayCommand)DownloadCommand).RaiseCanExecuteChanged();
            ((RelayCommand)CancelDownloadCommand).RaiseCanExecuteChanged();
        }
    }

    #endregion

    #region Commands

    public ICommand SearchCommand { get; }
    public ICommand DownloadCommand { get; }
    public ICommand CancelDownloadCommand { get; }
    public ICommand CategoryChangedCommand { get; }

    #endregion

    #region Methods

    private async Task LoadInitialDataAsync()
    {
        try
        {
            // 加载 Minecraft 版本列表
            var versions = await _downloadService.GetMinecraftVersionsAsync();
            MinecraftVersions.Clear();
            foreach (var version in versions)
            {
                MinecraftVersions.Add(version.Version);
            }

            if (MinecraftVersions.Count > 0)
            {
                SelectedMinecraftVersion = MinecraftVersions[0];
            }

            // 加载默认列表
            await LoadMinecraftVersionsAsync();
        }
        catch (Exception ex)
        {
            _logger.Error($"加载初始数据失败: {ex.Message}", ex);
            MessageBox.Show("加载数据失败，请检查网络连接。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task OnCategoryChanged(string category)
    {
        if (Enum.TryParse<DownloadItemType>(category, out var itemType))
        {
            SelectedCategory = itemType;
            await LoadItemsByCategoryAsync();
        }
    }

    private async Task LoadItemsByCategoryAsync()
    {
        try
        {
            AvailableItems.Clear();

            List<DownloadItem> items = SelectedCategory switch
            {
                DownloadItemType.MinecraftVersion => await _downloadService.GetMinecraftVersionsAsync(),
                DownloadItemType.ModLoader => await _downloadService.GetModLoadersAsync(SelectedMinecraftVersion),
                DownloadItemType.Mod => await _downloadService.SearchModsAsync("", SelectedMinecraftVersion),
                _ => new List<DownloadItem>()
            };

            foreach (var item in items)
            {
                AvailableItems.Add(item);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"加载分类项目失败: {ex.Message}", ex);
        }
    }

    private async Task LoadMinecraftVersionsAsync()
    {
        try
        {
            AvailableItems.Clear();
            var versions = await _downloadService.GetMinecraftVersionsAsync();
            foreach (var version in versions)
            {
                AvailableItems.Add(version);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"加载 Minecraft 版本失败: {ex.Message}", ex);
        }
    }

    private async Task LoadItemsForVersionAsync()
    {
        if (string.IsNullOrEmpty(SelectedMinecraftVersion))
            return;

        await LoadItemsByCategoryAsync();
    }

    private async Task SearchItemsAsync()
    {
        try
        {
            if (SelectedCategory == DownloadItemType.Mod)
            {
                AvailableItems.Clear();
                var mods = await _downloadService.SearchModsAsync(SearchQuery, SelectedMinecraftVersion);
                foreach (var mod in mods)
                {
                    AvailableItems.Add(mod);
                }
            }
            else
            {
                await LoadItemsByCategoryAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"搜索失败: {ex.Message}", ex);
        }
    }

    private async Task DownloadSelectedItemAsync()
    {
        if (SelectedItem == null)
            return;

        try
        {
            IsDownloading = true;
            _downloadCancellation = new CancellationTokenSource();

            var progress = new Progress<DownloadProgressInfo>(p =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CurrentProgress = p;
                });
            });

            var success = await _downloadService.DownloadItemAsync(
                SelectedItem,
                progress,
                _downloadCancellation.Token
            );

            if (success)
            {
                // 自动安装
                await _downloadService.InstallItemAsync(SelectedItem);
                MessageBox.Show($"{SelectedItem.Name} 下载并安装成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);

                // 刷新列表
                await LoadItemsByCategoryAsync();
            }
            else if (!_downloadCancellation.Token.IsCancellationRequested)
            {
                MessageBox.Show($"{SelectedItem.Name} 下载失败。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"下载失败: {ex.Message}", ex);
            MessageBox.Show($"下载失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsDownloading = false;
            CurrentProgress = null;
            _downloadCancellation?.Dispose();
            _downloadCancellation = null;
        }
    }

    private void CancelDownload()
    {
        _downloadCancellation?.Cancel();
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}

/// <summary>
/// 支持泛型参数的 RelayCommand
/// </summary>
public class RelayCommand<T> : ICommand
{
    private readonly Action<T> _execute;
    private readonly Func<T, bool>? _canExecute;

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public RelayCommand(Action<T> execute, Func<T, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter)
    {
        if (parameter is T typedParameter)
            return _canExecute?.Invoke(typedParameter) ?? true;
        return false;
    }

    public void Execute(object? parameter)
    {
        if (parameter is T typedParameter)
            _execute(typedParameter);
    }

    public void RaiseCanExecuteChanged()
    {
        CommandManager.InvalidateRequerySuggested();
    }
}
