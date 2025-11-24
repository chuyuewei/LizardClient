using LizardClient.Core.Interfaces;
using LizardClient.Core.Models;
using LizardClient.ModSystem.API;
using System.Reflection;

namespace LizardClient.ModSystem.Loader;

/// <summary>
/// 模组加载器，负责加载和管理模组
/// </summary>
public sealed class ModLoader
{
    private readonly Dictionary<string, IMod> _loadedMods;
    private readonly ILogger _logger;

    public ModLoader(ILogger logger)
    {
        _logger = logger;
        _loadedMods = new Dictionary<string, IMod>();
    }

    /// <summary>
    /// 获取所有已加载的模组
    /// </summary>
    public IReadOnlyDictionary<string, IMod> LoadedMods => _loadedMods;

    /// <summary>
    /// 从当前程序集加载所有模组
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
                    _logger.Error($"创建模组实例失败: {type.Name}", ex);
                }
            }

            _logger.Info($"从程序集加载了 {_loadedMods.Count} 个模组");
        }
        catch (Exception ex)
        {
            _logger.Error("加载模组失败", ex);
        }
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

            // 检查依赖项
            foreach (var dependency in mod.Info.Dependencies)
            {
                if (!_loadedMods.ContainsKey(dependency))
                {
                    _logger.Warning($"模组 {mod.Info.Id} 的依赖项 {dependency} 未加载");
                }
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
