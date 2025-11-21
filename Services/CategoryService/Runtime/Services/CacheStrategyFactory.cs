using System.Collections.Generic;
using System.Linq;

namespace EasyPack.Category
{
    /// <summary>
    /// 缓存策略工厂
    /// </summary>
    public static class CacheStrategyFactory
    {
        /// <summary>
        /// 创建缓存策略实例
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="strategy">策略类型</param>
        /// <returns>缓存策略实例</returns>
        public static ICacheStrategy<T> Create<T>(CacheStrategy strategy)
        {
            return strategy switch
            {
                CacheStrategy.Loose => new LooseCacheStrategy<T>(),
                CacheStrategy.Balanced => new BalancedCacheStrategy<T>(),
                CacheStrategy.Efficient => new EfficientCacheStrategy<T>(),
                CacheStrategy.Aggressive => new AggressiveCacheStrategy<T>(),
                _ => new BalancedCacheStrategy<T>()
            };
        }
    }

    /// <summary>
    /// 松散缓存策略
    /// 特点：缓存所有查询结果，无驱逐策略
    /// 适用场景：内存充足，查询模式稳定的场景
    /// </summary>
    internal class LooseCacheStrategy<T> : ICacheStrategy<T>
    {
        private readonly Dictionary<string, IReadOnlyList<T>> _cache = new();

        public bool Get(string key, out IReadOnlyList<T> result)
        {
            return _cache.TryGetValue(key, out result);
        }

        public void Set(string key, IReadOnlyList<T> value)
        {
            _cache[key] = value;
        }

        public void Invalidate(string key)
        {
            _cache.Remove(key);
        }

        public void Clear()
        {
            _cache.Clear();
        }
    }

    /// <summary>
    /// 平衡缓存策略
    /// 特点：LRU 驱逐算法，3 次访问阈值后才缓存
    /// 适用场景：通用场景，平衡内存和性能
    /// </summary>
    internal class BalancedCacheStrategy<T> : ICacheStrategy<T>
    {
        private readonly Dictionary<string, CacheEntry> _cache;
        private readonly Dictionary<string, int> _accessCount;
        private readonly int _maxCacheSize;
        private readonly int _accessThreshold;

        private class CacheEntry
        {
            public IReadOnlyList<T> Data;
            public long LastAccessTick;
        }

        public BalancedCacheStrategy(int maxCacheSize = 1000, int accessThreshold = 3)
        {
            _cache = new Dictionary<string, CacheEntry>();
            _accessCount = new Dictionary<string, int>();
            _maxCacheSize = maxCacheSize;
            _accessThreshold = accessThreshold;
        }

        public bool Get(string key, out IReadOnlyList<T> result)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                entry.LastAccessTick = System.DateTime.UtcNow.Ticks;
                result = entry.Data;
                return true;
            }

            // 记录访问次数
            if (_accessCount.ContainsKey(key))
            {
                _accessCount[key]++;
            }
            else
            {
                _accessCount[key] = 1;
            }

            result = null;
            return false;
        }

        public void Set(string key, IReadOnlyList<T> value)
        {
            // 检查访问阈值
            if (_accessCount.TryGetValue(key, out var count) && count < _accessThreshold)
            {
                return; // 访问次数不足，不缓存
            }

            // 如果达到最大缓存大小，执行 LRU 驱逐
            if (_cache.Count >= _maxCacheSize && !_cache.ContainsKey(key))
            {
                var lruKey = _cache.OrderBy(kvp => kvp.Value.LastAccessTick).First().Key;
                _cache.Remove(lruKey);
                _accessCount.Remove(lruKey);
            }

            _cache[key] = new CacheEntry
            {
                Data = value,
                LastAccessTick = System.DateTime.UtcNow.Ticks
            };
        }

        public void Invalidate(string key)
        {
            _cache.Remove(key);
            _accessCount.Remove(key);
        }

        public void Clear()
        {
            _cache.Clear();
            _accessCount.Clear();
        }
    }

    /// <summary>
    /// 高效缓存策略
    /// 特点：仅缓存实体 ID，动态获取实体数据
    /// 适用场景：内存受限，实体对象较大的场景
    /// </summary>
    internal class EfficientCacheStrategy<T> : ICacheStrategy<T>
    {
        private readonly Dictionary<string, IReadOnlyList<string>> _idCache;
        private readonly System.Func<string, T> _entityResolver;

        public EfficientCacheStrategy(System.Func<string, T> entityResolver = null)
        {
            _idCache = new Dictionary<string, IReadOnlyList<string>>();
            _entityResolver = entityResolver;
        }

        /// <summary>
        /// 从缓存获取结果
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="result">缓存结果</param>
        /// <returns>是否命中缓存</returns>
        public bool Get(string key, out IReadOnlyList<T> result)
        {
            // 高效策略不缓存完整实体，总是返回 false
            // 实际项目中可以扩展为缓存 ID 列表，然后动态查询实体
            result = null;
            return false;
        }

        public void Set(string key, IReadOnlyList<T> value)
        {
            // 可选：缓存 ID 列表（需要 ID 提取器）
            // 当前简化实现不缓存
        }

        public void Invalidate(string key)
        {
            _idCache.Remove(key);
        }

        public void Clear()
        {
            _idCache.Clear();
        }
    }

    /// <summary>
    /// 激进缓存策略
    /// 特点：预加载常用模式，不主动驱逐
    /// 适用场景：查询模式高度可预测的场景
    /// </summary>
    internal class AggressiveCacheStrategy<T> : ICacheStrategy<T>
    {
        private readonly Dictionary<string, IReadOnlyList<T>> _cache;
        private readonly HashSet<string> _preloadedPatterns = new();

        public AggressiveCacheStrategy()
        {
            _cache = new Dictionary<string, IReadOnlyList<T>>();
        }

        public bool Get(string key, out IReadOnlyList<T> result)
        {
            return _cache.TryGetValue(key, out result);
        }

        public void Set(string key, IReadOnlyList<T> value)
        {
            // 激进策略永久缓存，不驱逐
            _cache[key] = value;

            // 标记为已预加载
            if (key.Contains("*") || key.Contains("?"))
            {
                _preloadedPatterns.Add(key);
            }
        }

        public void Invalidate(string key)
        {
            // 激进策略不主动失效缓存
            // 除非显式调用 Clear
        }

        public void Clear()
        {
            _cache.Clear();
            _preloadedPatterns.Clear();
        }

        /// <summary>
        /// 预加载常用查询模式
        /// </summary>
        public void PreloadPatterns(IEnumerable<string> patterns)
        {
            foreach (var pattern in patterns)
            {
                _preloadedPatterns.Add(pattern);
            }
        }
    }
}
