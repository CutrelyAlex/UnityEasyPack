using UnityEngine;

namespace EasyPack
{
    public class Slot : ISlot
    {
        public int Index { get; set; }

        public IItem Item { get; set; }

        public int ItemCount { get; set; }

        public bool IsOccupied { get; set; }

        public CustomItemCondition SlotCondition { get; set; }
        public IContainer Container { get; set; }

        public bool SetItem(IItem item, int count = 1)
        {

            if (IsOccupied && Item != null && item != null && Item.ID == item.ID)
            {
                ItemCount = count;
                return true;
            }
            else if(item == null)
            {
                return false;
            }

            // 设置物品基本信息
            Item = item;
            ItemCount = count;
            IsOccupied = true;

            return true;
        }


        public int GetItemCount()
        {
            return ItemCount;
        }


        public bool CheckSlotCondition(IItem item)
        {
            if (item == null)
                return false;

            if (SlotCondition != null && !SlotCondition.IsCondition(item))
                return false;


            return true;
        }
        public void ClearSlot()
        {
            Item = null;
            ItemCount = 0;
            IsOccupied = false;
        }
    }
}