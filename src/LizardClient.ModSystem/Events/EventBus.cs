using LizardClient.Core.Interfaces;
using System.Collections.Concurrent;

namespace LizardClient.ModSystem.Events;

/// <summary>
/// 事件总线 - 中央事件调度器
/// </summary>
public sealed class EventBus
{
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<Type, List<EventSubscription>> _subscriptions;
    private readonly object _subscriptionLock = new();

    public EventBus(ILogger logger)
    {
        _logger = logger;
        _subscriptions = new ConcurrentDictionary<Type, List<EventSubscription>>();
    }

    /// <summary>
    /// 订阅事件
    /// </summary>
    public void Subscribe<TEvent>(string modId, EventHandler<TEvent> handler, EventPriority priority = EventPriority.Normal)
        where TEvent : ModEvent
    {
        var eventType = typeof(TEvent);
        var subscription = new EventSubscription
        {
            ModId = modId,
            Priority = priority,
            Handler = evt => handler((TEvent)evt),
            EventType = eventType
        };

        lock (_subscriptionLock)
        {
            if (!_subscriptions.TryGetValue(eventType, out var subs))
            {
                subs = new List<EventSubscription>();
                _subscriptions[eventType] = subs;
            }

            subs.Add(subscription);
            // 按优先级排序
            subs.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }

        _logger.Info($"Mod {modId} subscribed to {eventType.Name} with priority {priority}");
    }

    /// <summary>
    /// 订阅异步事件
    /// </summary>
    public void SubscribeAsync<TEvent>(string modId, AsyncEventHandler<TEvent> handler, EventPriority priority = EventPriority.Normal)
        where TEvent : ModEvent
    {
        var eventType = typeof(TEvent);
        var subscription = new EventSubscription
        {
            ModId = modId,
            Priority = priority,
            AsyncHandler = evt => handler((TEvent)evt),
            EventType = eventType,
            IsAsync = true
        };

        lock (_subscriptionLock)
        {
            if (!_subscriptions.TryGetValue(eventType, out var subs))
            {
                subs = new List<EventSubscription>();
                _subscriptions[eventType] = subs;
            }

            subs.Add(subscription);
            subs.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }

        _logger.Info($"Mod {modId} subscribed to {eventType.Name} (async) with priority {priority}");
    }

    /// <summary>
    /// 取消订阅
    /// </summary>
    public void Unsubscribe<TEvent>(string modId) where TEvent : ModEvent
    {
        var eventType = typeof(TEvent);

        lock (_subscriptionLock)
        {
            if (_subscriptions.TryGetValue(eventType, out var subs))
            {
                subs.RemoveAll(s => s.ModId == modId);
                _logger.Info($"Mod {modId} unsubscribed from {eventType.Name}");
            }
        }
    }

    /// <summary>
    /// 取消模组的所有订阅
    /// </summary>
    public void UnsubscribeAll(string modId)
    {
        lock (_subscriptionLock)
        {
            foreach (var subs in _subscriptions.Values)
            {
                subs.RemoveAll(s => s.ModId == modId);
            }
        }

        _logger.Info($"Mod {modId} unsubscribed from all events");
    }

    /// <summary>
    /// 触发事件（同步）
    /// </summary>
    public void Fire<TEvent>(TEvent @event) where TEvent : ModEvent
    {
        var eventType = typeof(TEvent);

        if (!_subscriptions.TryGetValue(eventType, out var subs))
        {
            return; // 无订阅者
        }

        List<EventSubscription> subscribers;
        lock (_subscriptionLock)
        {
            subscribers = new List<EventSubscription>(subs);
        }

        foreach (var subscription in subscribers)
        {
            if (@event.IsCancelled && @event.IsCancellable)
            {
                break; // 事件已取消，停止传播
            }

            try
            {
                if (subscription.IsAsync)
                {
                    // 异步处理器，但同步等待
                    subscription.AsyncHandler?.Invoke(@event).Wait();
                }
                else
                {
                    subscription.Handler?.Invoke(@event);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error handling event {eventType.Name} in mod {subscription.ModId}: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// 触发事件（异步）
    /// </summary>
    public async Task FireAsync<TEvent>(TEvent @event) where TEvent : ModEvent
    {
        var eventType = typeof(TEvent);

        if (!_subscriptions.TryGetValue(eventType, out var subs))
        {
            return;
        }

        List<EventSubscription> subscribers;
        lock (_subscriptionLock)
        {
            subscribers = new List<EventSubscription>(subs);
        }

        foreach (var subscription in subscribers)
        {
            if (@event.IsCancelled && @event.IsCancellable)
            {
                break;
            }

            try
            {
                if (subscription.IsAsync && subscription.AsyncHandler != null)
                {
                    await subscription.AsyncHandler(@event);
                }
                else if (subscription.Handler != null)
                {
                    await Task.Run(() => subscription.Handler(@event));
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error handling event {eventType.Name} in mod {subscription.ModId}: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// 获取订阅者数量
    /// </summary>
    public int GetSubscriberCount<TEvent>() where TEvent : ModEvent
    {
        var eventType = typeof(TEvent);
        return _subscriptions.TryGetValue(eventType, out var subs) ? subs.Count : 0;
    }

    /// <summary>
    /// 获取所有订阅的事件类型
    /// </summary>
    public IEnumerable<Type> GetSubscribedEvents()
    {
        return _subscriptions.Keys;
    }

    /// <summary>
    /// 清除所有订阅
    /// </summary>
    public void ClearAll()
    {
        lock (_subscriptionLock)
        {
            _subscriptions.Clear();
        }
        _logger.Info("All event subscriptions cleared");
    }

    /// <summary>
    /// 事件订阅信息
    /// </summary>
    private class EventSubscription
    {
        public string ModId { get; set; } = string.Empty;
        public EventPriority Priority { get; set; }
        public Action<ModEvent>? Handler { get; set; }
        public Func<ModEvent, Task>? AsyncHandler { get; set; }
        public Type EventType { get; set; } = typeof(ModEvent);
        public bool IsAsync { get; set; }
    }
}
