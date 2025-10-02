using System;

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
}
