using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using LizardClient.Launcher.ViewModels;
using LizardClient.Launcher.Mvvm;

namespace LizardClient.Launcher.Views;

public partial class ModsPage : UserControl, INotifyPropertyChanged
{
    private string _searchText = "";
    private int _filterIndex = 0;
    private bool _isListView = true;
    private ModViewModel? _selectedMod;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ModsPage(ObservableCollection<ModViewModel> mods)
    {
        InitializeComponent();
        DataContext = this;
        Mods = mods;
        FilteredMods = CollectionViewSource.GetDefaultView(Mods);
        FilteredMods.Filter = FilterMod;
    }

    public ObservableCollection<ModViewModel> Mods { get; }
    public ICollectionView FilteredMods { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SearchText)));
                FilteredMods.Refresh();
            }
        }
    }

    public int FilterIndex
    {
        get => _filterIndex;
        set
        {
            if (_filterIndex != value)
            {
                _filterIndex = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FilterIndex)));
                FilteredMods.Refresh();
            }
        }
    }

    public bool IsListView
    {
        get => _isListView;
        set
        {
            if (_isListView != value)
            {
                _isListView = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsListView)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsGridView)));
            }
        }
    }

    public bool IsGridView => !IsListView;

    public ModViewModel? SelectedMod
    {
        get => _selectedMod;
        set
        {
            if (_selectedMod != value)
            {
                _selectedMod = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedMod)));
            }
        }
    }

    private bool FilterMod(object item)
    {
        if (item is not ModViewModel mod) return false;

        // Search filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            if (!mod.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) &&
                !mod.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Status filter
        switch (FilterIndex)
        {
            case 1: // Enabled
                if (!mod.IsEnabled) return false;
                break;
            case 2: // Disabled
                if (mod.IsEnabled) return false;
                break;
        }

        return true;
    }
}
