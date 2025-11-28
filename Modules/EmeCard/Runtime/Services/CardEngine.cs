using System;
using System.Collections.Generic;
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
        /// <summary>
        ///     虚空位置：子卡牌所在的位置（z=-1）。
        ///     子卡牌不占据实际地图位置，而是在虚空中。
        /// </summary>
        public static readonly Vector3 VOID_POSITION = new(0, 0, -1);

        #region 初始化

        public CardEngine(ICardFactory factory)
        {
            CardFactory = factory;
            factory.Owner = this;

            // TODO: 创建本地 CategoryManager（稍后可通过 InitializeCategoryServiceAsync 切换到服务托管）
            CategoryManager = new CategoryManager<Card, int>(card => card.UID);

            PreCacheAllCardTemplates();
            InitializeTargetSelectorCache();

            // 预注册标准事件类型
            _rules[CardEventTypes.TICK] = new();
            _rules[CardEventTypes.ADDED_TO_OWNER] = new();
            _rules[CardEventTypes.REMOVED_FROM_OWNER] = new();
            _rules[CardEventTypes.USE] = new();
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

                var serviceManager = categoryService.GetOrCreateManager<Card, int>(card => card.UID);
                if (serviceManager != null)
                {
                    // 如果本地 CategoryManager 中有数据，需要迁移
                    // 这里假设初始化时本地 Manager 是空的
                    CategoryManager = serviceManager;
                    Debug.Log("[CardEngine] 已切换到 CategoryService 托管的 CategoryManager");
                    return true;
                }

                return false;
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
            var cardIds = CardFactory?.GetAllCardIds();
            if (cardIds == null || cardIds.Count == 0) return;

            foreach (string id in cardIds)
            {
                // 为每个ID创建一个副本
                Card templateCard = CardFactory.Create(id);
                if (templateCard != null) _registeredCardsTemplates.Add(templateCard);
            }
        }

        #endregion

        #region 基本属性

        public ICardFactory CardFactory { get; set; }

        /// <summary>
        ///     分类管理系统，用于统一管理卡牌的分类和标签。
        ///     提供基于标签的 O(1) 查询和基于层级分类的 O(log n) 查询。
        ///     可以通过 InitializeCategoryServiceAsync 切换到 CategoryService 托管模式。
        /// </summary>
        public ICategoryManager<Card, int> CategoryManager { get; private set; }

        /// <summary>
        ///     引擎全局策略
        /// </summary>
        public EnginePolicy Policy { get; } = new();

        // Pump 状态标志
        private bool _isPumping;
        private bool _isFlushingEffectPool; // 防止效果池嵌套刷新

        // 分帧处理相关
        private float _frameStartTime; // 当前帧开始处理的时间（毫秒）
        private int _frameProcessedCount; // 当前帧已处理事件数

        // 时间限制分帧机制
        private bool _isBatchProcessing;
        private float _batchStartTime;
        private float _batchTimeLimit;

        /// <summary>
        ///     获取当前队列中待处理的事件数量
        /// </summary>
        public int PendingEventCount => _queue.Count;

        public bool IsBatchProcessing => _isBatchProcessing;

        #endregion

        #region 内部缓存字段

        // 规则表（按事件类型字符串索引）
        private readonly Dictionary<string, List<CardRule>> _rules = new();

        // 卡牌事件队列（统一使用 IEventEntry）
        private readonly Queue<IEventEntry> _queue = new();

        // 已注册的卡牌集合
        private readonly HashSet<Card> _registeredCardsTemplates = new();

        // 卡牌Key->Card缓存
        private readonly Dictionary<CardKey, Card> _cardMap = new();

        // id->index集合缓存
        private readonly Dictionary<string, HashSet<int>> _idIndexes = new();

        // id->Card列表缓存，用于快速查找
        private readonly Dictionary<string, List<Card>> _cardsById = new();

        // Custom规则按ID分组缓存
        private readonly Dictionary<string, List<CardRule>> _customRulesById = new();

        // UID->Card缓存，支持 O(1) UID 查询
        private readonly Dictionary<int, Card> _cardsByUID = new();

        // 位置->Card映射（一个位置最多一个卡牌）
        private readonly Dictionary<Vector3, Card> _cardsByPosition = new();

        // 位置->UID缓存
        private readonly Dictionary<int, Vector3> _positionByUID = new();

        // 全局效果池，跨事件收集效果，确保全局优先级排序
        private readonly EffectPool _globalEffectPool = new();

        // 全局效果池的规则顺序计数器
        private int _globalOrderIndex;

        #endregion

        #region 卡牌创建

        /// <summary>
        ///     按ID创建并注册卡牌实例。
        /// </summary>
        public T CreateCard<T>(string id) where T : Card
        {
            if (id == null) throw new ArgumentNullException(nameof(id));

            T card = null;
            if (CardFactory != null) card = CardFactory.Create<T>(id);

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
            var key = new CardKey(id, index);
            return _cardMap.GetValueOrDefault(key);
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
        public bool HasCard(Card card) => GetCardByKey(card.Id, card.Index) == card;

        /// <summary>
        ///     根据 UID 获取卡牌。
        /// </summary>
        /// <param name="uid">卡牌的唯一标识符。</param>
        /// <returns>找到的卡牌，或 null 如果未找到。</returns>
        public Card GetCardByUID(int uid)
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
        public Card GetCardByPosition(Vector3 position)
        {
            _cardsByPosition.TryGetValue(position, out Card card);
            return card;
        }

        /// <summary>
        ///     按UID查询卡牌的位置。
        /// </summary>
        /// <param name="uid">卡牌的UID。</param>
        /// <returns>卡牌所在位置，如果未找到返回 VOID_POSITION。</returns>
        public Vector3 GetPositionByUID(int uid) =>
            uid < 0 ? VOID_POSITION : _positionByUID.GetValueOrDefault(uid, VOID_POSITION);

        #endregion

        #region 卡牌缓存处理

        /// <summary>
        ///     添加卡牌到引擎，分配唯一Index、UID并订阅事件。
        /// </summary>
        public CardEngine AddCard(Card c)
        {
            if (c == null) return this;

            // 设置卡牌的Engine引用
            c.Engine = this;

            string id = c.Id;

            // 第一步: 处理 UID（必须先分配 UID，因为 CategoryManager 使用 UID 作为键）
            if (c.UID < 0)
            {
                // 未分配 UID，分配新的
                EmeCardSystem.CardFactory.AssignUID(c);
            }
            else if (_cardsByUID.ContainsKey(c.UID))
            {
                // UID 冲突，重新分配
                c.UID = -1;
                EmeCardSystem.CardFactory.AssignUID(c);
            }

            // 第二步: 分配索引（如果还未分配）
            if (!_idIndexes.TryGetValue(id, out var indexes))
            {
                indexes = new();
                _idIndexes[id] = indexes;
            }

            int assignedIndex = c.Index;
            // 只有当Index不已分配时才分配（Index < 0表示未分配），或者索引已被占用时，需要重新分配
            if (assignedIndex < 0 || indexes.Contains(assignedIndex))
            {
                assignedIndex = 0;
                while (indexes.Contains(assignedIndex))
                {
                    assignedIndex++;
                }

                c.Index = assignedIndex;
            }

            // 第三步: 检查是否已在实际存储中（使用 UID 检查，保证绝对唯一）
            if (_cardsByUID.ContainsKey(c.UID))
                return this; // 已存在，不重复添加

            c.OnEvent += OnCardEvent;

            // 第四步: 添加到索引
            indexes.Add(c.Index);
            var actualKey = new CardKey(c.Id, c.Index);
            _cardMap[actualKey] = c;

            // 添加到 UID 索引
            _cardsByUID[c.UID] = c;

            // 更新_cardsById缓存
            if (!_cardsById.TryGetValue(id, out var cardList))
            {
                cardList = new();
                _cardsById[id] = cardList;
            }

            cardList.Add(c);

            // 第五步: 注册到 CategoryManager（使用 UID 作为键，保证绝对唯一）
            RegisterToCategoryManager(c);

            // 第五点五步: 注册位置映射
            // 子卡牌初始位置为虚空，主卡牌位置为自身 Position
            Vector3 initialPosition = c.Owner == null ? c.Position : VOID_POSITION;
            _cardsByPosition[initialPosition] = c;
            _positionByUID[c.UID] = initialPosition;

            // 第六步: 将卡牌的所有标签加入TargetSelector缓存
            foreach (string tag in c.Tags)
            {
                TargetSelector.OnCardTagAdded(c, tag);
            }

            // 第七步: 递归注册所有已存在的子卡牌（处理工厂创建时已通过 AddChild 添加的子卡牌）
            if (c.Children != null && c.Children.Count > 0)
            {
                foreach (Card child in c.Children)
                {
                    if (child != null && !HasCard(child))
                    {
                        AddCard(child);
                    }
                }
            }

            return this;
        }

        /// <summary>
        ///     将子卡牌添加到父卡牌，同时确保子卡牌已注册到引擎。
        ///     此方法会：1) 将子卡牌注册到引擎（分配 UID、注册到 CategoryManager）
        ///              2) 建立父子关系
        /// </summary>
        /// <param name="parent">父卡牌（必须已注册到引擎）。</param>
        /// <param name="child">子卡牌。</param>
        /// <param name="intrinsic">是否作为固有子卡（固有子卡无法通过规则消耗或普通移除）。</param>
        /// <returns>当前引擎实例，支持链式调用。</returns>
        /// <exception cref="ArgumentNullException">parent 或 child 为 null。</exception>
        /// <exception cref="InvalidOperationException">parent 未注册到引擎，或 child 已有其他持有者。</exception>
        public CardEngine AddChildToCard(Card parent, Card child, bool intrinsic = false)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (child == null) throw new ArgumentNullException(nameof(child));

            // 验证父卡牌已注册到引擎
            if (!HasCard(parent))
            {
                throw new InvalidOperationException($"父卡牌 '{parent.Id}' 未注册到引擎，请先调用 AddCard。");
            }

            // 验证子卡牌没有其他持有者
            if (child.Owner != null)
            {
                throw new InvalidOperationException($"子卡牌 '{child.Id}' 已被其他卡牌持有。");
            }

            // 第一步：将子卡牌注册到引擎（如果尚未注册）
            if (!HasCard(child))
            {
                AddCard(child);
            }

            // 第二步：建立父子关系
            parent.AddChild(child, intrinsic);

            // 第三步：子卡牌移动到虚空位置
            MoveCardToPosition(child, VOID_POSITION);

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
        ///     转移卡牌到新位置，自动更新位置映射。
        ///     子卡牌移动将被强制转移到虚空位置。
        /// </summary>
        /// <param name="card">要转移的卡牌。</param>
        /// <param name="newPosition">新位置（子卡牌将被强制转移到虚空）</param>
        /// <returns>当前引擎实例，支持链式调用。</returns>
        public CardEngine MoveCardToPosition(Card card, Vector3 newPosition)
        {
            if (card == null) return this;

            // 子卡牌不能转移到实际位置，只能在虚空
            if (card.Owner != null)
            {
                newPosition = VOID_POSITION;
            }

            Vector3 oldPosition = card.Position;

            // 如果位置相同，无需更新
            if (oldPosition == newPosition) return this;

            // 从旧位置移除
            if (_cardsByPosition.TryGetValue(oldPosition, out Card cardAtOldPosition) && cardAtOldPosition == card)
            {
                _cardsByPosition.Remove(oldPosition);
            }

            // 更新卡牌位置
            card.Position = newPosition;

            // 添加到新位置
            _cardsByPosition[newPosition] = card;
            _positionByUID[card.UID] = newPosition;

            return this;
        }

        /// <summary>
        ///     当卡牌位置通过 Card.AddChild 改变时，通知引擎更新位置映射。
        /// </summary>
        /// <param name="card">发生位置变化的卡牌。</param>
        /// <param name="oldPosition">旧位置。</param>
        /// <param name="newPosition">新位置。</param>
        internal void NotifyCardPositionChanged(Card card, Vector3 oldPosition, Vector3 newPosition)
        {
            if (card == null) return;

            // 从旧位置移除
            if (_cardsByPosition.TryGetValue(oldPosition, out Card cardAtOldPosition) && cardAtOldPosition == card)
            {
                _cardsByPosition.Remove(oldPosition);
            }

            // 添加到新位置
            _cardsByPosition[newPosition] = card;
            _positionByUID[card.UID] = newPosition;
        }

        /// <summary>
        ///     移除卡牌，移除事件订阅、UID 映射与索引。
        /// </summary>
        public CardEngine RemoveCard(Card c)
        {
            if (c == null) return this;

            var key = new CardKey(c.Id, c.Index);
            if (_cardMap.TryGetValue(key, out Card existing) && ReferenceEquals(existing, c))
            {
                _cardMap.Remove(key);
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
                UnregisterFromCategoryManager(c);

                // 从位置映射中移除
                if (_cardsByPosition.TryGetValue(c.Position, out Card cardAtPosition) && cardAtPosition == c)
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
            }

            return this;
        }

        public void ClearAllCards()
        {
            // 从 CategoryManager 批量注销所有卡牌
            foreach (Card card in _cardMap.Values)
            {
                UnregisterFromCategoryManager(card);
            }

            _cardMap.Clear();
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
                    if (!string.IsNullOrWhiteSpace(tag))
                    {
                        registration = registration.WithTags(tag);
                    }
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
        private void UnregisterFromCategoryManager(Card card)
        {
            if (card == null) return;

            // 使用 UID（int）删除实体
            OperationResult result = CategoryManager.DeleteEntity(card.UID);
            if (!result.IsSuccess && result.ErrorCode != ErrorCode.NotFound)
            {
                Debug.LogWarning($"[CardEngine] CategoryManager 注销失败: Card UID={card.UID}, Error={result.ErrorMessage}");
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

    public readonly struct CardKey : IEquatable<CardKey>
    {
        public readonly string Id;
        public readonly int Index;

        public CardKey(string id, int index)
        {
            Id = id ?? string.Empty;
            Index = index;
        }

        public bool Equals(CardKey other) =>
            string.Equals(Id, other.Id, StringComparison.Ordinal) && Index == other.Index;

        public override bool Equals(object obj) => obj is CardKey k && Equals(k);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(Id ?? string.Empty);
                hash = hash * 31 + Index;
                return hash;
            }
        }

        public override string ToString() => $"{Id}#{Index}";
    }
}
