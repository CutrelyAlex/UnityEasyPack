using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyPack
{
    public static class TargetSelector
    {
        public static IReadOnlyList<Card> Select(TargetKind kind, CardRuleContext ctx, string value = null)
        {
            switch (kind)
            {
                case TargetKind.Source:
                    return ctx.Source != null ? new[] { ctx.Source } : Array.Empty<Card>();
                case TargetKind.Container:
                    return ctx.Container != null ? new[] { ctx.Container } : Array.Empty<Card>();
                case TargetKind.ContainerChildren:
                    return ctx.Container != null ? ctx.Container.Children.ToList() : Array.Empty<Card>();
                case TargetKind.ByTag:
                    return ctx.Container != null && !string.IsNullOrEmpty(value)
                        ? ctx.Container.Children.Where(c => c.HasTag(value)).ToList()
                        : Array.Empty<Card>();
                case TargetKind.ById:
                    return ctx.Container != null && !string.IsNullOrEmpty(value)
                        ? ctx.Container.Children.Where(c => string.Equals(c.Id, value, StringComparison.Ordinal)).ToList()
                        : Array.Empty<Card>();
                default:
                    return Array.Empty<Card>();
            }
        }
    }
}