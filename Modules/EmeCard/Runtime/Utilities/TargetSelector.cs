using System;
using System.Collections.Generic;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     根据 TargetScope、FilterMode 等参数从上下文中按稳定顺序选择卡牌。
    ///     Tag/Category 基于 CardData 筛选。
    /// </summary>
    public static class TargetSelector
    {
        public static IReadOnlyList<Card> Select(
            TargetScope scope,
            CardFilterMode filter,
            CardRuleContext ctx,
            string filterValue = null,
            int? maxDepth = null) =>
            ctx?.MatchRoot == null
                ? Array.Empty<Card>()
                : Select(scope, filter, ctx.MatchRoot, filterValue, maxDepth ?? ctx.MaxDepth);

        public static IReadOnlyList<Card> Select(
            TargetScope scope,
            CardFilterMode filter,
            Card root,
            string filterValue = null,
            int maxDepth = int.MaxValue)
        {
            if (root == null || scope == TargetScope.Matched)
            {
                return Array.Empty<Card>();
            }

            IReadOnlyList<Card> candidates;
            switch (scope)
            {
                case TargetScope.Children:
                    candidates = new List<Card>(root.Children);
                    break;

                case TargetScope.Descendants:
                    int depth = maxDepth <= 0 ? int.MaxValue : maxDepth;
                    candidates = TraversalUtil.EnumerateDescendantsAsList(root, depth);
                    break;

                default:
                    return Array.Empty<Card>();
            }

            return ApplyFilter(candidates, filter, filterValue);
        }

        public static IReadOnlyList<Card> SelectForEffect(ITargetSelection selection, CardRuleContext ctx)
        {
            if (selection == null || ctx == null || selection.Scope == TargetScope.Matched)
            {
                return Array.Empty<Card>();
            }

            Card root = selection.Root == SelectionRoot.Source ? ctx.Source : ctx.EffectRoot;
            if (root == null)
            {
                return Array.Empty<Card>();
            }

            IReadOnlyList<Card> targets = Select(
                selection.Scope,
                selection.Filter,
                root,
                selection.FilterValue,
                selection.MaxDepth ?? ctx.MaxDepth);

            if (selection.Take is not > 0 || targets.Count <= selection.Take.Value)
            {
                return targets;
            }

            int takeCount = selection.Take.Value;
            var limited = new List<Card>(takeCount);
            for (int i = 0; i < takeCount; i++)
            {
                limited.Add(targets[i]);
            }

            return limited;
        }

        public static IReadOnlyList<Card> ApplyFilter(
            IReadOnlyList<Card> cards,
            CardFilterMode filter,
            string filterValue)
        {
            if (cards == null || cards.Count == 0)
            {
                return Array.Empty<Card>();
            }

            if (filter == CardFilterMode.None)
            {
                return new List<Card>(cards);
            }

            if (string.IsNullOrEmpty(filterValue))
            {
                return Array.Empty<Card>();
            }

            var results = new List<Card>();
            foreach (Card card in cards)
            {
                if (Matches(card, filter, filterValue))
                {
                    results.Add(card);
                }
            }

            return results;
        }

        private static bool Matches(Card card, CardFilterMode filter, string filterValue)
        {
            if (card == null)
            {
                return false;
            }

            switch (filter)
            {
                case CardFilterMode.ByTag:
                    return card.HasTag(filterValue);

                case CardFilterMode.ById:
                    return string.Equals(card.Id, filterValue, StringComparison.Ordinal);

                case CardFilterMode.ByCategory:
                    string cardCategory = card.Data?.Category;
                    return !string.IsNullOrEmpty(cardCategory) &&
                           (string.Equals(cardCategory, filterValue, StringComparison.OrdinalIgnoreCase) ||
                            cardCategory.StartsWith(filterValue + ".", StringComparison.OrdinalIgnoreCase));

                default:
                    return false;
            }
        }
    }
}
