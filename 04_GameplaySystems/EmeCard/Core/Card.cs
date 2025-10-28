using System;
using System.Collections.Generic;
using System.Linq;
using EasyPack.GamePropertySystem;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    /// 卡牌类别。
    /// </summary>
    public enum CardCategory
    {
        /// <summary>物品/实体类。</summary>
        Object,
        /// <summary>属性/状态类。</summary>
        Attribute,
        /// <summary>行为/动作类。</summary>
        Action,
        /// <summary>环境类。</summary>
        Environment
    }

    /// <summary>
    /// 抽象卡牌：<br/>
    /// - 作为“容器”可持有子卡牌（<see cref="Children"/>），并维护所属关系（<see cref="Owner"/>）。<br/>
    /// - 具备标签系统（<see cref="Tags"/>），用于规则匹配与检索。<br/>
    /// - 暴露统一事件入口（<see cref="OnEvent"/>），通过 <see cref="RaiseEvent(CardEvent)"/> 分发，包括
    ///   <see cref="CardEventType.Tick"/>、<see cref="CardEventType.Use"/>、<see cref="CardEventType.Custom"/>，
    ///   以及持有关系变化（<see cref="CardEventType.AddedToOwner"/> / <see cref="CardEventType.RemovedFromOwner"/>）。<br/>
    /// - 可选关联多个 <see cref="GameProperty"/>
    /// </summary>
    public class Card
    {
        /// <summary>
        /// 无参构造函数
        /// </summary>
        public Card() { }

        /// <summary>
        /// 构造函数：创建卡牌，可选单个属性
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

            if (Data?.DefaultTags != null)
            {
                foreach (var t in Data.DefaultTags)
                    if (!string.IsNullOrEmpty(t)) _tags.Add(t);
            }
            if (extraTags != null)
            {
                foreach (var t in extraTags)
                    if (!string.IsNullOrEmpty(t)) _tags.Add(t);
            }
        }

        /// <summary>
        /// 构造函数：创建卡牌，可选多个属性
        /// </summary>
        /// <param name="data">卡牌数据</param>
        /// <param name="properties">属性列表</param>
        /// <param name="extraTags">额外标签</param>
        public Card(CardData data, IEnumerable<GameProperty> properties, params string[] extraTags)
        {
            Data = data;
            Properties = properties?.ToList() ?? new List<GameProperty>();

            if (Data?.DefaultTags != null)
            {
                foreach (var t in Data.DefaultTags)
                    if (!string.IsNullOrEmpty(t)) _tags.Add(t);
            }
            if (extraTags != null)
            {
                foreach (var t in extraTags)
                    if (!string.IsNullOrEmpty(t)) _tags.Add(t);
            }
        }

        /// <summary>
        /// 简化构造函数：仅提供卡牌数据和标签（无属性）
        /// 此构造函数消除了传入 null 时的歧义
        /// </summary>
        /// <param name="data">卡牌数据</param>
        /// <param name="extraTags">额外标签</param>
        public Card(CardData data, params string[] extraTags)
            : this(data, (IEnumerable<GameProperty>)null, extraTags)
        {
        }


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
        /// 实例索引：用于区分同一 ID 的多个实例（由持有者在 AddChild 时分配，从 0 起）。
        /// </summary>
        public int Index { get; set; } = 0;

        /// <summary>
        /// 卡牌标识，来自 <see cref="Data"/>。
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
        /// 卡牌类别，来自 <see cref="Data"/>；若为空则默认 <see cref="CardCategory.Object"/>。
        /// </summary>
        public CardCategory Category => Data != null ? Data.Category : CardCategory.Object;

        /// <summary>
        /// 数值属性。
        /// </summary>
        public List<GameProperty> Properties { get; set; } = new List<GameProperty>();
        public GameProperty GetProperty(string id) => Properties?.FirstOrDefault(p => p.ID == id);
        public GameProperty GetProperty(int index=0) => Properties[index];
        #endregion

        #region 标签和持有关系

        private readonly HashSet<string> _tags = new(StringComparer.Ordinal);

        /// <summary>
        /// 标签集合。标签用于规则匹配（大小写敏感，比较器为 <see cref="StringComparer.Ordinal"/>）。
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

        private readonly List<Card> _children = new();

        /// <summary>
        /// 子卡牌列表（只读视图）。
        /// 规则匹配通常只扫描该层级，不会递归扫描更深层级。
        /// </summary>
        public IReadOnlyList<Card> Children => _children;

        public IReadOnlyList<Card> Intrinsics => _intrinsics.ToList();

        public int ChildrenCount => Children.Count;

        // 固有子卡牌（不可被消耗/移除）
        private readonly HashSet<Card> _intrinsics = new();

        /// <summary>
        /// 判断某子卡是否为固有子卡。
        /// </summary>
        /// <param name="child">要检查的子卡。</param>
        /// <returns>如果是固有子卡返回 true</returns>
        public bool IsIntrinsic(Card child)
        {
            if (child == null) return false;
            return _intrinsics.Contains(child);
        }


        /// <summary>
        /// 将子卡牌加入当前卡牌作为持有者。
        /// </summary>
        /// <param name="child">子卡牌实例。</param>
        /// <param name="intrinsic">是否作为“固有子卡”；固有子卡无法通过规则消耗或普通移除。</param>
        /// <remarks>
        /// 成功加入后，将向子卡派发 <see cref="CardEventType.AddedToOwner"/> 事件，
        /// 其 <see cref="CardEvent.Data"/> 为旧持有者（此处即 <c>this</c>）。
        /// </remarks>
        public Card AddChild(Card child, bool intrinsic = false)
        {
            if (child == null) throw new ArgumentNullException(nameof(child));
            if (child.Owner != null) throw new InvalidOperationException("子卡牌已被其他卡牌持有。");
            if (child == this) throw new Exception("卡牌不能添加自身为子卡牌。");


            _children.Add(child);
            child.Owner = this;
            if (intrinsic) _intrinsics.Add(child);

            // 通知子卡
            child.RaiseEvent(new CardEvent(CardEventType.AddedToOwner, data: this));

            return this;
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
                child.RaiseEvent(new CardEvent(CardEventType.RemovedFromOwner, data: this));
                child.Owner = null;
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
        /// <param name="data">可选自定义信息；由订阅者按需解释（例如目标信息）。</param>
        public void Use(object data = null) => RaiseEvent(new CardEvent(CardEventType.Use, data: data));

        /// <summary>
        /// 触发自定义事件（<see cref="CardEventType.Custom"/>）。
        /// </summary>
        /// <param name="id">自定义事件标识，用于规则过滤。</param>
        /// <param name="data">可选自定义信息。</param>
        public void Custom(string id, object data = null) => RaiseEvent(new CardEvent(CardEventType.Custom, id, data));
        #endregion
    }
}
