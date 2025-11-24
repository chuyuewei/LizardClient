using LizardClient.Core.Models;

namespace LizardClient.Core.Interfaces;

/// <summary>
/// 配置服务接口，提供配置的读取、保存和管理功能
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// 加载客户端配置
    /// </summary>
    /// <returns>客户端配置对象</returns>
    Task<ClientConfiguration> LoadConfigurationAsync();

    /// <summary>
    /// 保存客户端配置
    /// </summary>
    /// <param name="configuration">要保存的配置对象</param>
    Task SaveConfigurationAsync(ClientConfiguration configuration);

    /// <summary>
    /// 获取所有游戏配置文件
    /// </summary>
    /// <returns>游戏配置文件列表</returns>
    Task<List<GameProfile>> GetGameProfilesAsync();

    /// <summary>
    /// 根据 ID 获取游戏配置文件
    /// </summary>
    /// <param name="profileId">配置文件 ID</param>
    /// <returns>游戏配置文件，如果不存在则返回 null</returns>
    Task<GameProfile?> GetGameProfileAsync(Guid profileId);

    /// <summary>
    /// 保存游戏配置文件
    /// </summary>
    /// <param name="profile">要保存的游戏配置文件</param>
    Task SaveGameProfileAsync(GameProfile profile);

    /// <summary>
    /// 删除游戏配置文件
    /// </summary>
    /// <param name="profileId">要删除的配置文件 ID</param>
    Task DeleteGameProfileAsync(Guid profileId);

    /// <summary>
    /// 验证配置有效性
    /// </summary>
    /// <param name="configuration">要验证的配置</param>
    /// <returns>验证结果，成功返回 true</returns>
    bool ValidateConfiguration(ClientConfiguration configuration);

    /// <summary>
    /// 重置为默认配置
    /// </summary>
    Task ResetToDefaultAsync();
}
