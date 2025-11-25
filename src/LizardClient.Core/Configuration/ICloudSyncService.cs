namespace LizardClient.Core.Configuration;

/// <summary>
/// 同步状态
/// </summary>
public enum SyncStatus
{
    /// <summary>
    /// 同步</summary>
    Synced,

    /// <summary>
    /// 同步中
    /// </summary>
    Syncing,

    /// <summary>
    /// 未同步
    /// </summary>
    NotSynced,

    /// <summary>
    /// 冲突
    /// </summary>
    Conflict,

    /// <summary>
    /// 错误
    /// </summary>
    Error,

    /// <summary>
    /// 离线
    /// </summary>
    Offline
}

/// <summary>
/// 冲突解决策略
/// </summary>
public enum ConflictResolution
{
    /// <summary>
    /// 使用本地版本
    /// </summary>
    UseLocal,

    /// <summary>
    /// 使用远程版本
    /// </summary>
    UseRemote,

    /// <summary>
    /// 合并
    /// </summary>
    Merge,

    /// <summary>
    /// 取消
    /// </summary>
    Cancel
}

/// <summary>
/// 同步结果
/// </summary>
public sealed class SyncResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public SyncStatus Status { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 同步时间
    /// </summary>
    public DateTime SyncTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 同步的配置文件数量
    /// </summary>
    public int SyncedProfiles { get; set; }
}

/// <summary>
/// 云同步服务接口
/// </summary>
public interface ICloudSyncService
{
    /// <summary>
    /// 上传配置文件到云端
    /// </summary>
    Task<SyncResult> UploadConfigurationAsync(ConfigurationProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// 从云端下载配置文件
    /// </summary>
    Task<ConfigurationProfile?> DownloadConfigurationAsync(string profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取同步状态
    /// </summary>
    Task<SyncStatus> GetSyncStatusAsync();

    /// <summary>
    /// 同步所有配置文件
    /// </summary>
    Task<SyncResult> SyncAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 解决同步冲突
    /// </summary>
    Task<bool> ResolveConflictAsync(string profileId, ConflictResolution resolution);

    /// <summary>
    /// 检查是否在线
    /// </summary>
    Task<bool> IsOnlineAsync();

    /// <summary>
    /// 列出云端所有配置文件
    /// </summary>
    Task<List<string>> ListCloudProfilesAsync();

    /// <summary>
    /// 删除云端配置文件
    /// </summary>
    Task<bool> DeleteCloudProfileAsync(string profileId);
}

/// <summary>
/// 本地云同步服务实现（默认实现，不实际连接云端）
/// </summary>
public sealed class LocalCloudSyncService : ICloudSyncService
{
    private readonly string _syncDirectory;

    public LocalCloudSyncService(string syncDirectory = "./.cloud_sync")
    {
        _syncDirectory = syncDirectory;
        Directory.CreateDirectory(_syncDirectory);
    }

    public async Task<SyncResult> UploadConfigurationAsync(ConfigurationProfile profile, CancellationToken cancellationToken = default)
    {
        try
        {
            var filePath = Path.Combine(_syncDirectory, $"{profile.Id}.json");
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(profile, Newtonsoft.Json.Formatting.Indented);
            await File.WriteAllTextAsync(filePath, json, cancellationToken);

            return new SyncResult
            {
                Success = true,
                Status = SyncStatus.Synced,
                SyncedProfiles = 1
            };
        }
        catch (Exception ex)
        {
            return new SyncResult
            {
                Success = false,
                Status = SyncStatus.Error,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<ConfigurationProfile?> DownloadConfigurationAsync(string profileId, CancellationToken cancellationToken = default)
    {
        try
        {
            var filePath = Path.Combine(_syncDirectory, $"{profileId}.json");
            if (!File.Exists(filePath))
            {
                return null;
            }

            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<ConfigurationProfile>(json);
        }
        catch
        {
            return null;
        }
    }

    public Task<SyncStatus> GetSyncStatusAsync()
    {
        return Task.FromResult(Directory.Exists(_syncDirectory) ? SyncStatus.Synced : SyncStatus.NotSynced);
    }

    public async Task<SyncResult> SyncAllAsync(CancellationToken cancellationToken = default)
    {
        // 本地实现只是简单返回成功
        return await Task.FromResult(new SyncResult
        {
            Success = true,
            Status = SyncStatus.Synced
        });
    }

    public Task<bool> ResolveConflictAsync(string profileId, ConflictResolution resolution)
    {
        return Task.FromResult(true);
    }

    public Task<bool> IsOnlineAsync()
    {
        return Task.FromResult(true); // 本地总是"在线"
    }

    public Task<List<string>> ListCloudProfilesAsync()
    {
        var profiles = Directory.Exists(_syncDirectory)
            ? Directory.GetFiles(_syncDirectory, "*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(f => f != null)
                .Cast<string>()
                .ToList()
            : new List<string>();

        return Task.FromResult(profiles);
    }

    public Task<bool> DeleteCloudProfileAsync(string profileId)
    {
        try
        {
            var filePath = Path.Combine(_syncDirectory, $"{profileId}.json");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
}
