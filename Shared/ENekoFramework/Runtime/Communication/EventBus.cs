using System;
using System.Collections.Generic;
using EasyPack.ENekoFramework.Editor;

namespace EasyPack.ENekoFramework
{
    /// <summary>
    ///     事件监控回调委托
    /// </summary>
    public delegate void EventMonitoringCallback(Type eventType, object eventData, int subscriberCount);

    /// <summary>
    ///     事件总线
    ///     负责事件发布/订阅，支持 At-most-once 语义和 WeakReference
    /// </summary>
    public class EventBus
    {
        private readonly Dictionary<Type, List<WeakReference>> _subscriptions;

        /// <summary>
        ///     事件发布时的监控回调（用于编辑器监控）
        /// </summary>
        public static EventMonitoringCallback OnEventPublished { get; set; }

        /// <summary>
        ///     构造函数
        /// </summary>
        public EventBus() => _subscriptions = new();

        /// <summary>
        ///     订阅事件（使用 WeakReference 防止内存泄漏）
        /// </summary>
        /// <typeparam name="TEvent">事件类型</typeparam>
        /// <param name="handler">事件处理函数</param>
        public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            Type eventType = typeof(TEvent);
            if (!_subscriptions.ContainsKey(eventType)) _subscriptions[eventType] = new();

            // 使用 WeakReference 包装 handler
            _subscriptions[eventType].Add(new(handler));
        }

        /// <summary>
        ///     发布事件（处理函数抛出异常不会重试）
        /// </summary>
        /// <typeparam name="TEvent">事件类型</typeparam>
        /// <param name="eventData">事件数据</param>
        public void Publish<TEvent>(TEvent eventData) where TEvent : IEvent
        {
            Type eventType = typeof(TEvent);

            // 调用事件监控回调
#if UNITY_EDITOR
            if (OnEventPublished != null && ShouldInvokeEventMonitoring())
            {
                int subscriberCount = GetSubscriberCount<TEvent>();
                OnEventPublished.Invoke(eventType, eventData, subscriberCount);
            }
#endif

            if (!_subscriptions.TryGetValue(eventType, out var handlers))
            {
                return;
            }

            var deadReferences = new List<WeakReference>();

            foreach (WeakReference weakRef in handlers)
            {
                if (!weakRef.IsAlive || weakRef.Target is not Action<TEvent> handler)
                {
                    deadReferences.Add(weakRef);
                    continue;
                }

                try
                {
                    // At-most-once: 异常不重试，直接跳过
                    handler.Invoke(eventData);
                }
                catch
                {
                    // 按照 At-most-once 语义，异常不重试
                    // 静默忽略异常，继续通知其他订阅者
                }
            }

            // 清理已被 GC 的弱引用
            foreach (WeakReference deadRef in deadReferences)
            {
                handlers.Remove(deadRef);
            }
        }

        /// <summary>
        ///     检查是否应该调用事件监控回调
        /// </summary>
        private static bool ShouldInvokeEventMonitoring()
        {
#if UNITY_EDITOR
            return EditorMonitoringConfig.EnableEventMonitoring;
#else
            return false;
#endif
        }

        /// <summary>
        ///     取消订阅事件
        /// </summary>
        /// <typeparam name="TEvent">事件类型</typeparam>
        /// <param name="handler">要取消的事件处理函数</param>
        public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent
        {
            if (handler == null)
            {
                return;
            }

            Type eventType = typeof(TEvent);
            if (!_subscriptions.TryGetValue(eventType, out var handlers))
            {
                return;
            }

            handlers.RemoveAll(weakRef =>
            {
                if (!weakRef.IsAlive)
                {
                    return true;
                }

                var target = weakRef.Target as Action<TEvent>;
                return target == handler;
            });
        }

        /// <summary>
        ///     清空所有订阅
        /// </summary>
        public void ClearAllSubscriptions()
        {
            _subscriptions.Clear();
        }

        /// <summary>
        ///     获取指定事件类型的订阅者数量（仅包含存活的订阅者）
        /// </summary>
        /// <typeparam name="TEvent">事件类型</typeparam>
        /// <returns>订阅者数量</returns>
        public int GetSubscriberCount<TEvent>() where TEvent : IEvent
        {
            Type eventType = typeof(TEvent);
            if (!_subscriptions.TryGetValue(eventType, out var handlers))
            {
                return 0;
            }

            int aliveCount = handlers.FindAll(weakRef => weakRef.IsAlive).Count;

            handlers.RemoveAll(weakRef => !weakRef.IsAlive);

            return aliveCount;
        }
    }
}