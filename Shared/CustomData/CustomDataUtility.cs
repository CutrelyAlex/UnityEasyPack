using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyPack.CustomData
{
    /// <summary>
    /// CustomData 工具类
    /// </summary>
    public static class CustomDataUtility
    {
        #region 转换方法

        public static List<CustomDataEntry> ToEntries(Dictionary<string, object> dict, ICustomDataSerializer fallbackSerializer = null)
        {
            var list = new List<CustomDataEntry>();
            if (dict == null) return list;

            foreach (var kv in dict)
            {
                var entry = new CustomDataEntry { Id = kv.Key, Serializer = fallbackSerializer };
                entry.SetValue(kv.Value);
                list.Add(entry);
            }
            return list;
        }

        public static Dictionary<string, object> ToDictionary(IEnumerable<CustomDataEntry> entries)
        {
            var dict = new Dictionary<string, object>();
            if (entries == null) return dict;

            foreach (var e in entries)
            {
                dict[e.Id] = e.GetValue();
            }
            return dict;
        }

        #endregion

        #region 获取值

        /// <summary>尝试获取自定义数据值</summary>
        /// <typeparam name="T">期望的值类型</typeparam>
        /// <param name="entries">CustomData 列表</param>
        /// <param name="id">数据键</param>
        /// <param name="value">输出值</param>
        /// <returns>如果找到并成功转换返回 true，否则返回 false</returns>
        public static bool TryGetValue<T>(IEnumerable<CustomDataEntry> entries, string id, out T value)
        {
            value = default;
            if (entries == null) return false;

            foreach (var e in entries)
            {
                if (e.Id != id) continue;

                var obj = e.GetValue();
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

                return false;
            }

            return false;
        }

        /// <summary>获取自定义数据值，如果不存在则返回默认值</summary>
        /// <typeparam name="T">期望的值类型</typeparam>
        /// <param name="entries">CustomData 列表</param>
        /// <param name="id">数据键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>找到的值或默认值</returns>
        public static T GetValue<T>(List<CustomDataEntry> entries, string id, T defaultValue = default)
        {
            if (TryGetValue(entries, id, out T value))
                return value;
            return defaultValue;
        }

        #endregion

        #region 设置值

        /// <summary>设置自定义数据值（如果已存在则更新，不存在则添加）</summary>
        /// <param name="entries">CustomData 列表</param>
        /// <param name="id">数据键</param>
        /// <param name="value">数据值</param>
        public static void SetValue(List<CustomDataEntry> entries, string id, object value)
        {
            if (entries == null)
                throw new System.ArgumentNullException(nameof(entries));

            // 查找现有条目
            var existing = entries.FirstOrDefault(e => e.Id == id);
            if (existing != null)
            {
                existing.SetValue(value);
            }
            else
            {
                var entry = new CustomDataEntry { Id = id };
                entry.SetValue(value);
                entries.Add(entry);
            }
        }

        #endregion

        #region 移除和检查

        /// <summary>移除自定义数据</summary>
        /// <param name="entries">CustomData 列表</param>
        /// <param name="id">数据键</param>
        /// <returns>如果移除成功返回 true，否则返回 false</returns>
        public static bool RemoveValue(List<CustomDataEntry> entries, string id)
        {
            if (entries == null) return false;
            return entries.RemoveAll(e => e.Id == id) > 0;
        }

        /// <summary>检查是否存在指定的自定义数据</summary>
        /// <param name="entries">CustomData 列表</param>
        /// <param name="id">数据键</param>
        /// <returns>如果存在返回 true，否则返回 false</returns>
        public static bool HasValue(List<CustomDataEntry> entries, string id)
        {
            return entries != null && entries.Any(e => e.Id == id);
        }

        /// <summary>清空所有自定义数据</summary>
        /// <param name="entries">CustomData 列表</param>
        public static void ClearAll(List<CustomDataEntry> entries)
        {
            entries?.Clear();
        }

        /// <summary>获取所有数据的键</summary>
        /// <param name="entries">CustomData 列表</param>
        /// <returns>键的集合</returns>
        public static IEnumerable<string> GetKeys(List<CustomDataEntry> entries)
        {
            if (entries == null) return Enumerable.Empty<string>();
            return entries.Select(e => e.Id);
        }

        #endregion

        #region 批量操作

        /// <summary>批量设置多个自定义数据</summary>
        /// <param name="entries">CustomData 列表</param>
        /// <param name="values">要设置的键值对</param>
        public static void SetValues(List<CustomDataEntry> entries, Dictionary<string, object> values)
        {
            if (entries == null || values == null) return;

            foreach (var kv in values)
            {
                SetValue(entries, kv.Key, kv.Value);
            }
        }

        /// <summary>深拷贝 CustomData 列表</summary>
        /// <param name="source">源列表</param>
        /// <returns>拷贝后的新列表</returns>
        public static List<CustomDataEntry> Clone(List<CustomDataEntry> source)
        {
            if (source == null || source.Count == 0)
                return new List<CustomDataEntry>();

            var cloned = new List<CustomDataEntry>();
            foreach (var entry in source)
            {
                var clonedEntry = new CustomDataEntry
                {
                    Id = entry.Id,
                    Type = entry.Type,
                    IntValue = entry.IntValue,
                    FloatValue = entry.FloatValue,
                    BoolValue = entry.BoolValue,
                    StringValue = entry.StringValue,
                    Vector2Value = entry.Vector2Value,
                    Vector3Value = entry.Vector3Value,
                    ColorValue = entry.ColorValue,
                    JsonValue = entry.JsonValue,
                    JsonClrType = entry.JsonClrType,
                    Serializer = entry.Serializer
                };
                cloned.Add(clonedEntry);
            }
            return cloned;
        }

        #endregion

        #region 查询和过滤

        /// <summary>获取指定类型的所有数据</summary>
        /// <param name="entries">CustomData 列表</param>
        /// <param name="type">数据类型</param>
        /// <returns>符合类型的数据 ID 集合</returns>
        public static IEnumerable<string> GetValuesByType(List<CustomDataEntry> entries, CustomDataType type)
        {
            if (entries == null) return Enumerable.Empty<string>();
            return entries.Where(e => e.Type == type).Select(e => e.Id);
        }

        /// <summary>获取满足条件的所有数据</summary>
        /// <param name="entries">CustomData 列表</param>
        /// <param name="predicate">过滤条件</param>
        /// <returns>符合条件的数据条目</returns>
        public static IEnumerable<CustomDataEntry> GetEntriesWhere(List<CustomDataEntry> entries, System.Func<CustomDataEntry, bool> predicate)
        {
            if (entries == null) return Enumerable.Empty<CustomDataEntry>();
            return entries.Where(predicate);
        }

        /// <summary>获取数据的数量</summary>
        /// <param name="entries">CustomData 列表</param>
        /// <returns>数据个数</returns>
        public static int Count(List<CustomDataEntry> entries)
        {
            return entries?.Count ?? 0;
        }

        /// <summary>判断列表是否为空</summary>
        /// <param name="entries">CustomData 列表</param>
        /// <returns>如果为空返回 true，否则返回 false</returns>
        public static bool IsEmpty(List<CustomDataEntry> entries)
        {
            return entries == null || entries.Count == 0;
        }

        #endregion

        #region 增量操作（数值类型）

        /// <summary>增加数值类型的数据值</summary>
        /// <param name="entries">CustomData 列表</param>
        /// <param name="id">数据键</param>
        /// <param name="delta">增加量</param>
        /// <returns>增加后的新值，如果数据不存在或类型不匹配返回 delta</returns>
        public static int AddInt(List<CustomDataEntry> entries, string id, int delta = 1)
        {
            if (entries == null) return delta;
            int current = GetValue(entries, id, 0);
            int newValue = current + delta;
            SetValue(entries, id, newValue);
            return newValue;
        }

        /// <summary>增加浮点数类型的数据值</summary>
        /// <param name="entries">CustomData 列表</param>
        /// <param name="id">数据键</param>
        /// <param name="delta">增加量</param>
        /// <returns>增加后的新值，如果数据不存在或类型不匹配返回 delta</returns>
        public static float AddFloat(List<CustomDataEntry> entries, string id, float delta = 1f)
        {
            if (entries == null) return delta;
            float current = GetValue(entries, id, 0f);
            float newValue = current + delta;
            SetValue(entries, id, newValue);
            return newValue;
        }

        #endregion

        #region 快捷方法 - 基础类型

        /// <summary>快速设置 int 值</summary>
        public static void SetInt(List<CustomDataEntry> entries, string id, int value) => SetValue(entries, id, value);

        /// <summary>快速设置 float 值</summary>
        public static void SetFloat(List<CustomDataEntry> entries, string id, float value) => SetValue(entries, id, value);

        /// <summary>快速设置 bool 值</summary>
        public static void SetBool(List<CustomDataEntry> entries, string id, bool value) => SetValue(entries, id, value);

        /// <summary>快速设置 string 值</summary>
        public static void SetString(List<CustomDataEntry> entries, string id, string value) => SetValue(entries, id, value);

        /// <summary>快速设置 Vector2 值</summary>
        public static void SetVector2(List<CustomDataEntry> entries, string id, Vector2 value) => SetValue(entries, id, value);

        /// <summary>快速设置 Vector3 值</summary>
        public static void SetVector3(List<CustomDataEntry> entries, string id, Vector3 value) => SetValue(entries, id, value);

        /// <summary>快速设置 Color 值</summary>
        public static void SetColor(List<CustomDataEntry> entries, string id, Color value) => SetValue(entries, id, value);

        /// <summary>快速获取 int 值</summary>
        public static int GetInt(List<CustomDataEntry> entries, string id, int defaultValue = 0) => GetValue(entries, id, defaultValue);

        /// <summary>快速获取 float 值</summary>
        public static float GetFloat(List<CustomDataEntry> entries, string id, float defaultValue = 0f) => GetValue(entries, id, defaultValue);

        /// <summary>快速获取 bool 值</summary>
        public static bool GetBool(List<CustomDataEntry> entries, string id, bool defaultValue = false) => GetValue(entries, id, defaultValue);

        /// <summary>快速获取 string 值</summary>
        public static string GetString(List<CustomDataEntry> entries, string id, string defaultValue = "") => GetValue(entries, id, defaultValue);

        /// <summary>快速获取 Vector2 值</summary>
        public static Vector2 GetVector2(List<CustomDataEntry> entries, string id, Vector2? defaultValue = null) => GetValue(entries, id, defaultValue ?? Vector2.zero);

        /// <summary>快速获取 Vector3 值</summary>
        public static Vector3 GetVector3(List<CustomDataEntry> entries, string id, Vector3? defaultValue = null) => GetValue(entries, id, defaultValue ?? Vector3.zero);

        /// <summary>快速获取 Color 值</summary>
        public static Color GetColor(List<CustomDataEntry> entries, string id, Color? defaultValue = null) => GetValue(entries, id, defaultValue ?? Color.white);

        #endregion

        #region 条件操作

        /// <summary>如果数据存在则执行操作</summary>
        /// <typeparam name="T">数据值类型</typeparam>
        /// <param name="entries">CustomData 列表</param>
        /// <param name="id">数据键</param>
        /// <param name="action">执行的操作</param>
        /// <returns>如果数据存在并执行成功返回 true，否则返回 false</returns>
        public static bool IfHasValue<T>(List<CustomDataEntry> entries, string id, System.Action<T> action)
        {
            if (TryGetValue(entries, id, out T value))
            {
                action?.Invoke(value);
                return true;
            }
            return false;
        }

        /// <summary>如果数据存在则执行操作，否则执行默认操作</summary>
        /// <typeparam name="T">数据值类型</typeparam>
        /// <param name="entries">CustomData 列表</param>
        /// <param name="id">数据键</param>
        /// <param name="onExists">数据存在时执行的操作</param>
        /// <param name="onNotExists">数据不存在时执行的操作</param>
        public static void IfElse<T>(List<CustomDataEntry> entries, string id, System.Action<T> onExists, System.Action onNotExists)
        {
            if (TryGetValue(entries, id, out T value))
                onExists?.Invoke(value);
            else
                onNotExists?.Invoke();
        }

        #endregion

        #region 合并和更新

        /// <summary>将另一个 CustomData 列表合并到当前列表（覆盖模式）</summary>
        /// <param name="entries">目标列表</param>
        /// <param name="other">要合并的列表</param>
        public static void Merge(List<CustomDataEntry> entries, List<CustomDataEntry> other)
        {
            if (entries == null || other == null) return;

            foreach (var entry in other)
            {
                SetValue(entries, entry.Id, entry.GetValue());
            }
        }

        /// <summary>获取差异（返回在 other 中存在但在 entries 中不存在的键）</summary>
        /// <param name="entries">当前列表</param>
        /// <param name="other">对比列表</param>
        /// <returns>差异键的集合</returns>
        public static IEnumerable<string> GetDifference(List<CustomDataEntry> entries, List<CustomDataEntry> other)
        {
            var entriesKeys = new HashSet<string>(GetKeys(entries));
            var otherKeys = new HashSet<string>(GetKeys(other));
            otherKeys.ExceptWith(entriesKeys);
            return otherKeys;
        }

        #endregion
    }
}