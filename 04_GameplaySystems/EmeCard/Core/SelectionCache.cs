using System.Collections.Generic;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    /// 选择缓存，使用时间戳+容器引用保证唯一性
    /// 每个规则处理周期（ProcessCore）后自动清空
    /// </summary>
    public class SelectionCache
    {
        private readonly struct CacheKey
        {
            public readonly Card Container;
            public readonly int MaxDepth;
            public readonly long Timestamp;

            public CacheKey(Card container, int maxDepth, long timestamp)
            {
                Container = container;
                MaxDepth = maxDepth;
                Timestamp = timestamp;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is CacheKey)) return false;
                CacheKey other = (CacheKey)obj;
                return ReferenceEquals(Container, other.Container)
                       && MaxDepth == other.MaxDepth
                       && Timestamp == other.Timestamp;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Container);
                    hash = hash * 31 + MaxDepth;
                    hash = hash * 31 + (int)(Timestamp ^ (Timestamp >> 32));
                    return hash;
                }
            }
        }

        private readonly Dictionary<CacheKey, List<Card>> _cache = new();
        private long _currentTimestamp;

        /// <summary>
        /// 开始新的缓存周期，更新时间戳
        /// </summary>
        public void BeginCycle()
        {
            _currentTimestamp = System.DateTime.UtcNow.Ticks;
        }

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
        }

        /// <summary>
        /// 尝试获取缓存的后代列表
        /// </summary>
        public bool TryGetDescendants(Card container, int maxDepth, out List<Card> result)
        {
            var key = new CacheKey(container, maxDepth, _currentTimestamp);
            return _cache.TryGetValue(key, out result);
        }

        /// <summary>
        /// 缓存后代列表
        /// </summary>
        public void CacheDescendants(Card container, int maxDepth, List<Card> descendants)
        {
            var key = new CacheKey(container, maxDepth, _currentTimestamp);
            // 创建副本避免外部修改影响缓存
            _cache[key] = new List<Card>(descendants);
        }

        /// <summary>
        /// 获取当前缓存条目数量
        /// </summary>
        public int Count => _cache.Count;
    }
}
