using System;

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
        
        /// <summary>
        ///     槽位中物品的数量
        /// </summary>
        int ItemCount { get; }
        
        bool IsOccupied { get; } // 是否被占用
        public Container Container { get; set; } // 所属容器
        CustomItemCondition SlotCondition { get; }
        bool CheckSlotCondition(IItem item);
        
        bool SetItem(IItem item);
    
        bool SetItem(IItem item, int count);
        
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

        /// <summary>
        ///     槽位中物品的数量
        /// </summary>

        public int ItemCount => Item?.Count ?? 0;

        public bool IsOccupied { get; set; }

        public CustomItemCondition SlotCondition { get; set; }
        public Container Container { get; set; }

        /// <summary>
        ///     设置槽位物品
        /// </summary>
        /// <param name="item">要设置的物品</param>
        /// <returns>设置是否成功</returns>
        /// <remarks>
        ///     此方法会：
        ///     1. 验证槽位条件
        ///     2. 设置物品引用和占用状态
        ///     3. 不会触发任何事件
        /// </remarks>
        public bool SetItem(IItem item)
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

            // 设置物品
            Item = item;
            IsOccupied = true;

            return true;
        }

        /// <summary>
        ///     设置槽位物品并指定数量 不触发事件
        /// </summary>
        /// <param name="item">要设置的物品</param>
        /// <param name="count">物品数量</param>
        /// <returns>设置是否成功</returns>
        public bool SetItem(IItem item, int count)
        {
            if (item == null)
            {
                return false;
            }
            
            // 设置物品数量
            item.Count = count;
            
            return SetItem(item);
        }

        public bool CheckSlotCondition(IItem item) =>
            item != null
            && (SlotCondition == null || SlotCondition.CheckCondition(item));

        /// <summary>
        ///     清空槽位（不触发事件）
        /// </summary>

        public void ClearSlot()
        {
            Item = null;
            IsOccupied = false;
        }
    }
}