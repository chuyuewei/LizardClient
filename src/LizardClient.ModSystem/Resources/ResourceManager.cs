using LizardClient.Core.Interfaces;
using System.IO.Compression;

namespace LizardClient.ModSystem.Resources;

/// <summary>
/// 模组资源管理器
/// 负责加载和管理模组资源（纹理、音频、配置文件等）
/// </summary>
public sealed class ResourceManager
{
    private readonly ILogger _logger;
    private readonly Dictionary<string, ModResourceContext> _modResources;
    private readonly string _cacheDirectory;

    public ResourceManager(ILogger logger, string cacheDirectory = "./.mod_cache")
    {
        _logger = logger;
        _modResources = new Dictionary<string, ModResourceContext>();
        _cacheDirectory = cacheDirectory;

        // 创建缓存目录
        if (!Directory.Exists(_cacheDirectory))
        {
            Directory.CreateDirectory(_cacheDirectory);
        }
    }

    /// <summary>
    /// 注册模组资源
    /// </summary>
    public void RegisterModResources(string modId, string modPath)
    {
        if (_modResources.ContainsKey(modId))
        {
            _logger.Warning($"Mod {modId} resources already registered");
            return;
        }

        var context = new ModResourceContext
        {
            ModId = modId,
            ModPath = modPath,
            IsArchive = modPath.EndsWith(".jar", StringComparison.OrdinalIgnoreCase) ||
                        modPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
        };

        _modResources[modId] = context;
        _logger.Info($"Registered resources for mod: {modId}");
    }

    /// <summary>
    /// 加载文本资源
    /// </summary>
    public string? LoadTextResource(string modId, string resourcePath)
    {
        if (!_modResources.TryGetValue(modId, out var context))
        {
            _logger.Warning($"Mod {modId} not registered");
            return null;
        }

        try
        {
            if (context.IsArchive)
            {
                using var archive = ZipFile.OpenRead(context.ModPath);
                var entry = archive.GetEntry(resourcePath);

                if (entry == null)
                {
                    _logger.Warning($"Resource not found: {resourcePath} in mod {modId}");
                    return null;
                }

                using var stream = entry.Open();
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
            else
            {
                var fullPath = Path.Combine(context.ModPath, resourcePath);
                if (!File.Exists(fullPath))
                {
                    _logger.Warning($"Resource not found: {fullPath}");
                    return null;
                }

                return File.ReadAllText(fullPath);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load resource {resourcePath} from mod {modId}: {ex.Message}", ex);
            return null;
        }
    }

    /// <summary>
    /// 加载二进制资源
    /// </summary>
    public byte[]? LoadBinaryResource(string modId, string resourcePath)
    {
        if (!_modResources.TryGetValue(modId, out var context))
        {
            _logger.Warning($"Mod {modId} not registered");
            return null;
        }

        try
        {
            if (context.IsArchive)
            {
                using var archive = ZipFile.OpenRead(context.ModPath);
                var entry = archive.GetEntry(resourcePath);

                if (entry == null)
                {
                    _logger.Warning($"Resource not found: {resourcePath} in mod {modId}");
                    return null;
                }

                using var stream = entry.Open();
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                return ms.ToArray();
            }
            else
            {
                var fullPath = Path.Combine(context.ModPath, resourcePath);
                if (!File.Exists(fullPath))
                {
                    _logger.Warning($"Resource not found: {fullPath}");
                    return null;
                }

                return File.ReadAllBytes(fullPath);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load resource {resourcePath} from mod {modId}: {ex.Message}", ex);
            return null;
        }
    }

    /// <summary>
    /// 提取资源到缓存目录
    /// </summary>
    public string? ExtractResourceToCache(string modId, string resourcePath)
    {
        var data = LoadBinaryResource(modId, resourcePath);
        if (data == null)
            return null;

        try
        {
            var cacheFilename = $"{modId}_{Path.GetFileName(resourcePath)}";
            var cachePath = Path.Combine(_cacheDirectory, cacheFilename);

            File.WriteAllBytes(cachePath, data);
            _logger.Info($"Extracted resource {resourcePath} to cache: {cachePath}");
            return cachePath;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to extract resource to cache: {ex.Message}", ex);
            return null;
        }
    }

    /// <summary>
    /// 列出模组的所有资源
    /// </summary>
    public List<string> ListResources(string modId, string? directory = null)
    {
        if (!_modResources.TryGetValue(modId, out var context))
        {
            return new List<string>();
        }

        try
        {
            if (context.IsArchive)
            {
                using var archive = ZipFile.OpenRead(context.ModPath);
                var entries = archive.Entries
                    .Where(e => !e.FullName.EndsWith("/")) // 排除目录
                    .Select(e => e.FullName);

                if (!string.IsNullOrEmpty(directory))
                {
                    entries = entries.Where(e => e.StartsWith(directory));
                }

                return entries.ToList();
            }
            else
            {
                var searchPath = string.IsNullOrEmpty(directory)
                    ? context.ModPath
                    : Path.Combine(context.ModPath, directory);

                if (!Directory.Exists(searchPath))
                    return new List<string>();

                return Directory.GetFiles(searchPath, "*", SearchOption.AllDirectories)
                    .Select(f => Path.GetRelativePath(context.ModPath, f))
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to list resources for mod {modId}: {ex.Message}", ex);
            return new List<string>();
        }
    }

    /// <summary>
    /// 卸载模组资源
    /// </summary>
    public void UnregisterModResources(string modId)
    {
        if (_modResources.Remove(modId))
        {
            // 清理缓存文件
            try
            {
                var cacheFiles = Directory.GetFiles(_cacheDirectory, $"{modId}_*");
                foreach (var file in cacheFiles)
                {
                    File.Delete(file);
                }
            }
            catch (Exception ex)
            {
                _logger.Warning($"Failed to cleanup cache for mod {modId}: {ex.Message}");
            }

            _logger.Info($"Unregistered resources for mod: {modId}");
        }
    }

    /// <summary>
    /// 清理所有缓存
    /// </summary>
    public void ClearCache()
    {
        try
        {
            if (Directory.Exists(_cacheDirectory))
            {
                Directory.Delete(_cacheDirectory, true);
                Directory.CreateDirectory(_cacheDirectory);
            }

            _logger.Info("Resource cache cleared");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to clear cache: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 模组资源上下文
    /// </summary>
    private class ModResourceContext
    {
        public string ModId { get; set; } = string.Empty;
        public string ModPath { get; set; } = string.Empty;
        public bool IsArchive { get; set; }
    }
}
