using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyPack.CustomData
{
    /// <summary>
    ///     CustomDataCollection 扩展方法
    ///     为 IEnumerable&lt;CustomDataEntry&gt; 提供便利方法
    /// </summary>
    public static class CustomDataCollectionExtensions
    {
        /// <summary>
        ///     尝试从可枚举集合获取值
        /// </summary>
        public static bool TryGetValue<T>(this IEnumerable<CustomDataEntry> entries, string id, out T value)
        {
            value = default;
            if (entries == null || string.IsNullOrEmpty(id)) return false;

            // 如果是 CustomDataCollection，委托给实例方法
            if (entries is CustomDataCollection collection) return collection.TryGetValue(id, out value);

            // 否则线性遍历
            foreach (CustomDataEntry e in entries)
            {
                if (e.Key != id) continue;

                object obj = e.GetValue();

                switch (obj)
                {
                    // 快路径：直接类型匹配
                    case T t:
                        value = t;
                        return true;
                    // 慢路径：JSON 反序列化
                    case string json:
                        try
                        {
                            value = JsonUtility.FromJson<T>(json);
                            return true;
                        }
                        catch
                        {
                            Debug.LogWarning("从CustomDataEntries 获取值失败");
                        }

                        break;
                }

                return false;
            }

            return false;
        }

        /// <summary>从可枚举集合获取值</summary>
        public static T GetValue<T>(this IEnumerable<CustomDataEntry> entries, string id, T defaultValue = default) =>
            entries.TryGetValue(id, out T value) ? value : defaultValue;

        /// <summary>转换为字典</summary>
        public static Dictionary<string, object> ToDictionary(this IEnumerable<CustomDataEntry> entries)
        {
            var dict = new Dictionary<string, object>();
            if (entries == null) return dict;

            foreach (CustomDataEntry e in entries)
            {
                dict[e.Key] = e.GetValue();
            }

            return dict;
        }

        /// <summary>转换为泛型字典</summary>
        public static Dictionary<string, T> ToDictionary<T>(this IEnumerable<CustomDataEntry> entries)
        {
            var dict = new Dictionary<string, T>();
            if (entries == null) return dict;

            var customDataEntries = entries as CustomDataEntry[] ?? entries.ToArray();
            foreach (CustomDataEntry e in customDataEntries)
            {
                if (e.Key != null && customDataEntries.TryGetValue(e.Key, out T value))
                    dict[e.Key] = value;
            }

            return dict;
        }

        /// <summary>获取所有的键</summary>
        public static IEnumerable<string> GetKeys(this IEnumerable<CustomDataEntry> entries)
        {
            return entries == null ? Enumerable.Empty<string>() : entries.Select(e => e.Key);
        }

        /// <summary>获取数据数量</summary>
        public static int Count(this IEnumerable<CustomDataEntry> entries) => entries?.Count() ?? 0;

        /// <summary>检查是否为空</summary>
        public static bool IsEmpty(this IEnumerable<CustomDataEntry> entries) => entries == null || !entries.Any();
    }
}