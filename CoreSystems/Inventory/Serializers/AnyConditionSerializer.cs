using System;
using UnityEngine;

namespace EasyPack
{
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
}
