using System.Collections.Generic;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    /// 所有子条件全部命中才为真；匹配结果为所有子条件的匹配集合并（去重）。
    /// 空子集视为“真”（真空真）。
    /// </summary>
    public sealed class AllRequirement : IRuleRequirement
    {
        public List<IRuleRequirement> Children { get; } = new List<IRuleRequirement>();

        public bool TryMatch(CardRuleContext ctx, out List<Card> matched)
        {
            matched = new List<Card>();
            if (Children == null || Children.Count == 0) return true; // 真空真

            // 创建共享缓存供所有子条件复用
            var sharedCache = new SelectionCache();

            var set = new HashSet<Card>();
            foreach (var child in Children)
            {
                if (child == null) return false;

                // 如果子条件是 CardsRequirement，传入共享缓存
                List<Card> picks;
                bool childMatched;
                if (child is CardsRequirement cardsReq)
                {
                    childMatched = cardsReq.TryMatchWithCache(ctx, out picks, sharedCache);
                }
                else
                {
                    childMatched = child.TryMatch(ctx, out picks);
                }

                if (!childMatched) return false;

                if (picks != null && picks.Count > 0)
                {
                    foreach (var c in picks) set.Add(c);
                }
            }
            if (set.Count > 0) matched.AddRange(set);
            return true;
        }
    }
}
