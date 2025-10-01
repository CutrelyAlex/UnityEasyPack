using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// ItemType条件序列化器
    /// </summary>
    public class ItemTypeConditionSerializer : IConditionSerializer
    {
        public string Kind => "ItemType";

        public bool CanHandle(IItemCondition condition)
        {
            return condition is ItemTypeCondition;
        }

        public SerializedCondition Serialize(IItemCondition condition)
        {
            if (!(condition is ItemTypeCondition itemTypeCondition))
                throw new ArgumentException("条件类型不匹配");

            var dto = new SerializedCondition { Kind = Kind };
            var entry = new CustomDataEntry { Id = "ItemType" };
            entry.SetValue(itemTypeCondition.ItemType, CustomDataType.String);
            dto.Params.Add(entry);
            return dto;
        }

        public IItemCondition Deserialize(SerializedCondition dto)
        {
            if (dto == null || dto.Params == null)
                return new ItemTypeCondition("");

            string itemType = null;
            foreach (var p in dto.Params)
            {
                if (p?.Id == "ItemType")
                {
                    itemType = p.StringValue ?? p.GetValue() as string;
                    break;
                }
            }

            return new ItemTypeCondition(itemType ?? "");
        }
    }

    /// <summary>
    /// Attribute条件序列化器
    /// </summary>
    public class AttributeConditionSerializer : IConditionSerializer
    {
        public string Kind => "Attr";

        public bool CanHandle(IItemCondition condition)
        {
            return condition is AttributeCondition;
        }

        public SerializedCondition Serialize(IItemCondition condition)
        {
            if (!(condition is AttributeCondition attrCondition))
                throw new ArgumentException("条件类型不匹配");

            var dto = new SerializedCondition { Kind = Kind };

            var name = new CustomDataEntry { Id = "Name" };
            name.SetValue(attrCondition.AttributeName, CustomDataType.String);

            var cmp = new CustomDataEntry { Id = "Cmp" };
            cmp.SetValue((int)attrCondition.ComparisonType, CustomDataType.Int);

            var val = new CustomDataEntry { Id = "Value" };
            val.SetValue(attrCondition.AttributeValue);

            dto.Params.Add(name);
            dto.Params.Add(cmp);
            dto.Params.Add(val);
            return dto;
        }

        public IItemCondition Deserialize(SerializedCondition dto)
        {
            if (dto == null || dto.Params == null)
                return new AttributeCondition("", null);

            string name = null;
            object value = null;
            int cmp = (int)AttributeComparisonType.Equal;

            foreach (var p in dto.Params)
            {
                if (p == null) continue;
                switch (p.Id)
                {
                    case "Name": name = p.StringValue ?? p.GetValue() as string; break;
                    case "Cmp": cmp = p.IntValue; break;
                    case "Value": value = p.GetValue(); break;
                }
            }

            return new AttributeCondition(name ?? "", value, (AttributeComparisonType)cmp);
        }
    }

    /// <summary>
    /// All条件序列化器（所有子条件都满足）
    /// </summary>
    public class AllConditionSerializer : IConditionSerializer
    {
        public string Kind => "All";

        public bool CanHandle(IItemCondition condition)
        {
            return condition is AllCondition;
        }

        public SerializedCondition Serialize(IItemCondition condition)
        {
            if (!(condition is AllCondition allCondition))
                throw new ArgumentException("条件类型不匹配");

            var dto = new SerializedCondition { Kind = Kind };
            
            // 序列化子条件列表 - 使用计数器存储每个子条件
            int childIndex = 0;
            foreach (var child in allCondition.Children)
            {
                if (child != null)
                {
                    var childDto = SerializationRegistry.SerializeCondition(child);
                    if (childDto != null)
                    {
                        // 每个子条件作为独立参数存储
                        var childEntry = new CustomDataEntry { Id = $"Child_{childIndex}" };
                        childEntry.SetValue(UnityEngine.JsonUtility.ToJson(childDto), CustomDataType.String);
                        dto.Params.Add(childEntry);
                        childIndex++;
                    }
                }
            }

            // 存储子条件数量
            var countEntry = new CustomDataEntry { Id = "ChildCount" };
            countEntry.SetValue(childIndex, CustomDataType.Int);
            dto.Params.Add(countEntry);

            return dto;
        }

        public IItemCondition Deserialize(SerializedCondition dto)
        {
            var allCondition = new AllCondition();
            
            if (dto == null || dto.Params == null)
                return allCondition;

            // 获取子条件数量
            int childCount = 0;
            foreach (var p in dto.Params)
            {
                if (p?.Id == "ChildCount")
                {
                    childCount = p.IntValue;
                    break;
                }
            }

            // 反序列化每个子条件
            for (int i = 0; i < childCount; i++)
            {
                string childId = $"Child_{i}";
                foreach (var p in dto.Params)
                {
                    if (p?.Id == childId)
                    {
                        var childJsonStr = p.StringValue ?? p.GetValue() as string;
                        if (!string.IsNullOrEmpty(childJsonStr))
                        {
                            try
                            {
                                var childDto = UnityEngine.JsonUtility.FromJson<SerializedCondition>(childJsonStr);
                                var childCondition = SerializationRegistry.DeserializeCondition(childDto);
                                if (childCondition != null)
                                {
                                    allCondition.Add(childCondition);
                                }
                            }
                            catch (Exception e)
                            {
                                Debug.LogWarning($"反序列化子条件失败 (索引{i}): {e.Message}");
                            }
                        }
                        break;
                    }
                }
            }

            return allCondition;
        }
    }

    /// <summary>
    /// Any条件序列化器（任意子条件满足）
    /// </summary>
    public class AnyConditionSerializer : IConditionSerializer
    {
        public string Kind => "Any";

        public bool CanHandle(IItemCondition condition)
        {
            return condition is AnyCondition;
        }

        public SerializedCondition Serialize(IItemCondition condition)
        {
            if (!(condition is AnyCondition anyCondition))
                throw new ArgumentException("条件类型不匹配");

            var dto = new SerializedCondition { Kind = Kind };
            
            // 序列化子条件列表 - 使用计数器存储每个子条件
            int childIndex = 0;
            foreach (var child in anyCondition.Children)
            {
                if (child != null)
                {
                    var childDto = SerializationRegistry.SerializeCondition(child);
                    if (childDto != null)
                    {
                        // 每个子条件作为独立参数存储
                        var childEntry = new CustomDataEntry { Id = $"Child_{childIndex}" };
                        childEntry.SetValue(UnityEngine.JsonUtility.ToJson(childDto), CustomDataType.String);
                        dto.Params.Add(childEntry);
                        childIndex++;
                    }
                }
            }

            // 存储子条件数量
            var countEntry = new CustomDataEntry { Id = "ChildCount" };
            countEntry.SetValue(childIndex, CustomDataType.Int);
            dto.Params.Add(countEntry);

            return dto;
        }

        public IItemCondition Deserialize(SerializedCondition dto)
        {
            var anyCondition = new AnyCondition();
            
            if (dto == null || dto.Params == null)
                return anyCondition;

            // 获取子条件数量
            int childCount = 0;
            foreach (var p in dto.Params)
            {
                if (p?.Id == "ChildCount")
                {
                    childCount = p.IntValue;
                    break;
                }
            }

            // 反序列化每个子条件
            for (int i = 0; i < childCount; i++)
            {
                string childId = $"Child_{i}";
                foreach (var p in dto.Params)
                {
                    if (p?.Id == childId)
                    {
                        var childJsonStr = p.StringValue ?? p.GetValue() as string;
                        if (!string.IsNullOrEmpty(childJsonStr))
                        {
                            try
                            {
                                var childDto = UnityEngine.JsonUtility.FromJson<SerializedCondition>(childJsonStr);
                                var childCondition = SerializationRegistry.DeserializeCondition(childDto);
                                if (childCondition != null)
                                {
                                    anyCondition.Add(childCondition);
                                }
                            }
                            catch (Exception e)
                            {
                                Debug.LogWarning($"反序列化子条件失败 (索引{i}): {e.Message}");
                            }
                        }
                        break;
                    }
                }
            }

            return anyCondition;
        }
    }

    /// <summary>
    /// Not条件序列化器（条件取反）
    /// </summary>
    public class NotConditionSerializer : IConditionSerializer
    {
        public string Kind => "Not";

        public bool CanHandle(IItemCondition condition)
        {
            return condition is NotCondition;
        }

        public SerializedCondition Serialize(IItemCondition condition)
        {
            if (!(condition is NotCondition notCondition))
                throw new ArgumentException("条件类型不匹配");

            var dto = new SerializedCondition { Kind = Kind };

            if (notCondition.Inner != null)
            {
                var innerDto = SerializationRegistry.SerializeCondition(notCondition.Inner);
                if (innerDto != null)
                {
                    var innerJson = UnityEngine.JsonUtility.ToJson(innerDto);
                    var innerEntry = new CustomDataEntry { Id = "Inner" };
                    innerEntry.SetValue(innerJson, CustomDataType.String);
                    dto.Params.Add(innerEntry);
                }
            }

            return dto;
        }

        public IItemCondition Deserialize(SerializedCondition dto)
        {
            var notCondition = new NotCondition();
            
            if (dto == null || dto.Params == null)
                return notCondition;

            foreach (var p in dto.Params)
            {
                if (p?.Id == "Inner")
                {
                    var innerJsonStr = p.StringValue ?? p.GetValue() as string;
                    if (!string.IsNullOrEmpty(innerJsonStr))
                    {
                        try
                        {
                            var innerDto = UnityEngine.JsonUtility.FromJson<SerializedCondition>(innerJsonStr);
                            notCondition.Inner = SerializationRegistry.DeserializeCondition(innerDto);
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning($"反序列化内层条件失败: {e.Message}");
                        }
                    }
                    break;
                }
            }

            return notCondition;
        }
    }
}
