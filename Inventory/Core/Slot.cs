namespace EasyPack
{
    public class Slot : ISlot
    {
        public int Index { get; set; }

        public IItem Item { get; set; }

        public int ItemCount { get; set; }

        public bool IsOccupied { get; set; }

        public bool HasMultiSlotItem { get; set; } // 是否有多格物品

        public ItemCondition SlotCondition { get; set; }

        public bool AddItem(IItem item, int count = 1)
        {
            throw new System.NotImplementedException();
        }

        public bool CanAcceptItem(IItem item)
        {
            throw new System.NotImplementedException();
        }

        public bool RemoveItem(int count = 1)
        {
            throw new System.NotImplementedException();
        }
    }
}