
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
    public interface ICombineGameProperty
    {
        string ID { get; }
        GameProperty ResultHolder { get; }
        GameProperty GetProperty(string id);
        Func<ICombineGameProperty, float> Calculater { get; }
        float GetValue();
        float GetBaseValue();
        bool IsValid();
        public void Dispose();
    }
}