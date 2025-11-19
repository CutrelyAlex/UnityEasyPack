using System.Collections.Generic;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    /// 单个子条件取反：子条件不命中时为真；不返回匹配集合（始终为空）。
    /// 常用于“排除条件”。
    /// </summary>
    public sealed class NotRequirement : IRuleRequirement
    {
        public IRuleRequirement Inner { get; set; }

        public bool TryMatch(CardRuleContext ctx, out List<Card> matched)
        {
            matched = new List<Card>();
            if (Inner == null) return true; // 没有子条件则视为真
            return !Inner.TryMatch(ctx, out _);
        }
    }
}
