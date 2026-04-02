using System;
using System.Collections.Generic;
using System.Threading;
using EasyPack.GamePropertySystem;

namespace EasyPack.EmeCardSystem
{
    // 按 ID 创建卡牌实例
    public interface ICardFactory
    {
        Card Create(string id);
        T Create<T>(string id) where T : Card;
        IReadOnlyCollection<string> GetAllCardIds();

        /// <summary>
        ///     获取工厂中注册的模板数据。
        /// </summary>
        CardData GetTemplateData(string id);
    }

    /// <summary>
    ///     卡牌工厂公开接口
    /// </summary>
    public interface ICardFactoryRegistry
    {
        void Register(string id, Func<Card> ctor);
        void Register(IReadOnlyDictionary<string, Func<Card>> productionList);

        /// <summary>
        ///     基于现有卡牌模板注册一个变体。
        /// </summary>
        /// <param name="baseId">基础卡牌 ID。</param>
        /// <param name="newId">新变体 ID。</param>
        /// <param name="tweakAction">可选的微调操作，用于修改变体属性或元数据。</param>
        void RegisterVariant(string baseId, string newId, Action<Card> tweakAction = null);

        /// <summary>
        ///     同步 UID 计数器，确保后续分配的 UID 大于当前已知的最大值。
        /// </summary>
        void SyncUID(long maxUID);

        IReadOnlyCollection<string> GetAllCardIds();
    }

    public sealed class CardFactory : ICardFactory, ICardFactoryRegistry
    {
        private readonly Dictionary<string, Func<Card>> _constructors = new(StringComparer.Ordinal);

        /// <summary>
        ///     工厂级模板数据字典（ID → CardData 副本）。
        ///     在 Create 时自动从 Card._initialData 捕获，也可通过 RegisterTemplate 显式注册。
        /// </summary>
        private readonly Dictionary<string, CardData> _templates = new(StringComparer.Ordinal);

        /// <summary>
        ///     下一个可用的 UID，起始值 1001。
        /// </summary>
        private static long _nextUID = 1001;

        public void Register(string id, Func<Card> ctor)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            _constructors[id] = ctor ?? throw new ArgumentNullException(nameof(ctor));
        }

        public void Register(IReadOnlyDictionary<string, Func<Card>> productionList)
        {
            foreach ((string id, var ctor) in productionList)
            {
                Register(id, ctor);
            }
        }

        public void RegisterVariant(string baseId, string newId, Action<Card> tweakAction = null)
        {
            if (string.IsNullOrEmpty(baseId)) throw new ArgumentNullException(nameof(baseId));
            if (string.IsNullOrEmpty(newId)) throw new ArgumentNullException(nameof(newId));

            if (!_constructors.TryGetValue(baseId, out var baseCtor))
            {
                throw new KeyNotFoundException($"未找到基础卡牌 ID: {baseId}");
            }

            Register(newId, () =>
            {
                // 1. 调用基础构造器获取原型
                Card baseCard = baseCtor();

                // 2. 克隆静态数据并应用新 ID（从暂存数据或工厂模板获取）
                CardData baseData = baseCard._initialData ?? GetTemplateData(baseId);
                if (baseData == null)
                {
                    throw new InvalidOperationException($"无法获取基础卡牌 '{baseId}' 的 CardData");
                }
                CardData newData = baseData.Clone(newId);

                // 3. 创建新卡牌实例
                var newCard = new Card(newData);

                // 4. 复制运行时属性 (GameProperties)
                if (baseCard.Properties is { Count: > 0 })
                {
                    foreach (GameProperty prop in baseCard.Properties)
                    {
                        // 简单复制 ID 和值
                        newCard.Properties.Add(new(prop.ID, prop.GetValue()));
                    }
                }

                // 5. 执行自定义微调
                tweakAction?.Invoke(newCard);

                return newCard;
            });
        }

        /// <summary>
        ///     获取所有已注册的卡牌ID
        /// </summary>
        public IReadOnlyCollection<string> GetAllCardIds() => _constructors.Keys;

        /// <summary>
        ///     获取所有已注册的卡牌ID（ICardFactory 接口实现）
        /// </summary>
        IReadOnlyCollection<string> ICardFactory.GetAllCardIds() => GetAllCardIds();

        /// <summary>
        ///     为新卡牌分配唯一的 UID。
        ///     使用 <see cref="Interlocked.Increment" /> 确保线程安全和顺序递增。
        /// </summary>
        /// <returns>分配的 UID，从 1001 开始。</returns>
        /// <exception cref="InvalidOperationException">当 UID 达到上限时抛出。</exception>
        private static long AllocateUID()
        {
            long uid = Interlocked.Increment(ref _nextUID);
            return uid;
        }

        /// <summary>
        ///     为现有卡牌分配 UID（由 Create 方法或 CardEngine 内部使用）。<br />
        /// </summary>
        /// <param name="card">要分配 UID 的卡牌。</param>
        /// <exception cref="ArgumentNullException">当 card 为 null 时抛出。</exception>
        /// <exception cref="InvalidOperationException">当卡牌已有 UID 时抛出。</exception>
        public static void AssignUID(Card card)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card), "卡牌不能为 null");
            }

            if (card.UID != -1)
            {
                throw new InvalidOperationException($"卡牌已有 UID {card.UID}，无法重新分配");
            }

            card.UID = AllocateUID();
        }

        /// <summary>
        ///     重置 UID 计数器。
        /// </summary>
        internal static void ResetForTesting()
        {
            _nextUID = 1000;
        }

        /// <summary>
        ///     获取当前的 UID 计数器值。
        /// </summary>
        public static long GetCurrentUID() => _nextUID;

        /// <summary>
        ///     同步 UID 计数器，确保后续分配的 UID 大于当前已知的最大值。
        /// </summary>
        public static void SyncUID(long maxUID)
        {
            while (true)
            {
                long current = Interlocked.Read(ref _nextUID);
                if (maxUID < current)
                {
                    return;
                }

                long original = Interlocked.CompareExchange(ref _nextUID, maxUID, current);
                if (original == current)
                {
                    return;
                }
            }
        }

        /// <summary>
        ///     同步 UID 计数器
        /// </summary>
        void ICardFactoryRegistry.SyncUID(long maxUID) => SyncUID(maxUID);

        public Card Create(string id) => Create<Card>(id);

        public T Create<T>(string id) where T : Card
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_constructors.TryGetValue(id, out var ctor))
            {
                var card = ctor() as T;
                if (card == null) return null;

                // 自动捕获模板数据：从 Card 的暂存 _initialData 中提取并注册
                CardData initialData = card.ConsumeInitialData();
                if (initialData != null && !string.IsNullOrEmpty(initialData.ID)
                    && !_templates.ContainsKey(initialData.ID))
                {
                    _templates[initialData.ID] = initialData.Clone(initialData.ID);
                }

                if (card.UID == -1) AssignUID(card);

                return card;
            }

            return null;
        }

        // 显式接口实现 - 仅供 CardEngine 内部通过 ICardFactory 接口调用
        Card ICardFactory.Create(string id) => Create<Card>(id);

        /// <summary>
        ///     获取指定 ID 的模板数据。
        /// </summary>
        public CardData GetTemplateData(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return _templates.TryGetValue(id, out CardData data) ? data : null;
        }

        /// <summary>
        ///     显式注册模板数据（用于不经过 Create 流程的场景）。
        /// </summary>
        public void RegisterTemplate(CardData data)
        {
            if (data == null || string.IsNullOrEmpty(data.ID)) return;
            _templates[data.ID] = data.Clone(data.ID);
        }

        T ICardFactory.Create<T>(string id) => Create<T>(id);
    }
}