using UnityEngine;

namespace EasyPack
{
    public interface ISlot
    {
        int Index { get; }
        IItem Item { get; }
        int ItemCount { get; }
        bool IsOccupied { get; } // 是否有物品
        public IContainer Container { get; set; } // 所属容器
        CustomItemCondition SlotCondition { get; }
        bool CheckSlotCondition(IItem item);
        bool SetItem(IItem item, int count = 1);
        void ClearSlot();

    }
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
            if (item == null)
            {
                return false;
            }

            if (IsOccupied && Item != null && item != null && Item.ID == item.ID)
            {
                ItemCount = count;
                return true;
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
            return item != null 
                && (SlotCondition == null || SlotCondition.IsCondition(item));
        }
        public void ClearSlot()
        {
            Item = null;
            ItemCount = 0;
            IsOccupied = false;
        }
    }
}