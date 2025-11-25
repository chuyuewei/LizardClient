using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using LizardClient.Core.Interfaces;
using LizardClient.Core.Models;

namespace LizardClient.Core.Services;

/// <summary>
/// 账户服务实现
/// </summary>
public class AccountService : IAccountService
{
    private readonly ILogger _logger;
    private readonly string _accountsFilePath;
    private readonly HttpClient _httpClient;
    private List<PlayerAccount> _accounts = new();

    // Microsoft OAuth 配置
    private const string ClientId = "00000000-0000-0000-0000-000000000000"; // 演示用，实际需要真实的 Client ID
    private const string RedirectUri = "http://localhost:8080/auth";
    private const string AuthEndpoint = "https://login.microsoftonline.com/consumers/oauth2/v2.0/authorize";
    private const string TokenEndpoint = "https://login.microsoftonline.com/consumers/oauth2/v2.0/token";

    public AccountService(ILogger logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();

        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LizardClient"
        );
        Directory.CreateDirectory(appDataPath);
        _accountsFilePath = Path.Combine(appDataPath, "accounts.json");

        _ = LoadAccountsAsync();
    }

    public async Task<List<PlayerAccount>> GetAccountsAsync()
    {
        return _accounts.ToList();
    }

    public async Task<PlayerAccount?> GetActiveAccountAsync()
    {
        return _accounts.FirstOrDefault(a => a.IsActive);
    }

    public async Task SetActiveAccountAsync(Guid accountId)
    {
        foreach (var account in _accounts)
        {
            account.IsActive = account.Id == accountId;
            if (account.IsActive)
            {
                account.LastUsed = DateTime.Now;
            }
        }
        await SaveAccountsAsync();
    }

    public async Task AddAccountAsync(PlayerAccount account)
    {
        // 如果是第一个账户，设为活动账户
        if (_accounts.Count == 0)
        {
            account.IsActive = true;
        }

        _accounts.Add(account);
        await SaveAccountsAsync();
        _logger.Info($"添加账户: {account.Username}");
    }

    public async Task RemoveAccountAsync(Guid accountId)
    {
        var account = _accounts.FirstOrDefault(a => a.Id == accountId);
        if (account != null)
        {
            _accounts.Remove(account);

            // 如果删除的是活动账户，选择另一个账户作为活动账户
            if (account.IsActive && _accounts.Count > 0)
            {
                _accounts[0].IsActive = true;
            }

            await SaveAccountsAsync();
            _logger.Info($"删除账户: {account.Username}");
        }
    }

    public async Task UpdateAccountAsync(PlayerAccount account)
    {
        var existingAccount = _accounts.FirstOrDefault(a => a.Id == account.Id);
        if (existingAccount != null)
        {
            var index = _accounts.IndexOf(existingAccount);
            _accounts[index] = account;
            await SaveAccountsAsync();
        }
    }

    public async Task<AuthenticationResult> LoginMicrosoftAsync()
    {
        try
        {
            _logger.Info("开始 Microsoft 登录流程");

            // 注意：这是一个简化的演示实现
            // 实际应用中需要实现完整的 OAuth 流程，包括：
            // 1. 打开浏览器到授权页面
            // 2. 启动本地服务器监听回调
            // 3. 获取授权码
            // 4. 交换 access token
            // 5. 使用 Xbox Live 和 Minecraft API 获取玩家信息

            return new AuthenticationResult
            {
                Success = false,
                ErrorMessage = "Microsoft 登录功能需要配置 OAuth Client ID。\n\n这是一个框架演示，实际使用时需要：\n1. 在 Azure 注册应用\n2. 配置 OAuth 回调\n3. 实现完整的认证流程"
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Microsoft 登录失败: {ex.Message}", ex);
            return new AuthenticationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<AuthenticationResult> CreateOfflineAccountAsync(string username)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = "用户名不能为空"
                };
            }

            // 检查用户名是否已存在
            if (_accounts.Any(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = "该用户名已存在"
                };
            }

            var account = new PlayerAccount
            {
                Username = username,
                Uuid = Guid.NewGuid().ToString("N"),
                Type = AccountType.Offline,
                Status = AccountStatus.Valid,
                IsActive = _accounts.Count == 0,
                SkinUrl = $"https://crafatar.com/avatars/steve?size=128&default=MHF_Steve&overlay"
            };

            await AddAccountAsync(account);

            _logger.Info($"创建离线账户: {username}");

            return new AuthenticationResult
            {
                Success = true,
                Account = account
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"创建离线账户失败: {ex.Message}", ex);
            return new AuthenticationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<bool> RefreshTokenAsync(PlayerAccount account)
    {
        if (account.Type == AccountType.Offline)
            return true;

        try
        {
            // 实际应用中需要调用 Microsoft API 刷新 token
            _logger.Info($"刷新 token: {account.Username}");
            return false; // 演示实现
        }
        catch (Exception ex)
        {
            _logger.Error($"刷新 token 失败: {ex.Message}", ex);
            return false;
        }
    }

    public async Task<bool> ValidateAccountAsync(PlayerAccount account)
    {
        if (account.Type == AccountType.Offline)
            return true;

        return account.IsTokenValid();
    }

    public async Task<string?> GetSkinUrlAsync(PlayerAccount account)
    {
        if (!string.IsNullOrEmpty(account.SkinUrl))
            return account.SkinUrl;

        // 默认皮肤
        return "https://crafatar.com/avatars/steve?size=128&default=MHF_Steve&overlay";
    }

    private async Task LoadAccountsAsync()
    {
        try
        {
            if (File.Exists(_accountsFilePath))
            {
                var json = await File.ReadAllTextAsync(_accountsFilePath);
                var accounts = JsonSerializer.Deserialize<List<PlayerAccount>>(json);
                if (accounts != null)
                {
                    _accounts = accounts;
                    _logger.Info($"加载了 {_accounts.Count} 个账户");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"加载账户失败: {ex.Message}", ex);
        }
    }

    private async Task SaveAccountsAsync()
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(_accounts, options);
            await File.WriteAllTextAsync(_accountsFilePath, json);
            _logger.Info("账户已保存");
        }
        catch (Exception ex)
        {
            _logger.Error($"保存账户失败: {ex.Message}", ex);
        }
    }
}
