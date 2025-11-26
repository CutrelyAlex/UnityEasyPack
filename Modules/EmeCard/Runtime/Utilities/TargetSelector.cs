using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     目标选择器：根据 TargetScope、FilterMode 等参数从上下文中选择卡牌。
    /// </summary>
    public static class TargetSelector
    {
        // Tag → 拥有该Tag的所有卡牌集合
        private static readonly Dictionary<string, HashSet<Card>> _tagCardCache = new();

        // 标记缓存是否已初始化
        private static bool _isCacheInitialized = false;

        /// <summary>
        ///     初始化Tag→Card缓存。应
        ///     在系统初始化完成后调用一次。
        /// </summary>
        /// <param name="allCards">系统中所有已注册的卡牌</param>
        public static void InitializeTagCache(IEnumerable<Card> allCards)
        {
            _tagCardCache.Clear();

            if (allCards == null) return;

            foreach (Card card in allCards)
            {
                if (card == null || card.Tags == null) continue;

                foreach (string tag in card.Tags)
                {
                    if (string.IsNullOrEmpty(tag)) continue;

                    if (!_tagCardCache.TryGetValue(tag, out var cardSet))
                    {
                        cardSet = new();
                        _tagCardCache[tag] = cardSet;
                    }

                    cardSet.Add(card);
                }
            }

            _isCacheInitialized = true;
        }

        /// <summary>
        ///     清除Tag→Card缓存
        /// </summary>
        public static void ClearTagCache()
        {
            _tagCardCache.Clear();
            _isCacheInitialized = false;
        }

        /// <summary>
        ///     当卡牌添加Tag时，更新缓存
        /// </summary>
        internal static void OnCardTagAdded(Card card, string tag)
        {
            if (!_isCacheInitialized || card == null || string.IsNullOrEmpty(tag))
                return;

            if (!_tagCardCache.TryGetValue(tag, out var cardSet))
            {
                cardSet = new();
                _tagCardCache[tag] = cardSet;
            }

            cardSet.Add(card);
        }

        /// <summary>
        ///     当卡牌移除Tag时，更新缓存
        /// </summary>
        internal static void OnCardTagRemoved(Card card, string tag)
        {
            if (!_isCacheInitialized || card == null || string.IsNullOrEmpty(tag))
                return;

            if (_tagCardCache.TryGetValue(tag, out var cardSet))
            {
                cardSet.Remove(card);
                if (cardSet.Count == 0) _tagCardCache.Remove(tag);
            }
        }

        /// <summary>
        ///     获取拥有指定Tag的所有卡牌（来自缓存）
        /// </summary>
        private static HashSet<Card> GetCardsByTagFromCache(string tag)
        {
            if (!_isCacheInitialized || string.IsNullOrEmpty(tag))
                return null;

            _tagCardCache.TryGetValue(tag, out var cardSet);
            return cardSet;
        }

        /// <summary>
        ///     根据作用域和过滤条件选择目标卡牌。
        /// </summary>
        /// <param name="scope">选择范围（Matched/Children/Descendants）</param>
        /// <param name="filter">过滤模式（None/ByTag/ById/ByCategory）</param>
        /// <param name="ctx">规则上下文</param>
        /// <param name="filterValue">过滤值（标签名/ID/Category名）</param>
        /// <param name="maxDepth">递归最大深度（仅对 Descendants 生效）</param>
        /// <returns>符合条件的卡牌列表</returns>
        public static IReadOnlyList<Card> Select(
            TargetScope scope,
            CardFilterMode filter,
            CardRuleContext ctx,
            string filterValue = null,
            int? maxDepth = null)
        {
            if (ctx == null || ctx.MatchRoot == null)
                return Array.Empty<Card>();

            return Select(scope, filter, ctx.MatchRoot, filterValue, maxDepth ?? ctx.MaxDepth);
        }

        /// <summary>
        ///     根据作用域和过滤条件选择目标卡牌。
        /// </summary>
        public static IReadOnlyList<Card> Select(
            TargetScope scope,
            CardFilterMode filter,
            Card root,
            string filterValue = null,
            int maxDepth = int.MaxValue)
        {
            if (root == null)
                return Array.Empty<Card>();

            // 特殊处理：Matched 不应该在这里处理，由调用方直接使用匹配结果
            if (scope == TargetScope.Matched) return Array.Empty<Card>();

            List<Card> candidates;

            // 第一步：根据 Scope 选择候选集
            switch (scope)
            {
                case TargetScope.Children:
                    candidates = new(root.Children);
                    break;

                case TargetScope.Descendants:
                {
                    int depth = maxDepth;
                    if (depth <= 0) depth = int.MaxValue;
                    candidates = TraversalUtil.EnumerateDescendants(root, depth).ToList();
                }
                    break;

                default:
                    return Array.Empty<Card>();
            }

            // 第二步：根据 FilterMode 过滤
            return ApplyFilter(candidates, filter, filterValue);
        }

        /// <summary>
        ///     供效果使用的选择方法：根据 ITargetSelection 配置构建局部上下文并选择目标。
        /// </summary>
        /// <param name="selection">目标选择配置</param>
        /// <param name="ctx">当前规则上下文</param>
        /// <returns>符合条件的卡牌列表</returns>
        public static IReadOnlyList<Card> SelectForEffect(ITargetSelection selection, CardRuleContext ctx)
        {
            if (selection == null || ctx == null)
                return Array.Empty<Card>();

            // Matched 由调用方处理
            if (selection.Scope == TargetScope.Matched)
                return Array.Empty<Card>();

            // 确定根容器
            Card root = selection.Root == SelectionRoot.Source ? ctx.Source : ctx.MatchRoot;
            if (root == null)
                return Array.Empty<Card>();

            // 构建局部上下文
            var localCtx = new CardRuleContext(
                ctx.Source,
                root,
                ctx.Event,
                ctx.Factory,
                selection.MaxDepth ?? ctx.MaxDepth
            );

            // 选择目标
            var targets = Select(
                selection.Scope,
                selection.Filter,
                localCtx,
                selection.FilterValue,
                selection.MaxDepth
            );

            // 应用 Take 限制
            if (selection.Take is > 0 && targets.Count > selection.Take.Value)
                return targets.Take(selection.Take.Value).ToList();

            return targets;
        }

        private static bool TryParseCategory(string value, out CardCategory cat)
        {
            cat = default;
            if (string.IsNullOrEmpty(value)) return false;
            return Enum.TryParse(value, true, out cat);
        }

        /// <summary>
        ///     对已有的卡牌列表应用过滤条件。
        /// </summary>
        /// <param name="cards">要过滤的卡牌列表</param>
        /// <param name="filter">过滤模式</param>
        /// <param name="filterValue">过滤值</param>
        /// <returns>过滤后的卡牌列表</returns>
        public static IReadOnlyList<Card> ApplyFilter(IReadOnlyList<Card> cards, CardFilterMode filter,
                                                      string filterValue)
        {
            if (cards == null || cards.Count == 0)
                return Array.Empty<Card>();

            switch (filter)
            {
                case CardFilterMode.None:
                    return cards;

                case CardFilterMode.ByTag:
                    if (string.IsNullOrEmpty(filterValue))
                        return Array.Empty<Card>();

                    // 尝试使用缓存
                    var cachedCardSet = GetCardsByTagFromCache(filterValue);
                    if (cachedCardSet is { Count: > 0 })
                    {
                        // 使用缓存：取缓存中与候选集的交集
                        var tagResults = new List<Card>(Math.Min(cards.Count, cachedCardSet.Count));
                        for (int i = 0; i < cards.Count; i++)
                            if (cachedCardSet.Contains(cards[i]))
                                tagResults.Add(cards[i]);

                        return tagResults;
                    }

                    // 回退：缓存未初始化，使用原有逻辑
                    var tagResultsFallback = new List<Card>(cards.Count / 2);
                    for (int i = 0; i < cards.Count; i++)
                        if (cards[i].HasTag(filterValue))
                            tagResultsFallback.Add(cards[i]);

                    return tagResultsFallback;

                case CardFilterMode.ById:
                    if (string.IsNullOrEmpty(filterValue))
                        return Array.Empty<Card>();
                    var idResults = new List<Card>(cards.Count / 4);
                    for (int i = 0; i < cards.Count; i++)
                        if (string.Equals(cards[i].Id, filterValue, StringComparison.Ordinal))
                            idResults.Add(cards[i]);

                    return idResults;

                case CardFilterMode.ByCategory:
                    if (TryParseCategory(filterValue, out CardCategory cat))
                    {
                        var catResults = new List<Card>(cards.Count / 2);
                        for (int i = 0; i < cards.Count; i++)
                            if (cards[i].Category == cat)
                                catResults.Add(cards[i]);

                        return catResults;
                    }

                    return Array.Empty<Card>();

                default:
                    return Array.Empty<Card>();
            }
        }
    }
}