using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EasyPack.Category;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     线程安全目标选择器：根据 TargetScope、FilterMode 等参数从上下文中选择卡牌。
    /// </summary>
    public static class TargetSelector
    {
        // Tag → 拥有该Tag的所有卡牌集合
        private static readonly ConcurrentDictionary<string, HashSet<Card>> _tagCardCache = new();

        // 缓存更新锁
        private static readonly ReaderWriterLockSlim _cacheLock = new(LockRecursionPolicy.SupportsRecursion);

        // 标记缓存是否已初始化
        private static volatile bool _isCacheInitialized = false;

        // CategoryManager 引用
        private static ICategoryManager<Card, int> _categoryManager;

        /// <summary>
        ///     设置 CategoryManager 引用。
        /// </summary>
        public static void SetCategoryManager(ICategoryManager<Card, int> categoryManager)
        {
            _categoryManager = categoryManager;
        }

        /// <summary>
        ///     初始化Tag→Card缓存。
        ///     应在系统初始化完成后调用一次。
        /// </summary>
        /// <param name="allCards">系统中所有已注册的卡牌</param>
        /// <param name="categoryManager">可选的 CategoryManager 引用</param>
        public static void InitializeTagCache(IEnumerable<Card> allCards, ICategoryManager<Card, int> categoryManager = null)
        {
            if (categoryManager != null)
                _categoryManager = categoryManager;

            _cacheLock.EnterWriteLock();
            try
            {
                _tagCardCache.Clear();

                if (allCards == null)
                {
                    _isCacheInitialized = true;
                    return;
                }

                foreach (Card card in allCards)
                {
                    if (card == null) continue;

                    // 优先使用 CategoryManager，回退到 Card.Tags
                    IEnumerable<string> tags = GetCardTags(card);
                    if (tags == null) continue;

                    foreach (string tag in tags)
                    {
                        if (string.IsNullOrEmpty(tag)) continue;
                        AddCardToTagCache(card, tag);
                    }
                }

                _isCacheInitialized = true;
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        /// <summary>
        ///     获取卡牌的标签。
        /// </summary>
        private static IEnumerable<string> GetCardTags(Card card)
        {
            if (card == null) return null;

            // 使用 CategoryManager 获取标签
            if (_categoryManager != null)
            {
                return _categoryManager.GetTags(card);
            }

            // 没有 CategoryManager 时返回空集合
            return Array.Empty<string>();
        }

        /// <summary>
        ///     向缓存添加卡牌-标签关系（内部方法，需在写锁内调用）。
        /// </summary>
        private static void AddCardToTagCache(Card card, string tag)
        {
            var cardSet = _tagCardCache.GetOrAdd(tag, _ => new HashSet<Card>());
            lock (cardSet)
            {
                cardSet.Add(card);
            }
        }

        /// <summary>
        ///     清除Tag→Card缓存
        /// </summary>
        public static void ClearTagCache()
        {
            _cacheLock.EnterWriteLock();
            try
            {
                _tagCardCache.Clear();
                _isCacheInitialized = false;
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        /// <summary>
        ///     当卡牌添加Tag时，更新缓存
        /// </summary>
        internal static void OnCardTagAdded(Card card, string tag)
        {
            if (!_isCacheInitialized || card == null || string.IsNullOrEmpty(tag))
                return;

            var cardSet = _tagCardCache.GetOrAdd(tag, _ => new HashSet<Card>());
            lock (cardSet)
            {
                cardSet.Add(card);
            }
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
                lock (cardSet)
                {
                    cardSet.Remove(card);
                }

                // 如果集合为空，尝试移除
                if (cardSet.Count == 0)
                {
                    _tagCardCache.TryRemove(tag, out _);
                }
            }
        }

        /// <summary>
        ///     获取拥有指定Tag的所有卡牌
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

            return Select(scope, filter, ctx.MatchRoot, filterValue, maxDepth ?? ctx.MaxDepth, ctx.Engine?.CategoryManager);
        }

        /// <summary>
        ///     根据作用域和过滤条件选择目标卡牌。
        /// </summary>
        public static IReadOnlyList<Card> Select(
            TargetScope scope,
            CardFilterMode filter,
            Card root,
            string filterValue = null,
            int maxDepth = int.MaxValue,
            ICategoryManager<Card, int> categoryManager = null)
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
                    // 创建副本以避免并发修改
                    candidates = new List<Card>(root.Children);
                    break;

                case TargetScope.Descendants:
                {
                    int depth = maxDepth;
                    if (depth <= 0) depth = int.MaxValue;
                    return ApplyFilter(TraversalUtil.EnumerateDescendantsAsList(root, depth), filter, filterValue, categoryManager);
                }

                default:
                    return Array.Empty<Card>();
            }

            // 第二步：根据 FilterMode 过滤
            return ApplyFilter(candidates, filter, filterValue, categoryManager);
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

            // 确定根容器：效果使用 EffectRoot
            Card root = selection.Root == SelectionRoot.Source ? ctx.Source : ctx.EffectRoot;
            if (root == null)
                return Array.Empty<Card>();

            // 选择目标
            var targets = Select(
                selection.Scope,
                selection.Filter,
                root,
                selection.FilterValue,
                selection.MaxDepth ?? ctx.MaxDepth,
                ctx.Engine?.CategoryManager
            );

            // 应用 Take 限制
            if (selection.Take is > 0 && targets.Count > selection.Take.Value)
            {
                int takeCount = selection.Take.Value;
                var limited = new List<Card>(takeCount);
                for (int i = 0; i < takeCount; i++)
                {
                    limited.Add(targets[i]);
                }
                return limited;
            }

            return targets;
        }



        /// <summary>
        ///     对已有的卡牌列表应用过滤条件（线程安全）。
        /// </summary>
        /// <param name="cards">要过滤的卡牌列表</param>
        /// <param name="filter">过滤模式</param>
        /// <param name="filterValue">过滤值</param>
        /// <param name="categoryManager">可选的 CategoryManager（用于标签和分类查询）</param>
        /// <returns>过滤后的卡牌列表</returns>
        public static IReadOnlyList<Card> ApplyFilter(
            IReadOnlyList<Card> cards,
            CardFilterMode filter,
            string filterValue,
            ICategoryManager<Card, int> categoryManager = null)
        {
            if (cards == null || cards.Count == 0)
                return Array.Empty<Card>();

            switch (filter)
            {
                case CardFilterMode.None:
                    return cards;

                case CardFilterMode.ByTag:
                    return FilterByTag(cards, filterValue, categoryManager);

                case CardFilterMode.ById:
                    return FilterById(cards, filterValue);

                case CardFilterMode.ByCategory:
                    return FilterByCategory(cards, filterValue, categoryManager);

                default:
                    return Array.Empty<Card>();
            }
        }

        /// <summary>
        ///     按标签过滤（线程安全）。
        /// </summary>
        private static IReadOnlyList<Card> FilterByTag(
            IReadOnlyList<Card> cards,
            string tag,
            ICategoryManager<Card, int> categoryManager)
        {
            if (string.IsNullOrEmpty(tag))
                return Array.Empty<Card>();

            // 优先使用 CategoryManager
            if (categoryManager != null)
            {
                var results = new List<Card>(cards.Count);
                for (int i = 0, count = cards.Count; i < count; i++)
                {
                    var card = cards[i];
                    if (categoryManager.HasTag(card, tag))
                        results.Add(card);
                }
                return results;
            }

            // 尝试使用缓存
            var cachedCardSet = GetCardsByTagFromCache(tag);
            if (cachedCardSet is { Count: > 0 })
            {
                // 使用缓存：取缓存中与候选集的交集
                var tagResults = new List<Card>(cards.Count);
                lock (cachedCardSet)
                {
                    for (int i = 0, count = cards.Count; i < count; i++)
                    {
                        var card = cards[i];
                        if (cachedCardSet.Contains(card))
                            tagResults.Add(card);
                    }
                }
                return tagResults;
            }

            // 回退：缓存未初始化，使用 Card.HasTag
            var fallbackResults = new List<Card>(cards.Count);
            for (int i = 0, count = cards.Count; i < count; i++)
            {
                var card = cards[i];
#pragma warning disable CS0618
                if (card.HasTag(tag))
                    fallbackResults.Add(card);
#pragma warning restore CS0618
            }
            return fallbackResults;
        }

        /// <summary>
        ///     按 ID 过滤。
        /// </summary>
        private static IReadOnlyList<Card> FilterById(IReadOnlyList<Card> cards, string id)
        {
            if (string.IsNullOrEmpty(id))
                return Array.Empty<Card>();

            var results = new List<Card>(cards.Count);
            for (int i = 0, count = cards.Count; i < count; i++)
            {
                var card = cards[i];
                if (string.Equals(card.Id, id, StringComparison.Ordinal))
                    results.Add(card);
            }
            return results;
        }

        /// <summary>
        ///     按分类过滤。使用 CategoryManager 检查卡牌是否属于指定分类。
        /// </summary>
        private static IReadOnlyList<Card> FilterByCategory(
            IReadOnlyList<Card> cards,
            string categoryStr,
            ICategoryManager<Card, int> categoryManager)
        {
            if (string.IsNullOrEmpty(categoryStr))
                return Array.Empty<Card>();

            // 使用 CategoryManager 检查分类
            if (categoryManager != null)
            {
                var results = new List<Card>(cards.Count);
                for (int i = 0, count = cards.Count; i < count; i++)
                {
                    var card = cards[i];
                    // 使用 IsInCategory 检查
                    if (categoryManager.IsInCategory(card, categoryStr, includeChildren: true))
                    {
                        results.Add(card);
                    }
                }
                return results;
            }

            // 回退：通过 DefaultCategory 匹配
            var catResults = new List<Card>(cards.Count);
            for (int i = 0, count = cards.Count; i < count; i++)
            {
                var card = cards[i];
                if (string.Equals(card.Data?.Category, categoryStr, StringComparison.OrdinalIgnoreCase))
                    catResults.Add(card);
            }
            return catResults;
        }
    }
}