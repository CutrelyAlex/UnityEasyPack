using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// GameProperty ȫ�ֻ��������
    /// ȷ����ͬID��GameProperty��ȫ�ַ�Χ��Ψһ�������ظ�����
    /// </summary>
    public static class GamePropertyManager
    {
        private static readonly ConcurrentDictionary<string, GameProperty> _cachedProperties = new ConcurrentDictionary<string, GameProperty>();
        private static readonly ConcurrentDictionary<string, int> _referenceCounters = new ConcurrentDictionary<string, int>();

        #region �����ͻ�ȡ

        /// <summary>
        /// ��ȡ�򴴽�GamePropertyʵ��
        /// ����Ѵ�����ͬID��ʵ�������ػ����ʵ�����������ü���
        /// </summary>
        /// <param name="id">����ID</param>
        /// <param name="initValue">��ʼֵ�����ڴ�����ʵ��ʱʹ�ã�</param>
        /// <returns>GamePropertyʵ��</returns>
        public static GameProperty GetOrCreate(string id, float initValue = 0f)
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning("GameProperty ID����Ϊ�ջ�null");
                return null;
            }

            return _cachedProperties.AddOrUpdate(id,
                // ������ʵ��
                key => {
                    var newProperty = new GameProperty(key, initValue);
                    _referenceCounters.TryAdd(key, 1);
                    return newProperty;
                },
                // ��������ʵ��
                (key, existingProperty) => {
                    _referenceCounters.AddOrUpdate(key, 1, (k, count) => count + 1);
                    return existingProperty;
                });
        }

        /// <summary>
        /// ��ȡ�ѻ����GamePropertyʵ������������ʵ��
        /// </summary>
        /// <param name="id">����ID</param>
        /// <returns>GamePropertyʵ�����������򷵻�null</returns>
        public static GameProperty Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return _cachedProperties.TryGetValue(id, out var property) ? property : null;
        }

        /// <summary>
        /// ���ָ��ID��GameProperty�Ƿ�����ڻ�����
        /// </summary>
        /// <param name="id">����ID</param>
        /// <returns>���ڷ���true�����򷵻�false</returns>
        public static bool Contains(string id)
        {
            return !string.IsNullOrEmpty(id) && _cachedProperties.ContainsKey(id);
        }

        #endregion

        #region ���ü�������

        /// <summary>
        /// ����ָ��GameProperty�����ü���
        /// </summary>
        /// <param name="id">����ID</param>
        /// <returns>���Ӻ�����ü���</returns>
        public static int AddReference(string id)
        {
            if (string.IsNullOrEmpty(id)) return 0;
            return _referenceCounters.AddOrUpdate(id, 1, (key, count) => count + 1);
        }

        /// <summary>
        /// ����ָ��GameProperty�����ü���
        /// �����ü���Ϊ0ʱ���ӻ������Ƴ���ʵ��
        /// </summary>
        /// <param name="id">����ID</param>
        /// <returns>���ٺ�����ü���</returns>
        public static int RemoveReference(string id)
        {
            if (string.IsNullOrEmpty(id)) return 0;

            if (_referenceCounters.TryGetValue(id, out var currentCount))
            {
                var newCount = currentCount - 1;
                if (newCount <= 0)
                {
                    // ���ü���Ϊ0���ӻ������Ƴ�
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
        /// ��ȡָ��GameProperty�����ü���
        /// </summary>
        /// <param name="id">����ID</param>
        /// <returns>���ü���</returns>
        public static int GetReferenceCount(string id)
        {
            if (string.IsNullOrEmpty(id)) return 0;
            return _referenceCounters.TryGetValue(id, out var count) ? count : 0;
        }

        #endregion

        #region �������

        /// <summary>
        /// ��ȡ���л����GamePropertyʵ��
        /// </summary>
        /// <returns>���л����GamePropertyʵ��</returns>
        public static IEnumerable<GameProperty> GetAllCached()
        {
            return _cachedProperties.Values.ToList();
        }

        /// <summary>
        /// ��ȡ���л����GameProperty��ID
        /// </summary>
        /// <returns>���л����GameProperty��ID</returns>
        public static IEnumerable<string> GetAllCachedIds()
        {
            return _cachedProperties.Keys.ToList();
        }

        /// <summary>
        /// ��ȡ��ǰ�����GameProperty����
        /// </summary>
        public static int CachedCount => _cachedProperties.Count;

        /// <summary>
        /// ǿ��������л����GameProperty
        /// ���棺�˲������ܵ������е�GameProperty����ʧЧ
        /// </summary>
        public static void ClearAll()
        {
            _cachedProperties.Clear();
            _referenceCounters.Clear();
        }

        /// <summary>
        /// ����û�����õ�GamePropertyʵ��
        /// </summary>
        /// <returns>�����ʵ������</returns>
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
        /// ǿ���Ƴ�ָ��ID��GameProperty�ӻ���
        /// �˲������ܵ������е�GameProperty����ʧЧ
        /// </summary>
        /// <param name="id">����ID</param>
        /// <returns>�Ƴ��ɹ�����true�����򷵻�false</returns>
        public static bool ForceRemove(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;

            var removed = _cachedProperties.TryRemove(id, out _);
            _referenceCounters.TryRemove(id, out _);
            return removed;
        }

        #endregion

        #region ���Ժ�ͳ��

        /// <summary>
        /// ��ȡ����ͳ����Ϣ
        /// </summary>
        /// <returns>����ͳ����Ϣ</returns>
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
    /// GameProperty����ͳ����Ϣ
    /// </summary>
    public class GamePropertyCacheStats
    {
        public int TotalCachedCount { get; set; }
        public int TotalReferenceCount { get; set; }
        public Dictionary<string, GamePropertyInfo> Properties { get; set; } = new Dictionary<string, GamePropertyInfo>();
    }

    /// <summary>
    /// GamePropertyʵ����Ϣ
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