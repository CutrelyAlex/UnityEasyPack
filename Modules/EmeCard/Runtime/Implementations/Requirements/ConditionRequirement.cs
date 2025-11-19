using System;
using System.Collections.Generic;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    /// 自定义条件要求（不基于子卡筛选）<br></br>
    /// 支持两种模式：<br></br>
    /// 1. 仅条件判断 - 返回 bool<br></br>
    /// 2. 条件判断 + 卡牌选择 - 返回 (bool matched, List&lt;Card&gt; cards)<br></br>
    /// </summary>
    public sealed class ConditionRequirement : IRuleRequirement
    {
        private readonly Func<CardRuleContext, (bool matched, List<Card> cards)> _tuplePredicate;
        private readonly Func<CardRuleContext, bool> _boolPredicate;

        /// <summary>构造函数：仅条件判断</summary>
        /// <param name="condition">条件判断委托</param>
        public ConditionRequirement(Func<CardRuleContext, bool> condition)
        {
            _boolPredicate = condition ?? throw new ArgumentNullException(nameof(condition));
            _tuplePredicate = null;
        }

        /// <summary>构造函数：条件判断 + 卡牌选择</summary>
        /// <param name="predicate">返回匹配结果和卡牌列表的委托</param>
        public ConditionRequirement(Func<CardRuleContext, (bool matched, List<Card> cards)> predicate)
        {
            _tuplePredicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
            _boolPredicate = null;
        }

        public bool TryMatch(CardRuleContext ctx, out List<Card> matched)
        {
            if (_tuplePredicate != null)
            {
                var (isMatched, cards) = _tuplePredicate(ctx);
                matched = cards ?? new List<Card>();
                return isMatched;
            }
            else
            {
                matched = new List<Card>();
                return _boolPredicate(ctx);
            }
        }
    }
}
