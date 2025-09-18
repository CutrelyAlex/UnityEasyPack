using System;
using System.Collections.Generic;

namespace EasyPack
{
    /// <summary>
    /// 卡牌类别。
    /// </summary>
    public enum CardCategory
    {
        /// <summary>物品类。</summary>
        Item,
        /// <summary>属性/状态类。</summary>
        Attribute,
        /// <summary>行为/动作类。</summary>
        Action
    }

    /// <summary>
    /// 抽象卡牌：
    /// - 作为“容器”可持有子卡牌（<see cref="Children"/>），并维护所属关系（<see cref="Owner"/>）。
    /// - 具备标签系统（<see cref="Tags"/>），用于规则匹配与检索。
    /// - 暴露统一事件入口（<see cref="OnEvent"/>），通过 <see cref="RaiseEvent(CardEvent)"/> 分发，包括
    ///   <see cref="CardEventType.Tick"/>、<see cref="CardEventType.Use"/>、<see cref="CardEventType.Custom"/>、<see cref="CardEventType.Condition"/>、
    ///   以及持有关系变化（Added/Removed）。
    /// - 可选关联一个 <see cref="GameProperty"/>（实例级数值）。
    /// </summary>
    public abstract class Card
    {
        #region 基本数据

        private CardData _data;

        /// <summary>
        /// 该卡牌的静态数据（ID/名称/描述/默认标签等）。
        /// 赋值时会清空并载入默认标签（<see cref="CardData.DefaultTags"/>）。
        /// </summary>
        public CardData Data
        {
            get => _data;
            set
            {
                _data = value;
                _tags.Clear();
                if (_data != null && _data.DefaultTags != null)
                {
                    foreach (var t in _data.DefaultTags) if (!string.IsNullOrEmpty(t)) _tags.Add(t);
                }
            }
        }

        /// <summary>
        /// 卡牌唯一标识，来自 <see cref="Data"/>。
        /// </summary>
        public string Id => Data != null ? Data.ID : string.Empty;

        /// <summary>
        /// 卡牌显示名称，来自 <see cref="Data"/>。
        /// </summary>
        public string Name => Data != null ? Data.Name : string.Empty;

        /// <summary>
        /// 卡牌描述，来自 <see cref="Data"/>。
        /// </summary>
        public string Description => Data != null ? Data.Description : string.Empty;

        /// <summary>
        /// 卡牌类别，来自 <see cref="Data"/>；若为空则默认 <see cref="CardCategory.Item"/>。
        /// </summary>
        public CardCategory Category => Data != null ? Data.Category : CardCategory.Item;

        /// <summary>
        /// 可选：实例级属性（数值系统），与 <see cref="Data"/> 解耦。
        /// </summary>
        public GameProperty Property { get; set; }

        #endregion

        #region 标签和持有关系

        private readonly HashSet<string> _tags = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// 标签集合（只读视图）。标签用于规则匹配（大小写敏感，比较器为 <see cref="StringComparer.Ordinal"/>）。
        /// </summary>
        public IReadOnlyCollection<string> Tags => _tags;

        /// <summary>
        /// 判断是否包含指定标签。
        /// </summary>
        /// <param name="tag">标签文本。</param>
        /// <returns>若包含返回 true。</returns>
        public bool HasTag(string tag) => !string.IsNullOrEmpty(tag) && _tags.Contains(tag);

        /// <summary>
        /// 添加一个标签。
        /// </summary>
        /// <param name="tag">标签文本。</param>
        /// <returns>若成功新增（之前不存在）返回 true；否则返回 false。</returns>
        public bool AddTag(string tag) => !string.IsNullOrEmpty(tag) && _tags.Add(tag);

        /// <summary>
        /// 移除一个标签。
        /// </summary>
        /// <param name="tag">标签文本。</param>
        /// <returns>若成功移除返回 true；否则返回 false。</returns>
        public bool RemoveTag(string tag) => !string.IsNullOrEmpty(tag) && _tags.Remove(tag);

        /// <summary>
        /// 当前卡牌的持有者（父卡）。
        /// </summary>
        public Card Owner { get; private set; }

        private readonly List<Card> _children = new List<Card>();

        /// <summary>
        /// 子卡牌列表（只读视图）。
        /// 规则匹配通常只扫描该层级，不会递归扫描更深层级。
        /// </summary>
        public IReadOnlyList<Card> Children => _children;

        // 固有子卡牌（不可被消耗/移除）
        private readonly HashSet<Card> _intrinsics = new HashSet<Card>();

        /// <summary>
        /// 将子卡牌加入当前卡牌作为持有者。
        /// </summary>
        /// <param name="child">子卡牌实例。</param>
        /// <param name="intrinsic">是否作为“固有子卡”；固有子卡无法通过规则消耗或普通移除。</param>
        /// <remarks>
        /// 成功加入后，将向子卡派发 <see cref="CardEventType.AddedToOwner"/> 事件，
        /// 其 <see cref="CardEvent.Data"/> 为旧持有者（此处即 <c>this</c>）。
        /// </remarks>
        public void AddChild(Card child, bool intrinsic = false)
        {
            if (child == null) throw new ArgumentNullException(nameof(child));
            if (child.Owner != null) throw new InvalidOperationException("子卡牌已被其他卡牌持有。");

            _children.Add(child);
            child.Owner = this;
            if (intrinsic) _intrinsics.Add(child);

            // 通知子卡
            child.RaiseEvent(new CardEvent(CardEventType.AddedToOwner, data: this));
        }

        /// <summary>
        /// 从当前卡牌移除一个子卡牌。
        /// </summary>
        /// <param name="child">要移除的子卡牌。</param>
        /// <param name="force">是否强制移除；当为 false 时，固有子卡不会被移除。</param>
        /// <returns>若移除成功返回 true；否则返回 false。</returns>
        /// <remarks>
        /// 移除成功后，将向子卡派发 <see cref="CardEventType.RemovedFromOwner"/> 事件，
        /// 其 <see cref="CardEvent.Data"/> 为旧持有者实例。
        /// </remarks>
        public bool RemoveChild(Card child, bool force = false)
        {
            if (child == null) return false;
            if (!force && _intrinsics.Contains(child)) return false; // 固有不可移除

            var removed = _children.Remove(child);
            if (removed)
            {
                _intrinsics.Remove(child);
                var oldOwner = this;
                child.Owner = null;
                child.RaiseEvent(new CardEvent(CardEventType.RemovedFromOwner, data: oldOwner));
            }
            return removed;
        }

        #endregion

        #region 事件回调

        /// <summary>
        /// 卡牌统一事件回调。
        /// 订阅者（如规则引擎）可监听以实现配方、效果与副作用。
        /// </summary>
        public event Action<Card, CardEvent> OnEvent;

        /// <summary>
        /// 分发一个卡牌事件到 <see cref="OnEvent"/>。
        /// </summary>
        /// <param name="evt">事件载体。</param>
        public void RaiseEvent(CardEvent evt)
        {
            OnEvent?.Invoke(this, evt);
        }

        /// <summary>
        /// 触发按时事件（<see cref="CardEventType.Tick"/>）。
        /// </summary>
        /// <param name="deltaTime">时间步长（秒）。将作为 <see cref="CardEvent.Data"/> 传递。</param>
        public void Tick(float deltaTime) => RaiseEvent(new CardEvent(CardEventType.Tick, data: deltaTime));

        /// <summary>
        /// 触发主动使用事件（<see cref="CardEventType.Use"/>）。
        /// </summary>
        /// <param name="data">可选载荷；由订阅者按需解释（例如目标信息）。</param>
        public void Use(object data = null) => RaiseEvent(new CardEvent(CardEventType.Use, data: data));

        /// <summary>
        /// 触发自定义事件（<see cref="CardEventType.Custom"/>）。
        /// </summary>
        /// <param name="id">自定义事件标识，用于规则过滤。</param>
        /// <param name="data">可选载荷。</param>
        public void Custom(string id, object data = null) => RaiseEvent(new CardEvent(CardEventType.Custom, id, data));

        /// <summary>
        /// 触发条件事件（<see cref="CardEventType.Condition"/>）。
        /// 常用于“玩家移动”“时间切换”“区域进入”等外部条件达成场景。
        /// </summary>
        /// <param name="id">条件事件标识，用于规则过滤。</param>
        /// <param name="data">可选载荷（例如位置、时间段等）。</param>
        public void Condition(string id, object data = null) => RaiseEvent(new CardEvent(CardEventType.Condition, id, data));

        #endregion
    }
}