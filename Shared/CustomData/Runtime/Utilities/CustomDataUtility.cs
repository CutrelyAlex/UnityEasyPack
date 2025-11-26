using System.Collections.Generic;

namespace EasyPack.CustomData
{
    /// <summary>
    ///     CustomData 工具类
    /// </summary>
    public static class CustomDataUtility
    {
        #region 转换方法

        public static CustomDataCollection ToEntries(Dictionary<string, object> dict,
                                                     ICustomDataSerializer fallbackSerializer = null)
        {
            var list = new CustomDataCollection();
            if (dict == null) return list;

            foreach (var kv in dict)
            {
                var entry = new CustomDataEntry { Key = kv.Key, Serializer = fallbackSerializer };
                entry.SetValue(kv.Value);
                list.Add(entry);
            }

            return list;
        }

        public static Dictionary<string, object> ToDictionary(IEnumerable<CustomDataEntry> entries)
        {
            var dict = new Dictionary<string, object>();
            if (entries == null) return dict;

            foreach (CustomDataEntry e in entries)
            {
                dict[e.Key] = e.GetValue();
            }

            return dict;
        }

        #endregion

        #region 合并和更新

        /// <summary>将另一个 CustomData 列表合并到当前列表（覆盖模式）</summary>
        /// <param name="entries">目标列表</param>
        /// <param name="other">要合并的列表</param>
        public static void Merge(CustomDataCollection entries, CustomDataCollection other)
        {
            if (entries == null || other == null) return;

            foreach (CustomDataEntry entry in other)
            {
                entries.SetValue(entry.Key, entry.GetValue());
            }
        }

        /// <summary>获取差异（返回在 other 中存在但在 entries 中不存在的键）</summary>
        /// <param name="entries">当前列表</param>
        /// <param name="other">对比列表</param>
        /// <returns>差异键的集合</returns>
        public static IEnumerable<string> GetDifference(CustomDataCollection entries, CustomDataCollection other)
        {
            var entriesKeys = new HashSet<string>(entries.GetKeys());
            var otherKeys = new HashSet<string>(other.GetKeys());
            otherKeys.ExceptWith(entriesKeys);
            return otherKeys;
        }

        #endregion
    }
}