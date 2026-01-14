using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyPack.CustomData
{
    /// <summary>
    ///     CustomData 集合类
    ///     实现 IList&lt;CustomDataEntry&gt;
    ///     接口内部维护字典缓存
    ///     建议使用此类作为集合，因为性能是O(1)的
    /// </summary>
    [Serializable]
    public class CustomDataCollection : IList<CustomDataEntry>, ISerializationCallbackReceiver
    {
        [SerializeField] private List<CustomDataEntry> _list = new();

        /// <summary>
        ///     缓存映射 key -> index
        /// </summary>
        [NonSerialized] private Dictionary<string, int> _keyIndexMap;

        /// <summary>
        ///     Entry对象缓存：key -> entry
        /// </summary>
        [NonSerialized] private Dictionary<string, CustomDataEntry> _entryCache;

        [NonSerialized] private bool _cacheDirty = true;

        public CustomDataCollection() { }
 
        public CustomDataCollection(int capacity) => _list = new(capacity);

        public CustomDataCollection(IEnumerable<CustomDataEntry> collection)
        {
            _list = new(collection);
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

        /// <summary>
        ///     通过键名获取或设置条目
        /// </summary>
        public CustomDataEntry this[string key]
        {
            get
            {
                EnsureCache();
                if (_keyIndexMap.TryGetValue(key, out int index))
                {
                    return _list[index];
                }
                throw new KeyNotFoundException($"键 '{key}' 不存在于 CustomDataCollection 中");
            }
            set
            {
                EnsureCache();
                if (_keyIndexMap.TryGetValue(key, out int index))
                {
                    _list[index] = value;
                    MarkDirty();
                }
                else
                {
                    throw new KeyNotFoundException($"键 '{key}' 不存在于 CustomDataCollection 中");
                }
            }
        }

        public int Count => _list.Count;

        public bool IsReadOnly => false;

        public override bool Equals(object obj)
        {
            if (obj is not CustomDataCollection other) return false;
            if (Count != other.Count) return false;

            for (int i = 0; i < _list.Count; i++)
            {
                CustomDataEntry entry = _list[i];
                if (!other.HasValue(entry.Key))
                {
                    return false;
                }
                else
                {
                    CustomDataEntry otherEntry = other[entry.Key];
                    if (!entry.Equals(otherEntry))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            return _list.Count;
        }

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
        }

        public bool Contains(CustomDataEntry item) => _list.Contains(item);

        public void CopyTo(CustomDataEntry[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public int IndexOf(CustomDataEntry item) => _list.IndexOf(item);

        public void Insert(int index, CustomDataEntry item)
        {
            _list.Insert(index, item);
            MarkDirty();
        }


        public bool Remove(CustomDataEntry item)
        {
            bool result = _list.Remove(item);
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

        public IEnumerator<CustomDataEntry> GetEnumerator() => _list.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();

        #endregion

        #region 扩展方法

        /// <summary>
        ///     添加多个项
        /// </summary>
        public void AddRange(IEnumerable<CustomDataEntry> collection)
        {
            _list.AddRange(collection);
            MarkDirty();
        }

        /// <summary>
        ///     在指定位置插入多个项
        /// </summary>
        public void InsertRange(int index, IEnumerable<CustomDataEntry> collection)
        {
            _list.InsertRange(index, collection);
            MarkDirty();
        }

        /// <summary>
        ///     移除多个项
        /// </summary>
        public void RemoveRange(int index, int count)
        {
            _list.RemoveRange(index, count);
            MarkDirty();
        }

        /// <summary>
        ///     移除满足条件的所有项
        /// </summary>
        public int RemoveAll(Predicate<CustomDataEntry> match)
        {
            int result = _list.RemoveAll(match);
            if (result > 0) MarkDirty();
            return result;
        }

        /// <summary>
        ///     排序
        /// </summary>
        public void Sort()
        {
            _list.Sort();
            MarkDirty();
        }

        /// <summary>
        ///     使用比较器排序
        /// </summary>
        public void Sort(IComparer<CustomDataEntry> comparer)
        {
            _list.Sort(comparer);
            MarkDirty();
        }

        /// <summary>
        ///     使用比较方法排序
        /// </summary>
        public void Sort(Comparison<CustomDataEntry> comparison)
        {
            _list.Sort(comparison);
            MarkDirty();
        }

        /// <summary>
        ///     反转
        /// </summary>
        public void Reverse()
        {
            _list.Reverse();
            MarkDirty();
        }

        /// <summary>
        ///     获取子列表
        /// </summary>
        public List<CustomDataEntry> GetRange(int index, int count) => _list.GetRange(index, count);

        /// <summary>
        ///     二分查找
        /// </summary>
        public int BinarySearch(CustomDataEntry item) => _list.BinarySearch(item);

        /// <summary>
        ///     查找第一个匹配项
        /// </summary>
        public CustomDataEntry Find(Predicate<CustomDataEntry> match) => _list.Find(match);

        /// <summary>
        ///     查找所有匹配项
        /// </summary>
        public List<CustomDataEntry> FindAll(Predicate<CustomDataEntry> match) => _list.FindAll(match);

        /// <summary>转换为字典</summary>
        public Dictionary<string, object> ToDictionary()
        {
            var dict = new Dictionary<string, object>();
            foreach (CustomDataEntry e in _list)
            {
                dict[e.Key] = e.GetValue();
            }
            return dict;
        }

        /// <summary>转换为泛型字典</summary>
        public Dictionary<string, T> ToDictionary<T>()
        {
            var dict = new Dictionary<string, T>();
            foreach (CustomDataEntry e in _list)
            {
                if (e.Key != null && TryGetValue(e.Key, out T value))
                {
                    dict[e.Key] = value;
                }
            }
            return dict;
        }

        #endregion

        #region 缓存管理

        /// <summary>
        ///     重建缓存索引映射和Entry对象缓存
        /// </summary>
        private void RebuildCache()
        {
            if (_keyIndexMap == null)
            {
                _keyIndexMap = new(_list.Count);
            }
            else
            {
                _keyIndexMap.Clear();
            }

            if (_entryCache == null)
            {
                _entryCache = new(_list.Count);
            }
            else
            {
                _entryCache.Clear();
            }

            // 构建 key -> index 和 key -> entry 映射
            for (int i = 0; i < _list.Count; i++)
            {
                CustomDataEntry entry = _list[i];
                if (!string.IsNullOrEmpty(entry.Key))
                {
                    _keyIndexMap[entry.Key] = i;
                    _entryCache[entry.Key] = entry;
                }
            }

            _cacheDirty = false;
        }

        /// <summary>
        ///     确保缓存索引已初始化
        /// </summary>
        private void EnsureCache()
        {
            if (_cacheDirty || _keyIndexMap == null) RebuildCache();
        }

        /// <summary>
        ///     标记缓存为脏
        /// </summary>
        private void MarkDirty()
        {
            _cacheDirty = true;
        }

        #endregion

        #region 查找方法

        /// <summary>
        ///     尝试获取指定键的值，如果存在则返回true并设置out参数
        ///     时间复杂度：O(1)
        /// </summary>
        /// <typeparam name="T">值的类型</typeparam>
        /// <param name="key">要查找的键</param>
        /// <param name="value">如果找到则设置为对应的值，否则为默认值</param>
        /// <returns>如果键存在则返回true，否则返回false</returns>
        public bool TryGetValue<T>(string key, out T value)
        {
            value = default;
            if (string.IsNullOrEmpty(key)) return false;

            EnsureCache();

            if (_entryCache.TryGetValue(key, out CustomDataEntry entry))
            {
                object obj = entry.GetValue();
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
                catch (SystemException)
                {
                    Debug.LogWarning("从Key转换到值失败");
                }
            }

            return false;
        }

        /// <summary>
        ///     获取指定键的值
        /// </summary>
        public T Get<T>(string key, T defaultValue = default)
        {
            if (key == null) return defaultValue;
            return TryGetValue(key, out T value) ? value : defaultValue;
        }

        /// <summary>
        ///     获取指定键的值
        /// </summary>
        public string GetToString(string key) => TryGetValue(key, out object value) ? value.ToString() : "null";

        /// <summary>
        ///     设置指定键的值
        /// </summary>
        public void Set(string key, object value)
        {
            EnsureCache();

            if (_keyIndexMap.TryGetValue(key, out int index))
            {
                // 存在：直接更新entry和缓存
                CustomDataEntry entry = _list[index];
                entry.SetValue(value);
                _entryCache[key] = entry;
            }
            else
            {
                // 不存在：添加新条目
                var newEntry = new CustomDataEntry { Key = key };
                newEntry.SetValue(value);
                int newIndex = _list.Count; // 新条目会被添加到末尾
                _list.Add(newEntry);
                _keyIndexMap[key] = newIndex; // 缓存索引
                _entryCache[key] = newEntry; // 缓存entry对象
            }
        }

        /// <summary>
        ///     移除指定键的值，如果存在则返回true
        ///     时间复杂度：O(1) 平均情况
        /// </summary>
        public bool Remove(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;

            EnsureCache();

            if (!_keyIndexMap.TryGetValue(key, out int indexToRemove)) return false;

            int lastIndex = _list.Count - 1;

            if (indexToRemove != lastIndex)
            {
                // 交换：将末尾元素移到删除位置
                CustomDataEntry lastEntry = _list[lastIndex];
                _list[indexToRemove] = lastEntry;

                // 更新被交换元素的索引缓存
                if (!string.IsNullOrEmpty(lastEntry.Key)) _keyIndexMap[lastEntry.Key] = indexToRemove;
            }

            // 移除末尾
            _list.RemoveAt(lastIndex);
            _keyIndexMap.Remove(key);
            _entryCache.Remove(key); // 同时删除entry缓存

            return true;
        }

        /// <summary>
        ///     批量删除指定键
        /// </summary>
        /// <param name="keys">要删除的键集合</param>
        /// <returns>实际删除的元素个数</returns>
        public int RemoveValues(IEnumerable<string> keys)
        {
            EnsureCache();

            var keysToRemove = new HashSet<string>(keys);
            int removedCount = 0;
            int writeIndex = 0; // 写指针

            for (int i = 0; i < _list.Count; i++)
            {
                CustomDataEntry entry = _list[i];
                if (!keysToRemove.Contains(entry.Key))
                {
                    // 保留此元素
                    if (writeIndex != i) _list[writeIndex] = _list[i];

                    // 更新索引缓存
                    if (!string.IsNullOrEmpty(entry.Key)) _keyIndexMap[entry.Key] = writeIndex;

                    writeIndex++;
                }
                else
                {
                    // 删除此元素
                    removedCount++;
                    _keyIndexMap.Remove(entry.Key);
                    _entryCache.Remove(entry.Key); // 同时删除entry缓存
                }
            }

            // 移除末尾多余元素
            if (writeIndex < _list.Count) _list.RemoveRange(writeIndex, _list.Count - writeIndex);

            return removedCount;
        }

        /// <summary>
        ///     检查指定键是否存在
        ///     时间复杂度：O(1)
        /// </summary>
        /// <param name="key">要检查的键</param>
        /// <returns>如果键存在则返回true，否则返回false</returns>
        public bool HasValue(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            EnsureCache();
            return _entryCache.ContainsKey(key); // 使用entry缓存查询
        }

        /// <summary>
        ///     批量获取多个值
        /// </summary>
        public Dictionary<string, T> GetValues<T>(IEnumerable<string> ids, T defaultValue = default)
        {
            var result = new Dictionary<string, T>();
            if (ids == null) return result;

            foreach (string id in ids)
            {
                result[id] = Get(id, defaultValue);
            }

            return result;
        }

        /// <summary>
        ///     获取第一个匹配条件的值
        /// </summary>
        public T GetFirstValue<T>(Func<string, T, bool> predicate, T defaultValue = default)
        {
            if (predicate == null) return defaultValue;

            foreach (CustomDataEntry entry in _list)
            {
                if (entry.Key == null) continue;
                if (TryGetValue(entry.Key, out T value) && predicate(entry.Key, value)) return value;
            }

            return defaultValue;
        }

        /// <summary>
        ///     批量设置多个自定义数据值
        /// </summary>
        public void SetValues(Dictionary<string, object> values)
        {
            if (values == null) return;

            foreach (var kv in values)
            {
                Set(kv.Key, kv.Value);
            }
        }

        /// <summary>
        ///     获取所有数据的键
        /// </summary>
        public IEnumerable<string> GetKeys()
        {
            return _list.Select(e => e.Key);
        }

        /// <summary>
        ///     获取指定类型的所有数据键
        /// </summary>
        public IEnumerable<string> GetKeysByType(CustomDataType type)
        {
            return _list.Where(e => e.Type == type).Select(e => e.Key);
        }

        /// <summary>
        ///     获取满足条件的所有数据条目
        /// </summary>
        public IEnumerable<CustomDataEntry> GetEntriesWhere(Func<CustomDataEntry, bool> predicate) =>
            predicate == null ? Enumerable.Empty<CustomDataEntry>() : _list.Where(predicate);

        /// <summary>
        ///     判断列表是否为空
        /// </summary>
        public bool IsEmpty => _list.Count == 0;

        /// <summary>
        ///     将另一个 CustomData 列表合并到当前列表（覆盖模式）
        /// </summary>
        public void Merge(CustomDataCollection other)
        {
            if (other == null) return;

            foreach (CustomDataEntry entry in other)
            {
                Set(entry.Key, entry.GetValue());
            }
        }

        /// <summary>
        ///     获取差异（返回在 other 中存在但在当前列表中不存在的键）
        /// </summary>
        public IEnumerable<string> GetDifference(CustomDataCollection other)
        {
            var thisKeys = new HashSet<string>(GetKeys());
            var otherKeys = new HashSet<string>(other?.GetKeys() ?? Enumerable.Empty<string>());
            otherKeys.ExceptWith(thisKeys);
            return otherKeys;
        }

        /// <summary>
        ///     深拷贝当前列表
        /// </summary>
        public CustomDataCollection Clone()
        {
            if (Count == 0)
            {
                return new();
            }

            var cloned = new CustomDataCollection();
            foreach (CustomDataEntry entry in _list)
            {
                var clonedEntry = new CustomDataEntry
                {
                    Key = entry.Key,
                    Type = entry.Type,
                    IntValue = entry.IntValue,
                    LongValue = entry.LongValue,
                    FloatValue = entry.FloatValue,
                    BoolValue = entry.BoolValue,
                    StringValue = entry.StringValue,
                    Vector2Value = entry.Vector2Value,
                    Vector3Value = entry.Vector3Value,
                    ColorValue = entry.ColorValue,
                    JsonValue = entry.JsonValue,
                    JsonClrType = entry.JsonClrType,
                    Serializer = entry.Serializer,
                };
                cloned.Add(clonedEntry);
            }

            return cloned;
        }
        #endregion

        #region 条件操作

        /// <summary>如果数据存在则执行操作</summary>
        public bool IfHasValue<T>(string id, Action<T> action)
        {
            if (TryGetValue(id, out T value))
            {
                action?.Invoke(value);
                return true;
            }

            return false;
        }

        /// <summary>如果数据存在则执行操作，否则执行默认操作</summary>
        public void IfElse<T>(string id, Action<T> onExists, Action onNotExists)
        {
            if (TryGetValue(id, out T value))
            {
                onExists?.Invoke(value);
            }
            else
            {
                onNotExists?.Invoke();
            }
        }

        #endregion

        #region 序列化支持

        /// <summary>
        ///     获取内部列表
        /// </summary>
        public List<CustomDataEntry> GetInternalList() => _list;

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