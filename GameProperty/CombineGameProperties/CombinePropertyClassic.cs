using System;
using System.Collections.Generic;

namespace EasyPack
{
    /// <summary>
    /// 经典RPG属性组合实现
    /// 公式：(基础+增益) × (1+增益倍率) - 减益 × (1+减益倍率)
    /// </summary>
    public class CombinePropertyClassic : CombineGameProperty
    {
        #region 私有字段

        /// <summary>
        /// 子属性字典
        /// </summary>
        private readonly Dictionary<string, GameProperty> _gameProperties = new Dictionary<string, GameProperty>();

        /// <summary>
        /// 事件处理器字典，用于管理事件订阅
        /// </summary>
        private readonly Dictionary<GameProperty, Action<float, float>> _eventHandlers = new();

        /// <summary>
        /// 缓存的组合值
        /// </summary>
        private float _cacheValue;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化经典组合属性
        /// </summary>
        /// <param name="id">属性ID</param>
        /// <param name="baseValue">基础值</param>
        /// <param name="configProperty">配置属性名称</param>
        /// <param name="increaseValue">增益值属性名称</param>
        /// <param name="increaseMul">增益倍率属性名称</param>
        /// <param name="decreaseValue">减益值属性名称</param>
        /// <param name="decreaseMul">减益倍率属性名称</param>
        public CombinePropertyClassic(
            string id,
            float baseValue,
            string configProperty,
            string increaseValue,
            string increaseMul,
            string decreaseValue,
            string decreaseMul)
            : base(id, baseValue)
        {
            // 创建并注册子属性
            RegisterProperty(new GameProperty(configProperty, baseValue));
            RegisterProperty(new GameProperty(increaseValue, 0));
            RegisterProperty(new GameProperty(increaseMul, 0));
            RegisterProperty(new GameProperty(decreaseValue, 0));
            RegisterProperty(new GameProperty(decreaseMul, 0));

            // 获取属性引用
            var baseProperty = GetProperty(configProperty);
            var baseBuffValue = GetProperty(increaseValue);
            var baseBuffMul = GetProperty(increaseMul);
            var deBuffValue = GetProperty(decreaseValue);
            var deBuffMul = GetProperty(decreaseMul);

            // 设置经典RPG计算公式
            Calculater = e =>
            {
                return (baseProperty.GetValue() + baseBuffValue.GetValue()) * (baseBuffMul.GetValue() + 1)
                       - deBuffValue.GetValue() * (1 + deBuffMul.GetValue());
            };

            // 订阅属性变化事件
            SubscribeToPropertyChanges();

            // 初始化缓存值
            _cacheValue = Calculater(this);
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
        /// 获取计算后的值，使用缓存优化
        /// </summary>
        protected override float GetCalculatedValue()
        {
            var calculatedValue = Calculater?.Invoke(this) ?? _baseCombineValue;
            ResultHolder.SetBaseValue(calculatedValue);
            return ResultHolder.GetValue();
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
        /// <returns>注册的属性</returns>
        public GameProperty RegisterProperty(GameProperty gameProperty)
        {
            ThrowIfDisposed();

            if (gameProperty == null)
                throw new ArgumentNullException(nameof(gameProperty));

            _gameProperties[gameProperty.ID] = gameProperty;

            // 如果已经完成初始化，需要重新订阅事件
            if (_eventHandlers.Count > 0)
            {
                SubscribeToPropertyChanges();
            }

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

        #region 私有方法

        /// <summary>
        /// 订阅子属性的变化事件
        /// </summary>
        private void SubscribeToPropertyChanges()
        {
            foreach (var prop in _gameProperties.Values)
            {
                // 如果已经订阅过，跳过
                if (_eventHandlers.ContainsKey(prop))
                    continue;

                var handler = new Action<float, float>((oldVal, newVal) =>
                {
                    var oldCombine = _cacheValue;
                    var newCombine = Calculater(this);
                    if (!oldCombine.Equals(newCombine))
                    {
                        _cacheValue = newCombine;
                        // 这里可以添加额外的变化处理逻辑
                    }
                });

                _eventHandlers[prop] = handler;
                prop.OnValueChanged += handler;
            }
        }

        #endregion
    }
}