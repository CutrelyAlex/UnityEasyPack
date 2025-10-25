using System;

namespace EasyPack.GamePropertySystem
{
    /// <summary>
    /// 组合属性接口，支持复杂的属性计算和组合
    /// 用于处理复杂的游戏数值需求，如：
    /// - 增益Buff的增益效果翻倍（套娃）
    /// - 乘法减益百分比的数值增加（复杂计算）
    /// - 多层嵌套的伤害计算公式
    /// </summary>
    public interface ICombineGameProperty
    {
        /// <summary>
        /// 获取属性的唯一标识符
        /// </summary>
        string ID { get; }

        /// <summary>
        /// 获取结果持有者，存储计算后的最终值
        /// </summary>
        GameProperty ResultHolder { get; }

        /// <summary>
        /// 根据ID获取内部的 GameProperty
        /// </summary>
        /// <param name="id">属性ID，为空时返回 ResultHolder</param>
        /// <returns>对应的 GameProperty 实例</returns>
        GameProperty GetProperty(string id);

        /// <summary>
        /// 获取计算器函数，定义如何从组合属性计算最终值
        /// </summary>
        Func<ICombineGameProperty, float> Calculater { get; }

        /// <summary>
        /// 获取计算后的最终值（应用所有修饰符和计算规则）
        /// </summary>
        /// <returns>计算后的属性值</returns>
        float GetValue();

        /// <summary>
        /// 获取基础值（未应用修饰符）
        /// </summary>
        /// <returns>基础属性值</returns>
        float GetBaseValue();

        /// <summary>
        /// 检查组合属性对象是否有效
        /// </summary>
        /// <returns>有效返回 true，否则返回 false</returns>
        bool IsValid();

        /// <summary>
        /// 释放组合属性占用的资源，清理依赖关系
        /// </summary>
        void Dispose();
    }
}
