using LizardClient.Core.Interfaces;

namespace LizardClient.ModSystem.Hooks;

/// <summary>
/// Hook类型枚举
/// </summary>
public enum HookType
{
    /// <summary>
    /// Tick事件前
    /// </summary>
    PreTick,

    /// <summary>
    /// Tick事件后
    /// </summary>
    PostTick,

    /// <summary>
    /// 渲染前
    /// </summary>
    PreRender,

    /// <summary>
    /// 渲染后
    /// </summary>
    PostRender,

    /// <summary>
    /// 实体生成前
    /// </summary>
    PreEntitySpawn,

    /// <summary>
    /// 实体生成后
    /// </summary>
    PostEntitySpawn,

    /// <summary>
    /// 方块放置前
    /// </summary>
    PreBlockPlace,

    /// <summary>
    /// 方块放置后
    /// </summary>
    PostBlockPlace,

    /// <summary>
    /// 输入处理前
    /// </summary>
    PreInput,

    /// <summary>
    /// 输入处理后
    /// </summary>
    PostInput
}

/// <summary>
/// Hook委托
/// </summary>
public delegate void HookCallback(object? context);

/// <summary>
/// Hook注册表
/// 管理模组的Hook注册和调用
/// </summary>
public sealed class HookRegistry
{
    private readonly ILogger _logger;
    private readonly Dictionary<HookType, List<HookEntry>> _hooks;
    private readonly object _hookLock = new();

    public HookRegistry(ILogger logger)
    {
        _logger = logger;
        _hooks = new Dictionary<HookType, List<HookEntry>>();

        // 初始化所有Hook类型
        foreach (HookType hookType in Enum.GetValues(typeof(HookType)))
        {
            _hooks[hookType] = new List<HookEntry>();
        }
    }

    /// <summary>
    /// 注册Hook
    /// </summary>
    public void RegisterHook(string modId, HookType hookType, HookCallback callback, int priority = 0)
    {
        lock (_hookLock)
        {
            var entry = new HookEntry
            {
                ModId = modId,
                HookType = hookType,
                Callback = callback,
                Priority = priority
            };

            _hooks[hookType].Add(entry);

            // 按优先级排序（数字越小优先级越高）
            _hooks[hookType].Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }

        _logger.Info($"Mod {modId} registered hook: {hookType} (priority: {priority})");
    }

    /// <summary>
    /// 取消注册Hook
    /// </summary>
    public void UnregisterHook(string modId, HookType hookType)
    {
        lock (_hookLock)
        {
            _hooks[hookType].RemoveAll(h => h.ModId == modId);
        }

        _logger.Info($"Mod {modId} unregistered hook: {hookType}");
    }

    /// <summary>
    /// 取消模组的所有Hook
    /// </summary>
    public void UnregisterAllHooks(string modId)
    {
        lock (_hookLock)
        {
            foreach (var hooks in _hooks.Values)
            {
                hooks.RemoveAll(h => h.ModId == modId);
            }
        }

        _logger.Info($"Mod {modId} unregistered all hooks");
    }

    /// <summary>
    /// 触发Hook
    /// </summary>
    public void TriggerHook(HookType hookType, object? context = null)
    {
        List<HookEntry> hooksCopy;

        lock (_hookLock)
        {
            hooksCopy = new List<HookEntry>(_hooks[hookType]);
        }

        foreach (var hook in hooksCopy)
        {
            try
            {
                hook.Callback(context);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error executing hook {hookType} for mod {hook.ModId}: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// 获取Hook数量
    /// </summary>
    public int GetHookCount(HookType hookType)
    {
        lock (_hookLock)
        {
            return _hooks[hookType].Count;
        }
    }

    /// <summary>
    /// 获取所有Hook信息
    /// </summary>
    public Dictionary<HookType, int> GetAllHookCounts()
    {
        lock (_hookLock)
        {
            return _hooks.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Count
            );
        }
    }

    /// <summary>
    /// Hook入口
    /// </summary>
    private class HookEntry
    {
        public string ModId { get; set; } = string.Empty;
        public HookType HookType { get; set; }
        public HookCallback Callback { get; set; } = _ => { };
        public int Priority { get; set; }
    }
}
