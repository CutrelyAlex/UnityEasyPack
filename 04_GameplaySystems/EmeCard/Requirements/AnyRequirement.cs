using System.Collections.Generic;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    /// 任意一个子条件命中即为真；匹配结果为所有命中子条件的匹配集合并（去重）。
    /// 空子集视为“假”。
    /// </summary>
    public sealed class AnyRequirement : IRuleRequirement
    {
        public List<IRuleRequirement> Children { get; } = new List<IRuleRequirement>();

        public bool TryMatch(CardRuleContext ctx, out List<Card> matched)
        {
            matched = new List<Card>();
            if (Children == null || Children.Count == 0) return false;

            bool any = false;
            var set = new HashSet<Card>();
            foreach (var child in Children)
            {
                if (child == null) continue;
                if (child.TryMatch(ctx, out var picks))
                {
                    any = true;
                    if (picks != null && picks.Count > 0)
                    {
                        foreach (var c in picks) set.Add(c);
                    }
                }
            }
            if (any && set.Count > 0) matched.AddRange(set);
            return any;
        }
    }
}
