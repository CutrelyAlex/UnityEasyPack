using System;
using System.Collections.Generic;
using System.Threading;

namespace EasyPack.EmeCardSystem
{
    // 按 ID 创建卡牌实例（仅供内部使用）
    internal interface ICardFactory
    {
        Card Create(string id);
        T Create<T>(string id) where T : Card;
        IReadOnlyCollection<string> GetAllCardIds();
    }

    /// <summary>
    ///     卡牌工厂公开接口
    /// </summary>
    public interface ICardFactoryRegistry
    {
        void Register(string id, Func<Card> ctor);
        void Register(IReadOnlyDictionary<string, Func<Card>> productionList);
        IReadOnlyCollection<string> GetAllCardIds();
    }

    public sealed class CardFactory : ICardFactory, ICardFactoryRegistry
    {
        private readonly Dictionary<string, Func<Card>> _constructors = new(StringComparer.Ordinal);

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
                throw new ArgumentNullException(nameof(card), "卡牌不能为 null");

            if (card.UID != -1)
                throw new InvalidOperationException($"卡牌已有 UID {card.UID}，无法重新分配");

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

        public Card Create(string id) => Create<Card>(id);

        public T Create<T>(string id) where T : Card
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_constructors.TryGetValue(id, out var ctor))
            {
                var card = ctor() as T;
                if (card is { UID: -1 }) AssignUID(card);

                return card;
            }

            return null;
        }

        // 显式接口实现 - 仅供 CardEngine 内部通过 ICardFactory 接口调用
        Card ICardFactory.Create(string id) => Create<Card>(id);

        T ICardFactory.Create<T>(string id) => Create<T>(id);
    }
}