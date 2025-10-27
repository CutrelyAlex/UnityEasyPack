using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack.ENekoFramework
{
    /// <summary>
    /// 跨架构事件桥接器
    /// 允许不同架构之间通过显式注册和转发来传递事件
    /// </summary>
    public class EventBridge : IDisposable
    {
        private readonly Dictionary<string, List<Delegate>> _listeners = new Dictionary<string, List<Delegate>>();
        private readonly object _lock = new object();
        private bool _disposed = false;
        
        /// <summary>
        /// 注册事件监听器
        /// </summary>
        /// <typeparam name="TEventData">事件数据类型</typeparam>
        /// <param name="eventName">事件名称</param>
        /// <param name="listener">事件监听器回调</param>
        public void Register<TEventData>(string eventName, Action<string, TEventData> listener)
        {
            if (_disposed)
            {
                Debug.LogWarning("[EventBridge] Attempted to register listener after dispose");
                return;
            }
            
            if (string.IsNullOrEmpty(eventName))
            {
                throw new ArgumentNullException(nameof(eventName));
            }
            
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }
            
            lock (_lock)
            {
                if (!_listeners.ContainsKey(eventName))
                {
                    _listeners[eventName] = new List<Delegate>();
                }
                
                _listeners[eventName].Add(listener);
            }
        }
        
        /// <summary>
        /// 注销事件监听器
        /// </summary>
        /// <param name="eventName">事件名称</param>
        public void Unregister(string eventName)
        {
            if (_disposed)
            {
                return;
            }
            
            lock (_lock)
            {
                _listeners.Remove(eventName);
            }
        }
        
        /// <summary>
        /// 转发事件到所有已注册的监听器
        /// </summary>
        /// <typeparam name="TEventData">事件数据类型</typeparam>
        /// <param name="eventName">事件名称</param>
        /// <param name="data">事件数据</param>
        public void Forward<TEventData>(string eventName, TEventData data)
        {
            if (_disposed)
            {
                return;
            }
            
            if (string.IsNullOrEmpty(eventName))
            {
                Debug.LogWarning("[EventBridge] Attempted to forward event with null/empty name");
                return;
            }
            
            List<Delegate> listeners;
            
            lock (_lock)
            {
                if (!_listeners.TryGetValue(eventName, out listeners) || listeners.Count == 0)
                {
                    return;
                }
                
                listeners = new List<Delegate>(listeners);
            }
            
            // 在锁外调用监听器
            foreach (var listener in listeners)
            {
                try
                {
                    if (listener is Action<string, TEventData> typedListener)
                    {
                        typedListener.Invoke(eventName, data);
                    }
                    else
                    {
                        Debug.LogWarning($"[EventBridge] Listener for '{eventName}' has type mismatch. " +
                                       $"Expected: Action<string, {typeof(TEventData).Name}>, " +
                                       $"Actual: {listener.GetType().Name}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EventBridge] Exception in event listener for '{eventName}': {ex}");
                }
            }
        }
        
        /// <summary>
        /// 释放所有监听器
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            
            lock (_lock)
            {
                _listeners.Clear();
                _disposed = true;
            }
        }
    }
}
