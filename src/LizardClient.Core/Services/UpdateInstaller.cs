using LizardClient.Core.Interfaces;
using LizardClient.Core.Models;
using LizardClient.Core.Utilities;
using System.Diagnostics;
using System.IO.Compression;

namespace LizardClient.Core.Services;

/// <summary>
/// 更新安装器，负责应用更新并处理回滚
/// </summary>
public sealed class UpdateInstaller
{
    private readonly ILogger _logger;
    private readonly BackupManager _backupManager;
    private string? _lastBackupPath;

    public UpdateInstaller(ILogger logger, BackupManager backupManager)
    {
        _logger = logger;
        _backupManager = backupManager;
    }

    /// <summary>
    /// 安装更新
    /// </summary>
    /// <param name="updatePackagePath">更新包路径</param>
    /// <param name="targetDirectory">目标安装目录</param>
    /// <param name="currentVersion">当前版本（用于备份）</param>
    /// <param name="verifyIntegrity">是否验证文件完整性</param>
    /// <returns>是否成功</returns>
    public async Task<(bool success, string? errorMessage)> InstallUpdateAsync(
        string updatePackagePath,
        string targetDirectory,
        string currentVersion,
        bool verifyIntegrity = true)
    {
        try
        {
            _logger.Info($"开始安装更新: {updatePackagePath}");

            // 1. 验证更新包
            if (!File.Exists(updatePackagePath))
            {
                return (false, "更新包文件不存在");
            }

            // 2. 创建备份
            _logger.Info("正在创建备份...");
            try
            {
                _lastBackupPath = await _backupManager.CreateBackupAsync(
                    targetDirectory,
                    currentVersion
                );
                _logger.Info($"备份已创建: {_lastBackupPath}");
            }
            catch (Exception ex)
            {
                _logger.Error($"创建备份失败: {ex.Message}", ex);
                return (false, $"创建备份失败: {ex.Message}");
            }

            // 3. 解析更新包
            UpdatePackage? package = null;
            try
            {
                package = await ParseUpdatePackageAsync(updatePackagePath);
                if (package == null)
                {
                    throw new InvalidOperationException("无法解析更新包");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"解析更新包失败: {ex.Message}", ex);
                return (false, $"解析更新包失败: {ex.Message}");
            }

            // 4. 应用更新文件
            _logger.Info("正在应用更新...");
            try
            {
                await ApplyUpdateFilesAsync(updatePackagePath, targetDirectory, package);
            }
            catch (Exception ex)
            {
                _logger.Error($"应用更新失败: {ex.Message}", ex);

                // 回滚
                _logger.Warning("正在回滚更新...");
                await RollbackAsync(targetDirectory);

                return (false, $"应用更新失败: {ex.Message}");
            }

            // 5. 删除废弃文件
            try
            {
                DeleteObsoleteFiles(targetDirectory, package.FilesToDelete);
            }
            catch (Exception ex)
            {
                _logger.Warning($"删除废弃文件时出错: {ex.Message}");
            }

            // 6. 验证安装
            if (verifyIntegrity)
            {
                _logger.Info("正在验证安装...");
                var verifyResult = await VerifyInstallationAsync(targetDirectory, package);

                if (!verifyResult.isValid)
                {
                    _logger.Error($"安装验证失败: {verifyResult.errorMessage}");

                    // 回滚
                    _logger.Warning("正在回滚更新...");
                    await RollbackAsync(targetDirectory);

                    return (false, $"安装验证失败: {verifyResult.errorMessage}");
                }
            }

            _logger.Info("更新安装成功");
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.Error($"安装更新时发生未知错误: {ex.Message}", ex);

            // 尝试回滚
            try
            {
                await RollbackAsync(targetDirectory);
            }
            catch (Exception rollbackEx)
            {
                _logger.Error($"回滚失败: {rollbackEx.Message}", rollbackEx);
            }

            return (false, $"安装失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 解析更新包
    /// </summary>
    private async Task<UpdatePackage?> ParseUpdatePackageAsync(string packagePath)
    {
        try
        {
            using var archive = ZipFile.OpenRead(packagePath);

            // 查找清单文件
            var manifestEntry = archive.GetEntry("manifest.json");
            if (manifestEntry == null)
            {
                _logger.Warning("更新包中未找到 manifest.json，使用默认配置");

                // 如果没有清单，创建一个默认的包信息
                return new UpdatePackage
                {
                    PackagePath = packagePath,
                    Files = archive.Entries
                        .Where(e => !string.IsNullOrEmpty(e.Name))
                        .Select(e => new UpdateFile
                        {
                            RelativePath = e.FullName,
                            Size = e.Length,
                            Action = UpdateFileAction.Replace
                        })
                        .ToList()
                };
            }

            // 读取清单
            using var stream = manifestEntry.Open();
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();

            var package = Newtonsoft.Json.JsonConvert.DeserializeObject<UpdatePackage>(json);
            if (package != null)
            {
                package.PackagePath = packagePath;
            }

            return package;
        }
        catch (Exception ex)
        {
            _logger.Error($"解析更新包失败: {ex.Message}", ex);
            return null;
        }
    }

    /// <summary>
    /// 应用更新文件
    /// </summary>
    private async Task ApplyUpdateFilesAsync(
        string packagePath,
        string targetDirectory,
        UpdatePackage package)
    {
        await Task.Run(() =>
        {
            using var archive = ZipFile.OpenRead(packagePath);

            foreach (var file in package.Files)
            {
                if (file.Action == UpdateFileAction.Delete)
                    continue;

                var entry = archive.GetEntry(file.RelativePath);
                if (entry == null)
                {
                    _logger.Warning($"更新包中未找到文件: {file.RelativePath}");
                    continue;
                }

                var destinationPath = Path.Combine(targetDirectory, file.RelativePath);

                // 创建目录
                var directory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 如果文件已存在且正在使用，尝试重命名
                if (File.Exists(destinationPath))
                {
                    try
                    {
                        File.Delete(destinationPath);
                    }
                    catch (IOException)
                    {
                        // 文件被占用，重命名旧文件
                        var oldPath = destinationPath + ".old";
                        if (File.Exists(oldPath))
                        {
                            File.Delete(oldPath);
                        }
                        File.Move(destinationPath, oldPath);
                        _logger.Info($"文件被占用，已重命名: {file.RelativePath}");
                    }
                }

                // 提取文件
                entry.ExtractToFile(destinationPath, true);
                _logger.Info($"  - 已更新: {file.RelativePath}");
            }
        });
    }

    /// <summary>
    /// 删除废弃文件
    /// </summary>
    private void DeleteObsoleteFiles(string targetDirectory, List<string> filesToDelete)
    {
        if (!filesToDelete.Any())
            return;

        _logger.Info($"删除 {filesToDelete.Count} 个废弃文件...");

        foreach (var relativePath in filesToDelete)
        {
            try
            {
                var fullPath = Path.Combine(targetDirectory, relativePath);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.Info($"  - 已删除: {relativePath}");
                }
            }
            catch (Exception ex)
            {
                _logger.Warning($"删除文件失败 {relativePath}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 验证安装
    /// </summary>
    private async Task<(bool isValid, string? errorMessage)> VerifyInstallationAsync(
        string targetDirectory,
        UpdatePackage package)
    {
        try
        {
            // 验证关键文件是否存在
            var missingFiles = new List<string>();

            foreach (var file in package.Files.Where(f => f.Action != UpdateFileAction.Delete))
            {
                var fullPath = Path.Combine(targetDirectory, file.RelativePath);
                if (!File.Exists(fullPath))
                {
                    missingFiles.Add(file.RelativePath);
                }
            }

            if (missingFiles.Any())
            {
                return (false, $"缺少文件: {string.Join(", ", missingFiles)}");
            }

            // TODO: 可以添加更多验证，如文件哈希值验证
            await Task.CompletedTask;

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// 回滚更新
    /// </summary>
    public async Task RollbackAsync(string targetDirectory)
    {
        try
        {
            if (string.IsNullOrEmpty(_lastBackupPath) || !File.Exists(_lastBackupPath))
            {
                throw new InvalidOperationException("没有可用的备份");
            }

            _logger.Info($"正在从备份恢复: {_lastBackupPath}");
            await _backupManager.RestoreBackupAsync(_lastBackupPath, targetDirectory, overwriteExisting: true);

            _logger.Info("回滚完成");
        }
        catch (Exception ex)
        {
            _logger.Error($"回滚失败: {ex.Message}", ex);
            throw;
        }
    }

    /// <summary>
    /// 调度应用程序重启
    /// </summary>
    public void ScheduleRestart(string executablePath, int delaySeconds = 2)
    {
        try
        {
            _logger.Info($"将在 {delaySeconds} 秒后重启应用程序");

            // 创建批处理脚本重启应用程序
            var scriptPath = Path.Combine(Path.GetTempPath(), "restart_app.bat");
            var scriptContent = $@"
@echo off
timeout /t {delaySeconds} /nobreak > nul
start """" ""{executablePath}""
del ""%~f0""
";
            File.WriteAllText(scriptPath, scriptContent);

            // 启动脚本
            var startInfo = new ProcessStartInfo
            {
                FileName = scriptPath,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            Process.Start(startInfo);

            _logger.Info("重启已调度");
        }
        catch (Exception ex)
        {
            _logger.Error($"调度重启失败: {ex.Message}", ex);
        }
    }
}
