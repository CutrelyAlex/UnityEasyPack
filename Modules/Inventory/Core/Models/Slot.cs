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
        ///     槽位中物品的数量（代理到 Item.Count）
        /// </summary>
        int ItemCount { get; set; }
        
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
        /// <remarks>
        ///     设置 ItemCount 会：
        ///     1. 修改 Item.Count
        ///     2. 触发 Container.OnSlotQuantityChanged 事件
        ///     3. 触发 Container.TriggerItemTotalCountChanged 更新全局计数
        ///     4. 更新容器缓存
        /// </remarks>
        public int ItemCount
        {
            get => Item?.Count ?? 0;
            set
            {
                if (Item != null)
                {
                    int oldCount = Item.Count;
                    if (oldCount != value)
                    {
                        Item.Count = value;
                        
                        // 触发容器的槽位数量变更事件
                        Container?.OnSlotQuantityChanged(Index, Item, oldCount, value);
                        
                        // 触发物品总数变更事件
                        Container?.TriggerItemTotalCountChanged(Item.ID, Item);
                    }
                }
            }
        }

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
        ///     3. 触发容器的槽位数量变更事件（从旧数量到新数量）
        ///     4. 更新物品总数缓存
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

            // 记录旧物品信息（用于触发事件）
            var oldItem = Item;
            int oldCount = oldItem?.Count ?? 0;
            string oldItemId = oldItem?.ID;

            // 设置物品
            Item = item;
            IsOccupied = true;

            // 通知容器槽位内容已变更
            if (Container != null)
            {
                int newCount = item.Count;
                
                // 触发槽位数量变更事件
                Container.OnSlotQuantityChanged(Index, item, oldCount, newCount);
                
                // 如果更换了不同的物品，需要更新两个物品的总数
                if (oldItemId != null && oldItemId != item.ID)
                {
                    // 旧物品总数减少
                    Container.TriggerItemTotalCountChanged(oldItemId, null);
                }
                
                // 新物品总数变更
                Container.TriggerItemTotalCountChanged(item.ID, item);
            }

            return true;
        }

        /// <summary>
        ///     设置槽位物品并指定数量
        /// </summary>
        /// <param name="item">要设置的物品</param>
        /// <param name="count">物品数量</param>
        /// <returns>设置是否成功</returns>
        /// <marks>等同于 item.Count = count; SetItem(item);</marks>
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
        ///     清空槽位（移除物品）
        /// </summary>
        /// <remarks>
        ///     此方法会：
        ///     1. 清空物品引用和占用状态
        ///     2. 触发容器的槽位数量变更事件（数量变为 0）
        ///     3. 更新物品总数缓存
        /// </remarks>
        public void ClearSlot()
        {
            if (Item != null && Container != null)
            {
                int oldCount = Item.Count;
                string itemId = Item.ID;
                
                // 清空前触发事件
                Container.OnSlotQuantityChanged(Index, Item, oldCount, 0);
                
                // 清空槽位
                Item = null;
                IsOccupied = false;
                
                // 更新物品总数
                Container.TriggerItemTotalCountChanged(itemId, null);
            }
            else
            {
                // 没有容器或已经为空，直接清空
                Item = null;
                IsOccupied = false;
            }
        }
    }
}