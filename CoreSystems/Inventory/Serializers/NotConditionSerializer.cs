using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack
{

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
