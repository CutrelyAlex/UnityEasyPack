using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using EasyPack.Architecture;
using EasyPack.Category;
using EasyPack.CustomData;
using UnityEngine;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     卡牌引擎核心类 - 主文件（初始化、属性、字段、卡牌管理）
    /// </summary>
    public sealed partial class CardEngine
    {
        #region 初始化

        public CardEngine(CardFactory factory)
        {
            _cardFactory = factory;
            // 注入工厂到序列化器
            CardJsonSerializer.Factory = factory;

            CategoryManager = new CategoryManager<Card, long>(card => card.UID);

            PreCacheAllCardTemplates();
            InitializeTargetSelectorCache();

            // 预注册标准事件类型
            _rules[CardEventTypes.TICK] = new();
            _rules[CardEventTypes.ADDED_TO_OWNER] = new();
            _rules[CardEventTypes.REMOVED_FROM_OWNER] = new();
            _rules[CardEventTypes.USE] = new();

            // 预注册 Pump 生命周期事件类型
            _rules[CardEventTypes.PUMP_START] = new();
            _rules[CardEventTypes.PUMP_END] = new();
        }

        public void Init()
        {
            PreCacheAllCardTemplates();
            InitializeTargetSelectorCache();
        }

        /// <summary>
        ///     初始化TargetSelector的Tag缓存。应在所有卡牌注册完成后调用。
        /// </summary>
        private void InitializeTargetSelectorCache()
        {
            // 同时传递 CategoryManager 以支持线程安全的标签查询
            TargetSelector.InitializeTagCache(_registeredCardsTemplates, CategoryManager);
        }

        /// <summary>
        ///     清除TargetSelector的Tag缓存
        /// </summary>
        public void ClearTargetSelectorCache()
        {
            TargetSelector.ClearTagCache();
        }

        /// <summary>
        ///     从工厂创建所有卡牌的副本并缓存。
        ///     应在系统初始化时调用一次。
        /// </summary>
        private void PreCacheAllCardTemplates()
        {
            _registeredCardsTemplates.Clear();

            // 获取工厂中所有注册的卡牌ID
            var cardIds = _cardFactory?.GetAllCardIds();
            if (cardIds == null || cardIds.Count == 0) return;

            foreach (string id in cardIds)
            {
                // 为每个ID创建一个副本
                Card templateCard = _cardFactory.Create(id);
                if (templateCard != null) _registeredCardsTemplates.Add(templateCard);
            }
        }

        #endregion

        #region 基本属性

        /// <summary>
        ///     卡牌工厂。
        /// </summary>
        private readonly ICardFactory _cardFactory;

        /// <summary>
        ///     卡牌工厂注册接口
        /// </summary>
        public ICardFactoryRegistry CardFactory => _cardFactory as ICardFactoryRegistry;

        /// <summary>
        ///     分类管理系统，用于统一管理卡牌的分类和标签。
        ///     提供基于标签的 O(1) 查询和基于层级分类的 O(log n) 查询。
        /// </summary>
        public ICategoryManager<Card, long> CategoryManager { get; private set; }

        #endregion

        #region 卡牌创建

        /// <summary>
        ///     按ID创建并注册卡牌实例。
        /// </summary>
        public T CreateCard<T>(string id) where T : Card
        {
            if (id == null) throw new ArgumentNullException(nameof(id));

            T card = null;
            if (_cardFactory != null) card = _cardFactory.Create<T>(id);

            if (card == null) return null;

            // AddCard 会设置 Engine 引用并注册到 CategoryManager
            AddCard(card);

            return card;
        }

        /// <summary>
        ///     按ID创建并注册Card类型的卡牌。
        /// </summary>
        public Card CreateCard(string id) => CreateCard<Card>(id);

        #endregion

        #region 查询服务

        /// <summary>
        ///     获取所有在卡牌工厂中注册的卡牌模板。
        /// </summary>
        public HashSet<Card> GetAllCardsTemplates() => _registeredCardsTemplates;

        /// <summary>
        ///     获取所有的卡牌。
        /// </summary>
        public IEnumerable<Card> GetAllCards() => _cardsByUID.Values;

        /// <summary>
        ///     按ID和Index精确查找卡牌。
        /// </summary>
        public Card GetCardByKey(string id, int index) =>
            string.IsNullOrEmpty(id) ? null : _cardsByKey.GetValueOrDefault((id, index));

        /// <summary>
        ///     按ID返回所有已注册卡牌。
        /// </summary>
        public IEnumerable<Card> GetCardsById(string id)
        {
            if (string.IsNullOrEmpty(id)) yield break;
            if (_cardsById.TryGetValue(id, out var cards))
            {
                foreach (Card card in cards)
                {
                    yield return card;
                }
            }
        }

        /// <summary>
        ///     按ID返回第一个已注册卡牌。
        /// </summary>
        public Card GetCardById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_cardsById.TryGetValue(id, out var cards) && cards.Count > 0) return cards[0];

            return null;
        }

        /// <summary>
        ///     检查指定卡牌是否接入引擎
        /// </summary>
        /// <returns></returns>
        public bool HasCard(Card card) => card != null && GetCardByUID(card.UID) != null;

        /// <summary>
        ///     根据 UID 获取卡牌。
        /// </summary>
        /// <param name="uid">卡牌的唯一标识符。</param>
        /// <returns>找到的卡牌，或 null 如果未找到。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Card GetCardByUID(long uid)
        {
            _cardsByUID.TryGetValue(uid, out Card card);
            return card;
        }

        /// <summary>
        ///     按标签查询卡牌。
        /// </summary>
        /// <param name="tag">要查询的标签。</param>
        /// <returns>包含该标签的所有卡牌列表。</returns>
        public IReadOnlyList<Card> GetCardsByTag(string tag) =>
            string.IsNullOrEmpty(tag) ? Array.Empty<Card>() : CategoryManager.GetByTag(tag);

        /// <summary>
        ///     按分类查询卡牌，支持通配符匹配和子分类包含。
        /// </summary>
        /// <param name="pattern">分类名称或通配符模式（如 "Object"、"Creature.*"）。</param>
        /// <param name="includeChildren">是否包含子分类中的卡牌。</param>
        /// <returns>匹配分类的所有卡牌列表。</returns>
        public IReadOnlyList<Card> GetCardsByCategory(string pattern, bool includeChildren = false) =>
            string.IsNullOrEmpty(pattern)
                ? Array.Empty<Card>()
                : CategoryManager.GetByCategory(pattern, includeChildren);

        /// <summary>
        ///     按分类和标签的交集查询卡牌。
        /// </summary>
        /// <param name="category">分类名称。</param>
        /// <param name="tag">标签名称。</param>
        /// <param name="includeChildren">是否包含子分类。</param>
        /// <returns>同时匹配分类和标签的卡牌列表。</returns>
        public IReadOnlyList<Card> GetCardsByCategoryAndTag(string category, string tag, bool includeChildren = true)
        {
            if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(tag)) return Array.Empty<Card>();
            return CategoryManager.GetByCategoryAndTag(category, tag, includeChildren);
        }

        /// <summary>
        ///     按位置查询卡牌。
        /// </summary>
        /// <param name="position">要查询的位置。</param>
        /// <returns>在该位置的卡牌，如果未找到返回null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Card GetCardByPosition(Vector3Int position)
        {
            _cardsByPosition.TryGetValue(position, out Card card);
            return card;
        }

        /// <summary>
        ///     按UID查询卡牌的位置。
        /// </summary>
        /// <param name="uid">卡牌的UID。</param>
        /// <returns>卡牌所在位置，如果未找到或UID无效返回 null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3Int? GetPositionByUID(long uid)
        {
            if (uid < 0) return null;
            return _positionByUID.TryGetValue(uid, out var position) ? position : null;
        }

        #endregion

        #region CategoryManager 集成

        /// <summary>
        ///     将卡牌注册到 CategoryManager，使用 UID 作为唯一标识。
        /// </summary>
        /// <param name="card">要注册的卡牌。</param>
        private void RegisterToCategoryManager(Card card)
        {
            if (card == null) return;

            string category = ExtractCategoryPath(card);

            // 使用 UID注册实体，保证唯一
            IEntityRegistration registration = CategoryManager.RegisterEntity(card, category);

            // 应用运行时的DefaultTags（来自CardData）
            if (card.Data?.DefaultTags != null)
            {
                foreach (string tag in card.Data.DefaultTags)
                {
                    if (!string.IsNullOrWhiteSpace(tag))
                    {
                        registration = registration.WithTags(tag);
                    }
                }
            }

            // 待应用的额外标签
            if (card.PendingExtraTags != null && card.PendingExtraTags.Count > 0)
            {
                foreach (string tag in card.PendingExtraTags)
                {
                    if (string.IsNullOrWhiteSpace(tag)) continue;

                    registration = registration.WithTags(tag);
                }

                // 清空临时标签列表
                card.PendingExtraTags = null;
            }

            // 应用默认的metadata
            CustomDataCollection defaultMetaData = card.Data?.DefaultMetaData;
            if (defaultMetaData != null)
            {
                registration = registration.WithMetadata(defaultMetaData.Clone());
            }

            // 完成注册
            OperationResult result = registration.Complete();
            if (!result.IsSuccess)
            {
                Debug.LogWarning(
                    $"[CardEngine] CategoryManager 注册失败: Card UID={card.UID}, Id={card.Id}#{card.Index}, Error={result.ErrorMessage}");
            }
        }

        /// <summary>
        ///     从 CategoryManager 注销卡牌（使用 UID）。
        /// </summary>
        /// <param name="card">要注销的卡牌。</param>
        private void UnregisterFromCategoryManager(long uid)
        {
            if (uid < 0) return;

            // 使用 UID（int）删除实体
            OperationResult result = CategoryManager.DeleteEntity(uid);
            if (!result.IsSuccess && result.ErrorCode != ErrorCode.NotFound)
            {
                Debug.LogWarning($"[CardEngine] CategoryManager 注销失败: Card UID={uid}, Error={result.ErrorMessage}");
            }
        }

        /// <summary>
        ///     从卡牌数据中提取分类路径字符串。
        /// </summary>
        /// <param name="card">卡牌实例。</param>
        /// <returns>分类路径字符串。</returns>
        private static string ExtractCategoryPath(Card card)
        {
            if (card?.Data == null)
            {
                return CardData.DEFAULT_CATEGORY;
            }

            // 优先使用新的 DefaultCategory，如果为空则回退到 DEFAULT_CATEGORY
            return string.IsNullOrEmpty(card.Data.Category)
                ? CardData.DEFAULT_CATEGORY
                : card.Data.Category;
        }

        #endregion
    }
}