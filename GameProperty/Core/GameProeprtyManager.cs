using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// GameProperty 全局缓存管理器
    /// 确保相同ID的GameProperty在全局范围内唯一，避免重复创建
    /// </summary>
    public static class GamePropertyManager
    {
        private static readonly ConcurrentDictionary<string, GameProperty> _cachedProperties = new ConcurrentDictionary<string, GameProperty>();
        private static readonly ConcurrentDictionary<string, int> _referenceCounters = new ConcurrentDictionary<string, int>();

        #region 创建和获取

        /// <summary>
        /// 获取或创建GameProperty实例
        /// 如果已存在相同ID的实例，返回缓存的实例并增加引用计数
        /// </summary>
        /// <param name="id">属性ID</param>
        /// <param name="initValue">初始值（仅在创建新实例时使用）</param>
        /// <returns>GameProperty实例</returns>
        public static GameProperty GetOrCreate(string id, float initValue = 0f)
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning("GameProperty ID不能为空或null");
                return null;
            }

            return _cachedProperties.AddOrUpdate(id,
                // 创建新实例
                key => {
                    var newProperty = new GameProperty(key, initValue);
                    _referenceCounters.TryAdd(key, 1);
                    return newProperty;
                },
                // 更新现有实例
                (key, existingProperty) => {
                    _referenceCounters.AddOrUpdate(key, 1, (k, count) => count + 1);
                    return existingProperty;
                });
        }

        /// <summary>
        /// 获取已缓存的GameProperty实例，不创建新实例
        /// </summary>
        /// <param name="id">属性ID</param>
        /// <returns>GameProperty实例，不存在则返回null</returns>
        public static GameProperty Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return _cachedProperties.TryGetValue(id, out var property) ? property : null;
        }

        /// <summary>
        /// 检查指定ID的GameProperty是否存在于缓存中
        /// </summary>
        /// <param name="id">属性ID</param>
        /// <returns>存在返回true，否则返回false</returns>
        public static bool Contains(string id)
        {
            return !string.IsNullOrEmpty(id) && _cachedProperties.ContainsKey(id);
        }

        #endregion

        #region 引用计数管理

        /// <summary>
        /// 增加指定GameProperty的引用计数
        /// </summary>
        /// <param name="id">属性ID</param>
        /// <returns>增加后的引用计数</returns>
        public static int AddReference(string id)
        {
            if (string.IsNullOrEmpty(id)) return 0;
            return _referenceCounters.AddOrUpdate(id, 1, (key, count) => count + 1);
        }

        /// <summary>
        /// 减少指定GameProperty的引用计数
        /// 当引用计数为0时，从缓存中移除该实例
        /// </summary>
        /// <param name="id">属性ID</param>
        /// <returns>减少后的引用计数</returns>
        public static int RemoveReference(string id)
        {
            if (string.IsNullOrEmpty(id)) return 0;

            if (_referenceCounters.TryGetValue(id, out var currentCount))
            {
                var newCount = currentCount - 1;
                if (newCount <= 0)
                {
                    // 引用计数为0，从缓存中移除
                    _referenceCounters.TryRemove(id, out _);
                    _cachedProperties.TryRemove(id, out _);
                    return 0;
                }
                else
                {
                    _referenceCounters.TryUpdate(id, newCount, currentCount);
                    return newCount;
                }
            }

            return 0;
        }

        /// <summary>
        /// 获取指定GameProperty的引用计数
        /// </summary>
        /// <param name="id">属性ID</param>
        /// <returns>引用计数</returns>
        public static int GetReferenceCount(string id)
        {
            if (string.IsNullOrEmpty(id)) return 0;
            return _referenceCounters.TryGetValue(id, out var count) ? count : 0;
        }

        #endregion

        #region 缓存管理

        /// <summary>
        /// 获取所有缓存的GameProperty实例
        /// </summary>
        /// <returns>所有缓存的GameProperty实例</returns>
        public static IEnumerable<GameProperty> GetAllCached()
        {
            return _cachedProperties.Values.ToList();
        }

        /// <summary>
        /// 获取所有缓存的GameProperty的ID
        /// </summary>
        /// <returns>所有缓存的GameProperty的ID</returns>
        public static IEnumerable<string> GetAllCachedIds()
        {
            return _cachedProperties.Keys.ToList();
        }

        /// <summary>
        /// 获取当前缓存的GameProperty数量
        /// </summary>
        public static int CachedCount => _cachedProperties.Count;

        /// <summary>
        /// 强制清空所有缓存的GameProperty
        /// 警告：此操作可能导致现有的GameProperty引用失效
        /// </summary>
        public static void ClearAll()
        {
            _cachedProperties.Clear();
            _referenceCounters.Clear();
        }

        /// <summary>
        /// 清理没有引用的GameProperty实例
        /// </summary>
        /// <returns>清理的实例数量</returns>
        public static int CleanupUnreferencedProperties()
        {
            var unreferencedKeys = new List<string>();

            foreach (var kvp in _referenceCounters)
            {
                if (kvp.Value <= 0)
                {
                    unreferencedKeys.Add(kvp.Key);
                }
            }

            foreach (var key in unreferencedKeys)
            {
                _cachedProperties.TryRemove(key, out _);
                _referenceCounters.TryRemove(key, out _);
            }

            return unreferencedKeys.Count;
        }

        /// <summary>
        /// 强制移除指定ID的GameProperty从缓存
        /// 此操作可能导致现有的GameProperty引用失效
        /// </summary>
        /// <param name="id">属性ID</param>
        /// <returns>移除成功返回true，否则返回false</returns>
        public static bool ForceRemove(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;

            var removed = _cachedProperties.TryRemove(id, out _);
            _referenceCounters.TryRemove(id, out _);
            return removed;
        }

        #endregion

        #region 调试和统计

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        /// <returns>缓存统计信息</returns>
        public static GamePropertyCacheStats GetCacheStats()
        {
            return new GamePropertyCacheStats
            {
                TotalCachedCount = _cachedProperties.Count,
                TotalReferenceCount = _referenceCounters.Values.Sum(),
                Properties = _cachedProperties.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new GamePropertyInfo
                    {
                        ID = kvp.Key,
                        ReferenceCount = _referenceCounters.TryGetValue(kvp.Key, out var count) ? count : 0,
                        BaseValue = kvp.Value.GetBaseValue(),
                        CurrentValue = kvp.Value.GetValue(),
                        ModifierCount = kvp.Value.ModifierCount
                    })
            };
        }
        #endregion
    }

    /// <summary>
    /// GameProperty缓存统计信息
    /// </summary>
    public class GamePropertyCacheStats
    {
        public int TotalCachedCount { get; set; }
        public int TotalReferenceCount { get; set; }
        public Dictionary<string, GamePropertyInfo> Properties { get; set; } = new Dictionary<string, GamePropertyInfo>();
    }

    /// <summary>
    /// GameProperty实例信息
    /// </summary>
    public class GamePropertyInfo
    {
        public string ID { get; set; }
        public int ReferenceCount { get; set; }
        public float BaseValue { get; set; }
        public float CurrentValue { get; set; }
        public int ModifierCount { get; set; }
    }
}