using LizardClient.Core.Interfaces;
using LizardClient.Core.Models;
using LizardClient.ModSystem.API;
using LizardClient.ModSystem.Core;
using LizardClient.ModSystem.Models;
using Newtonsoft.Json;
using System.IO.Compression;
using System.Reflection;

namespace LizardClient.ModSystem.Loader;

/// <summary>
/// 模组加载器，负责加载和管理模组
/// </summary>
public sealed class ModLoader
{
    private readonly Dictionary<string, IMod> _loadedMods;
    private readonly ILogger _logger;
    private readonly DependencyResolver _dependencyResolver;

    public ModLoader(ILogger logger)
    {
        _logger = logger;
        _loadedMods = new Dictionary<string, IMod>();
        _dependencyResolver = new DependencyResolver(logger);
    }

    /// <summary>
    /// 获取所有已加载的模组
    /// </summary>
    public IReadOnlyDictionary<string, IMod> LoadedMods => _loadedMods;

    /// <summary>
    /// 从当前程序集加载所有模组 (内置模组)
    /// </summary>
    public void LoadModsFromAssembly()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var modTypes = assembly.GetTypes()
                .Where(t => typeof(IMod).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

            foreach (var type in modTypes)
            {
                try
                {
                    var mod = (IMod?)Activator.CreateInstance(type);
                    if (mod != null)
                    {
                        RegisterMod(mod);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"创建内置模组实例失败: {type.Name}", ex);
                }
            }

            _logger.Info($"从程序集加载了 {_loadedMods.Count} 个模组");
        }
        catch (Exception ex)
        {
            _logger.Error("加载内置模组失败", ex);
        }
    }

    /// <summary>
    /// 扫描并加载模组目录中的模组
    /// </summary>
    public void LoadModsFromDirectory(string modsDirectory)
    {
        if (!Directory.Exists(modsDirectory))
        {
            Directory.CreateDirectory(modsDirectory);
            return;
        }

        _logger.Info($"开始扫描模组目录: {modsDirectory}");

        var discoveredMods = new List<(ModMetadata Metadata, string FilePath, bool IsZip)>();

        // 1. 扫描目录
        foreach (var file in Directory.GetFiles(modsDirectory, "*.jar")) // 支持 .jar (通常是 zip 格式)
        {
            var metadata = ParseModMetadataFromZip(file);
            if (metadata != null)
            {
                discoveredMods.Add((metadata, file, true));
            }
        }

        foreach (var file in Directory.GetFiles(modsDirectory, "*.zip"))
        {
            var metadata = ParseModMetadataFromZip(file);
            if (metadata != null)
            {
                discoveredMods.Add((metadata, file, true));
            }
        }

        foreach (var dir in Directory.GetDirectories(modsDirectory))
        {
            var metadata = ParseModMetadataFromDirectory(dir);
            if (metadata != null)
            {
                discoveredMods.Add((metadata, dir, false));
            }
        }

        // 2. 解决依赖
        var resolutionResult = _dependencyResolver.Resolve(discoveredMods.Select(m => m.Metadata));

        if (!resolutionResult.IsSuccess)
        {
            _logger.Error("模组依赖解析失败:");
            foreach (var error in resolutionResult.Errors)
            {
                _logger.Error($"- {error}");
            }
            return;
        }

        foreach (var warning in resolutionResult.Warnings)
        {
            _logger.Warning(warning);
        }

        // 3. 按顺序加载模组
        foreach (var metadata in resolutionResult.SortedMods)
        {
            var modInfo = discoveredMods.First(m => m.Metadata.Id == metadata.Id);
            LoadMod(modInfo.Metadata, modInfo.FilePath, modInfo.IsZip);
        }
    }

    private ModMetadata? ParseModMetadataFromZip(string zipPath)
    {
        try
        {
            using var archive = ZipFile.OpenRead(zipPath);
            var entry = archive.GetEntry("mod.json");
            if (entry == null) return null;

            using var stream = entry.Open();
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var metadata = JsonConvert.DeserializeObject<ModMetadata>(json);

            if (metadata != null && metadata.IsValid())
            {
                return metadata;
            }
        }
        catch (Exception ex)
        {
            _logger.Warning($"解析模组文件失败 {Path.GetFileName(zipPath)}: {ex.Message}");
        }
        return null;
    }

    private ModMetadata? ParseModMetadataFromDirectory(string dirPath)
    {
        try
        {
            var jsonPath = Path.Combine(dirPath, "mod.json");
            if (!File.Exists(jsonPath)) return null;

            var json = File.ReadAllText(jsonPath);
            var metadata = JsonConvert.DeserializeObject<ModMetadata>(json);

            if (metadata != null && metadata.IsValid())
            {
                return metadata;
            }
        }
        catch (Exception ex)
        {
            _logger.Warning($"解析模组目录失败 {Path.GetFileName(dirPath)}: {ex.Message}");
        }
        return null;
    }

    private void LoadMod(ModMetadata metadata, string path, bool isZip)
    {
        try
        {
            Assembly? assembly = null;

            if (isZip)
            {
                // 从 Zip 加载 DLL
                // 这里简化处理：假设 DLL 名称与 EntryPoint 所在程序集一致，或者直接扫描 DLL
                // 实际实现需要更复杂的类加载机制 (AssemblyLoadContext)
                _logger.Warning($"暂不支持直接从 Zip 加载代码: {metadata.Name}");
                return;
            }
            else
            {
                // 从目录加载 DLL
                var dllFiles = Directory.GetFiles(path, "*.dll");
                foreach (var dll in dllFiles)
                {
                    try
                    {
                        var loadedAssembly = Assembly.LoadFrom(dll);
                        // 检查是否包含 IMod 实现
                        if (loadedAssembly.GetTypes().Any(t => typeof(IMod).IsAssignableFrom(t)))
                        {
                            assembly = loadedAssembly;
                            break;
                        }
                    }
                    catch { /* 忽略非模组 DLL */ }
                }
            }

            if (assembly != null)
            {
                var modType = assembly.GetTypes().FirstOrDefault(t => typeof(IMod).IsAssignableFrom(t) && !t.IsAbstract);
                if (modType != null)
                {
                    var mod = (IMod?)Activator.CreateInstance(modType);
                    if (mod != null)
                    {
                        // 更新模组信息 (使用 mod.json 中的信息覆盖代码中的默认信息)
                        UpdateModInfo(mod, metadata);
                        RegisterMod(mod);
                    }
                }
            }
            else
            {
                _logger.Warning($"未在 {path} 中找到模组程序集");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"加载模组失败 {metadata.Name}: {ex.Message}", ex);
        }
    }

    private void UpdateModInfo(IMod mod, ModMetadata metadata)
    {
        mod.Info.Id = metadata.Id;
        mod.Info.Name = metadata.Name;
        mod.Info.Version = metadata.Version;
        mod.Info.Description = metadata.Description;
        mod.Info.Author = metadata.Author;
        // 转换依赖列表
        mod.Info.Dependencies = metadata.Dependencies.Select(d => d.ModId).ToList();
    }

    /// <summary>
    /// 注册单个模组
    /// </summary>
    public void RegisterMod(IMod mod)
    {
        try
        {
            if (_loadedMods.ContainsKey(mod.Info.Id))
            {
                _logger.Warning($"模组已存在: {mod.Info.Id}");
                return;
            }

            mod.OnLoad();
            _loadedMods[mod.Info.Id] = mod;

            if (mod.Info.EnabledByDefault)
            {
                mod.IsEnabled = true;
            }

            _logger.Info($"注册模组: {mod.Info.Name} ({mod.Info.Id}) v{mod.Info.Version}");
        }
        catch (Exception ex)
        {
            _logger.Error($"注册模组失败: {mod.Info.Id}", ex);
        }
    }

    /// <summary>
    /// 卸载单个模组
    /// </summary>
    public void UnregisterMod(string modId)
    {
        if (_loadedMods.TryGetValue(modId, out var mod))
        {
            try
            {
                if (mod.IsEnabled)
                {
                    mod.IsEnabled = false;
                }

                mod.OnUnload();
                _loadedMods.Remove(modId);
                _logger.Info($"卸载模组: {modId}");
            }
            catch (Exception ex)
            {
                _logger.Error($"卸载模组失败: {modId}", ex);
            }
        }
    }

    /// <summary>
    /// 卸载所有模组
    /// </summary>
    public void UnloadAllMods()
    {
        var modIds = _loadedMods.Keys.ToList();
        foreach (var modId in modIds)
        {
            UnregisterMod(modId);
        }
    }

    /// <summary>
    /// 触发所有启用模组的 Tick 事件
    /// </summary>
    public void TickMods()
    {
        foreach (var mod in _loadedMods.Values.Where(m => m.IsEnabled))
        {
            try
            {
                mod.OnTick();
            }
            catch (Exception ex)
            {
                _logger.Error($"模组 Tick 失败: {mod.Info.Id}", ex);
            }
        }
    }

    /// <summary>
    /// 触发所有启用模组的 Render 事件
    /// </summary>
    public void RenderMods()
    {
        foreach (var mod in _loadedMods.Values.Where(m => m.IsEnabled))
        {
            try
            {
                mod.OnRender();
            }
            catch (Exception ex)
            {
                _logger.Error($"模组 Render 失败: {mod.Info.Id}", ex);
            }
        }
    }

    /// <summary>
    /// 触发所有启用模组的 Input 事件
    /// </summary>
    public void TriggerInput(int key, InputAction action)
    {
        foreach (var mod in _loadedMods.Values.Where(m => m.IsEnabled))
        {
            try
            {
                mod.OnInput(key, action);
            }
            catch (Exception ex)
            {
                _logger.Error($"模组 Input 失败: {mod.Info.Id}", ex);
            }
        }
    }

    /// <summary>
    /// 获取模组
    /// </summary>
    public IMod? GetMod(string modId)
    {
        return _loadedMods.TryGetValue(modId, out var mod) ? mod : null;
    }

    /// <summary>
    /// 启用模组
    /// </summary>
    public void EnableMod(string modId)
    {
        if (_loadedMods.TryGetValue(modId, out var mod))
        {
            mod.IsEnabled = true;
            _logger.Info($"启用模组: {modId}");
        }
    }

    /// <summary>
    /// 禁用模组
    /// </summary>
    public void DisableMod(string modId)
    {
        if (_loadedMods.TryGetValue(modId, out var mod))
        {
            mod.IsEnabled = false;
            _logger.Info($"禁用模组: {modId}");
        }
    }
}
