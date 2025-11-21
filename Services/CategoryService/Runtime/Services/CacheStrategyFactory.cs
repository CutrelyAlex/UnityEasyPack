using System;
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
        public static ICacheStrategyBase<T> Create<T>(CacheStrategy strategy)
        {
            return strategy switch
            {
                CacheStrategy.HotspotTracking => new HotspotTrackingCacheStrategy<T>(),
                CacheStrategy.LRUFrequencyHybrid => new LRUFrequencyHybridStrategy<T>(),
                CacheStrategy.ShardedNoEviction => new ShardedNoEvictionStrategy<T>(),
                _ => new LRUFrequencyHybridStrategy<T>()
            };
        }
    }

    /// <summary>
    /// 缓存策略接口
    /// </summary>
    public interface ICacheStrategyBase<T> : ICacheStrategy<T>
    {
        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        CacheStatistics GetStatistics();

        /// <summary>
        /// 尝试从缓存获取，并记录访问统计
        /// </summary>
        bool TryGetOptimized(string key, out IReadOnlyList<T> result);
    }

    /// <summary>
    /// 缓存统计信息
    /// </summary>
    public class CacheStatistics
    {
        public int TotalEntries { get; set; }
        public int HitCount { get; set; }
        public int MissCount { get; set; }
        public double HitRate => TotalEntries > 0 ? (double)HitCount / (HitCount + MissCount) : 0;
        public long MemoryUsageBytes { get; set; }
    }

    #region HotspotTracking

    internal class HotspotTrackingCacheStrategy<T> : ICacheStrategyBase<T>
    {
        private readonly Dictionary<string, IReadOnlyList<T>> _hotCache = new();
        private readonly Dictionary<string, int> _accessCounter = new();
        private const int HOTPOINT_THRESHOLD = 10; // 访问次数 >= 10 才缓存 // TODO: 此类设置转为可配置
        private const int COUNTER_CLEANUP_INTERVAL = 1000; // 每 1000 次操作清理一次计数器
        private int _operationCount = 0;

        private int _hits = 0;
        private int _misses = 0;

        public bool Get(string key, out IReadOnlyList<T> result)
        {
            _operationCount++;

            if (_hotCache.TryGetValue(key, out result))
            {
                _hits++;
                return true;
            }

            // 记录冷数据访问
            if (_accessCounter.TryGetValue(key, out var count))
            {
                _accessCounter[key] = count + 1;

                // 访问次数达到阈值，晋升为热点数据
                if (count + 1 >= HOTPOINT_THRESHOLD)
                {
                    // 标记，等待下次 Set 时缓存
                }
            }
            else
            {
                _accessCounter[key] = 1;
            }

            _misses++;

            // 定期清理计数器
            if (_operationCount % COUNTER_CLEANUP_INTERVAL == 0)
            {
                CleanupCounters();
            }

            return false;
        }

        public void Set(string key, IReadOnlyList<T> value)
        {
            // 检查是否应该缓存此数据
            if (_accessCounter.TryGetValue(key, out var count) && count >= HOTPOINT_THRESHOLD)
            {
                _hotCache[key] = value;
            }
            else if (_hotCache.ContainsKey(key))
            {
                // 已缓存的热点数据更新
                _hotCache[key] = value;
            }

            // 否则不缓存冷数据
        }

        public void Invalidate(string key)
        {
            _hotCache.Remove(key);
            _accessCounter.Remove(key);
        }

        public void Clear()
        {
            _hotCache.Clear();
            _accessCounter.Clear();
            _hits = 0;
            _misses = 0;
            _operationCount = 0;
        }

        public CacheStatistics GetStatistics()
        {
            return new CacheStatistics
            {
                TotalEntries = _hotCache.Count,
                HitCount = _hits,
                MissCount = _misses,
                MemoryUsageBytes = _hotCache.Count * 1000 // 粗略估计
            };
        }

        public bool TryGetOptimized(string key, out IReadOnlyList<T> result)
        {
            return Get(key, out result);
        }

        private void CleanupCounters()
        {
            // 移除不在热缓存中且访问次数较低的计数器
            var keysToRemove = _accessCounter
                .Where(kvp => !_hotCache.ContainsKey(kvp.Key) && kvp.Value < HOTPOINT_THRESHOLD)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _accessCounter.Remove(key);
            }
        }
    }

    #endregion

    #region LRUFrequencyHybrid

    internal class LRUFrequencyHybridStrategy<T> : ICacheStrategyBase<T>
    {
        private readonly Dictionary<string, CacheEntryV2> _cache = new();
        private readonly int _maxCacheSize;
        private const int FREQUENCY_DECAY_INTERVAL = 500; // 频率衰减间隔
        private int _operationCount = 0;
        private int _hits = 0;
        private int _misses = 0;

        private class CacheEntryV2
        {
            public IReadOnlyList<T> Data;
            public long LastAccessTick;
            public int AccessFrequency; // 访问频率计数
            public long CreatedTick;
        }

        public LRUFrequencyHybridStrategy(int maxCacheSize = 2000)
        {
            _maxCacheSize = maxCacheSize;
        }

        public bool Get(string key, out IReadOnlyList<T> result)
        {
            _operationCount++;

            if (_cache.TryGetValue(key, out var entry))
            {
                entry.LastAccessTick = DateTime.UtcNow.Ticks;
                entry.AccessFrequency = Math.Min(entry.AccessFrequency + 1, 255); // 上限 255
                result = entry.Data;
                _hits++;
                return true;
            }

            _misses++;
            result = null;

            // 定期衰减频率
            if (_operationCount % FREQUENCY_DECAY_INTERVAL == 0)
            {
                DecayFrequencies();
            }

            return false;
        }

        public void Set(string key, IReadOnlyList<T> value)
        {
            if (_cache.ContainsKey(key))
            {
                // 更新现有项
                _cache[key].Data = value;
                _cache[key].LastAccessTick = DateTime.UtcNow.Ticks;
            }
            else
            {
                // 新增项检查是否需要驱逐
                if (_cache.Count >= _maxCacheSize)
                {
                    EvictLRUFrequency();
                }

                _cache[key] = new CacheEntryV2
                {
                    Data = value,
                    LastAccessTick = DateTime.UtcNow.Ticks,
                    AccessFrequency = 1,
                    CreatedTick = DateTime.UtcNow.Ticks
                };
            }
        }

        public void Invalidate(string key)
        {
            _cache.Remove(key);
        }

        public void Clear()
        {
            _cache.Clear();
            _hits = 0;
            _misses = 0;
            _operationCount = 0;
        }

        public CacheStatistics GetStatistics()
        {
            return new CacheStatistics
            {
                TotalEntries = _cache.Count,
                HitCount = _hits,
                MissCount = _misses,
                MemoryUsageBytes = _cache.Count * 2000
            };
        }

        public bool TryGetOptimized(string key, out IReadOnlyList<T> result)
        {
            return Get(key, out result);
        }

        private void EvictLRUFrequency()
        {
            var now = DateTime.UtcNow.Ticks;
            const long TICK_PER_SECOND = 10_000_000;

            // 计算每个项的驱逐分数：访问时间距离 × 频率倒数
            var evictionScores = _cache.Select(kvp =>
            {
                var age = (now - kvp.Value.LastAccessTick) / TICK_PER_SECOND; // 秒
                var score = age / (double)(kvp.Value.AccessFrequency + 1); // 时间 / 频率
                return (kvp.Key, Score: score);
            }).OrderByDescending(x => x.Score); // 分数高的优先驱逐

            var keyToRemove = evictionScores.First().Key;
            _cache.Remove(keyToRemove);
        }

        private void DecayFrequencies()
        {
            // 频率衰减：所有项的频率减半，以适应工作集变化
            foreach (var entry in _cache.Values)
            {
                entry.AccessFrequency = Math.Max(1, entry.AccessFrequency / 2);
            }
        }
    }

    #endregion

    #region ShardedNoEviction

    /// <summary>
    /// 分片缓存策略
    /// 分片设计 + 本地缓存 + 无驱逐
    /// 内存占用：最大
    /// 性能：最快
    /// 准确性：100%
    /// </summary>
    internal class ShardedNoEvictionStrategy<T> : ICacheStrategyBase<T>
    {
        private const int SHARD_COUNT = 8; // 分片数量（2的幂）
        private const int SHARD_MASK = SHARD_COUNT - 1;
        private readonly Dictionary<string, IReadOnlyList<T>>[] _shards;
        private string _lastAccessKey = "";
        private IReadOnlyList<T> _lastAccessValue;
        private int _hits = 0;
        private int _misses = 0;

        public ShardedNoEvictionStrategy()
        {
            _shards = new Dictionary<string, IReadOnlyList<T>>[SHARD_COUNT];
            for (int i = 0; i < SHARD_COUNT; i++)
            {
                _shards[i] = new Dictionary<string, IReadOnlyList<T>>();
            }
        }

        public bool Get(string key, out IReadOnlyList<T> result)
        {
            // 快速路径：检查本地缓存（最近访问）
            if (_lastAccessKey == key && _lastAccessValue != null)
            {
                result = _lastAccessValue;
                _hits++;
                return true;
            }

            // 分片查询
            int shardIndex = GetShardIndex(key);
            var shard = _shards[shardIndex];

            if (shard.TryGetValue(key, out result))
            {
                // 更新本地缓存
                _lastAccessKey = key;
                _lastAccessValue = result;
                _hits++;
                return true;
            }

            _misses++;
            return false;
        }

        public void Set(string key, IReadOnlyList<T> value)
        {
            int shardIndex = GetShardIndex(key);
            var shard = _shards[shardIndex];
            shard[key] = value;

            // 更新本地缓存
            _lastAccessKey = key;
            _lastAccessValue = value;
        }

        public void Invalidate(string key)
        {
            int shardIndex = GetShardIndex(key);
            var shard = _shards[shardIndex];
            shard.Remove(key);

            if (key == _lastAccessKey)
            {
                _lastAccessKey = "";
                _lastAccessValue = null;
            }
        }

        public void Clear()
        {
            for (int i = 0; i < SHARD_COUNT; i++)
            {
                _shards[i].Clear();
            }
            _lastAccessKey = "";
            _lastAccessValue = null;
            _hits = 0;
            _misses = 0;
        }

        public CacheStatistics GetStatistics()
        {
            int totalEntries = _shards.Sum(s => s.Count);
            return new CacheStatistics
            {
                TotalEntries = totalEntries,
                HitCount = _hits,
                MissCount = _misses,
                MemoryUsageBytes = totalEntries * 3000 // TODO: 粗略估计 
            };
        }

        public bool TryGetOptimized(string key, out IReadOnlyList<T> result)
        {
            return Get(key, out result);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private int GetShardIndex(string key)
        {
            return key.GetHashCode() & int.MaxValue & SHARD_MASK;
        }
    }

    #endregion
}
