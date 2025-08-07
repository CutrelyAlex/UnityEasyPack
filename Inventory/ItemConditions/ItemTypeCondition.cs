namespace EasyPack
{
    public class ItemTypeCondition : IItemCondition
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
    }
}