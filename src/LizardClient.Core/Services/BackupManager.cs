using LizardClient.Core.Interfaces;
using System.IO.Compression;

namespace LizardClient.Core.Services;

/// <summary>
/// 备份管理器，负责创建和恢复版本备份
/// </summary>
public sealed class BackupManager
{
    private readonly ILogger _logger;
    private readonly string _backupDirectory;
    private readonly int _maxBackupCount;

    public BackupManager(ILogger logger, string? backupDirectory = null, int maxBackupCount = 3)
    {
        _logger = logger;
        _backupDirectory = backupDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            ".lizardclient",
            "backups"
        );
        _maxBackupCount = maxBackupCount;

        // 确保备份目录存在
        Directory.CreateDirectory(_backupDirectory);
    }

    /// <summary>
    /// 创建完整备份
    /// </summary>
    /// <param name="sourceDirectory">源目录</param>
    /// <param name="version">版本号（用于备份命名）</param>
    /// <returns>备份文件路径</returns>
    public async Task<string> CreateBackupAsync(string sourceDirectory, string? version = null)
    {
        try
        {
            if (!Directory.Exists(sourceDirectory))
            {
                throw new DirectoryNotFoundException($"源目录不存在: {sourceDirectory}");
            }

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupName = string.IsNullOrEmpty(version)
                ? $"backup_{timestamp}.zip"
                : $"backup_v{version}_{timestamp}.zip";

            var backupPath = Path.Combine(_backupDirectory, backupName);

            _logger.Info($"正在创建备份: {backupPath}");

            // 创建压缩备份
            await Task.Run(() =>
            {
                ZipFile.CreateFromDirectory(
                    sourceDirectory,
                    backupPath,
                    CompressionLevel.Optimal,
                    includeBaseDirectory: false
                );
            });

            var fileInfo = new FileInfo(backupPath);
            _logger.Info($"备份创建成功 (大小: {fileInfo.Length / 1024 / 1024:F2} MB)");

            // 清理旧备份
            await CleanupOldBackupsAsync();

            return backupPath;
        }
        catch (Exception ex)
        {
            _logger.Error($"创建备份失败: {ex.Message}", ex);
            throw;
        }
    }

    /// <summary>
    /// 创建增量备份（仅备份指定的文件）
    /// </summary>
    /// <param name="sourceDirectory">源目录</param>
    /// <param name="filesToBackup">要备份的文件相对路径列表</param>
    /// <param name="version">版本号</param>
    /// <returns>备份文件路径</returns>
    public async Task<string> CreateIncrementalBackupAsync(
        string sourceDirectory,
        List<string> filesToBackup,
        string? version = null)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupName = string.IsNullOrEmpty(version)
                ? $"incremental_{timestamp}.zip"
                : $"incremental_v{version}_{timestamp}.zip";

            var backupPath = Path.Combine(_backupDirectory, backupName);

            _logger.Info($"正在创建增量备份: {backupPath}");

            await Task.Run(() =>
            {
                using var archive = ZipFile.Open(backupPath, ZipArchiveMode.Create);

                foreach (var relativePath in filesToBackup)
                {
                    var fullPath = Path.Combine(sourceDirectory, relativePath);
                    if (File.Exists(fullPath))
                    {
                        archive.CreateEntryFromFile(fullPath, relativePath, CompressionLevel.Optimal);
                        _logger.Info($"  - 已备份: {relativePath}");
                    }
                    else
                    {
                        _logger.Warning($"  - 文件不存在，跳过: {relativePath}");
                    }
                }
            });

            _logger.Info($"增量备份创建成功");
            return backupPath;
        }
        catch (Exception ex)
        {
            _logger.Error($"创建增量备份失败: {ex.Message}", ex);
            throw;
        }
    }

    /// <summary>
    /// 从备份恢复
    /// </summary>
    /// <param name="backupPath">备份文件路径</param>
    /// <param name="targetDirectory">目标目录</param>
    /// <param name="overwriteExisting">是否覆盖现有文件</param>
    public async Task RestoreBackupAsync(
        string backupPath,
        string targetDirectory,
        bool overwriteExisting = true)
    {
        try
        {
            if (!File.Exists(backupPath))
            {
                throw new FileNotFoundException($"备份文件不存在: {backupPath}");
            }

            _logger.Info($"正在从备份恢复: {backupPath} -> {targetDirectory}");

            // 确保目标目录存在
            Directory.CreateDirectory(targetDirectory);

            await Task.Run(() =>
            {
                using var archive = ZipFile.OpenRead(backupPath);

                foreach (var entry in archive.Entries)
                {
                    // 跳过目录项
                    if (string.IsNullOrEmpty(entry.Name))
                        continue;

                    var destinationPath = Path.Combine(targetDirectory, entry.FullName);

                    // 创建父目录
                    var directory = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    // 检查文件是否存在
                    if (File.Exists(destinationPath))
                    {
                        if (!overwriteExisting)
                        {
                            _logger.Info($"  - 跳过已存在文件: {entry.FullName}");
                            continue;
                        }

                        // 删除现有文件
                        File.Delete(destinationPath);
                    }

                    // 提取文件
                    entry.ExtractToFile(destinationPath, overwriteExisting);
                    _logger.Info($"  - 已恢复: {entry.FullName}");
                }
            });

            _logger.Info("备份恢复完成");
        }
        catch (Exception ex)
        {
            _logger.Error($"恢复备份失败: {ex.Message}", ex);
            throw;
        }
    }

    /// <summary>
    /// 清理旧备份（保留最近的 N 个备份）
    /// </summary>
    public async Task CleanupOldBackupsAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                var backups = Directory.GetFiles(_backupDirectory, "*.zip")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();

                _logger.Info($"当前备份数量: {backups.Count}");

                if (backups.Count <= _maxBackupCount)
                {
                    _logger.Info($"备份数量未超过限制 ({_maxBackupCount})，无需清理");
                    return;
                }

                // 删除超出保留数量的备份
                var toDelete = backups.Skip(_maxBackupCount).ToList();
                _logger.Info($"清理 {toDelete.Count} 个旧备份...");

                foreach (var backup in toDelete)
                {
                    try
                    {
                        backup.Delete();
                        _logger.Info($"  - 已删除: {backup.Name}");
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning($"  - 删除失败 {backup.Name}: {ex.Message}");
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"清理备份失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 获取所有备份信息
    /// </summary>
    public List<BackupInfo> GetAllBackups()
    {
        try
        {
            return Directory.GetFiles(_backupDirectory, "*.zip")
                .Select(f => new FileInfo(f))
                .Select(f => new BackupInfo
                {
                    FilePath = f.FullName,
                    FileName = f.Name,
                    Size = f.Length,
                    CreatedTime = f.CreationTime,
                    Version = ExtractVersionFromFileName(f.Name)
                })
                .OrderByDescending(b => b.CreatedTime)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.Error($"获取备份列表失败: {ex.Message}", ex);
            return new List<BackupInfo>();
        }
    }

    /// <summary>
    /// 获取最新的备份
    /// </summary>
    public BackupInfo? GetLatestBackup()
    {
        return GetAllBackups().FirstOrDefault();
    }

    /// <summary>
    /// 删除指定备份
    /// </summary>
    public void DeleteBackup(string backupPath)
    {
        try
        {
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
                _logger.Info($"已删除备份: {backupPath}");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"删除备份失败: {ex.Message}", ex);
            throw;
        }
    }

    /// <summary>
    /// 从文件名提取版本号
    /// </summary>
    private string? ExtractVersionFromFileName(string fileName)
    {
        // 匹配 backup_v1.2.3_timestamp.zip 格式
        var match = System.Text.RegularExpressions.Regex.Match(fileName, @"_v([\d\.]+)_");
        return match.Success ? match.Groups[1].Value : null;
    }
}

/// <summary>
/// 备份信息
/// </summary>
public sealed class BackupInfo
{
    /// <summary>
    /// 备份文件路径
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 备份文件名
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 备份大小（字节）
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; }

    /// <summary>
    /// 版本号（如果有）
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// 格式化的文件大小
    /// </summary>
    public string FormattedSize
    {
        get
        {
            double size = Size;
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }
    }
}
