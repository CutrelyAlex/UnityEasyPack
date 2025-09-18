namespace EasyPack
{
    public class ItemTypeCondition : IItemCondition, ISerializableCondition
    {
        public string ItemType { get; set; }

        public ItemTypeCondition(string itemType)
        {
            ItemType = itemType;
        }

        public void SetItemType(string itemType)
        {
            ItemType = itemType;
        }

        public bool IsCondition(IItem item)
        {
            return item != null && item.Type == ItemType;
        }

        // 序列化支持
        public string Kind => "ItemType";

        public ConditionDTO ToDto()
        {
            var dto = new ConditionDTO { Kind = Kind };
            var entry = new CustomDataEntry { Id = "ItemType" };
            entry.SetValue(ItemType, CustomDataType.String);
            dto.Params.Add(entry);
            return dto;
        }

        public static ItemTypeCondition FromDto(ConditionDTO dto)
        {
            if (dto == null || dto.Params == null) return null;
            string t = null;
            foreach (var p in dto.Params)
            {
                if (p?.Id == "ItemType")
                {
                    t = p.StringValue ?? p.GetValue() as string;
                    break;
                }
            }
            return string.IsNullOrEmpty(t) ? null : new ItemTypeCondition(t);
        }
    }
}