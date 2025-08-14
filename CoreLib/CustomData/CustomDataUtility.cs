using System.Collections.Generic;
using UnityEngine;

namespace EasyPack
{
    public static class CustomDataUtility
    {
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
    }
}