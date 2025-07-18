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

        #region 修改/获取

        /// <summary>
        /// 设置ResultHolder的基础值
        /// </summary>
        /// <param name="value">新的基础值</param>
        public void SetBaseValue(float value)
        {
            ThrowIfDisposed();
            ResultHolder.SetBaseValue(value);
        }

        /// <summary>
        /// 获取ResultHolder的引用，用于外部操作
        /// </summary>
        /// <returns>ResultHolder的引用</returns>
        public GameProperty GetResultHolder()
        {
            ThrowIfDisposed();
            return ResultHolder;
        }

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
    }
}