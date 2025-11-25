using LizardClient.Core.Models;

namespace LizardClient.Core.Interfaces;

/// <summary>
/// 账户服务接口
/// </summary>
public interface IAccountService
{
    /// <summary>
    /// 获取所有账户
    /// </summary>
    Task<List<PlayerAccount>> GetAccountsAsync();

    /// <summary>
    /// 获取活动账户
    /// </summary>
    Task<PlayerAccount?> GetActiveAccountAsync();

    /// <summary>
    /// 设置活动账户
    /// </summary>
    Task SetActiveAccountAsync(Guid accountId);

    /// <summary>
    /// 添加账户
    /// </summary>
    Task AddAccountAsync(PlayerAccount account);

    /// <summary>
    /// 删除账户
    /// </summary>
    Task RemoveAccountAsync(Guid accountId);

    /// <summary>
    /// 更新账户
    /// </summary>
    Task UpdateAccountAsync(PlayerAccount account);

    /// <summary>
    /// Microsoft 登录
    /// </summary>
    Task<AuthenticationResult> LoginMicrosoftAsync();

    /// <summary>
    /// 创建离线账户
    /// </summary>
    Task<AuthenticationResult> CreateOfflineAccountAsync(string username);

    /// <summary>
    /// 刷新账户 token
    /// </summary>
    Task<bool> RefreshTokenAsync(PlayerAccount account);

    /// <summary>
    /// 验证账户
    /// </summary>
    Task<bool> ValidateAccountAsync(PlayerAccount account);

    /// <summary>
    /// 获取皮肤 URL
    /// </summary>
    Task<string?> GetSkinUrlAsync(PlayerAccount account);
}
