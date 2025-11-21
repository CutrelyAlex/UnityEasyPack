using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyPack.Category
{
    /// <summary>
    /// 缓存策略工厂
    /// 提供三级缓存架构的实现
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
                CacheStrategy.Premium => new PremiumCacheStrategy<T>(),
                _ => new BalancedCacheStrategy<T>()
            };
        }
    }

    /// <summary>
    /// 基础缓存策略（Loose）
    /// 特点：缓存所有查询结果，无驱逐策略
    /// 优点：查询性能最优，无额外开销
    /// 缺点：内存占用可能很大
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
    /// 平衡缓存策略（Balanced）
    /// 特点：自动缓存所有查询 + LRU 驱逐算法
    /// 优点：平衡内存占用和查询性能
    /// 缺点：达到上限时需要驱逐最少使用的项
    /// 适用场景：通用场景，既需要好的性能又需要控制内存占用
    /// </summary>
    internal class BalancedCacheStrategy<T> : ICacheStrategy<T>
    {
        private readonly Dictionary<string, CacheEntry> _cache;
        private readonly int _maxCacheSize;

        private class CacheEntry
        {
            public IReadOnlyList<T> Data;
            public long LastAccessTick;
        }

        public BalancedCacheStrategy(int maxCacheSize = 1000)
        {
            _cache = new Dictionary<string, CacheEntry>();
            _maxCacheSize = maxCacheSize;
        }

        public bool Get(string key, out IReadOnlyList<T> result)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                entry.LastAccessTick = DateTime.UtcNow.Ticks;
                result = entry.Data;
                return true;
            }

            result = null;
            return false;
        }

        public void Set(string key, IReadOnlyList<T> value)
        {
            // 如果达到最大缓存大小且不存在此键，执行 LRU 驱逐
            if (_cache.Count >= _maxCacheSize && !_cache.ContainsKey(key))
            {
                var lruKey = _cache.OrderBy(kvp => kvp.Value.LastAccessTick).First().Key;
                _cache.Remove(lruKey);
            }

            _cache[key] = new CacheEntry
            {
                Data = value,
                LastAccessTick = DateTime.UtcNow.Ticks
            };
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
    /// 高级缓存策略（Premium）
    /// 特点：完全缓存，永久存储，无驱逐
    /// 优点：最高性能，所有查询都命中缓存
    /// 缺点：内存占用最大，无法限制缓存大小
    /// 适用场景：高频查询，数据集确定且较小，需要最佳性能的场景
    /// </summary>
    internal class PremiumCacheStrategy<T> : ICacheStrategy<T>
    {
        private readonly Dictionary<string, IReadOnlyList<T>> _cache = new();

        public bool Get(string key, out IReadOnlyList<T> result)
        {
            return _cache.TryGetValue(key, out result);
        }

        public void Set(string key, IReadOnlyList<T> value)
        {
            // Premium 缓存永久存储，不驱逐
            _cache[key] = value;
        }

        public void Invalidate(string key)
        {
            // Premium 缓存通常不主动失效
            _cache.Remove(key);
        }

        public void Clear()
        {
            _cache.Clear();
        }
    }
}
