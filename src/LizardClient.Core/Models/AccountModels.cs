namespace LizardClient.Core.Models;

/// <summary>
/// 账户类型
/// </summary>
public enum AccountType
{
    /// <summary>
    /// 离线账户
    /// </summary>
    Offline,

    /// <summary>
    /// Microsoft 账户
    /// </summary>
    Microsoft,

    /// <summary>
    /// Mojang 账户（旧版）
    /// </summary>
    Mojang
}

/// <summary>
/// 账户状态
/// </summary>
public enum AccountStatus
{
    /// <summary>
    /// 有效
    /// </summary>
    Valid,

    /// <summary>
    /// 需要刷新
    /// </summary>
    NeedsRefresh,

    /// <summary>
    /// 已过期
    /// </summary>
    Expired,

    /// <summary>
    /// 错误
    /// </summary>
    Error
}

/// <summary>
/// 玩家账户
/// </summary>
public class PlayerAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string Uuid { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public AccountStatus Status { get; set; } = AccountStatus.Valid;
    public bool IsActive { get; set; }

    // 认证信息
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? TokenExpiry { get; set; }

    // 皮肤信息
    public string? SkinUrl { get; set; }
    public bool IsSlimSkin { get; set; }

    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime LastUsed { get; set; } = DateTime.Now;

    /// <summary>
    /// 检查 token 是否有效
    /// </summary>
    public bool IsTokenValid()
    {
        if (Type == AccountType.Offline)
            return true;

        if (string.IsNullOrEmpty(AccessToken))
            return false;

        if (TokenExpiry.HasValue && TokenExpiry.Value <= DateTime.Now)
            return false;

        return true;
    }
}

/// <summary>
/// 认证结果
/// </summary>
public class AuthenticationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public PlayerAccount? Account { get; set; }
}

/// <summary>
/// Microsoft OAuth 响应
/// </summary>
public class MicrosoftAuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = string.Empty;
}

/// <summary>
/// Xbox Live 认证响应
/// </summary>
public class XboxLiveAuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string UserHash { get; set; } = string.Empty;
}

/// <summary>
/// Minecraft 认证响应
/// </summary>
public class MinecraftAuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public MinecraftProfile? Profile { get; set; }
}

/// <summary>
/// Minecraft 玩家资料
/// </summary>
public class MinecraftProfile
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<MinecraftSkin>? Skins { get; set; }
}

/// <summary>
/// Minecraft 皮肤
/// </summary>
public class MinecraftSkin
{
    public string Url { get; set; } = string.Empty;
    public string Variant { get; set; } = "classic"; // classic or slim
}
