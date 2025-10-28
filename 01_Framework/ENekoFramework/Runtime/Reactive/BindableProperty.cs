using System;
using System.Collections.Generic;

namespace EasyPack.ENekoFramework
{
    /// <summary>
    /// 支持变更通知的可绑定属性接口。
    /// </summary> 
    /// <typeparam name="T">属性值的类型</typeparam>
    public interface IBindableProperty<T>
    {
        /// <summary>
        /// 属性的当前值。
        /// </summary>
        T Value { get; set; }
    
        /// <summary>
        /// 属性值变更时触发的事件。
        /// </summary>
        event Action<T> OnValueChanged;

        /// <summary>
        /// 注册值变更监听器。
        /// </summary>
        /// <param name="listener">监听器对象（用于 WeakReference）</param>
        /// <param name="onChanged">值变更时要调用的回调</param>
        void Register(object listener, Action<T> onChanged);
    
        /// <summary>
        /// 注销监听器。
        /// </summary>
        /// <param name="listener">要移除的监听器对象</param>
        void Unregister(object listener);
    }

    /// <summary>
    /// 支持变更通知和批处理的可绑定属性实现。
    /// 对监听器使用 WeakReference 以防止内存泄漏。
    /// </summary>
    /// <typeparam name="T">属性值的类型</typeparam>
    public class BindableProperty<T> : IBindableProperty<T>
    {
        private T _value;
        private readonly List<WeakReference> _listeners = new List<WeakReference>();
        private readonly List<Action<T>> _callbacks = new List<Action<T>>();
        private readonly object _lock = new object();
        private bool _isBatching;
        private bool _hasChangedDuringBatch;

        /// <summary>
        /// 属性的当前值。
        /// 设置值会触发变更通知，除非启用了批处理。
        /// </summary>
        public T Value
        {
            get => _value;
            set
            {
                if (EqualityComparer<T>.Default.Equals(_value, value))
                {
                    return; // 无变更
                }

                _value = value;

                if (_isBatching)
                {
                    _hasChangedDuringBatch = true;
                }
                else
                {
                    NotifyListeners();
                }
            }
        }

        /// <summary>
        /// 属性值变更时触发的事件。
        /// </summary>
        public event Action<T> OnValueChanged;

        /// <summary>
        /// 使用初始值创建新的可绑定属性。
        /// </summary>
        /// <param name="initialValue">属性的初始值</param>
        public BindableProperty(T initialValue = default)
        {
            _value = initialValue;
        }

        /// <summary>
        /// 注册值变更监听器。
        /// </summary>
        /// <param name="listener">监听器对象（用于 WeakReference）</param>
        /// <param name="onChanged">值变更时要调用的回调</param>
        public void Register(object listener, Action<T> onChanged)
        {
            lock (_lock)
            {
                _listeners.Add(new WeakReference(listener));
                _callbacks.Add(onChanged);
            }
        }

        /// <summary>
        /// 注销监听器。
        /// </summary>
        /// <param name="listener">要移除的监听器对象</param>
        public void Unregister(object listener)
        {
            lock (_lock)
            {
                for (int i = _listeners.Count - 1; i >= 0; i--)
                {
                    if (!_listeners[i].IsAlive || _listeners[i].Target == listener)
                    {
                        _listeners.RemoveAt(i);
                        _callbacks.RemoveAt(i);
                    }
                }
            }
        }

        /// <summary>
        /// 开始批量更新。批处理期间的变更只会触发一次通知。
        /// 调用 EndBatch() 以提交变更并通知监听器。
        /// </summary>
        public void BeginBatch()
        {
            _isBatching = true;
            _hasChangedDuringBatch = false;
        }

        /// <summary>
        /// 结束批量更新，如果发生了任何变更则通知监听器。
        /// </summary>
        public void EndBatch()
        {
            _isBatching = false;

            if (_hasChangedDuringBatch)
            {
                NotifyListeners();
                _hasChangedDuringBatch = false;
            }
        }

        /// <summary>
        /// 设置值而不触发变更通知。
        /// 适用于初始化场景。
        /// </summary>
        /// <param name="value">新值</param>
        public void SetValueWithoutNotify(T value)
        {
            _value = value;
        }

        private void NotifyListeners()
        {
            // 调用事件订阅者
            OnValueChanged?.Invoke(_value);

            // 调用已注册的监听器
            lock (_lock)
            {
                var deadListeners = new List<int>();

                for (int i = 0; i < _listeners.Count; i++)
                {
                    if (!_listeners[i].IsAlive)
                    {
                        deadListeners.Add(i);
                        continue;
                    }

                    try
                    {
                        _callbacks[i]?.Invoke(_value);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"BindableProperty 监听器错误：{ex.Message}");
                    }
                }

                // 清理死监听器
                for (int i = deadListeners.Count - 1; i >= 0; i--)
                {
                    int index = deadListeners[i];
                    _listeners.RemoveAt(index);
                    _callbacks.RemoveAt(index);
                }
            }
        }

        /// <summary>
        /// 为方便起见隐式转换为值类型。
        /// </summary>
        public static implicit operator T(BindableProperty<T> property) => property.Value;
    }
}
