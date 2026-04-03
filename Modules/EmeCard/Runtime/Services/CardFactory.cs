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

        /// <summary>
        ///     获取所有已注册的模板数据。
        /// </summary>
        IReadOnlyDictionary<string, CardData> GetAllTemplateData();
    }

    /// <summary>
    ///     卡牌工厂公开接口
    /// </summary>
    public interface ICardFactoryRegistry
    {
        /// <summary>
        ///     注册一条卡牌模板数据。
        ///     运行时 Create 时，工厂将自动基于此模板构建 Card 实例。
        /// </summary>
        void RegisterData(string id, CardData data);

        /// <summary>
        ///     批量注册卡牌模板数据。
        /// </summary>
        void RegisterData(IReadOnlyDictionary<string, CardData> dataList);

        /// <summary>
        ///     基于现有卡牌模板注册一个变体。
        /// </summary>
        /// <param name="baseId">基础卡牌 ID。</param>
        /// <param name="newId">新变体 ID。</param>
        /// <param name="tweakAction">可选的微调操作，用于修改变体的 CardData。</param>
        void RegisterVariant(string baseId, string newId, Action<CardData> tweakAction = null);

        /// <summary>
        ///     同步 UID 计数器，确保后续分配的 UID 大于当前已知的最大值。
        /// </summary>
        void SyncUID(long maxUID);

        IReadOnlyCollection<string> GetAllCardIds();
    }

    public sealed class CardFactory : ICardFactory, ICardFactoryRegistry
    {
        /// <summary>
        ///     模板数据字典（ID → CardData 副本）。
        ///     通过 RegisterData 注册，Create 时基于模板构建 Card 实例。
        /// </summary>
        private readonly Dictionary<string, CardData> _templates = new(StringComparer.Ordinal);

        /// <summary>
        ///     下一个可用的 UID，起始值 1001。
        /// </summary>
        private static long _nextUID = 1001;

        /// <summary>
        ///     注册一条卡牌模板数据。
        /// </summary>
        public void RegisterData(string id, CardData data)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            if (data == null) throw new ArgumentNullException(nameof(data));
            _templates[id] = data.Clone(data.ID);
        }

        /// <summary>
        ///     批量注册卡牌模板数据。
        /// </summary>
        public void RegisterData(IReadOnlyDictionary<string, CardData> dataList)
        {
            if (dataList == null) throw new ArgumentNullException(nameof(dataList));
            foreach ((string id, CardData data) in dataList)
            {
                RegisterData(id, data);
            }
        }

        public void RegisterVariant(string baseId, string newId, Action<CardData> tweakAction = null)
        {
            if (string.IsNullOrEmpty(baseId)) throw new ArgumentNullException(nameof(baseId));
            if (string.IsNullOrEmpty(newId)) throw new ArgumentNullException(nameof(newId));

            CardData baseData = GetTemplateData(baseId);
            if (baseData == null)
            {
                throw new KeyNotFoundException($"未找到基础卡牌 ID: {baseId}");
            }

            CardData newData = baseData.Clone(newId);
            tweakAction?.Invoke(newData);
            _templates[newId] = newData;
        }

        /// <summary>
        ///     获取所有已注册的卡牌ID
        /// </summary>
        public IReadOnlyCollection<string> GetAllCardIds() => _templates.Keys;

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

            if (!_templates.TryGetValue(id, out CardData templateData)) return null;

            var card = new Card(id) as T;
            if (card == null) return null;

            // 从模板的 DefaultProperties 初始化属性
            if (templateData.DefaultProperties is { Count: > 0 })
            {
                foreach (var prop in templateData.DefaultProperties)
                {
                    card.Properties.Add(new GameProperty(prop.id, prop.value));
                }
            }

            if (card.UID == -1) AssignUID(card);

            return card;
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
        ///     获取所有已注册的模板数据。
        /// </summary>
        public IReadOnlyDictionary<string, CardData> GetAllTemplateData() => _templates;

        /// <summary>
        ///     显式注册模板数据。
        /// </summary>
        public void RegisterTemplate(CardData data)
        {
            if (data == null || string.IsNullOrEmpty(data.ID)) return;
            _templates[data.ID] = data.Clone(data.ID);
        }

        T ICardFactory.Create<T>(string id) => Create<T>(id);
    }
}