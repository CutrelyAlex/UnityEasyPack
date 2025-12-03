using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using EasyPack.Architecture;
using EasyPack.Category;
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

        /// <summary>
        ///     异步初始化 CategoryService
        /// </summary>
        /// <returns>是否成功初始化</returns>
        public async Task<bool> InitializeCategoryServiceAsync()
        {
            try
            {
                ICategoryService categoryService = await EasyPackArchitecture.GetCategoryServiceAsync();
                if (categoryService == null)
                {
                    Debug.LogWarning("[CardEngine] 无法获取 CategoryService，继续使用本地 CategoryManager");
                    return false;
                }

                var serviceManager = categoryService.GetOrCreateManager<Card, long>(card => card.UID);
                if (serviceManager == null) return false;
                // 如果本地 CategoryManager 中有数据，需要迁移
                // 这里假设初始化时本地 Manager 是空的
                CategoryManager = serviceManager;
                Debug.Log("[CardEngine] 已切换到 CategoryService 托管的 CategoryManager");
                return true;

            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CardEngine] 初始化 CategoryService 失败: {ex.Message}");
                return false;
            }
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
        ///     可以通过 InitializeCategoryServiceAsync 切换到 CategoryService 托管模式。
        /// </summary>
        public ICategoryManager<Card, long> CategoryManager { get; private set; }

        #endregion

        #region 内部缓存字段

        // 已注册的卡牌集合
        private readonly HashSet<Card> _registeredCardsTemplates = new();


        // id->index集合缓存
        private readonly Dictionary<string, HashSet<int>> _idIndexes = new();

        // id->maxIndex缓存
        private readonly Dictionary<string, int> _idMaxIndexes = new();

        // id->Card列表缓存，用于快速查找
        private readonly Dictionary<string, List<Card>> _cardsById = new();


        // UID->Card缓存，支持 O(1) UID 查询
        private readonly Dictionary<long, Card> _cardsByUID = new();

        // 位置->Card映射（一个位置最多一个卡牌）,我们认为此处是主世界位置
        // 即子卡牌的位置不会出现在此映射中，子卡牌的位置通过父卡牌动态派生
        private readonly Dictionary<Vector3Int?, Card> _cardsByPosition = new();

        // UID->位置缓存
        private readonly Dictionary<long, Vector3Int?> _positionByUID = new();

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
        ///     按ID和Index精确查找卡牌。
        /// </summary>
        public Card GetCardByKey(string id, int index)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (Card card in _cardsById[id])
            {
                if (card.Index == index) return card;
            }
            return null;
        }

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

        #region 卡牌缓存处理

        /// <summary>
        ///     添加卡牌到引擎，分配唯一Index、UID并订阅事件。
        /// </summary>
        public CardEngine AddCard(Card card)
        {
            if (card == null) return this;

            // 设置卡牌的Engine引用
            card.Engine = this;

            string id = card.Id;

            // 第一步: 处理 UID（必须先分配 UID，因为 CategoryManager 使用 UID 作为键）
            if (card.UID < 0)
            {
                // 未分配 UID，分配新的
                EmeCardSystem.CardFactory.AssignUID(card);
            }
            else if (_cardsByUID.ContainsKey(card.UID))
            {
                // UID 冲突，重新分配
                card.UID = -1;
                EmeCardSystem.CardFactory.AssignUID(card);
            }

            // 第二步: 分配索引（如果还未分配）
            if (!_idIndexes.TryGetValue(id, out var indexes))
            {
                indexes = new();
                _idIndexes[id] = indexes;
                _idMaxIndexes[id] = -1;  // 初始化为 -1，第一个卡牌 Index 为 0
            }

            // 只有当 Index 未分配时才分配（Index < 0 表示未分配）
            if (card.Index < 0)
            {
                var maxIndex = _idMaxIndexes[id];
                card.Index = maxIndex + 1;
                _idMaxIndexes[id] = card.Index;
            }
            else if (indexes.Contains(card.Index))
            {
                // Index 已被占用，重新分配
                var maxIndex = _idMaxIndexes[id];
                card.Index = maxIndex + 1;
                _idMaxIndexes[id] = card.Index;
            }

            // 第三步: 订阅卡牌事件
            card.OnEvent += OnCardEvent;

            // 第四步: 添加到索引
            indexes.Add(card.Index);

            // 添加到 UID 索引
            _cardsByUID[card.UID] = card;

            // 更新_cardsById缓存
            if (!_cardsById.TryGetValue(id, out var cardList))
            {
                cardList = new();
                _cardsById[id] = cardList;
            }

            cardList.Add(card);

            // 第五步: 注册到 CategoryManager（使用 UID 作为键，保证绝对唯一）
            RegisterToCategoryManager(card);


            // 第六步: 将卡牌的所有标签加入TargetSelector缓存
            foreach (string tag in card.Tags)
            {
                TargetSelector.OnCardTagAdded(card, tag);
            }

            // 第七步: 递归注册所有已存在的子卡牌（处理工厂创建时已通过 AddChild 添加的子卡牌）
            if (card.Children is { Count: > 0 })
            {
                foreach (Card child in card.Children)
                {
                    if (child != null && !HasCard(child))
                    {
                        AddCard(child);
                    }
                }
            }

            // 第八步: 注册位置映射（仅对根卡牌，即没有 Owner 的卡牌）
            // 子卡牌与父卡牌在同一位置，不单独索引
            _positionByUID[card.UID] = card.Position;

            // 初始化 RootCard 引用（根卡牌指向自己）
            if (card.RootCard == null)
            {
                card.RootCard = card;
            }

            // 只有根卡牌才会被添加到位置索引中
            if (card.Owner != null)
            {
                return this; // 子卡牌不添加到位置索引
            }

            var initialPosition = card.Position;
            if (initialPosition == null)
            {
                return this; // 无位置的根卡牌也不添加到位置索引
            }

            // 如果位置已被占用，记录错误
            if (_cardsByPosition.TryGetValue(initialPosition, out Card existingCard))
            {
                Debug.LogError("[CardEngine] 位置冲突警告: 位置 " + initialPosition +
                               " 已被卡牌 '" + existingCard.Id + "' (UID: " + existingCard.UID +
                               ") 占用，无法放置卡牌 '" + card.Id + "' (UID: " + card.UID + ").");
                return this;
            }

            _cardsByPosition[initialPosition] = card;

            return this;
        }

        /// <summary>
        ///     将子卡牌添加到父卡牌，同时确保子卡牌已注册到引擎。
        ///     此方法会：1) 将子卡牌注册到引擎（分配 UID、注册到 CategoryManager）
        ///              2) 建立父子关系
        ///     如果子卡牌已有持有者，则会先将其从原持有者移除。
        /// </summary>
        /// <param name="parent">父卡牌（必须已注册到引擎）。</param>
        /// <param name="child">子卡牌。</param>
        /// <param name="intrinsic">是否作为固有子卡（固有子卡无法通过规则消耗或普通移除）。</param>
        /// <returns>当前引擎实例，支持链式调用。</returns>
        /// <exception cref="ArgumentNullException">parent 或 child 为 null。</exception>
        public CardEngine AddChildToCard(Card parent, Card child, bool intrinsic = false)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (child == null) throw new ArgumentNullException(nameof(child));

            // 验证父卡牌已注册到引擎
            if (!HasCard(parent))
            {
                throw new InvalidOperationException($"父卡牌 '{parent.Id}' 未注册到引擎，请先调用 AddCard。");
            }

            // 将子卡牌从原持有者移除（如果有）
            child.Owner?.RemoveChild(child);

            // 第一步：将子卡牌注册到引擎（如果尚未注册）
            if (!HasCard(child))
            {
                AddCard(child);
            }

            // 第二步：建立父子关系（AddChild 内部会处理位置继承）
            parent.AddChild(child, intrinsic);

            return this;
        }

        /// <summary>
        ///     批量将多个子卡牌添加到父卡牌。
        /// </summary>
        /// <param name="parent">父卡牌（必须已注册到引擎）。</param>
        /// <param name="children">子卡牌集合。</param>
        /// <param name="intrinsic">是否作为固有子卡。</param>
        /// <returns>当前引擎实例，支持链式调用。</returns>
        public CardEngine AddChildrenToCard(Card parent, IEnumerable<Card> children, bool intrinsic = false)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (children == null) throw new ArgumentNullException(nameof(children));

            foreach (Card child in children)
            {
                if (child != null)
                {
                    AddChildToCard(parent, child, intrinsic);
                }
            }

            return this;
        }

        /// <summary>
        ///     转移根卡牌到新位置，自动更新位置映射。
        ///     只能用于根卡牌（无 Owner 的卡牌）。
        ///     子卡牌会通过 RootCard 引用动态获取根卡牌位置，无需递归更新。
        /// </summary>
        /// <param name="card">要转移的根卡牌（必须无 Owner）。</param>
        /// <param name="newPosition">新位置</param>
        /// <returns>当前引擎实例，支持链式调用。</returns>
        public CardEngine MoveCardToPosition(Card card, Vector3Int newPosition)
        {
            if (card is not { Owner: null }) return this;

            var oldPosition = card.Position;

            // 如果位置相同，无需更新
            if (oldPosition.HasValue && oldPosition.Value == newPosition) return this;

            // 从旧位置移除
            if (oldPosition != null
                && _cardsByPosition.TryGetValue(oldPosition, out Card cardAtOldPosition)
                && cardAtOldPosition == card)
            {
                _cardsByPosition.Remove(oldPosition);
            }

            // 更新根卡牌位置
            card.Position = newPosition;

            // 添加根卡牌索引到新位置
            _cardsByPosition[newPosition] = card;
            _positionByUID[card.UID] = newPosition;

            // 子卡牌无需递归更新，它们通过 RootCard.Position 动态获取根卡牌位置

            return this;
        }

        /// <summary>
        ///     当卡牌被添加为子卡牌时，从位置索引中移除它。
        ///     只有根卡牌（无 Owner 的卡牌）才会被位置索引管理。
        /// </summary>
        /// <param name="child">被添加为子卡牌的卡牌。</param>
        internal void NotifyChildAddedToParent(Card child)
        {
            // 从位置索引中移除（因为子卡牌不应该在位置索引中）
            if (child?.Position != null &&
                _cardsByPosition.TryGetValue(child.Position, out Card cardAtPosition) &&
                cardAtPosition == child)
            {
                _cardsByPosition.Remove(child.Position);
            }
        }

        /// <summary>
        ///     清除根卡牌的位置，将其从位置索引中移除但保留在引擎中。
        ///     卡牌的 Position 会被设为 null。
        ///     子卡牌无需更新，它们通过 RootCard.Position 动态获取根卡牌位置。
        /// </summary>
        /// <param name="card">要清除位置的根卡牌（必须无 Owner）。</param>
        /// <returns>当前引擎实例，支持链式调用。</returns>
        public CardEngine ClearCardPosition(Card card)
        {
            if (card is not { Owner: null }) return this;

            var oldPosition = card.Position;
            if (oldPosition == null) return this;

            // 从位置索引中移除
            if (_cardsByPosition.TryGetValue(oldPosition, out Card cardAtPosition) && cardAtPosition == card)
            {
                _cardsByPosition.Remove(oldPosition);
            }

            // 清除卡牌位置
            card.Position = null;
            _positionByUID[card.UID] = null;

            // 子卡牌无需递归更新，它们通过 RootCard.Position 动态获取根卡牌位置

            return this;
        }

        /// <summary>
        ///     当子卡牌从父卡牌移除时，如果它有位置，可能需要重新添加到位置索引。
        ///     调用方负责在移除子卡牌后调用此方法（如果卡牌需要保留在世界中）。
        /// </summary>
        /// <param name="card">从父卡牌移除的卡牌。</param>
        /// <param name="newPosition">新的世界位置。</param>
        internal void NotifyChildRemovedFromParent(Card card, Vector3Int newPosition)
        {
            if (card is not { Owner: null }) return; // 必须已移除 Owner

            // 检查位置是否被占用
            if (_cardsByPosition.TryGetValue(newPosition, out Card existingCard) && existingCard != card)
            {
                Debug.LogWarning($"[CardEngine] 位置 {newPosition} 已被卡牌 '{existingCard.Id}' 占用，" +
                                 $"无法放置卡牌 '{card.Id}'。");
                return;
            }

            // 添加到位置索引
            card.Position = newPosition;
            _cardsByPosition[newPosition] = card;
            _positionByUID[card.UID] = newPosition;
        }

        /// <summary>
        ///     移除卡牌，移除事件订阅、UID 映射与索引。
        /// </summary>
        public CardEngine RemoveCard(Card c)
        {
            if (c == null || !_cardsByUID.ContainsKey(c.UID)) return this;

            c.OnEvent -= OnCardEvent;
            // 从 UID 索引中移除
            if (c.UID >= 0) _cardsByUID.Remove(c.UID);

            if (_idIndexes.TryGetValue(c.Id, out var indexes))
            {
                indexes.Remove(c.Index);
                if (indexes.Count == 0) _idIndexes.Remove(c.Id);
            }

            // 更新_cardsById缓存
            if (_cardsById.TryGetValue(c.Id, out var cardList))
            {
                cardList.Remove(c);
                if (cardList.Count == 0) _cardsById.Remove(c.Id);
            }

            // 从TargetSelector缓存中移除卡牌的标签（在注销前获取标签）
            // 复制标签到数组
            var tags = c.Tags;
            int tagCount = tags.Count;
            string[] tagArray = null;

            if (tagCount > 0)
            {
                tagArray = new string[tagCount];
                int idx = 0;
                foreach (string tag in tags)
                {
                    tagArray[idx++] = tag;
                }
            }

            // 从 CategoryManager 注销（使用 UID）
            UnregisterFromCategoryManager(c.UID);

            // 从位置映射中移除（仅针对根卡牌，子卡牌不在位置索引中）
            if (c.Owner == null && c.Position != null &&
                _cardsByPosition.TryGetValue(c.Position, out Card cardAtPosition) && cardAtPosition == c)
            {
                _cardsByPosition.Remove(c.Position);
            }
            _positionByUID.Remove(c.UID);

            // 移除标签缓存
            if (tagArray != null)
            {
                for (int i = 0; i < tagArray.Length; i++)
                {
                    TargetSelector.OnCardTagRemoved(c, tagArray[i]);
                }
            }

            return this;
        }

        public void ClearAllCards()
        {
            // 从 CategoryManager 批量注销所有卡牌
            foreach (var uid in _cardsByUID.Keys)
            {
                UnregisterFromCategoryManager(uid);
            }

            _idIndexes.Clear();
            _cardsById.Clear();
            _cardsByUID.Clear();
            _cardsByPosition.Clear();
            _positionByUID.Clear();
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

            // 使用 UID（int）注册实体，保证绝对唯一
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

            // 完成注册
            OperationResult result = registration.Complete();
            if (!result.IsSuccess)
            {
                Debug.LogWarning($"[CardEngine] CategoryManager 注册失败: Card UID={card.UID}, Id={card.Id}#{card.Index}, Error={result.ErrorMessage}");
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
