using LizardClient.Core.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace LizardClient.Game.Authentication;

/// <summary>
/// 认证类型
/// </summary>
public enum AuthenticationType
{
    Offline,      // 离线模式
    Mojang,       // Mojang 账户
    Microsoft     // 微软账户
}

/// <summary>
/// 用户账户信息
/// </summary>
public sealed class UserAccount
{
    public string Username { get; set; } = string.Empty;
    public string UUID { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public AuthenticationType Type { get; set; } = AuthenticationType.Offline;
    public DateTime TokenExpiry { get; set; }
    public bool IsTokenValid => DateTime.UtcNow < TokenExpiry;
}

/// <summary>
/// 认证服务
/// </summary>
public sealed class AuthenticationService
{
    private readonly ILogger _logger;
    private const string MOJANG_AUTH_URL = "https://authserver.mojang.com";
    private const string MICROSOFT_AUTH_URL = "https://login.microsoftonline.com/consumers/oauth2/v2.0";
    
    private UserAccount? _currentAccount;

    public AuthenticationService(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 当前账户
    /// </summary>
    public UserAccount? CurrentAccount => _currentAccount;

    /// <summary>
    /// 离线登录
    /// </summary>
    public UserAccount LoginOffline(string username)
    {
        _logger.Info($"离线登录: {username}");

        _currentAccount = new UserAccount
        {
            Username = username,
            UUID = GenerateOfflineUUID(username),
            AccessToken = "0",
            Type = AuthenticationType.Offline,
            TokenExpiry = DateTime.MaxValue
        };

        return _currentAccount;
    }

    /// <summary>
    /// Mojang 账户登录
    /// </summary>
    public async Task<UserAccount?> LoginMojangAsync(string email, string password)
    {
        try
        {
            _logger.Info($"Mojang 登录: {email}");

            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(MOJANG_AUTH_URL);

            var requestData = new
            {
                agent = new
                {
                    name = "Minecraft",
                    version = 1
                },
                username = email,
                password = password,
                clientToken = Guid.NewGuid().ToString("N"),
                requestUser = true
            };

            var response = await httpClient.PostAsJsonAsync("/authenticate", requestData);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.Error($"Mojang 认证失败: {error}");
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<JObject>();
            if (result == null) return null;

            _currentAccount = new UserAccount
            {
                Username = result["selectedProfile"]?["name"]?.ToString() ?? email,
                UUID = result["selectedProfile"]?["id"]?.ToString() ?? string.Empty,
                AccessToken = result["accessToken"]?.ToString() ?? string.Empty,
                Type = AuthenticationType.Mojang,
                TokenExpiry = DateTime.UtcNow.AddHours(24)
            };

            _logger.Info($"Mojang 登录成功: {_currentAccount.Username}");
            return _currentAccount;
        }
        catch (Exception ex)
        {
            _logger.Error($"Mojang 登录失败: {ex.Message}", ex);
            return null;
        }
    }

    /// <summary>
    /// 微软账户登录 (OAuth 2.0)
    /// </summary>
    public async Task<UserAccount?> LoginMicrosoftAsync(string authorizationCode)
    {
        try
        {
            _logger.Info("微软账户登录...");

            // 第一步：获取 Microsoft Access Token
            var msToken = await GetMicrosoftAccessTokenAsync(authorizationCode);
            if (string.IsNullOrEmpty(msToken)) return null;

            // 第二步：使用 Microsoft Token 获取 Xbox Live Token
            var xblToken = await AuthenticateWithXboxLiveAsync(msToken);
            if (string.IsNullOrEmpty(xblToken)) return null;

            // 第三步：使用 XBL Token 获取 XSTS Token
            var xstsToken = await GetXSTSTokenAsync(xblToken);
            if (string.IsNullOrEmpty(xstsToken)) return null;

            // 第四步：使用 XSTS Token 获取 Minecraft Access Token
            var mcToken = await GetMinecraftTokenAsync(xstsToken);
            if (string.IsNullOrEmpty(mcToken)) return null;

            // 第五步：获取玩家信息
            var profile = await GetMinecraftProfileAsync(mcToken);
            if (profile == null) return null;

            _currentAccount = new UserAccount
            {
                Username = profile["name"]?.ToString() ?? "Player",
                UUID = profile["id"]?.ToString() ?? string.Empty,
                AccessToken = mcToken,
                Type = AuthenticationType.Microsoft,
                TokenExpiry = DateTime.UtcNow.AddHours(24)
            };

            _logger.Info($"微软登录成功: {_currentAccount.Username}");
            return _currentAccount;
        }
        catch (Exception ex)
        {
            _logger.Error($"微软登录失败: {ex.Message}", ex);
            return null;
        }
    }

    /// <summary>
    /// 刷新 Token
    /// </summary>
    public async Task<bool> RefreshTokenAsync()
    {
        if (_currentAccount == null) return false;

        try
        {
            if (_currentAccount.Type == AuthenticationType.Mojang)
            {
                return await RefreshMojangTokenAsync();
            }
            else if (_currentAccount.Type == AuthenticationType.Microsoft)
            {
                // Microsoft 刷新需要重新登录
                _logger.Warning("微软账户需要重新登录");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"刷新 Token 失败: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// 刷新 Mojang Token
    /// </summary>
    private async Task<bool> RefreshMojangTokenAsync()
    {
        if (_currentAccount == null) return false;

        try
        {
            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(MOJANG_AUTH_URL);

            var requestData = new
            {
                accessToken = _currentAccount.AccessToken,
                clientToken = Guid.NewGuid().ToString("N"),
                requestUser = true
            };

            var response = await httpClient.PostAsJsonAsync("/refresh", requestData);
            
            if (!response.IsSuccessStatusCode) return false;

            var result = await response.Content.ReadFromJsonAsync<JObject>();
            if (result == null) return false;

            _currentAccount.AccessToken = result["accessToken"]?.ToString() ?? string.Empty;
            _currentAccount.TokenExpiry = DateTime.UtcNow.AddHours(24);

            _logger.Info("Token 刷新成功");
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 登出
    /// </summary>
    public void Logout()
    {
        if (_currentAccount != null)
        {
            _logger.Info($"用户登出: {_currentAccount.Username}");
            _currentAccount = null;
        }
    }

    // === 微软认证流程辅助方法 ===

    private async Task<string?> GetMicrosoftAccessTokenAsync(string code)
    {
        // 实现省略 - 需要客户端 ID 和密钥
        await Task.Delay(0);
        return null;
    }

    private async Task<string?> AuthenticateWithXboxLiveAsync(string msToken)
    {
        await Task.Delay(0);
        return null;
    }

    private async Task<string?> GetXSTSTokenAsync(string xblToken)
    {
        await Task.Delay(0);
        return null;
    }

    private async Task<string?> GetMinecraftTokenAsync(string xstsToken)
    {
        await Task.Delay(0);
        return null;
    }

    private async Task<JObject?> GetMinecraftProfileAsync(string mcToken)
    {
        await Task.Delay(0);
        return null;
    }

    /// <summary>
    /// 生成离线模式 UUID
    /// </summary>
    private string GenerateOfflineUUID(string username)
    {
        using var sha1 = System.Security.Cryptography.SHA1.Create();
        var hash = sha1.ComputeHash(System.Text.Encoding.UTF8.GetBytes($"OfflinePlayer:{username}"));
        
        // 设置版本和变体位
        hash[6] = (byte)((hash[6] & 0x0f) | 0x30);
        hash[8] = (byte)((hash[8] & 0x3f) | 0x80);

        var uuid = BitConverter.ToString(hash, 0, 16).Replace("-", "").ToLower();
        return $"{uuid.Substring(0, 8)}-{uuid.Substring(8, 4)}-{uuid.Substring(12, 4)}-{uuid.Substring(16, 4)}-{uuid.Substring(20, 12)}";
    }
}
