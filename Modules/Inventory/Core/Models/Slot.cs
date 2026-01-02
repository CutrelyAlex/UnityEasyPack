namespace EasyPack.InventorySystem
{
    public interface ISlot
    {
        int Index { get; }
        IItem Item { get; }
        
        /// <summary>
        ///     当前槽位中物品的UID
        ///     -1 表示槽位为空或物品未分配UID
        /// </summary>
        long ItemUID { get; }
        
        int ItemCount { get; }
        bool IsOccupied { get; } // 是否被占用
        public Container Container { get; set; } // 所属容器
        CustomItemCondition SlotCondition { get; }
        bool CheckSlotCondition(IItem item);
        bool SetItem(IItem item, int count = 1);
        void ClearSlot();
    }

    public class Slot : ISlot
    {
        public int Index { get; set; }

        public IItem Item { get; set; }

        /// <summary>
        ///     当前槽位中物品的UID
        ///     通过Item.ItemUID同步，-1表示空槽位
        /// </summary>
        public long ItemUID => Item?.ItemUID ?? -1;

        public int ItemCount { get; set; }

        public bool IsOccupied { get; set; }

        public CustomItemCondition SlotCondition { get; set; }
        public Container Container { get; set; }

        /// <summary>
        ///     设置槽位物品
        /// </summary>
        /// <param name="item">要设置的物品</param>
        /// <param name="count">物品数量</param>
        /// <returns>设置是否成功</returns>
        public bool SetItem(IItem item, int count = 1)
        {
            // 物品不能为 null
            if (item == null)
            {
                return false;
            }

            // 验证槽位条件
            if (!CheckSlotCondition(item))
            {
                return false;
            }

            // 如果是相同物品，只更新数量
            if (IsOccupied && Item != null && Item.ID == item.ID)
            {
                ItemCount = count;
                return true;
            }

            // 设置新物品
            Item = item;
            ItemCount = count;
            IsOccupied = true;

            return true;
        }


        public int GetItemCount() => ItemCount;


        public bool CheckSlotCondition(IItem item) =>
            item != null
            && (SlotCondition == null || SlotCondition.CheckCondition(item));

        public void ClearSlot()
        {
            Item = null;
            ItemCount = 0;
            IsOccupied = false;
        }
    }
}