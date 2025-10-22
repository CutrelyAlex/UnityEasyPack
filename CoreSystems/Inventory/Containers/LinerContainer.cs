using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyPack
{
    public class LinerContainer : Container
    {
        public override bool IsGrid => false;
        public override Vector2 Grid => new(-1, -1);

        /// <summary>
        /// 创建一个线性容器
        /// </summary>
        /// <param name="id">容器ID</param>
        /// <param name="name">容器名称</param>
        /// <param name="type">容器类型</param>
        /// <param name="capacity">容量，-1表示无限</param>
        public LinerContainer(string id, string name, string type, int capacity = -1)
            : base(id, name, type, capacity)
        {
            InitializeSlots(capacity);
            RebuildCaches();
        }

        #region 容器操作
        /// <summary>
        /// 物品移动处理
        /// </summary>
        /// <param name="sourceSlotIndex">源槽位索引</param>
        /// <param name="targetContainer">目标容器</param>
        /// <returns>移动结果</returns>
        public bool MoveItemToContainer(int sourceSlotIndex, Container targetContainer)
        {
            if (!ValidateSourceSlot(sourceSlotIndex, out var sourceSlot, out var sourceItem, out var sourceCount))
                return false;

            if (!ValidateTargetContainer(targetContainer, sourceItem))
                return false;

            return ExecuteItemMove(sourceSlot, sourceSlotIndex, sourceItem, sourceCount, targetContainer);
        }

        /// <summary>
        /// 整理容器
        /// </summary>
        public void SortInventory()
        {
            var occupiedSlots = CollectOccupiedSlots();

            occupiedSlots.Sort((a, b) =>
            {
                int typeCompare = string.Compare(a.item.Type, b.item.Type, StringComparison.Ordinal);

                return typeCompare != 0
                ? typeCompare
                : string.Compare(a.item.Name, b.item.Name, StringComparison.Ordinal);
            });

            ExecuteInventoryOperationSafely(() =>
            {
                ClearAllSlotsInternal();
                FillSlotsWithSortedItems(occupiedSlots);
            });
        }

        /// <summary>
        /// 合并相同物品到较少的槽位中
        /// </summary>
        public void ConsolidateItems()
        {
            ExecuteInventoryOperationSafely(() =>
            {
                Dictionary<string, List<(int slotIndex, IItem item, int count)>>
                    itemGroups = GroupStackableItemsByID();

                ConsolidateItemGroups(itemGroups);
            });
        }

        /// <summary>
        /// 整理容器（排序 + 合并）
        /// </summary>
        public void OrganizeInventory()
        {
            ConsolidateItems();
            SortInventory();
        }
        #endregion

        #region 辅助方法

        private void InitializeSlots(int capacity)
        {
            if (capacity <= 0) return;

            for (int i = 0; i < capacity; i++)
            {
                _slots.Add(new Slot { Index = i, Container = this });
            }
        }

        private bool ValidateSourceSlot(int sourceSlotIndex, out ISlot sourceSlot, out IItem sourceItem, out int sourceCount)
        {
            sourceSlot = null;
            sourceItem = null;
            sourceCount = 0;

            if (sourceSlotIndex < 0 || sourceSlotIndex >= _slots.Count)
                return false;

            sourceSlot = _slots[sourceSlotIndex];
            if (!sourceSlot.IsOccupied || sourceSlot.Item == null)
                return false;

            sourceItem = sourceSlot.Item;
            sourceCount = sourceSlot.ItemCount;
            return true;
        }

        private bool ValidateTargetContainer(Container targetContainer, IItem sourceItem)
        {
            return !(targetContainer is Container targetContainerImpl) ||
                   targetContainerImpl.ValidateItemCondition(sourceItem);
        }

        private bool ExecuteItemMove(ISlot sourceSlot, int sourceSlotIndex, IItem sourceItem,
                                    int sourceCount, Container targetContainer)
        {
            var (result, addedCount) = targetContainer.AddItems(sourceItem, sourceCount);

            if (result != AddItemResult.Success || addedCount <= 0)
                return false;

            UpdateSourceSlotAfterMove(sourceSlot, sourceSlotIndex, sourceItem, sourceCount, addedCount);
            return true;
        }

        private void UpdateSourceSlotAfterMove(ISlot sourceSlot, int sourceSlotIndex, IItem sourceItem,
                                              int sourceCount, int addedCount)
        {
            if (addedCount == sourceCount)
            {
                HandleCompleteMove(sourceSlot, sourceSlotIndex, sourceItem, sourceCount);
            }
            else
            {
                HandlePartialMove(sourceSlot, sourceSlotIndex, sourceItem, sourceCount, addedCount);
            }
        }

        private void HandleCompleteMove(ISlot sourceSlot, int sourceSlotIndex, IItem sourceItem, int sourceCount)
        {
            sourceSlot.ClearSlot();
            UpdateCacheAfterMove(sourceSlotIndex, sourceItem, sourceCount, true);
            TriggerItemTotalCountChanged(sourceItem.ID);
        }

        private void HandlePartialMove(ISlot sourceSlot, int sourceSlotIndex, IItem sourceItem,
                                      int sourceCount, int addedCount)
        {
            int remainingCount = sourceCount - addedCount;
            sourceSlot.SetItem(sourceItem, remainingCount);
            UpdateCacheAfterMove(sourceSlotIndex, sourceItem, addedCount, false);
            TriggerItemTotalCountChanged(sourceItem.ID, sourceItem);
            OnSlotQuantityChanged(sourceSlotIndex, sourceItem, sourceCount, remainingCount);
        }

        private void UpdateCacheAfterMove(int sourceSlotIndex, IItem sourceItem, int count, bool isCompleteMove)
        {
            if (isCompleteMove)
            {
                _cacheService.UpdateEmptySlotCache(sourceSlotIndex, true);
                _cacheService.UpdateItemSlotIndexCache(sourceItem.ID, sourceSlotIndex, false);
                _cacheService.UpdateItemTypeCache(sourceItem.Type, sourceSlotIndex, false);
            }
            _cacheService.UpdateItemCountCache(sourceItem.ID, -count);
        }

        private List<(int index, IItem item, int count)> CollectOccupiedSlots()
        {
            var occupiedSlots = new List<(int index, IItem item, int count)>();

            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot.IsOccupied && slot.Item != null)
                {
                    occupiedSlots.Add((i, slot.Item, slot.ItemCount));
                }
            }

            return occupiedSlots;
        }

        private void ExecuteInventoryOperationSafely(System.Action operation)
        {
            var backupData = CreateInventoryBackup();

            BeginBatchUpdate();
            try
            {
                operation();
                RebuildCaches();
            }
            catch (Exception ex)
            {
                Debug.LogError($"容器操作失败: {ex.Message}，正在恢复数据");
                RestoreInventoryFromBackup(backupData);
            }
            finally
            {
                EndBatchUpdate();
            }
        }

        private (IItem item, int count)[] CreateInventoryBackup()
        {
            var backup = new (IItem item, int count)[_slots.Count];
            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                backup[i] = (slot.Item, slot.ItemCount);
            }
            return backup;
        }

        private void RestoreInventoryFromBackup((IItem item, int count)[] backupData)
        {
            for (int i = 0; i < _slots.Count && i < backupData.Length; i++)
            {
                var slot = _slots[i];
                var (item, count) = backupData[i];

                if (item != null)
                    slot.SetItem(item, count);
                else
                    slot.ClearSlot();
            }
            RebuildCaches();
        }

        private void ClearAllSlotsInternal()
        {
            foreach (var slot in _slots)
            {
                slot.ClearSlot();
            }
        }

        private void FillSlotsWithSortedItems(List<(int index, IItem item, int count)> sortedItems)
        {
            for (int i = 0; i < sortedItems.Count && i < _slots.Count; i++)
            {
                var (_, item, count) = sortedItems[i];
                _slots[i].SetItem(item, count);
            }
        }

        private Dictionary<string, List<(int slotIndex, IItem item, int count)>> GroupStackableItemsByID()
        {
            var itemGroups = new Dictionary<string, List<(int slotIndex, IItem item, int count)>>();

            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (!IsSlotEligibleForConsolidation(slot)) continue;

                AddSlotToItemGroup(itemGroups, i, slot);
            }

            return itemGroups;
        }

        private bool IsSlotEligibleForConsolidation(ISlot slot)
        {
            return slot.IsOccupied && slot.Item != null && slot.Item.IsStackable;
        }

        private void AddSlotToItemGroup(Dictionary<string, List<(int slotIndex, IItem item, int count)>> itemGroups,
                                       int slotIndex, ISlot slot)
        {
            string itemId = slot.Item.ID;
            if (!itemGroups.ContainsKey(itemId))
                itemGroups[itemId] = new List<(int, IItem, int)>();

            itemGroups[itemId].Add((slotIndex, slot.Item, slot.ItemCount));
        }

        private void ConsolidateItemGroups(Dictionary<string, List<(int slotIndex, IItem item, int count)>> itemGroups)
        {
            foreach (var itemGroup in itemGroups.Values)
            {
                if (itemGroup.Count >= 2)
                {
                    ConsolidateSingleItemGroup(itemGroup);
                }
            }
        }

        private void ConsolidateSingleItemGroup(List<(int slotIndex, IItem item, int count)> itemSlots)
        {
            var item = itemSlots[0].item;
            int totalCount = CalculateTotalItemCount(itemSlots);
            var targetSlots = GetSortedTargetSlots(itemSlots);

            ClearItemGroupSlots(itemSlots);
            RedistributeItemsToSlots(item, totalCount, targetSlots);
        }

        private int CalculateTotalItemCount(List<(int slotIndex, IItem item, int count)> itemSlots)
        {
            return itemSlots.Sum(slot => slot.count);
        }

        private List<int> GetSortedTargetSlots(List<(int slotIndex, IItem item, int count)> itemSlots)
        {
            return itemSlots.Select(s => s.slotIndex).OrderBy(index => index).ToList();
        }

        private void ClearItemGroupSlots(List<(int slotIndex, IItem item, int count)> itemSlots)
        {
            foreach (var (slotIndex, _, _) in itemSlots)
            {
                _slots[slotIndex].ClearSlot();
            }
        }

        private void RedistributeItemsToSlots(IItem item, int totalCount, List<int> targetSlots)
        {
            int maxStackCount = item.MaxStackCount;
            int remainingCount = totalCount;
            int targetIndex = 0;

            while (remainingCount > 0 && targetIndex < targetSlots.Count)
            {
                int slotIndex = targetSlots[targetIndex];
                int countForSlot = maxStackCount <= 0
                    ? remainingCount
                    : Math.Min(remainingCount, maxStackCount);

                _slots[slotIndex].SetItem(item, countForSlot);
                remainingCount -= countForSlot;
                targetIndex++;
            }
        }
        #endregion
    }
} // namespace EasyPack