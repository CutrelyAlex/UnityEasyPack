using System;
using System.Collections.Generic;
using System.Threading;

namespace EasyPack.EmeCardSystem
{
    // 按 ID 创建卡牌实例
    public interface ICardFactory
    {
        Card Create(string id);
        T Create<T>(string id) where T : Card;
        IReadOnlyCollection<string> GetAllCardIds();

        CardEngine Owner { get; set; }
    }

    public sealed class CardFactory : ICardFactory
    {
        private readonly Dictionary<string, Func<Card>> _constructors = new(StringComparer.Ordinal);

        /// <summary>
        ///     下一个可用的 UID，起始值 1001。
        /// </summary>
        private static int _nextUID = 1001;

        /// <summary>
        ///     UID 分配上限
        /// </summary>
        private const int MAX_UID = 999999;

        public CardEngine Owner { get; set; }

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
        ///     为新卡牌分配唯一的 UID。
        ///     使用 <see cref="Interlocked.Increment" /> 确保线程安全和顺序递增。
        /// </summary>
        /// <returns>分配的 UID，从 1001 开始。</returns>
        /// <exception cref="InvalidOperationException">当 UID 达到上限时抛出。</exception>
        private static int AllocateUID()
        {
            int uid = Interlocked.Increment(ref _nextUID);
            if (uid > MAX_UID) throw new InvalidOperationException($"无法分配 UID：已达到上限 {MAX_UID}。");

            return uid;
        }

        /// <summary>
        ///     为现有卡牌分配 UID（由 Create 方法或 CardEngine 内部使用）。<br />
        ///     TODO: UID仅在运行时有效，重启后可能会重新分配
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
        public static int GetCurrentUID() => _nextUID;

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
    }
}