using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using LizardClient.Core.Interfaces;
using LizardClient.Core.Models;

namespace LizardClient.Launcher.ViewModels;

/// <summary>
/// 账户页面 ViewModel
/// </summary>
public class AccountViewModel : INotifyPropertyChanged
{
    private readonly IAccountService _accountService;
    private readonly ILogger _logger;
    private PlayerAccount? _selectedAccount;
    private string _newAccountName = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<PlayerAccount> Accounts { get; } = new();

    public AccountViewModel(IAccountService accountService, ILogger logger)
    {
        _accountService = accountService;
        _logger = logger;

        // 初始化命令
        AddOfflineAccountCommand = new RelayCommand(async () => await AddOfflineAccountAsync());
        LoginMicrosoftCommand = new RelayCommand(async () => await LoginMicrosoftAsync());
        RemoveAccountCommand = new RelayCommand<PlayerAccount>(async (account) => await RemoveAccountAsync(account!));
        SetActiveAccountCommand = new RelayCommand<PlayerAccount>(async (account) => await SetActiveAccountAsync(account!));
        RefreshAccountCommand = new RelayCommand<PlayerAccount>(async (account) => await RefreshAccountAsync(account!));

        // 加载账户
        _ = LoadAccountsAsync();
    }

    #region Properties

    public PlayerAccount? SelectedAccount
    {
        get => _selectedAccount;
        set
        {
            _selectedAccount = value;
            OnPropertyChanged();
        }
    }

    public string NewAccountName
    {
        get => _newAccountName;
        set
        {
            _newAccountName = value;
            OnPropertyChanged();
        }
    }

    public PlayerAccount? ActiveAccount => Accounts.FirstOrDefault(a => a.IsActive);

    #endregion

    #region Commands

    public ICommand AddOfflineAccountCommand { get; }
    public ICommand LoginMicrosoftCommand { get; }
    public ICommand RemoveAccountCommand { get; }
    public ICommand SetActiveAccountCommand { get; }
    public ICommand RefreshAccountCommand { get; }

    #endregion

    #region Methods

    private async Task LoadAccountsAsync()
    {
        try
        {
            var accounts = await _accountService.GetAccountsAsync();
            Accounts.Clear();
            foreach (var account in accounts)
            {
                Accounts.Add(account);
            }
            OnPropertyChanged(nameof(ActiveAccount));
        }
        catch (Exception ex)
        {
            _logger.Error($"加载账户失败: {ex.Message}", ex);
            MessageBox.Show("加载账户失败，请检查日志。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task AddOfflineAccountAsync()
    {
        if (string.IsNullOrWhiteSpace(NewAccountName))
        {
            MessageBox.Show("请输入用户名", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var result = await _accountService.CreateOfflineAccountAsync(NewAccountName);

            if (result.Success && result.Account != null)
            {
                Accounts.Add(result.Account);
                NewAccountName = string.Empty;
                OnPropertyChanged(nameof(ActiveAccount));
                MessageBox.Show($"成功添加离线账户: {result.Account.Username}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(result.ErrorMessage ?? "添加账户失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"添加离线账户失败: {ex.Message}", ex);
            MessageBox.Show("添加账户失败，请检查日志。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoginMicrosoftAsync()
    {
        try
        {
            var result = await _accountService.LoginMicrosoftAsync();

            if (result.Success && result.Account != null)
            {
                Accounts.Add(result.Account);
                OnPropertyChanged(nameof(ActiveAccount));
                MessageBox.Show($"成功登录 Microsoft 账户: {result.Account.Username}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(result.ErrorMessage ?? "Microsoft 登录失败", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Microsoft 登录失败: {ex.Message}", ex);
            MessageBox.Show("Microsoft 登录失败，请检查日志。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task RemoveAccountAsync(PlayerAccount account)
    {
        if (account == null)
            return;

        var result = MessageBox.Show(
            $"确定要删除账户 '{account.Username}' 吗？",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                await _accountService.RemoveAccountAsync(account.Id);
                Accounts.Remove(account);
                OnPropertyChanged(nameof(ActiveAccount));
                MessageBox.Show($"已删除账户: {account.Username}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.Error($"删除账户失败: {ex.Message}", ex);
                MessageBox.Show("删除账户失败，请检查日志。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async Task SetActiveAccountAsync(PlayerAccount account)
    {
        if (account == null)
            return;

        try
        {
            foreach (var acc in Accounts)
            {
                acc.IsActive = false;
            }
            account.IsActive = true;

            await _accountService.SetActiveAccountAsync(account.Id);
            OnPropertyChanged(nameof(ActiveAccount));

            _logger.Info($"切换活动账户: {account.Username}");
        }
        catch (Exception ex)
        {
            _logger.Error($"切换账户失败: {ex.Message}", ex);
            MessageBox.Show("切换账户失败，请检查日志。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task RefreshAccountAsync(PlayerAccount account)
    {
        if (account == null)
            return;

        try
        {
            var success = await _accountService.RefreshTokenAsync(account);

            if (success)
            {
                account.Status = AccountStatus.Valid;
                await _accountService.UpdateAccountAsync(account);
                MessageBox.Show($"账户 {account.Username} 已刷新", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("刷新账户失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"刷新账户失败: {ex.Message}", ex);
            MessageBox.Show("刷新账户失败，请检查日志。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}
