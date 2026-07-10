using System.Collections.Generic;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     所有子条件全部命中才为真；匹配结果按子条件声明顺序聚合。
    ///     空子集视为“真”（真空真）。
    /// </summary>
    public sealed class AllRequirement : IRuleRequirement
    {
        public List<IRuleRequirement> Children { get; } = new();

        public bool TryMatch(CardRuleContext ctx, out List<Card> matched)
        {
            matched = new();
            if (Children == null || Children.Count == 0) return true; // 真空真

            foreach (IRuleRequirement child in Children)
            {
                if (child == null) return false;
                if (!child.TryMatch(ctx, out var picks)) return false;
                if (picks is { Count: > 0 })
                {
                    matched.AddRange(picks);
                }
            }

            return true;
        }
    }
}
