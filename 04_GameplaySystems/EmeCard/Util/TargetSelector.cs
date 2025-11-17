using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    /// 缓存子树枚举结果，避免同一上下文中重复遍历
    /// </summary>
    public sealed class SelectionCache
    {
        private readonly Dictionary<long, List<Card>> _descendantCache = new();

        /// <summary>
        /// 生成缓存键
        /// 容器ID的哈希 + 深度。
        /// </summary>
        private static long MakeCacheKey(Card container, int maxDepth)
        {
            return ((long)container.GetHashCode() << 32) | (uint)maxDepth;
        }

        /// <summary>
        /// 获取或构建指定容器和深度的子树列表
        /// </summary>
        public List<Card> GetOrBuildDescendants(Card container, int maxDepth)
        {
            long key = MakeCacheKey(container, maxDepth);
            if (_descendantCache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var list = new List<Card>();
            foreach (var card in TraversalUtil.EnumerateDescendants(container, maxDepth))
            {
                list.Add(card);
            }

            _descendantCache[key] = list;
            return list;
        }

        /// <summary>
        /// 清除所有缓存
        /// 通常在规则处理完毕后调用
        /// </summary>
        public void Clear()
        {
            _descendantCache.Clear();
        }
    }

    /// <summary>
    /// 目标选择器：根据 TargetScope、FilterMode 等参数从上下文中选择卡牌。
    /// </summary>
    public static class TargetSelector
    {
        /// <summary>
        /// 根据作用域和过滤条件选择目标卡牌。
        /// </summary>
        /// <param name="scope">选择范围（Matched/Children/Descendants）</param>
        /// <param name="filter">过滤模式（None/ByTag/ById/ByCategory）</param>
        /// <param name="ctx">规则上下文</param>
        /// <param name="filterValue">过滤值（标签名/ID/Category名）</param>
        /// <param name="maxDepth">递归最大深度（仅对 Descendants 生效）</param>
        /// <param name="limit">最大返回数量（可选，<=0 表示无限制）</param>
        /// <param name="cache">可选缓存实例，用于复用子树枚举结果</param>
        /// <returns>符合条件的卡牌列表</returns>
        public static IReadOnlyList<Card> Select(
            TargetScope scope,
            CardFilterMode filter,
            CardRuleContext ctx,
            string filterValue = null,
            int? maxDepth = null,
            int limit = 0,
            SelectionCache cache = null)
        {
            if (ctx == null || ctx.Container == null)
                return Array.Empty<Card>();

            // 特殊处理：Matched 不应该在这里处理，由调用方直接使用匹配结果
            if (scope == TargetScope.Matched)
            {
                return Array.Empty<Card>();
            }

            // 单次遍历流式筛选
            var results = new List<Card>();

            switch (scope)
            {
                case TargetScope.Children:
                    {
                        var children = ctx.Container.Children;
                        for (int i = 0; i < children.Count; i++)
                        {
                            if (MatchesFilter(children[i], filter, filterValue))
                            {
                                results.Add(children[i]);
                                if (limit > 0 && results.Count >= limit)
                                    return results;
                            }
                        }
                    }
                    break;

                case TargetScope.Descendants:
                    {
                        int depth = maxDepth ?? ctx.MaxDepth;
                        if (depth <= 0) depth = int.MaxValue;

                        // 若提供缓存，优先复用已缓存的子树列表
                        if (cache != null)
                        {
                            var descendants = cache.GetOrBuildDescendants(ctx.Container, depth);
                            for (int i = 0; i < descendants.Count; i++)
                            {
                                if (MatchesFilter(descendants[i], filter, filterValue))
                                {
                                    results.Add(descendants[i]);
                                    if (limit > 0 && results.Count >= limit)
                                        return results;
                                }
                            }
                        }
                        else
                        {
                            // 无缓存则流式遍历
                            foreach (var card in TraversalUtil.EnumerateDescendants(ctx.Container, depth))
                            {
                                if (MatchesFilter(card, filter, filterValue))
                                {
                                    results.Add(card);
                                    if (limit > 0 && results.Count >= limit)
                                        return results;
                                }
                            }
                        }
                    }
                    break;

                default:
                    return Array.Empty<Card>();
            }

            return results;
        }

        /// <summary>
        /// 供效果使用的选择方法：根据 ITargetSelection 配置构建局部上下文并选择目标。
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
            Card root = selection.Root == SelectionRoot.Source ? ctx.Source : ctx.Container;
            if (root == null)
                return Array.Empty<Card>();

            // 构建局部上下文
            var localCtx = new CardRuleContext(
                source: ctx.Source,
                container: root,
                evt: ctx.Event,
                factory: ctx.Factory,
                maxDepth: selection.MaxDepth ?? ctx.MaxDepth
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
            if (selection.Take.HasValue && selection.Take.Value > 0 && targets.Count > selection.Take.Value)
            {
                return targets.Take(selection.Take.Value).ToList();
            }

            return targets;
        }

        /// <summary>
        /// 过滤判断
        /// </summary>
        private static bool MatchesFilter(Card card, CardFilterMode filter, string filterValue)
        {
            switch (filter)
            {
                case CardFilterMode.None:
                    return true;

                case CardFilterMode.ByTag:
                    return !string.IsNullOrEmpty(filterValue) && card.HasTag(filterValue);

                case CardFilterMode.ById:
                    return !string.IsNullOrEmpty(filterValue) && string.Equals(card.Id, filterValue, StringComparison.Ordinal);

                case CardFilterMode.ByCategory:
                    return TryParseCategory(filterValue, out var cat) && card.Category == cat;

                default:
                    return false;
            }
        }

        private static bool TryParseCategory(string value, out CardCategory cat)
        {
            cat = default;
            if (string.IsNullOrEmpty(value)) return false;
            return Enum.TryParse(value, true, out cat);
        }

        /// <summary>
        /// 对已有的卡牌列表应用过滤条件。
        /// </summary>
        /// <param name="cards">要过滤的卡牌列表</param>
        /// <param name="filter">过滤模式</param>
        /// <param name="filterValue">过滤值</param>
        /// <returns>过滤后的卡牌列表</returns>
        public static IReadOnlyList<Card> ApplyFilter(IReadOnlyList<Card> cards, CardFilterMode filter, string filterValue)
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
                    var tagResults = new List<Card>(cards.Count / 2);
                    for (int i = 0; i < cards.Count; i++)
                    {
                        if (cards[i].HasTag(filterValue))
                            tagResults.Add(cards[i]);
                    }
                    return tagResults;

                case CardFilterMode.ById:
                    if (string.IsNullOrEmpty(filterValue))
                        return Array.Empty<Card>();
                    var idResults = new List<Card>(cards.Count / 4);
                    for (int i = 0; i < cards.Count; i++)
                    {
                        if (string.Equals(cards[i].Id, filterValue, StringComparison.Ordinal))
                            idResults.Add(cards[i]);
                    }
                    return idResults;

                case CardFilterMode.ByCategory:
                    if (TryParseCategory(filterValue, out var cat))
                    {
                        var catResults = new List<Card>(cards.Count / 2);
                        for (int i = 0; i < cards.Count; i++)
                        {
                            if (cards[i].Category == cat)
                                catResults.Add(cards[i]);
                        }
                        return catResults;
                    }
                    return Array.Empty<Card>();

                default:
                    return Array.Empty<Card>();
            }
        }
    }
}
