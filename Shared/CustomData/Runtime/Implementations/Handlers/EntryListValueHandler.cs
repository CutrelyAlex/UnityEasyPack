using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack.CustomData
{
    /// <summary>
    ///     CustomDataCollection 值处理器
    ///     处理 CustomDataCollection 和 CustomDataList 类型的序列化和反序列化
    /// </summary>
    public class EntryListValueHandler : IValueHandler
    {
        public CustomDataType SupportedType => CustomDataType.EntryList;

        public object GetValue(CustomDataEntry entry)
        {
            return entry.EntryListValue ?? new CustomDataCollection();
        }

        public void SetValue(CustomDataEntry entry, object value)
        {
            if (value == null)
            {
                entry.EntryListValue = new CustomDataCollection();
            }
            else if (value is IList<CustomDataEntry> listInterface)
            {
                entry.EntryListValue = listInterface;
            }
            else if (value is List<CustomDataEntry> list)
            {
                entry.EntryListValue = new CustomDataCollection(list);
            }
            else
            {
                Debug.LogWarning($"无法将 {value.GetType()} 类型的值设置为 CustomDataCollection");
                entry.EntryListValue = new CustomDataCollection();
            }

            entry.Type = CustomDataType.EntryList;
            ClearOtherValues(entry);
        }

        public bool TryDeserialize(CustomDataEntry entry, string data, Type jsonClrType = null)
        {
            try
            {
                entry.Type = CustomDataType.EntryList;
                
                if (string.IsNullOrEmpty(data))
                {
                    entry.EntryListValue = new CustomDataCollection();
                }
                else
                {
                    // 反序列化为列表
                    try
                    {
                        var listWrapper = JsonUtility.FromJson<EntryListWrapper>(data);
                        if (listWrapper?.items != null)
                        {
                            // 根据保存的类型创建正确的集合
                            if (listWrapper.collectionType == "CustomDataList")
                            {
                                entry.EntryListValue = new CustomDataList(listWrapper.items);
                            }
                            else
                            {
                                entry.EntryListValue = new CustomDataCollection(listWrapper.items);
                            }
                        }
                        else
                        {
                            entry.EntryListValue = new CustomDataCollection();
                        }
                    }
                    catch
                    {
                        // TODO: 向后兼容，之后删除
                        var oldWrapper = JsonUtility.FromJson<CustomDataCollectionWrapper>(data);
                        if (oldWrapper?.items != null)
                        {
                            entry.EntryListValue = new CustomDataCollection(oldWrapper.items);
                        }
                        else
                        {
                            entry.EntryListValue = new CustomDataCollection();
                        }
                    }
                }

                ClearOtherValues(entry);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"反序列化 EntryList 失败: {ex.Message}");
                entry.EntryListValue = new CustomDataCollection();
                return false;
            }
        }

        public string Serialize(CustomDataEntry entry)
        {
            // 确定集合类型
            string collectionType = entry.EntryListValue?.GetType().Name ?? "CustomDataCollection";
            
            if (entry.EntryListValue == null || entry.EntryListValue.Count == 0)
            {
                return JsonUtility.ToJson(new EntryListWrapper { 
                    items = new List<CustomDataEntry>(), 
                    collectionType = collectionType 
                });
            }

            var wrapper = new EntryListWrapper
            {
                items = new List<CustomDataEntry>(),
                collectionType = collectionType
            };

            // 支持 CustomDataCollection、CustomDataList 及其他 IList<CustomDataEntry> 实现
            for (int i = 0; i < entry.EntryListValue.Count; i++)
            {
                wrapper.items.Add(entry.EntryListValue[i]);
            }

            return JsonUtility.ToJson(wrapper);
        }

        public void Clear(CustomDataEntry entry)
        {
            entry.EntryListValue = null;
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
        ///     用于序列化的带类型信息的包装器类
        /// </summary>
        [Serializable]
        private class EntryListWrapper
        {
            public List<CustomDataEntry> items = new();
            public string collectionType = "CustomDataCollection";
        }

        /// <summary>
        ///     用于序列化的包装器类
        ///     TODO: 向后兼容，之后删除
        /// </summary>
        [Serializable]
        private class CustomDataCollectionWrapper
        {
            public List<CustomDataEntry> items = new();
        }
    }
}
