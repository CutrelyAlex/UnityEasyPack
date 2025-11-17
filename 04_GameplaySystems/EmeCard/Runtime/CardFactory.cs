using System;
using System.Collections.Generic;

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
        public CardEngine Owner { get; set; }

        public void Register(string id, Func<Card> ctor)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            _constructors[id] = ctor ?? throw new ArgumentNullException(nameof(ctor));
        }

        public void Register(IReadOnlyDictionary<string, Func<Card>> productionList)
        {
            foreach (var (id, ctor) in productionList)
            {
                Register(id, ctor);
            }
        }

        /// <summary>
        /// 获取所有已注册的卡牌ID
        /// </summary>
        public IReadOnlyCollection<string> GetAllCardIds()
        {
            return _constructors.Keys;
        }

        public Card Create(string id)
        {
            return Create<Card>(id);
        }
        public T Create<T>(string id) where T : Card
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_constructors.TryGetValue(id, out Func<Card> ctor))
            {
                return ctor() as T;
            }
            return null;
        }
    }
}
