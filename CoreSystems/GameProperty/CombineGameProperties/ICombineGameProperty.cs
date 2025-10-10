using System;
/// 这种组合式的Property，是为了应对例如这种需求：
///     策划：我想要一张卡牌，这张卡牌让 增益Buff 的 增益效果 翻倍 （套娃）
///     策划：我想要一张卡牌，这张卡牌让 乘法减益百分比 的 数值 增加 1% (复杂需求)
/// 甚至是：
///     策划：我要一个卡牌，这个卡牌可以让 反击伤害 增加  2*（2倍的攻击力 + 2 + 50% 的暴击伤害）*（1+50%）- 防御力*（1+50%）
///                                                         再并乘以攻击力的 50% 的百分比
///                                                         （特别复杂需求）
namespace EasyPack
{
    /// <summary>
    /// 组合属性
    /// </summary>
    public interface ICombineGameProperty
    {
        /// <summary>
        /// 属性的唯一标识符
        /// </summary>
        string ID { get; }
        
        /// <summary>
        /// 结果持有者，存储计算后的最终值
        /// </summary>
        GameProperty ResultHolder { get; }
        
        /// <summary>
        /// 获取内部属性
        /// </summary>
        GameProperty GetProperty(string id);
        
        /// <summary>
        /// 计算器函数，定义如何计算组合属性的值
        /// </summary>
        Func<ICombineGameProperty, float> Calculater { get; }
        
        /// <summary>
        /// 获取计算后的最终值
        /// </summary>
        float GetValue();
        
        /// <summary>
        /// 获取基础值
        /// </summary>
        float GetBaseValue();
        
        /// <summary>
        /// 检查对象是否有效
        /// </summary>
        bool IsValid();
        
        /// <summary>
        /// 释放资源
        /// </summary>
        void Dispose();
    }
}