using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack.CustomData
{
    /// <summary>
    ///     CustomDataCollection 值处理器
    ///     处理 CustomDataCollection 类型的序列化和反序列化
    /// </summary>
    public class CustomDataCollectionValueHandler : IValueHandler
    {
        public CustomDataType SupportedType => CustomDataType.CustomDataCollection;

        public object GetValue(CustomDataEntry entry)
        {
            return entry.CustomDataCollectionValue ?? new CustomDataCollection();
        }

        public void SetValue(CustomDataEntry entry, object value)
        {
            if (value == null)
            {
                entry.CustomDataCollectionValue = new CustomDataCollection();
            }
            else if (value is CustomDataCollection collection)
            {
                entry.CustomDataCollectionValue = collection;
            }
            else if (value is List<CustomDataEntry> list)
            {
                entry.CustomDataCollectionValue = new CustomDataCollection(list);
            }
            else
            {
                Debug.LogWarning($"无法将 {value.GetType()} 类型的值设置为 CustomDataCollection");
                entry.CustomDataCollectionValue = new CustomDataCollection();
            }

            entry.Type = CustomDataType.CustomDataCollection;
            ClearOtherValues(entry);
        }

        public bool TryDeserialize(CustomDataEntry entry, string data, Type jsonClrType = null)
        {
            try
            {
                entry.Type = CustomDataType.CustomDataCollection;
                
                if (string.IsNullOrEmpty(data))
                {
                    entry.CustomDataCollectionValue = new CustomDataCollection();
                }
                else
                {
                    // 反序列化为列表
                    var listWrapper = JsonUtility.FromJson<CustomDataCollectionWrapper>(data);
                    if (listWrapper?.items != null)
                    {
                        entry.CustomDataCollectionValue = new CustomDataCollection(listWrapper.items);
                    }
                    else
                    {
                        entry.CustomDataCollectionValue = new CustomDataCollection();
                    }
                }

                ClearOtherValues(entry);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"反序列化 CustomDataCollection 失败: {ex.Message}");
                entry.CustomDataCollectionValue = new CustomDataCollection();
                return false;
            }
        }

        public string Serialize(CustomDataEntry entry)
        {
            if (entry.CustomDataCollectionValue == null || entry.CustomDataCollectionValue.Count == 0)
            {
                return JsonUtility.ToJson(new CustomDataCollectionWrapper { items = new List<CustomDataEntry>() });
            }

            var wrapper = new CustomDataCollectionWrapper
            {
                items = new List<CustomDataEntry>()
            };

            for (int i = 0; i < entry.CustomDataCollectionValue.Count; i++)
            {
                wrapper.items.Add(entry.CustomDataCollectionValue[i]);
            }

            return JsonUtility.ToJson(wrapper);
        }

        public void Clear(CustomDataEntry entry)
        {
            entry.CustomDataCollectionValue = null;
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
        private class CustomDataCollectionWrapper
        {
            public List<CustomDataEntry> items = new();
        }
    }
}
