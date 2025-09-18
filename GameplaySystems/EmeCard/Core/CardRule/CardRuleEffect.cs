using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyPack
{
    // 决定效果作用对象
    public enum TargetKind
    {
        Matched,            // 匹配到的卡
        Source,             // 触发源
        Container,          // 匹配容器本体
        ContainerChildren,  // 容器内所有子卡
        ByTag,              // 按标签过滤容器内子卡
        ById                // 按ID过滤容器内子卡
    }

    // 规则效果接口
    public interface IRuleEffect
    {
        void Execute(CardRuleContext ctx, IReadOnlyList<Card> matched);
    }
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
                    return ctx.Container != null ? (IReadOnlyList<Card>)ctx.Container.Children.ToList() : Array.Empty<Card>();
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