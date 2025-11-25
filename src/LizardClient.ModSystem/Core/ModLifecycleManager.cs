using LizardClient.Core.Interfaces;
using LizardClient.ModSystem.API;
using LizardClient.ModSystem.Models;

namespace LizardClient.ModSystem.Core;

/// <summary>
/// 模组生命周期管理�?
/// 负责管理模组的状态转换、初始化、启用、禁用和卸载
/// </summary>
public sealed class ModLifecycleManager
{
    private readonly ILogger _logger;
    private readonly Dictionary<string, ModState> _modStates;
    private readonly Dictionary<string, IMod> _mods;
    private readonly object _stateLock = new();

    /// <summary>
    /// 模组状态改变事�?
    /// </summary>
    public event EventHandler<ModStateChangedEventArgs>? ModStateChanged;

    public ModLifecycleManager(ILogger logger)
    {
        _logger = logger;
        _modStates = new Dictionary<string, ModState>();
        _mods = new Dictionary<string, IMod>();
    }

    /// <summary>
    /// 注册模组
    /// </summary>
    public void RegisterMod(string modId, IMod mod)
    {
        lock (_stateLock)
        {
            if (_mods.ContainsKey(modId))
            {
                throw new InvalidOperationException($"Mod {modId} is already registered");
            }

            _mods[modId] = mod;
            SetState(modId, mod.Info.Name, ModState.Loaded);
        }
    }

    /// <summary>
    /// 获取模组状�?
    /// </summary>
    public ModState GetState(string modId)
    {
        lock (_stateLock)
        {
            return _modStates.TryGetValue(modId, out var state) ? state : ModState.Unloaded;
        }
    }

    /// <summary>
    /// 初始化模�?
    /// </summary>
    public async Task<bool> InitializeModAsync(string modId)
    {
        if (!_mods.TryGetValue(modId, out var mod))
        {
            _logger.Error($"Mod {modId} not found");
            return false;
        }

        var currentState = GetState(modId);
        if (currentState != ModState.Loaded)
        {
            _logger.Warning($"Cannot initialize mod {modId} from state {currentState}");
            return false;
        }

        try
        {
            SetState(modId, mod.Info.Name, ModState.Initializing);
            _logger.Info($"Initializing mod: {mod.Info.Name} ({modId})");

            await Task.Run(() => mod.OnLoad());

            SetState(modId, mod.Info.Name, ModState.Initialized);
            _logger.Info($"Mod initialized: {mod.Info.Name}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to initialize mod {modId}: {ex.Message}", ex);
            SetState(modId, mod.Info.Name, ModState.Failed, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 启用模组
    /// </summary>
    public async Task<bool> EnableModAsync(string modId)
    {
        if (!_mods.TryGetValue(modId, out var mod))
        {
            _logger.Error($"Mod {modId} not found");
            return false;
        }

        var currentState = GetState(modId);
        if (currentState != ModState.Initialized && currentState != ModState.Disabled)
        {
            _logger.Warning($"Cannot enable mod {modId} from state {currentState}");
            return false;
        }

        try
        {
            SetState(modId, mod.Info.Name, ModState.Enabling);
            _logger.Info($"Enabling mod: {mod.Info.Name} ({modId})");

            await Task.Run(() => mod.OnEnable());

            SetState(modId, mod.Info.Name, ModState.Enabled);
            _logger.Info($"Mod enabled: {mod.Info.Name}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to enable mod {modId}: {ex.Message}", ex);
            SetState(modId, mod.Info.Name, ModState.Failed, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 禁用模组
    /// </summary>
    public async Task<bool> DisableModAsync(string modId)
    {
        if (!_mods.TryGetValue(modId, out var mod))
        {
            _logger.Error($"Mod {modId} not found");
            return false;
        }

        var currentState = GetState(modId);
        if (currentState != ModState.Enabled)
        {
            _logger.Warning($"Cannot disable mod {modId} from state {currentState}");
            return false;
        }

        try
        {
            SetState(modId, mod.Info.Name, ModState.Disabling);
            _logger.Info($"Disabling mod: {mod.Info.Name} ({modId})");

            await Task.Run(() => mod.OnDisable());

            SetState(modId, mod.Info.Name, ModState.Disabled);
            _logger.Info($"Mod disabled: {mod.Info.Name}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to disable mod {modId}: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// 卸载模组
    /// </summary>
    public async Task<bool> UnloadModAsync(string modId)
    {
        if (!_mods.TryGetValue(modId, out var mod))
        {
            _logger.Error($"Mod {modId} not found");
            return false;
        }

        var currentState = GetState(modId);

        // 如果是启用状态，先禁�?
        if (currentState == ModState.Enabled)
        {
            if (!await DisableModAsync(modId))
            {
                return false;
            }
        }

        try
        {
            SetState(modId, mod.Info.Name, ModState.Unloading);
            _logger.Info($"Unloading mod: {mod.Info.Name} ({modId})");

            await Task.Run(() => mod.OnUnload());

            lock (_stateLock)
            {
                _mods.Remove(modId);
                _modStates.Remove(modId);
            }

            _logger.Info($"Mod unloaded: {mod.Info.Name}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to unload mod {modId}: {ex.Message}", ex);
            SetState(modId, mod.Info.Name, ModState.Failed, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 热重载模�?
    /// </summary>
    public async Task<bool> HotReloadModAsync(string modId)
    {
        if (!_mods.TryGetValue(modId, out var mod))
        {
            _logger.Error($"Mod {modId} not found");
            return false;
        }

        var wasEnabled = GetState(modId) == ModState.Enabled;

        try
        {
            SetState(modId, mod.Info.Name, ModState.Reloading);
            _logger.Info($"Hot reloading mod: {mod.Info.Name} ({modId})");

            // 禁用（如果已启用�?
            if (wasEnabled)
            {
                await Task.Run(() => mod.OnDisable());
            }

            // 卸载
            await Task.Run(() => mod.OnUnload());

            // 重新加载
            await Task.Run(() => mod.OnLoad());

            // 重新启用（如果之前是启用的）
            if (wasEnabled)
            {
                await Task.Run(() => mod.OnEnable());
                SetState(modId, mod.Info.Name, ModState.Enabled);
            }
            else
            {
                SetState(modId, mod.Info.Name, ModState.Initialized);
            }

            _logger.Info($"Mod hot reloaded: {mod.Info.Name}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to hot reload mod {modId}: {ex.Message}", ex);
            SetState(modId, mod.Info.Name, ModState.Failed, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 批量初始化模组（按依赖顺序）
    /// </summary>
    public async Task<Dictionary<string, bool>> InitializeModsAsync(IEnumerable<string> modIds)
    {
        var results = new Dictionary<string, bool>();

        foreach (var modId in modIds)
        {
            results[modId] = await InitializeModAsync(modId);
        }

        return results;
    }

    /// <summary>
    /// 批量启用模组
    /// </summary>
    public async Task<Dictionary<string, bool>> EnableModsAsync(IEnumerable<string> modIds)
    {
        var results = new Dictionary<string, bool>();

        foreach (var modId in modIds)
        {
            results[modId] = await EnableModAsync(modId);
        }

        return results;
    }

    /// <summary>
    /// 获取所有处于指定状态的模组
    /// </summary>
    public List<string> GetModsByState(ModState state)
    {
        lock (_stateLock)
        {
            return _modStates
                .Where(kvp => kvp.Value == state)
                .Select(kvp => kvp.Key)
                .ToList();
        }
    }

    /// <summary>
    /// 获取所有已启用的模�?
    /// </summary>
    public List<IMod> GetEnabledMods()
    {
        lock (_stateLock)
        {
            return _mods
                .Where(kvp => _modStates.TryGetValue(kvp.Key, out var state) && state == ModState.Enabled)
                .Select(kvp => kvp.Value)
                .ToList();
        }
    }

    /// <summary>
    /// 设置模组状态并触发事件
    /// </summary>
    private void SetState(string modId, string modName, ModState newState, string? errorMessage = null)
    {
        ModState oldState;

        lock (_stateLock)
        {
            _modStates.TryGetValue(modId, out oldState);
            _modStates[modId] = newState;
        }

        // 触发状态改变事�?
        ModStateChanged?.Invoke(this, new ModStateChangedEventArgs
        {
            ModId = modId,
            ModName = modName,
            OldState = oldState,
            NewState = newState,
            ErrorMessage = errorMessage
        });

        _logger.Info($"Mod {modName} ({modId}) state: {oldState} �?{newState}");
    }

    /// <summary>
    /// 检查是否可以安全卸载模组（检查依赖）
    /// </summary>
    public bool CanSafelyUnload(string modId, IEnumerable<ModMetadata> allMods)
    {
        var dependentMods = allMods
            .Where(m => m.Dependencies.Any(d => d.ModId == modId))
            .Where(m => GetState(m.Id) == ModState.Enabled)
            .ToList();

        if (dependentMods.Any())
        {
            _logger.Warning($"Cannot unload {modId}: required by {string.Join(", ", dependentMods.Select(m => m.Name))}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 获取所有模组状态快�?
    /// </summary>
    public Dictionary<string, ModState> GetAllStates()
    {
        lock (_stateLock)
        {
            return new Dictionary<string, ModState>(_modStates);
        }
    }
}
