using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// 组合属性的基类
    /// </summary>
    public abstract class CombineGameProperty : ICombineGameProperty, IDisposable
    {
        #region 基础属性

        /// <summary>
        /// 属性的唯一标识符
        /// </summary>
        public string ID { get; protected set; }

        /// <summary>
        /// 计算器函数，用于计算组合属性的值
        /// </summary>
        public Func<ICombineGameProperty, float> Calculater { get; set; }

        /// <summary>
        /// 结果持有者，承载最终计算结果
        /// </summary>
        public GameProperty ResultHolder { get; protected set; }

        /// <summary>
        /// 基础组合值
        /// </summary>
        protected readonly float _baseCombineValue;

        /// <summary>
        /// 是否已释放资源
        /// </summary>
        protected bool _isDisposed = false;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化组合属性基类
        /// </summary>
        /// <param name="id">属性ID</param>
        /// <param name="baseValue">基础值</param>
        protected CombineGameProperty(string id, float baseValue = 0)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("ID cannot be null or empty", nameof(id));

            ID = id;
            _baseCombineValue = baseValue;
            ResultHolder = new GameProperty(id , baseValue);

            // 设置默认计算器
            Calculater = e => _baseCombineValue;
        }

        protected CombineGameProperty(GameProperty gameProperty, float baseValue = 0)
        {
            if (gameProperty == null) return;
            ID = gameProperty.ID;
            _baseCombineValue = baseValue;
            ResultHolder = gameProperty;

            Calculater = e => _baseCombineValue;
        }

        #endregion

        #region 获取

        /// <summary>
        /// 获取基础组合值
        /// </summary>
        public virtual float GetBaseValue() => _baseCombineValue;

        /// <summary>
        /// 获取计算后的属性值
        /// </summary>
        public virtual float GetValue()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);

            return GetCalculatedValue();
        }

        /// <summary>
        /// 获取内部属性，由子类实现具体逻辑
        /// </summary>
        public abstract GameProperty GetProperty(string id);

        /// <summary>
        /// 检查对象是否有效
        /// </summary>
        public virtual bool IsValid() => !_isDisposed && ResultHolder != null;

        public virtual Func<ICombineGameProperty, float> GetCalculater()
        {
            return Calculater;
        }

        #endregion

        #region 保护方法
        /// <summary>
        /// 获取计算后的值，由子类重写以实现不同的计算逻辑
        /// </summary>
        protected virtual float GetCalculatedValue()
        {
            var calculatedValue = Calculater?.Invoke(this) ?? _baseCombineValue;

            // 先获取当前的 ResultHolder 值
            var currentValue = ResultHolder.GetValue();

            // 设置新的基础值
            ResultHolder.SetBaseValue(calculatedValue);

            // 获取应用修饰器后的最终值
            var finalValue = ResultHolder.GetValue();

            return finalValue;
        }

        /// <summary>
        /// 检查是否已释放，如果是则抛出异常
        /// </summary>
        protected void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        #endregion

        #region 事件支持

        /// <summary>
        /// 当属性值发生变化时触发
        /// </summary>
        public event Action<float, float> OnValueChanged
        {
            add => ResultHolder.OnValueChanged += value;
            remove => ResultHolder.OnValueChanged -= value;
        }

        #endregion

        #region 修饰器支持

        /// <summary>
        /// 添加修饰器到ResultHolder
        /// </summary>
        public virtual void AddModifierToHolder(IModifier modifier)
        {
            ThrowIfDisposed();

            if (modifier == null)
                throw new ArgumentNullException(nameof(modifier));

            ResultHolder.AddModifier(modifier);
        }

        /// <summary>
        /// 从ResultHolder移除修饰器
        /// </summary>
        public virtual void RemoveModifierFromHolder(IModifier modifier)
        {
            if (_isDisposed || modifier == null) return;
            ResultHolder.RemoveModifier(modifier);
        }

        /// <summary>
        /// 清空ResultHolder的所有修饰器
        /// </summary>
        public virtual void ClearModifiersFromHolder()
        {
            if (_isDisposed) return;
            ResultHolder.ClearModifiers();
        }

        #endregion

        #region IDisposable 实现

        /// <summary>
        /// 释放资源
        /// </summary>
        public virtual void Dispose()
        {
            if (_isDisposed) return;

            // 调用子类的清理逻辑
            DisposeCore();

            // 清理ResultHolder
            ResultHolder?.ClearModifiers();

            _isDisposed = true;
        }

        /// <summary>
        /// 清理逻辑
        /// </summary>
        protected virtual void DisposeCore()
        {
            // 基类不需要额外的清理逻辑
        }

        #endregion
    }
}