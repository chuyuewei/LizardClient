using System;
using System.Windows.Input;
using LizardClient.Core.Models;
using LizardClient.ModSystem.Models;
using LizardClient.Launcher.Mvvm;

namespace LizardClient.Launcher.ViewModels;

public class ModViewModel : BindableBase
{
    private readonly ModMetadata _metadata;
    private ModState _state;
    private bool _isEnabled;

    public ModViewModel(ModMetadata metadata, ModState state, bool isEnabled)
    {
        _metadata = metadata;
        _state = state;
        _isEnabled = isEnabled;

        ToggleEnabledCommand = new RelayCommand(ToggleEnabled);
        ConfigureCommand = new RelayCommand(Configure);
        UpdateCommand = new RelayCommand(Update);
        DeleteCommand = new RelayCommand(Delete);
    }

    public string Id => _metadata.Id;
    public string Name => _metadata.Name;
    public string Version => _metadata.Version;
    public string Description => _metadata.Description;
    public string Author => _metadata.Author;
    public string License => _metadata.License;

    public ModState State
    {
        get => _state;
        set => SetProperty(ref _state, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    // Placeholder for icon path
    public string IconPath => "/Assets/Icons/mod_placeholder.png";

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

