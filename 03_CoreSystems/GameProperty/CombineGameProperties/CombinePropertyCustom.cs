using System;
using System.Collections.Generic;

namespace EasyPack
{
    /// <summary>
    /// 自定义组合属性实现
    /// 支持完全自定义的计算逻辑
    /// </summary>
    public class CombinePropertyCustom : CombineGameProperty
    {
        #region 私有字段

        /// <summary>
        /// 子属性字典
        /// </summary>
        private readonly Dictionary<string, GameProperty> _gameProperties = new();

        /// <summary>
        /// 事件处理器字典
        /// </summary>
        private readonly Dictionary<GameProperty, Action<float, float>> _eventHandlers = new();

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化自定义组合属性
        /// </summary>
        /// <param name="id">属性ID</param>
        /// <param name="baseValue">基础值</param>
        public CombinePropertyCustom(string id, float baseValue = 0)
            : base(id, baseValue)
        {
            // 默认计算器
            Calculater = (combine) =>
            {
                float sum = _baseCombineValue;
                foreach (var prop in _gameProperties.Values)
                {
                    sum += prop.GetValue();
                }
                return sum;
            };
        }

        #endregion

        #region 重写方法

        /// <summary>
        /// 获取指定ID的子属性
        /// </summary>
        public override GameProperty GetProperty(string id)
        {
            ThrowIfDisposed();
            return _gameProperties.TryGetValue(id, out var property) ? property : null;
        }

        /// <summary>
        /// 释放资源时的特定清理逻辑
        /// </summary>
        protected override void DisposeCore()
        {
            // 清理事件订阅
            foreach (var kvp in _eventHandlers)
            {
                kvp.Key.OnValueChanged -= kvp.Value;
            }
            _eventHandlers.Clear();

            // 清理子属性
            _gameProperties.Clear();
        }

        #endregion

        #region 属性管理

        /// <summary>
        /// 注册子属性
        /// </summary>
        /// <param name="gameProperty">要注册的属性</param>
        /// <param name="handler">可选的自定义事件处理器，接收组合属性的旧值和新值</param>
        /// <returns>注册的属性</returns>
        public GameProperty RegisterProperty(GameProperty gameProperty, Action<float, float> handler = null)
        {
            ThrowIfDisposed();

            if (gameProperty == null)
                throw new ArgumentNullException(nameof(gameProperty));

            _gameProperties[gameProperty.ID] = gameProperty;

            Action<float, float> autoSyncHandler = (oldVal, newVal) =>
            {
                var oldCombineValue = ResultHolder.GetValue();

                var newCombineValue = GetValue();

                handler?.Invoke(oldCombineValue, newCombineValue);
            };

            _eventHandlers[gameProperty] = autoSyncHandler;
            gameProperty.OnValueChanged += autoSyncHandler;

            // 首次注册立即计算
            GetValue();

            return gameProperty;
        }

        /// <summary>
        /// 取消注册子属性
        /// </summary>
        /// <param name="gameProperty">要取消注册的属性</param>
        public void UnRegisterProperty(GameProperty gameProperty)
        {
            if (gameProperty == null || _isDisposed) return;

            if (_gameProperties.Remove(gameProperty.ID) && _eventHandlers.TryGetValue(gameProperty, out var handler))
            {
                gameProperty.OnValueChanged -= handler;
                _eventHandlers.Remove(gameProperty);
            }
        }

        #endregion
    }
}