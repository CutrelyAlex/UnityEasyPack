using System.Collections.Generic;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     任意一个子条件命中即为真；匹配结果按子条件声明顺序聚合。
    ///     空子集视为“假”。
    /// </summary>
    public sealed class AnyRequirement : IRuleRequirement
    {
        public List<IRuleRequirement> Children { get; } = new();

        public bool TryMatch(CardRuleContext ctx, out List<Card> matched)
        {
            matched = new();
            if (Children == null || Children.Count == 0) return false;

            bool any = false;
            foreach (IRuleRequirement child in Children)
            {
                if (child == null) continue;
                if (child.TryMatch(ctx, out var picks))
                {
                    any = true;
                    if (picks is { Count: > 0 })
                    {
                        matched.AddRange(picks);
                    }
                }
            }

            return any;
        }
    }
}
