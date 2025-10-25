namespace EasyPack.InventorySystem
{
    public class ItemTypeCondition : IItemCondition, ISerializableCondition
    {
        public string ItemType { get; set; }

        public ItemTypeCondition() : this(string.Empty)
        {
        }

        public ItemTypeCondition(string itemType)
        {
            ItemType = itemType;
        }

        public void SetItemType(string itemType)
        {
            ItemType = itemType;
        }

        public bool CheckCondition(IItem item)
        {
            return item != null && item.Type == ItemType;
        }

        // 序列化支持
        public string Kind => "ItemType";

        public SerializedCondition ToDto()
        {
            var dto = new SerializedCondition { Kind = Kind };
            var entry = new CustomDataEntry { Id = "ItemType" };
            entry.SetValue(ItemType, CustomDataType.String);
            dto.Params.Add(entry);
            return dto;
        }
        public ISerializableCondition FromDto(SerializedCondition dto)
        {
            if (dto == null || dto.Params == null) return this;
            string t = null;
            foreach (var p in dto.Params)
            {
                if (p?.Id == "ItemType")
                {
                    t = p.StringValue ?? p.GetValue() as string;
                    break;
                }
            }
            if (!string.IsNullOrEmpty(t))
            {
                ItemType = t;
            }
            return this;
        }
    }
}
