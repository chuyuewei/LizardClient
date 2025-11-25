using System;
using System.Windows.Input;
using LizardClient.Core.Models;
using LizardClient.Launcher.Mvvm;

namespace LizardClient.Launcher.ViewModels;

public class ModViewModel : BindableBase
{
    private readonly ModInfo _info;
    private bool _isEnabled;

    public ModViewModel(ModInfo info, bool isEnabled)
    {
        _info = info ?? throw new ArgumentNullException(nameof(info));
        _isEnabled = isEnabled;

        ToggleEnabledCommand = new RelayCommand(ToggleEnabled);
        ConfigureCommand = new RelayCommand(Configure);
        UpdateCommand = new RelayCommand(Update);
        DeleteCommand = new RelayCommand(Delete);
    }

    public string Id => _info.Id;
    public string Name => _info.Name;
    public string Version => _info.Version;
    public string Description => _info.Description;
    public string Author => _info.Author;
    public string License => ""; // ModInfo doesn't have License, defaulting to empty

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    // Placeholder for icon path
    public string IconPath => _info.IconPath ?? "/Assets/Icons/mod_placeholder.png";

    public ICommand ToggleEnabledCommand { get; }
    public ICommand ConfigureCommand { get; }
    public ICommand UpdateCommand { get; }
    public ICommand DeleteCommand { get; }

    private void ToggleEnabled()
    {
        IsEnabled = !IsEnabled;
        // Logic to actually enable/disable mod in ModLoader would go here
    }

    private void Configure()
    {
        // Open configuration dialog
    }

    private void Update()
    {
        // Check for updates
    }

    private void Delete()
    {
        // Delete mod
    }
}
