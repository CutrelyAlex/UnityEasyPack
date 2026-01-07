using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack.CustomData
{
    /// <summary>
    ///     CustomDataList 值处理器
    ///     处理 CustomDataCollection 类型的序列化和反序列化
    /// </summary>
    public class CustomDataListValueHandler : IValueHandler
    {
        public CustomDataType SupportedType => CustomDataType.CustomDataList;

        public object GetValue(CustomDataEntry entry)
        {
            return entry.CustomDataListValue ?? new CustomDataCollection();
        }

        public void SetValue(CustomDataEntry entry, object value)
        {
            if (value == null)
            {
                entry.CustomDataListValue = new CustomDataCollection();
            }
            else if (value is CustomDataCollection collection)
            {
                entry.CustomDataListValue = collection;
            }
            else if (value is List<CustomDataEntry> list)
            {
                entry.CustomDataListValue = new CustomDataCollection(list);
            }
            else
            {
                Debug.LogWarning($"无法将 {value.GetType()} 类型的值设置为 CustomDataList");
                entry.CustomDataListValue = new CustomDataCollection();
            }

            entry.Type = CustomDataType.CustomDataList;
            ClearOtherValues(entry);
        }

        public bool TryDeserialize(CustomDataEntry entry, string data, Type jsonClrType = null)
        {
            try
            {
                entry.Type = CustomDataType.CustomDataList;
                
                if (string.IsNullOrEmpty(data))
                {
                    entry.CustomDataListValue = new CustomDataCollection();
                }
                else
                {
                    // 反序列化为列表
                    var listWrapper = JsonUtility.FromJson<CustomDataListWrapper>(data);
                    if (listWrapper?.items != null)
                    {
                        entry.CustomDataListValue = new CustomDataCollection(listWrapper.items);
                    }
                    else
                    {
                        entry.CustomDataListValue = new CustomDataCollection();
                    }
                }

                ClearOtherValues(entry);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"反序列化 CustomDataList 失败: {ex.Message}");
                entry.CustomDataListValue = new CustomDataCollection();
                return false;
            }
        }

        public string Serialize(CustomDataEntry entry)
        {
            if (entry.CustomDataListValue == null || entry.CustomDataListValue.Count == 0)
            {
                return JsonUtility.ToJson(new CustomDataListWrapper { items = new List<CustomDataEntry>() });
            }

            var wrapper = new CustomDataListWrapper
            {
                items = new List<CustomDataEntry>()
            };

            for (int i = 0; i < entry.CustomDataListValue.Count; i++)
            {
                wrapper.items.Add(entry.CustomDataListValue[i]);
            }

            return JsonUtility.ToJson(wrapper);
        }

        public void Clear(CustomDataEntry entry)
        {
            entry.CustomDataListValue = null;
        }

        private static void ClearOtherValues(CustomDataEntry entry)
        {
            entry.IntValue = 0;
            entry.LongValue = 0;
            entry.FloatValue = 0;
            entry.BoolValue = false;
            entry.StringValue = null;
            entry.Vector2Value = default;
            entry.Vector3Value = default;
            entry.ColorValue = default;
            entry.JsonValue = null;
            entry.JsonClrType = null;
        }

        /// <summary>
        ///     用于序列化的包装器类
        /// </summary>
        [Serializable]
        private class CustomDataListWrapper
        {
            public List<CustomDataEntry> items = new();
        }
    }
}
