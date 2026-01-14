using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack.CustomData
{
    /// <summary>
    ///     CustomData 纯列表类
    ///     继承自 List&lt;CustomDataEntry&gt;，专注于索引访问
    /// </summary>
    [Serializable]
    public class CustomDataList : List<CustomDataEntry>, ISerializationCallbackReceiver
    {
        public CustomDataList() : base() { }

        public CustomDataList(int capacity) : base(capacity) { }

        public CustomDataList(IEnumerable<CustomDataEntry> collection) : base(collection) { }

        #region 便捷的工厂方法

        /// <summary>
        ///     从值数组创建 CustomDataList
        /// </summary>
        public static CustomDataList FromValues(params object[] values)
        {
            var list = new CustomDataList(values.Length);
            foreach (var value in values)
            {
                var entry = new CustomDataEntry { Key = "" };
                entry.SetValue(value);
                list.Add(entry);
            }
            return list;
        }

        /// <summary>
        ///     从 Entry 数组创建 CustomDataList
        /// </summary>
        public static CustomDataList FromEntries(params CustomDataEntry[] entries)
        {
            return new CustomDataList(entries);
        }

        #endregion

        #region 值访问方法

        /// <summary>
        ///     获取指定索引的值
        /// </summary>
        public T Get<T>(int index, T defaultValue = default)
        {
            if (index < 0 || index >= Count) return defaultValue;
            
            var entry = this[index];
            var obj = entry.GetValue();
            
            if (obj is T t)
            {
                return t;
            }

            try
            {
                if (obj is string json && typeof(T) != typeof(string))
                {
                    return JsonUtility.FromJson<T>(json);
                }
            }
            catch (Exception)
            {
                Debug.LogWarning($"从索引 {index} 转换到值失败");
            }

            return defaultValue;
        }

        /// <summary>
        ///     设置指定索引的值
        /// </summary>
        public void Set(int index, object value)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            var entry = this[index];
            entry.SetValue(value);
        }

        /// <summary>
        ///     添加值到列表末尾
        /// </summary>
        public void AddValue(object value)
        {
            var entry = new CustomDataEntry { Key = "" };
            entry.SetValue(value);
            Add(entry);
        }

        /// <summary>
        ///     在指定位置插入值
        /// </summary>
        public void InsertValue(int index, object value)
        {
            var entry = new CustomDataEntry { Key = "" };
            entry.SetValue(value);
            Insert(index, entry);
        }

        #endregion

        #region 转换方法

        /// <summary>
        ///     转换为指定类型的列表
        /// </summary>
        public List<T> ToList<T>()
        {
            var result = new List<T>(Count);
            for (int i = 0; i < Count; i++)
            {
                result.Add(Get<T>(i));
            }
            return result;
        }

        /// <summary>
        ///     转换为对象数组
        /// </summary>
        public new object[] ToArray()
        {
            var result = new object[Count];
            for (int i = 0; i < Count; i++)
            {
                result[i] = this[i].GetValue();
            }
            return result;
        }

        #endregion

        #region 序列化支持

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            // Unity 会自动序列化基类 List<CustomDataEntry>
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // 反序列化后无需特殊处理
        }

        #endregion
    }
}
