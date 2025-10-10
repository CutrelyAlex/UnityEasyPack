using System;

namespace EasyPack
{
    /// <summary>
    /// 单一属性组合实现
    /// 仅包含单一 GameProperty，直接返回该属性的值作为最终结果
    /// 适用于无需属性组合，仅需单属性表现的简单场景
    /// </summary>
    public class CombinePropertySingle : CombineGameProperty
    {
        #region 构造函数

        /// <summary>
        /// 初始化单一组合属性
        /// </summary>
        /// <param name="id">属性ID</param>
        /// <param name="baseValue">基础值</param>
        public CombinePropertySingle(string id, float baseValue = 0)
            : base(id, baseValue)
        {
            // 单一属性直接返回ResultHolder的值
            Calculater = e => ResultHolder.GetValue();
        }

        #endregion

        #region 获取

        /// <summary>
        /// 获取内部属性，Single类型只返回ResultHolder
        /// </summary>
        public override GameProperty GetProperty(string id = "")
        {
            ThrowIfDisposed();
            return ResultHolder;
        }

        /// <summary>
        /// 获取计算后的值，直接返回ResultHolder的值
        /// </summary>
        protected override float GetCalculatedValue()
        {
            return ResultHolder.GetValue();
        }

        #endregion

        #region 修饰符

        /// <summary>
        /// 设置基础值
        /// </summary>
        /// <param name="value">新的基础值</param>
        /// <returns>返回自身</returns>
        public CombinePropertySingle SetBaseValue(float value)
        {
            ThrowIfDisposed();
            ResultHolder.SetBaseValue(value);
            return this;
        }

        /// <summary>
        /// 添加修饰符
        /// </summary>
        /// <param name="modifier">要添加的修饰符</param>
        /// <returns>返回自身</returns>
        public CombinePropertySingle AddModifier(IModifier modifier)
        {
            ThrowIfDisposed();
            ResultHolder.AddModifier(modifier);
            return this;
        }

        /// <summary>
        /// 移除修饰符
        /// </summary>
        /// <param name="modifier">要移除的修饰符</param>
        /// <returns>返回自身</returns>
        public CombinePropertySingle RemoveModifier(IModifier modifier)
        {
            ThrowIfDisposed();
            ResultHolder.RemoveModifier(modifier);
            return this;
        }

        /// <summary>
        /// 清除所有修饰符
        /// </summary>
        /// <returns>返回自身</returns>
        public CombinePropertySingle ClearModifiers()
        {
            ThrowIfDisposed();
            ResultHolder.ClearModifiers();
            return this;
        }

        /// <summary>
        /// 订阅值变化事件
        /// </summary>
        /// <param name="handler">事件处理器</param>
        /// <returns>返回自身</returns>
        public CombinePropertySingle SubscribeValueChanged(Action<float, float> handler)
        {
            ThrowIfDisposed();
            if (handler != null)
            {
                ResultHolder.OnValueChanged += handler;
            }
            return this;
        }

        #endregion
    }
}