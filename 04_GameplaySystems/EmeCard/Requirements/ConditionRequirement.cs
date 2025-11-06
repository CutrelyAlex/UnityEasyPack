using System;
using System.Collections.Generic;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    /// 自定义条件要求（不基于子卡筛选）：
    /// - 使用外部提供的 Func&lt;CardRuleContext, bool&gt; 进行布尔校验；
    /// - 默认不返回匹配卡集合（返回空集合）。
    /// </summary>
    public sealed class ConditionRequirement : IRuleRequirement
    {
        public Func<CardRuleContext, bool> Condition { get; }
        
        public Func<CardRuleContext,List<Card>> Matched { get; }

        public ConditionRequirement(Func<CardRuleContext, bool> condition, Func<CardRuleContext,List<Card>> matched=null)
        {
            Condition = condition ?? throw new ArgumentNullException(nameof(condition));
            Matched = matched ?? (ctx => new List<Card>());
        }

        public bool TryMatch(CardRuleContext ctx, out List<Card> matched)
        {
            matched = Matched(ctx);
            return Condition(ctx);
        }
    }
}
