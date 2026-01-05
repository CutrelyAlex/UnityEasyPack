using System;
using System.Collections.Generic;
using System.Linq;
using EasyPack.Category;
using EasyPack.CustomData;
using EasyPack.GamePropertySystem;
using UnityEngine;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     抽象卡牌：<br />
    ///     - 作为"容器"可持有子卡牌（<see cref="Children" />），并维护所属关系（<see cref="Owner" />）。<br />
    ///     - 具备标签系统（<see cref="Tags" />），用于规则匹配与检索。<br />
    ///     - 暴露统一事件入口（<see cref="OnEvent" />），通过 <see cref="RaiseEvent(ICardEvent)" /> 分发，包括
    ///     Tick、Use、自定义事件，以及持有关系变化（AddedToOwner / RemovedFromOwner）。<br />
    ///     事件类型定义在 <see cref="CardEventTypes" /> 中。<br />
    ///     - 可选关联多个 <see cref="GameProperty" />
    /// </summary>
    public class Card : IEquatable<Card>
    {
        #region 构造函数

        /// <summary>
        ///     构造函数：创建卡牌，可选单个属性
        /// </summary>
        /// <param name="data">卡牌数据</param>
        /// <param name="gameProperty">可选的单个游戏属性</param>
        /// <param name="extraTags">额外标签</param>
        public Card(CardData data, GameProperty gameProperty = null, params string[] extraTags)
        {
            Data = data;
            if (gameProperty != null)
            {
                Properties.Add(gameProperty);
            }

            // 临时收集额外标签，等待注册到Engine时同步
            if (extraTags is { Length: > 0 })
            {
                PendingExtraTags = new(extraTags);
            }
        }

        /// <summary>
        ///     构造函数：创建卡牌，可选多个属性
        /// </summary>
        /// <param name="data">卡牌数据</param>
        /// <param name="properties">属性列表</param>
        /// <param name="extraTags">额外标签</param>
        public Card(CardData data, IEnumerable<GameProperty> properties, params string[] extraTags)
        {
            Data = data;
            Properties = properties?.ToList() ?? new List<GameProperty>();

            // 临时收集额外标签，等待注册到Engine时同步
            if (extraTags is { Length: > 0 })
            {
                PendingExtraTags = new(extraTags);
            }
        }

        /// <summary>
        ///     简化构造函数
        ///     提供卡牌数据和标签
        /// </summary>
        /// <param name="data">卡牌数据</param>
        /// <param name="extraTags">额外标签</param>
        public Card(CardData data, params string[] extraTags)
            : this(data, (IEnumerable<GameProperty>)null, extraTags) { }

        #endregion

        #region 基本数据

        /// <summary>
        ///     卡牌所属的CardEngine
        /// </summary>
        public CardEngine Engine { get; set; }

        /// <summary>
        ///     构造函数中临时收集的额外标签，在注册到Engine时同步到CategoryManager后清空。
        /// </summary>
        internal List<string> PendingExtraTags { get; set; }

        /// <summary>
        ///     该卡牌的静态数据（ID/名称/描述/默认标签等）。
        ///     注意：更改 Data 不会自动更新 CategoryManager 中的标签，
        ///     标签管理统一由 Engine 在注册时处理。
        /// </summary>
        public CardData Data { get; protected set; }

        /// <summary>
        ///     唯一标识符：由 CardFactory 分配，全局唯一，线程安全。
        ///     未分配时默认为 -1。
        /// </summary>
        public long UID { get; set; } = -1;

        /// <summary>
        ///     实例索引：用于区分同一 ID 的多个实例（由CardEngine在 AddCard 时分配，从 0 起）。
        ///     未分配时默认为 -1。
        /// </summary>
        public int Index { get; set; } = -1;

        /// <summary>
        ///     卡牌标识，来自 <see cref="Data" />。
        /// </summary>
        public string Id => Data != null ? Data.ID : string.Empty;

        public string IdAndIndex => Id + Index;

        /// <summary>
        ///     卡牌显示名称，来自 <see cref="Data" />。
        /// </summary>
        public string Name => Data != null ? Data.Name : string.Empty;

        /// <summary>
        ///     卡牌描述，来自 <see cref="Data" />。
        /// </summary>
        public string Description => Data != null ? Data.Description : string.Empty;

        public string Category
        {
            get
            {
                if (Engine?.CategoryManager != null && UID >= 0)
                {
                    return Engine.CategoryManager.GetReadableCategoryPath(UID);
                }

                return Data?.Category ?? CardData.DEFAULT_CATEGORY;
            }
        }

        /// <summary>
        ///     卡牌在世界中的位置。
        ///     - 对于根卡牌（Owner == null），返回自己的位置
        ///     - 对于子卡牌（Owner != null），返回根卡牌的位置
        /// </summary>
        private Vector3Int? _position;

        public Vector3Int? Position
        {
            get
            {
                // 如果当前卡牌有持有者，返回根卡牌的位置
                if (Owner != null && RootCard != null && !RootCard.Equals(this))
                {
                    return RootCard._position;
                }

                // 否则返回自己的位置
                return _position;
            }
            set
            {
                if (RootCard == null || RootCard.Equals(this))
                {
                    _position = value;
                }
            }
        }

        /// <summary>
        ///     数值属性。
        /// </summary>
        public List<GameProperty> Properties { get; set; } = new();

        public GameProperty GetProperty(string id)
        {
            return Properties?.FirstOrDefault(p => p.ID == id);
        }

        public GameProperty GetProperty(int index = 0)
        {
            if (index < 0 || index >= Properties.Count) return null;
            return Properties[index];
        }

        #endregion

        #region 标签和持有关系

        /// <summary>
        ///     标签集合。标签由 CategoryManager 统一管理。
        ///     <para>推荐使用 <see cref="HasTag" />、<see cref="AddTag" />、<see cref="RemoveTag" /> 方法操作标签。</para>
        /// </summary>
        /// <exception cref="InvalidOperationException">卡牌未注册到引擎时访问。</exception>
        public IReadOnlyCollection<string> Tags
        {
            get
            {
                if (Engine?.CategoryManager == null || Index < 0)
                {
                    return Array.Empty<string>();
                }

                return Engine.CategoryManager.GetTags(this);
            }
        }

        /// <summary>
        ///     检查卡牌是否包含指定标签。
        /// </summary>
        /// <param name="tag">要检查的标签。</param>
        /// <returns>如果包含该标签返回 true。</returns>
        public bool HasTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return false;
            if (Engine?.CategoryManager == null || UID < 0) return false;

            return Engine.CategoryManager.HasTag(this, tag);
        }

        public bool ChildHasTag(string tag, out Card target)
        {
            target = null;
            if (string.IsNullOrEmpty(tag)) return false;
            if (Engine?.CategoryManager == null || UID < 0) return false;

            foreach (Card child in Children)
            {
                if (Engine.CategoryManager.HasTag(child, tag))
                {
                    target = child;
                    return true;
                }
            }

            return false;
        }
        
        public bool ChildHasTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return false;
            if (Engine?.CategoryManager == null || UID < 0) return false;

            foreach (Card child in Children)
            {
                if (Engine.CategoryManager.HasTag(child, tag)) return true;
            }
            return false;
        }

        /// <summary>
        ///     添加标签到卡牌。
        /// </summary>
        /// <param name="tag">要添加的标签。</param>
        /// <returns>如果成功添加返回 true（标签之前不存在）。</returns>
        public bool AddTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return false;
            if (Engine?.CategoryManager == null || UID < 0) return false;

            // 检查是否已存在
            if (Engine.CategoryManager.HasTag(this, tag)) return false;

            OperationResult op = Engine.CategoryManager.AddTag(UID, tag);
            return op.IsSuccess;
        }

        /// <summary>
        ///     添加多个标签到卡牌。
        /// </summary>
        /// <param name="tags">要添加的标签数组。</param>
        /// <returns>如果成功添加至少一个新标签返回 true。</returns>
        public bool AddTags(string[] tags)
        {
            if (tags == null || tags.Length == 0) return false;
            if (Engine?.CategoryManager == null || UID < 0) return false;

            // 过滤出未存在的标签
            var newTags = new List<string>();
            foreach (string tag in tags)
            {
                if (!string.IsNullOrEmpty(tag) && !Engine.CategoryManager.HasTag(this, tag))
                {
                    newTags.Add(tag);
                }
            }

            if (newTags.Count == 0) return false;

            OperationResult op = Engine.CategoryManager.AddTags(UID, newTags.ToArray());
            return op.IsSuccess;
        }

        /// <summary>
        ///     从卡牌移除标签。
        /// </summary>
        /// <param name="tag">要移除的标签。</param>
        /// <returns>如果成功移除返回 true（标签之前存在）。</returns>
        public bool RemoveTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return false;
            if (Engine?.CategoryManager == null || UID < 0) return false;

            // 检查是否存在
            if (!Engine.CategoryManager.HasTag(this, tag)) return false;

            OperationResult op = Engine.CategoryManager.RemoveTag(UID, tag);
            return op.IsSuccess;
        }



        /// <summary>
        ///     当前卡牌的持有者（父卡）。
        /// </summary>
        public Card Owner { get; private set; }

        /// <summary>
        ///     根卡牌引用。对于根卡牌，RootCard 指向自身；对于子卡牌，RootCard 指向最顶层的根卡牌。
        /// </summary>
        public Card RootCard { get; set; }

        private readonly List<Card> _children = new();

        /// <summary>
        ///     子卡牌列表（只读视图）。
        ///     规则匹配通常只扫描该层级，不会递归扫描更深层级。
        /// </summary>
        public IReadOnlyList<Card> Children => _children;

        public IReadOnlyList<Card> Intrinsics => _intrinsics.ToList();

        public int ChildrenCount => Children.Count;

        // 固有子卡牌（不可被消耗/移除）
        private readonly HashSet<Card> _intrinsics = new(ReferenceEqualityComparer<Card>.Default);

        /// <summary>
        ///     判断某子卡是否为固有子卡。
        /// </summary>
        /// <param name="child">要检查的子卡。</param>
        /// <returns>如果是固有子卡返回 true</returns>
        public bool IsIntrinsic(Card child) => child != null && _intrinsics.Contains(child);

        /// <summary>
        ///     判断某子卡是否为子卡。
        /// </summary>
        /// <param name="child">要检查的子卡。</param>
        /// <returns>如果是固有子卡返回 true</returns>
        public bool IsChild(Card child) => child != null && _children.Contains(child);


        /// <summary>
        ///     检测传入的卡牌是否是当前卡牌或当前卡牌的递归父级卡牌
        /// </summary>
        /// <param name="potentialChild">传入卡牌。</param>
        /// <returns>若 potentialChild 是当前卡牌的祖父卡牌，返回 true；否则返回 false。</returns>
        public bool IsRecursiveParent(Card potentialChild)
        {
            if (potentialChild == null) return false;
            if (ReferenceEquals(potentialChild, this))
            {
                return true;
            }

            var visited = new HashSet<Card>(ReferenceEqualityComparer<Card>.Default);
            Card current = Owner;

            while (current != null)
            {
                if (ReferenceEquals(current, potentialChild)) return true;
                if (current.UID >= 0 && potentialChild.UID >= 0 && current.UID == potentialChild.UID)
                {
                    return true;
                }

                if (!visited.Add(current))
                {
                    break;
                }

                current = current.Owner;
            }

            return false;
        }

        /// <summary>
        ///     将子卡牌加入当前卡牌作为持有者。
        /// </summary>
        /// <param name="child">子卡牌实例。</param>
        /// <param name="intrinsic">是否作为"固有子卡"；固有子卡无法通过规则消耗或普通移除。</param>
        /// <remarks>
        ///     成功加入后，将向子卡派发 AddedToOwner 事件，
        ///     其事件数据为新持有者（<c>this</c>）。
        ///     子卡牌的位置与父卡牌相同（通过 Position 属性继承）。
        ///     注意：子卡牌不会被引擎的位置索引管理，只有根卡牌才会被索引。
        /// </remarks>
        /// <exception cref="InvalidOperationException">如果 child 已经是当前卡牌的祖父卡牌（会形成循环引用）。</exception>
        public Card AddChild(Card child, bool intrinsic = false)
        {
            if (child == null) return this;

            // 检测循环引用：如果 child 是当前卡牌的祖父卡牌，添加它将形成循环
            if (IsRecursiveParent(child))
            {
                throw new InvalidOperationException($"添加卡牌 '{child.Id}' 将形成循环依赖。");
            }

            // 如果子卡牌之前在位置索引中（作为根卡牌），需要从索引中移除
            if (Engine != null && child.UID >= 0 && child.Owner == null && child.Position != null)
            {
                Engine.NotifyChildAddedToParent(child);
            }

            _children.Add(child);
            child.Owner = this;

            // 子卡牌与父卡牌在同一位置
            child._position = _position;

            // 维护 RootCard 引用：子卡牌继承父卡牌的 RootCard
            child.RootCard = RootCard ?? this;

            if (intrinsic) _intrinsics.Add(child);

            // 通知子卡
            child.RaiseEvent(CardEventTypes.AddedToOwner.CreateEvent(this));
            return this;
        }

        /// <summary>
        ///     从当前卡牌移除一个子卡牌。
        /// </summary>
        /// <param name="child">要移除的子卡牌。</param>
        /// <param name="force">是否强制移除；当为 false 时，固有子卡不会被移除。</param>
        /// <returns>若移除成功返回 true；否则返回 false。</returns>
        /// <remarks>
        ///     移除成功后，将向子卡派发 RemovedFromOwner 事件<br />
        ///     移除后的子卡牌会保留移除前的逻辑位置，可使用 <see cref="CardEngine.TryMoveRootCardToPosition" /> 重新定位
        ///     child的 Owner 和 RootCard 引用将被清除。
        /// </remarks>
        public bool RemoveChild(Card child, bool force = false)
        {
            if (child == null) return false;
            if (!force && _intrinsics.Contains(child)) return false; // 固有不可移除

            bool removed = _children.Remove(child);
            if (!removed) return false;

            _intrinsics.Remove(child);
            child.Owner = null;
            child.RootCard = null; // 清除 RootCard 引用
            child.RaiseEvent(CardEventTypes.RemovedFromOwner.CreateEvent(this));

            return true;
        }

        #endregion
        #region MetaData帮助

        /// <summary>
        ///     当前卡牌注册时使用的分类管理器（运行时标签/元数据管理）。
        /// </summary>
        public ICategoryManager<Card, long> RuntimeCategoryManager => Engine?.CategoryManager;

        /// <summary>
        ///     在 CategoryManager 中存储的运行时元数据（如果已注册）。
        /// </summary>
        public CustomDataCollection RuntimeMetadata
        {
            get
            {
                if (RuntimeCategoryManager == null || UID < 0) return null;
                return RuntimeCategoryManager.GetMetadata(UID);
            }
        }

        /// <summary>
        ///     尝试获取当前卡牌的运行时元数据。
        /// </summary>
        public bool TryGetRuntimeMetadata(out CustomDataCollection metadata)
        {
            metadata = RuntimeMetadata;
            return metadata != null;
        }

        /// <summary>
        ///     对当前卡牌的运行时元数据执行操作（如果已注册）。
        /// </summary>
        public Card ModifyRuntimeMetadata(Action<CustomDataCollection> action)
        {
            if (action == null) return this;

            var metadata = RuntimeMetadata;
            if (metadata != null)
            {
                action(metadata);
            }

            return this;
        }

        #endregion
        #region 事件回调

        /// <summary>
        ///     卡牌统一事件回调。
        ///     订阅者（如规则引擎）可监听以实现配方、效果与副作用。
        /// </summary>
        public event Action<Card, ICardEvent> OnEvent;

        /// <summary>
        ///     分发一个卡牌事件到 <see cref="OnEvent" />。
        /// </summary>
        /// <param name="evt">事件载体（ICardEvent 接口）。</param>
        private void RaiseEventInternal(ICardEvent evt)
        {
            OnEvent?.Invoke(this, evt);
        }

        /// <summary>
        ///     触发按时事件（Tick）。
        /// </summary>
        /// <param name="deltaTime">时间步长（秒）。将作为事件 Data 传递。</param>
        public void Tick(float deltaTime)
        {
            RaiseEventInternal(CardEventTypes.Tick.CreateEvent(deltaTime));
        }

        /// <summary>
        ///     触发主动使用事件（Use）。
        /// </summary>
        /// <param name="target">目标卡</param>
        public void Use(Card target = null)
        {
            RaiseEventInternal(CardEventTypes.Use.CreateEvent(target));
        }

        /// <summary>
        ///     触发无数据事件。
        /// </summary>
        /// <param name="eventType">自定义事件类型标识，用于规则过滤。</param>
        public void RaiseEvent(string eventType, EEventPumpType eventPumpType = EEventPumpType.Normal)
        {
            RaiseEventInternal(new CardEvent<Unit>(eventType, Unit.Default, null, eventPumpType));
        }

        /// <summary>
        ///     触发自定义事件。
        /// </summary>
        /// <typeparam name="T">事件数据类型。</typeparam>
        /// <param name="eventType">自定义事件类型标识，用于规则过滤。</param>
        /// <param name="data">事件数据。</param>
        /// <param name="eventPumpType">泵入何处</param>
        public void RaiseEvent<T>(string eventType, T data, EEventPumpType eventPumpType = EEventPumpType.Normal)
        {
            RaiseEventInternal(new CardEvent<T>(eventType, data, null, eventPumpType));
        }

        /// <summary>
        ///     触发自定义事件（使用事件定义）。
        /// </summary>
        /// <typeparam name="T">事件数据类型。</typeparam>
        /// <param name="eventDef">事件类型定义。</param>
        /// <param name="data">事件数据。</param>
        public void RaiseEvent<T>(CardEventDefinition<T> eventDef, T data)
        {
            RaiseEventInternal(eventDef.CreateEvent(data));
        }

        /// <summary>
        ///     触发自定义事件（直接传递事件对象）。
        ///     <para>适用于使用 <see cref="CardEventDefinition{T}.CreateEvent" /> 创建的事件。</para>
        /// </summary>
        /// <typeparam name="T">事件数据类型。</typeparam>
        /// <param name="evt">已创建的事件对象。</param>
        /// <example>
        ///     var evt = CardEventTypes.Tick.CreateEvent(0.016f);
        ///     card.RaiseEvent(evt);
        /// </example>
        public void RaiseEvent<T>(CardEvent<T> evt)
        {
            RaiseEventInternal(evt);
        }

        /// <summary>
        ///     触发自定义事件（直接传递 ICardEvent 接口）。
        ///     <para>适用于需要动态构造或传递事件的场景。</para>
        /// </summary>
        /// <param name="evt">事件接口实例。</param>
        public void RaiseEvent(ICardEvent evt)
        {
            if (evt != null)
            {
                RaiseEventInternal(evt);
            }
        }

        #endregion

        #region Fluent API

        /// <summary>
        ///     链式添加属性
        /// </summary>
        public Card WithProperty(string id, float value)
        {
            Properties.Add(new GameProperty(id, value));
            return this;
        }

        /// <summary>
        ///     链式添加属性
        /// </summary>
        public Card WithProperty(GameProperty property)
        {
            if (property != null) Properties.Add(property);
            return this;
        }

        /// <summary>
        ///     链式添加多个属性
        /// </summary>
        public Card WithProperties(IEnumerable<GameProperty> properties)
        {
            if (properties != null) Properties.AddRange(properties);
            return this;
        }

        /// <summary>
        ///     链式添加子卡牌
        /// </summary>
        public Card WithChild(Card child, bool intrinsic = false)
        {
            AddChild(child, intrinsic);
            return this;
        }

        /// <summary>
        ///     链式添加标签
        /// </summary>
        public Card WithTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return this;
            if (Engine != null)
            {
                AddTag(tag);
            }
            else
            {
                PendingExtraTags ??= new List<string>();
                PendingExtraTags.Add(tag);
            }
            return this;
        }

        /// <summary>
        ///     链式添加多个标签
        /// </summary>
        public Card WithTags(params string[] tags)
        {
            if (tags == null || tags.Length == 0) return this;
            if (Engine != null)
            {
                AddTags(tags);
            }
            else
            {
                PendingExtraTags ??= new List<string>();
                PendingExtraTags.AddRange(tags);
            }
            return this;
        }

        /// <summary>
        ///     链式配置元数据
        /// </summary>
        public Card WithMetaData(Action<CustomDataCollection> action)
        {
            action?.Invoke(Data.DefaultMetaData);
            return this;
        }

        #endregion

        #region Hash和相等性比较

        /// <summary>
        ///     获取卡牌的哈希码，基于 UID 计算。
        /// </summary>
        public override int GetHashCode() =>
            // ReSharper disable once NonReadonlyMemberInGetHashCode
            UID.GetHashCode();

        /// <summary>
        ///     判断两个卡牌是否相等，基于 UID 进行比较。
        ///     UID 是全局唯一的，所以两个卡牌相等当且仅当它们的 UID 相同。
        /// </summary>
        /// <param name="obj">要比较的对象。</param>
        /// <returns>如果两个卡牌的 UID 相同返回 true；否则返回 false。</returns>
        public override bool Equals(object obj) => Equals(obj as Card);

        /// <summary>
        ///     判断两个卡牌是否相等，基于 UID 进行比较。
        /// </summary>
        /// <param name="other">要比较的另一个卡牌。</param>
        /// <returns>如果两个卡牌的 UID 相同返回 true；如果 other 为 null 返回 false。</returns>
        public bool Equals(Card other)
        {
            if (other == null) return false;
            return UID == other.UID;
        }

        #endregion
    }
}