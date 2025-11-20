using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack.CustomData
{
    /// <summary>
    /// CustomData 集合类
    /// 实现 IList&lt;CustomDataEntry&gt;
    /// 接口内部维护字典缓存
    /// 建议使用此类作为集合，因为性能是O(1)的
    /// </summary>
    [Serializable]
    public class CustomDataCollection : IList<CustomDataEntry>, IEnumerable<CustomDataEntry>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<CustomDataEntry> _list = new();

        /// <summary>
        /// 缓存映射 key -> index
        /// </summary>
        [NonSerialized]
        private Dictionary<string, int> _keyIndexMap;

        /// <summary>
        /// Entry对象缓存：key -> entry
        /// </summary>
        [NonSerialized]
        private Dictionary<string, CustomDataEntry> _entryCache;

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
            _keyIndexMap?.Clear();
            _entryCache?.Clear();
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
        /// 重建缓存索引映射和Entry对象缓存
        /// </summary>
        private void RebuildCache()
        {
            if (_keyIndexMap == null)
            {
                _keyIndexMap = new Dictionary<string, int>(_list.Count);
            }
            else
            {
                _keyIndexMap.Clear();
            }

            if (_entryCache == null)
            {
                _entryCache = new Dictionary<string, CustomDataEntry>(_list.Count);
            }
            else
            {
                _entryCache.Clear();
            }

            // 构建 key -> index 和 key -> entry 映射
            for (int i = 0; i < _list.Count; i++)
            {
                var entry = _list[i];
                if (!string.IsNullOrEmpty(entry.Key))
                {
                    _keyIndexMap[entry.Key] = i;
                    _entryCache[entry.Key] = entry;
                }
            }
            _cacheDirty = false;
        }

        /// <summary>
        /// 确保缓存索引已初始化
        /// </summary>
        private void EnsureCache()
        {
            if (_cacheDirty || _keyIndexMap == null)
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

        #region 查找方法

        /// <summary>
        /// 尝试获取指定键的值，如果存在则返回true并设置out参数
        /// 时间复杂度：O(1)
        /// </summary>
        /// <typeparam name="T">值的类型</typeparam>
        /// <param name="key">要查找的键</param>
        /// <param name="value">如果找到则设置为对应的值，否则为默认值</param>
        /// <returns>如果键存在则返回true，否则返回false</returns>
        public bool TryGetValue<T>(string key, out T value)
        {
            value = default;
            EnsureCache();
            
            if (_entryCache.TryGetValue(key, out var entry))
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
                catch(SystemException)
                {
                    Debug.LogWarning("从Key转换到值失败");
                }
            }
            return false;
        }

        /// <summary>
        /// 获取指定键的值，如果不存在则返回默认值
        /// </summary>
        /// <typeparam name="T">值的类型</typeparam>
        /// <param name="key">要查找的键</param>
        /// <param name="defaultValue">如果键不存在时返回的默认值</param>
        /// <returns>键对应的值或默认值</returns>
        public T GetValue<T>(string key, T defaultValue = default)
        {
            return TryGetValue(key, out T value) ? value : defaultValue;
        }

        /// <summary>
        /// 设置指定键的值，如果键不存在则添加新条目
        /// </summary>
        /// <param name="key">要设置的键</param>
        /// <param name="value">要设置的值</param>
        public void SetValue(string key, object value)
        {
            EnsureCache();

            if (_keyIndexMap.TryGetValue(key, out int index))
            {
                // 存在：直接更新entry和缓存
                var entry = _list[index];
                entry.SetValue(value);
                _entryCache[key] = entry;
            }
            else
            {
                // 不存在：添加新条目
                var newEntry = new CustomDataEntry { Key = key };
                newEntry.SetValue(value);
                int newIndex = _list.Count;  // 新条目会被添加到末尾
                _list.Add(newEntry);
                _keyIndexMap[key] = newIndex;  // 缓存索引
                _entryCache[key] = newEntry;    // 缓存entry对象
            }
        }

        /// <summary>
        /// 移除指定键的值，如果存在则返回true
        /// 时间复杂度：O(1) 平均情况
        /// </summary>
        /// <param name="key">要移除的键</param>
        /// <returns>如果键存在并成功移除则返回true，否则返回false</returns>
        public bool RemoveValue(string key)
        {
            EnsureCache();

            if (!_keyIndexMap.TryGetValue(key, out int indexToRemove))
            {
                return false; 
            }
            
            int lastIndex = _list.Count - 1;
            
            if (indexToRemove != lastIndex)
            {
                // 交换：将末尾元素移到删除位置
                var lastEntry = _list[lastIndex];
                _list[indexToRemove] = lastEntry;
                
                // 更新被交换元素的索引缓存
                if (!string.IsNullOrEmpty(lastEntry.Key))
                {
                    _keyIndexMap[lastEntry.Key] = indexToRemove;
                }
            }
            
            // 移除末尾
            _list.RemoveAt(lastIndex);
            _keyIndexMap.Remove(key);
            _entryCache.Remove(key);  // 同时删除entry缓存
            
            return true;
        }

        /// <summary>
        /// 批量删除指定键
        /// </summary>
        /// <param name="keys">要删除的键集合</param>
        /// <returns>实际删除的元素个数</returns>
        public int RemoveValues(IEnumerable<string> keys)
        {
            EnsureCache();
            
            var keysToRemove = new HashSet<string>(keys);
            int removedCount = 0;
            int writeIndex = 0;  // 写指针
            
            for (int i = 0; i < _list.Count; i++)
            {
                var entry = _list[i];
                if (!keysToRemove.Contains(entry.Key))
                {
                    // 保留此元素
                    if (writeIndex != i)
                    {
                        _list[writeIndex] = _list[i];
                    }
                    // 更新索引缓存
                    if (!string.IsNullOrEmpty(entry.Key))
                    {
                        _keyIndexMap[entry.Key] = writeIndex;
                    }
                    writeIndex++;
                }
                else
                {
                    // 删除此元素
                    removedCount++;
                    _keyIndexMap.Remove(entry.Key);
                    _entryCache.Remove(entry.Key);  // 同时删除entry缓存
                }
            }
            
            // 移除末尾多余元素
            if (writeIndex < _list.Count)
            {
                _list.RemoveRange(writeIndex, _list.Count - writeIndex);
            }
            
            return removedCount;
        }

        /// <summary>
        /// 检查指定键是否存在
        /// 时间复杂度：O(1)
        /// </summary>
        /// <param name="key">要检查的键</param>
        /// <returns>如果键存在则返回true，否则返回false</returns>
        public bool HasValue(string key)
        {
            EnsureCache();
            return _entryCache.ContainsKey(key);  // 使用entry缓存查询
        }

        #endregion

        #region 序列化支持

        /// <summary>
        /// 获取内部列表
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
            _keyIndexMap = null;
            _entryCache = null;
        }

        #endregion
    }
}
