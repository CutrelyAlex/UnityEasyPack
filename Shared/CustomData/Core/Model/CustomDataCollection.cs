using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyPack.CustomData
{
    /// <summary>
    /// 优化的 CustomData 集合类
    /// 实现 IList<CustomDataEntry> 接口，提供 O(1) 查找性能
    /// 内部维护字典缓存以加速键值查找，所有修改操作自动同步缓存
    /// </summary>
    [Serializable]
    public class CustomDataCollection : IList<CustomDataEntry>, IEnumerable<CustomDataEntry>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<CustomDataEntry> _list = new();

        [NonSerialized]
        private Dictionary<string, CustomDataEntry> _cache;

        [NonSerialized]
        private bool _cacheDirty = true;

        public CustomDataCollection() { }

        public CustomDataCollection(int capacity)
        {
            _list = new List<CustomDataEntry>(capacity);
        }

        public CustomDataCollection(IEnumerable<CustomDataEntry> collection)
        {
            _list = new List<CustomDataEntry>(collection);
            _cacheDirty = true;
        }

        #region IList<CustomDataEntry> 实现

        public CustomDataEntry this[int index]
        {
            get => _list[index];
            set
            {
                if (_list[index] != value)
                {
                    _list[index] = value;
                    MarkDirty();
                }
            }
        }

        public int Count => _list.Count;

        public bool IsReadOnly => false;

        public void Add(CustomDataEntry item)
        {
            _list.Add(item);
            MarkDirty();
        }

        public void Clear()
        {
            _list.Clear();
            _cache?.Clear();
            _cacheDirty = false;
        }

        public bool Contains(CustomDataEntry item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(CustomDataEntry[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public int IndexOf(CustomDataEntry item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, CustomDataEntry item)
        {
            _list.Insert(index, item);
            MarkDirty();
        }

        public bool Remove(CustomDataEntry item)
        {
            var result = _list.Remove(item);
            if (result) MarkDirty();
            return result;
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
            MarkDirty();
        }

        #endregion

        #region IEnumerable<CustomDataEntry> 实现

        public IEnumerator<CustomDataEntry> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        #endregion

        #region 扩展方法

        /// <summary>
        /// 添加多个项
        /// </summary>
        public void AddRange(IEnumerable<CustomDataEntry> collection)
        {
            _list.AddRange(collection);
            MarkDirty();
        }

        /// <summary>
        /// 在指定位置插入多个项
        /// </summary>
        public void InsertRange(int index, IEnumerable<CustomDataEntry> collection)
        {
            _list.InsertRange(index, collection);
            MarkDirty();
        }

        /// <summary>
        /// 移除多个项
        /// </summary>
        public void RemoveRange(int index, int count)
        {
            _list.RemoveRange(index, count);
            MarkDirty();
        }

        /// <summary>
        /// 移除满足条件的所有项
        /// </summary>
        public int RemoveAll(Predicate<CustomDataEntry> match)
        {
            var result = _list.RemoveAll(match);
            if (result > 0) MarkDirty();
            return result;
        }

        /// <summary>
        /// 排序
        /// </summary>
        public void Sort()
        {
            _list.Sort();
            MarkDirty();
        }

        /// <summary>
        /// 使用比较器排序
        /// </summary>
        public void Sort(IComparer<CustomDataEntry> comparer)
        {
            _list.Sort(comparer);
            MarkDirty();
        }

        /// <summary>
        /// 使用比较方法排序
        /// </summary>
        public void Sort(Comparison<CustomDataEntry> comparison)
        {
            _list.Sort(comparison);
            MarkDirty();
        }

        /// <summary>
        /// 反转
        /// </summary>
        public void Reverse()
        {
            _list.Reverse();
            MarkDirty();
        }

        /// <summary>
        /// 获取子列表
        /// </summary>
        public List<CustomDataEntry> GetRange(int index, int count)
        {
            return _list.GetRange(index, count);
        }

        /// <summary>
        /// 二分查找
        /// </summary>
        public int BinarySearch(CustomDataEntry item)
        {
            return _list.BinarySearch(item);
        }

        /// <summary>
        /// 查找第一个匹配项
        /// </summary>
        public CustomDataEntry Find(Predicate<CustomDataEntry> match)
        {
            return _list.Find(match);
        }

        /// <summary>
        /// 查找所有匹配项
        /// </summary>
        public List<CustomDataEntry> FindAll(Predicate<CustomDataEntry> match)
        {
            return _list.FindAll(match);
        }

        #endregion

        #region 缓存管理

        /// <summary>
        /// 重建缓存字典
        /// </summary>
        private void RebuildCache()
        {
            if (_cache == null)
            {
                _cache = new Dictionary<string, CustomDataEntry>(_list.Count);
            }
            else
            {
                _cache.Clear();
            }

            foreach (var entry in _list)
            {
                if (!string.IsNullOrEmpty(entry.Key))
                {
                    _cache[entry.Key] = entry;
                }
            }
            _cacheDirty = false;
        }

        /// <summary>
        /// 确保缓存已初始化
        /// </summary>
        private void EnsureCache()
        {
            if (_cacheDirty || _cache == null)
            {
                RebuildCache();
            }
        }

        /// <summary>
        /// 标记缓存为脏
        /// </summary>
        private void MarkDirty()
        {
            _cacheDirty = true;
        }

        #endregion

        #region 优化的查找方法

        /// <summary>
        /// 高性能获取值（O(1)）
        /// </summary>
        public bool TryGetValue<T>(string key, out T value)
        {
            value = default;
            EnsureCache();

            if (_cache.TryGetValue(key, out var entry))
            {
                var obj = entry.GetValue();
                if (obj is T t)
                {
                    value = t;
                    return true;
                }

                try
                {
                    if (obj is string json)
                    {
                        value = JsonUtility.FromJson<T>(json);
                        return true;
                    }
                }
                catch { }
            }
            return false;
        }

        /// <summary>
        /// 高性能获取值（O(1)）
        /// </summary>
        public T GetValue<T>(string key, T defaultValue = default)
        {
            return TryGetValue(key, out T value) ? value : defaultValue;
        }

        /// <summary>
        /// 高性能设置值（O(1)）
        /// </summary>
        public void SetValue(string key, object value)
        {
            EnsureCache();

            if (_cache.TryGetValue(key, out var entry))
            {
                entry.SetValue(value);
            }
            else
            {
                var newEntry = new CustomDataEntry { Key = key };
                newEntry.SetValue(value);
                _list.Add(newEntry);
                _cache[key] = newEntry;
            }
        }

        /// <summary>
        /// 高性能移除值（O(1)）
        /// </summary>
        public bool RemoveValue(string key)
        {
            EnsureCache();

            if (_cache.TryGetValue(key, out var entry))
            {
                _list.Remove(entry);
                _cache.Remove(key);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 高性能检查是否存在（O(1)）
        /// </summary>
        public bool HasValue(string key)
        {
            EnsureCache();
            return _cache.ContainsKey(key);
        }

        #endregion

        #region 序列化支持

        /// <summary>
        /// 获取内部列表（仅用于序列化）
        /// </summary>
        public List<CustomDataEntry> GetInternalList()
        {
            return _list;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            // 序列化前无需特殊处理
            // Unity 会自动序列化 _list
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // 反序列化后标记缓存为脏，在下次访问时重建
            _cacheDirty = true;
            _cache = null;
        }

        #endregion
    }
}
