using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack
{
    public class CombinePropertyCustom : ICombineGameProperty
    {
        public Dictionary<string, GameProperty> GameProperties { get; set; } = new Dictionary<string, GameProperty>();
        public string ID { get; }

        private readonly float _baseCombineValue;
        public float GetBaseValue() => _baseCombineValue;
        public Func<ICombineGameProperty, float> Calculater { get; set; }

        private readonly GameProperty _resultHolder;
        public GameProperty ResultHolder => _resultHolder;

        private readonly Dictionary<GameProperty, Action<float, float>> _eventHandlers = new();
        private readonly List<string> _managedPropertyIds = new List<string>(); // 跟踪管理的属性ID
        private bool _isDisposed = false;

        private float _cacheValue;

        /// <summary>
        /// 当组合属性值发生变化时触发
        /// </summary>
        public event Action<float, float> OnValueChanged;

        public bool IsValid() => !_isDisposed && _resultHolder != null;

        public float GetValue()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(CombinePropertyCustom));

            var newValue = Calculater?.Invoke(this) ?? _baseCombineValue;
            _resultHolder.SetBaseValue(newValue);
            return _resultHolder.GetValue();
        }

        public CombinePropertyCustom(string id, float baseValue = 0)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("ID cannot be null or empty", nameof(id));

            ID = id;
            _baseCombineValue = baseValue;
            _cacheValue = baseValue;

            _resultHolder = CombineGamePropertyManager.GetOrCreateGameProperty(id + "@ResultHolder", baseValue);
            _managedPropertyIds.Add(id + "@ResultHolder");

            Calculater = e => _baseCombineValue;
        }

        /// <summary>
        /// 注册GameProperty实例，优先使用缓存系统
        /// </summary>
        /// <param name="gameProperty">要注册的GameProperty实例</param>
        /// <returns>注册的GameProperty实例</returns>
        public GameProperty RegisterProperty(GameProperty gameProperty)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(CombinePropertyCustom));

            if (gameProperty == null)
                throw new ArgumentNullException(nameof(gameProperty));

            GameProperties[gameProperty.ID] = gameProperty;

            if (!_managedPropertyIds.Contains(gameProperty.ID))
            {
                _managedPropertyIds.Add(gameProperty.ID);
                CombineGamePropertyManager.GetOrCreateGameProperty(gameProperty.ID, gameProperty.GetBaseValue());
            }

            // 创建事件处理器，当子属性变化时重新计算组合属性
            var handler = new Action<float, float>((oldVal, newVal) =>
            {
                var oldCombineValue = _cacheValue;
                var newCombineValue = Calculater?.Invoke(this) ?? _baseCombineValue;

                if (!Mathf.Approximately(oldCombineValue, newCombineValue))
                {
                    _cacheValue = newCombineValue;
                    _resultHolder.SetBaseValue(newCombineValue);

                    OnValueChanged?.Invoke(oldCombineValue, newCombineValue);
                }
            });

            _eventHandlers[gameProperty] = handler;
            gameProperty.OnValueChanged += handler;

            return gameProperty;
        }

        /// <summary>
        /// 使用缓存系统注册新的GameProperty
        /// </summary>
        /// <param name="id">GameProperty的ID</param>
        /// <param name="baseValue">基础值</param>
        /// <returns>注册的GameProperty实例</returns>
        public GameProperty RegisterProperty(string id, float baseValue = 0f)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(CombinePropertyCustom));

            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("ID cannot be null or empty", nameof(id));

            var gameProperty = CombineGamePropertyManager.GetOrCreateGameProperty(id, baseValue);
            GameProperties[id] = gameProperty;

            if (!_managedPropertyIds.Contains(id))
            {
                _managedPropertyIds.Add(id);
            }

            // 当子属性变化时重新计算组合属性
            var handler = new Action<float, float>((oldVal, newVal) =>
            {
                var oldCombineValue = _cacheValue;
                var newCombineValue = Calculater?.Invoke(this) ?? _baseCombineValue;

                if (!Mathf.Approximately(oldCombineValue, newCombineValue))
                {
                    _cacheValue = newCombineValue;
                    _resultHolder.SetBaseValue(newCombineValue);
                    OnValueChanged?.Invoke(oldCombineValue, newCombineValue);
                }
            });

            _eventHandlers[gameProperty] = handler;
            gameProperty.OnValueChanged += handler;

            return gameProperty;
        }

        /// <summary>
        /// 取消注册GameProperty
        /// </summary>
        /// <param name="gameProperty">要取消注册的GameProperty</param>
        public void UnRegisterProperty(GameProperty gameProperty)
        {
            if (gameProperty == null || _isDisposed) return;

            if (GameProperties.Remove(gameProperty.ID) && _eventHandlers.TryGetValue(gameProperty, out var handler))
            {
                gameProperty.OnValueChanged -= handler;
                _eventHandlers.Remove(gameProperty);
            }

            if (_managedPropertyIds.Remove(gameProperty.ID))
            {
                CombineGamePropertyManager.ReleaseGameProperty(gameProperty.ID);
            }
        }

        /// <summary>
        /// 根据ID取消注册GameProperty
        /// </summary>
        /// <param name="id">GameProperty的ID</param>
        public void UnRegisterProperty(string id)
        {
            if (string.IsNullOrEmpty(id) || _isDisposed) return;

            if (GameProperties.TryGetValue(id, out var gameProperty))
            {
                UnRegisterProperty(gameProperty);
            }
        }

        public GameProperty GetProperty(string id) =>
            _isDisposed ? null : GameProperties.TryGetValue(id, out var property) ? property : null;

        /// <summary>
        /// 获取或创建GameProperty
        /// </summary>
        /// <param name="id">GameProperty的ID</param>
        /// <param name="baseValue">基础值</param>
        /// <returns>GameProperty实例</returns>
        public GameProperty GetOrCreateProperty(string id, float baseValue = 0f)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(CombinePropertyCustom));

            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("ID cannot be null or empty", nameof(id));

            if (GameProperties.TryGetValue(id, out var property))
            {
                return property;
            }

            return RegisterProperty(id, baseValue);
        }

        /// <summary>
        /// 手动触发重新计算
        /// 用于在修改 Calculater 后强制更新
        /// </summary>
        public void ForceRecalculate()
        {
            if (_isDisposed) return;

            var oldValue = _cacheValue;
            var newValue = Calculater?.Invoke(this) ?? _baseCombineValue;

            if (!Mathf.Approximately(oldValue, newValue))
            {
                _cacheValue = newValue;
                _resultHolder.SetBaseValue(newValue);
                OnValueChanged?.Invoke(oldValue, newValue);
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            // 取消订阅事件
            foreach (var kvp in _eventHandlers)
            {
                kvp.Key.OnValueChanged -= kvp.Value;
            }
            _eventHandlers.Clear();
            GameProperties.Clear();

            // 减少所有管理的GameProperty的引用计数
            foreach (var propertyId in _managedPropertyIds)
            {
                CombineGamePropertyManager.ReleaseGameProperty(propertyId);
            }
            _managedPropertyIds.Clear();

            _isDisposed = true;
        }
    }
}