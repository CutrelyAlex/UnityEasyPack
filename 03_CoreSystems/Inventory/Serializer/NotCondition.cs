using System;
using UnityEngine;

namespace EasyPack.InventorySystem
{

    /// <summary>
    /// 对单一子条件取反：子条件不成立时为真；如果 Inner 为 null，视为真。
    /// </summary>
    public sealed class NotCondition : IItemCondition, ISerializableCondition
    {
        public IItemCondition Inner { get; set; }

        public NotCondition() { }

        public NotCondition(IItemCondition inner)
        {
            Inner = inner;
        }

        /// <summary>
        /// 如果 Inner 为 null 则返回 true；否则返回 !Inner.IsCondition(item)。
        /// </summary>
        public bool CheckCondition(IItem item)
        {
            if (Inner == null) return true;
            return !Inner.CheckCondition(item);
        }

        public NotCondition Set(IItemCondition inner)
        {
            Inner = inner;
            return this;
        }

        public string Kind => "Not";

        // ISerializableCondition 实现
        public SerializedCondition ToDto()
        {
            var dto = new SerializedCondition { Kind = Kind };

            // 序列化内部条件
            if (Inner != null && Inner is ISerializableCondition serializableInner)
            {
                var innerDto = serializableInner.ToDto();
                if (innerDto != null)
                {
                    var innerEntry = new CustomDataEntry { Id = "Inner" };
                    innerEntry.SetValue(JsonUtility.ToJson(innerDto), CustomDataType.String);
                    dto.Params.Add(innerEntry);
                }
            }

            return dto;
        }

        public ISerializableCondition FromDto(SerializedCondition dto)
        {
            if (dto == null || dto.Params == null)
                return this;

            // 清空现有内部条件
            Inner = null;

            // 反序列化内部条件
            foreach (var p in dto.Params)
            {
                if (p?.Id == "Inner" && !string.IsNullOrEmpty(p.StringValue))
                {
                    try
                    {
                        var innerDto = JsonUtility.FromJson<SerializedCondition>(p.StringValue);
                        if (innerDto != null && !string.IsNullOrEmpty(innerDto.Kind))
                        {
                            var condType = ConditionTypeRegistry.GetConditionType(innerDto.Kind);
                            if (condType != null)
                            {
                                var condJson = JsonUtility.ToJson(innerDto);
                                Inner = SerializationServiceManager.DeserializeFromJson(condJson, condType) as IItemCondition;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[NotCondition] 反序列化内部条件失败: {ex.Message}");
                    }
                }
            }

            return this;
        }
    }
}
