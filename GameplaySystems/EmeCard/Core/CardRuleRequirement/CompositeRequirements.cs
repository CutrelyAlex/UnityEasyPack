using System.Collections.Generic;
using System.Linq;

namespace EasyPack
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

            var set = new HashSet<Card>();
            foreach (var child in Children)
            {
                if (child == null) return false;
                if (!child.TryMatch(ctx, out var picks)) return false;
                if (picks != null && picks.Count > 0)
                {
                    foreach (var c in picks) set.Add(c);
                }
            }
            if (set.Count > 0) matched.AddRange(set);
            return true;
        }
    }

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