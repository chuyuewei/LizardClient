namespace LizardClient.ModSystem.Events;

/// <summary>
/// 事件优先级
/// </summary>
public enum EventPriority
{
    /// <summary>
    /// 最高优先级 - 第一个处理
    /// </summary>
    Highest = 0,

    /// <summary>
    /// 高优先级
    /// </summary>
    High = 1,

    /// <summary>
    /// 普通优先级 (默认)
    /// </summary>
    Normal = 2,

    /// <summary>
    /// 低优先级
    /// </summary>
    Low = 3,

    /// <summary>
    /// 最低优先级 - 最后处理
    /// </summary>
    Lowest = 4
}

/// <summary>
/// 模组事件基类
/// </summary>
public abstract class ModEvent
{
    /// <summary>
    /// 事件是否被取消
    /// </summary>
    public bool IsCancelled { get; private set; }

    /// <summary>
    /// 是否可以取消该事件
    /// </summary>
    public virtual bool IsCancellable => true;

    /// <summary>
    /// 事件触发时间
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;

    /// <summary>
    /// 取消事件
    /// </summary>
    public void Cancel()
    {
        if (!IsCancellable)
        {
            throw new InvalidOperationException("This event cannot be cancelled");
        }
        IsCancelled = true;
    }

    /// <summary>
    /// 重置取消状态
    /// </summary>
    public void ResetCancellation()
    {
        IsCancelled = false;
    }
}

/// <summary>
/// 事件订阅者接口
/// </summary>
public interface IEventSubscriber
{
    /// <summary>
    /// 订阅者所属的模组ID
    /// </summary>
    string ModId { get; }

    /// <summary>
    /// 事件优先级
    /// </summary>
    EventPriority Priority { get; }
}

/// <summary>
/// 事件处理器委托
/// </summary>
public delegate void EventHandler<in TEvent>(TEvent @event) where TEvent : ModEvent;

/// <summary>
/// 异步事件处理器委托
/// </summary>
public delegate Task AsyncEventHandler<in TEvent>(TEvent @event) where TEvent : ModEvent;
