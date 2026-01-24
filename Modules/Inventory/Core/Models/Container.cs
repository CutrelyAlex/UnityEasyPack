using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace EasyPack.InventorySystem
{
    public abstract class Container : IContainer
    {
        #region 基本属性

        public string ID { get; }
        public string Name { get; }
        public string Type { get; set; }
        public int Capacity { get; set; } // -1表示无限容量

        /// <summary>
        ///     已使用的槽位数量
        /// </summary>
        public int UsedSlots => _slots.Count(s => s.IsOccupied);

        /// <summary>
        ///     剩余空闲槽位数量
        /// </summary>
        public int FreeSlots => Capacity < 0 ? int.MaxValue : Capacity - UsedSlots;

        public abstract bool IsGrid { get; } // 子类实现，决定是否为网格容器
        public abstract Vector2 Grid { get; } // 网格容器形状

        public List<IItemCondition> ContainerCondition { get; set; }
        protected readonly List<ISlot> _slots = new();
        public IReadOnlyList<ISlot> Slots => _slots.AsReadOnly();

        // 缓存管理器
        protected readonly ContainerCacheService _cacheService;

        // 查询服务
        private readonly IItemQueryService _queryService;

        /// <summary>
        ///     关联的库存服务
        /// </summary>
        public IInventoryService InventoryService { get; set; }
        
        /// <summary>
        ///     物品工厂引用，用于通过ItemData/itemId创建物品
        /// </summary>
        public IItemFactory ItemFactory => InventoryService?.ItemFactory;

        protected Container(string id, string name, string type, int capacity = -1)
        {
            ID = id;
            Name = name;
            Type = type;
            Capacity = capacity;
            ContainerCondition = new();

            _cacheService = new(capacity);
            _queryService = new ItemQueryService(_slots.AsReadOnly(), _cacheService);
            RebuildCaches();
        }

        #endregion

        #region 容器事件

        /// <summary>
        ///     添加物品操作结果事件（统一处理成功和失败）
        /// </summary>
        /// <param name="IItem">操作的物品</param>
        /// <param name="requestedCount">请求添加的数量</param>
        /// <param name="actualCount">实际添加的数量</param>
        /// <param name="result">操作结果</param>
        /// <param name="affectedSlots">涉及的槽位索引列表（失败时为空列表）</param>
        public event Action<IItem, int, int, AddItemResult, List<int>> OnItemAddResult;

        /// <summary>
        ///     移除物品操作结果事件（统一处理成功和失败）。
        /// </summary>
        /// <param name="itemId">
        ///     操作的物品 ID（字符串形式）。
        /// </param>
        /// <param name="requestedCount">
        ///     请求移除的数量。
        /// </param>
        /// <param name="actualCount">
        ///     实际移除的数量（成功时等于 <paramref name="requestedCount" />，失败时为 0）。
        /// </param>
        /// <param name="result">
        ///     操作结果，使用 <see cref="RemoveItemResult" /> 表示成功/失败等状态。
        /// </param>
        /// <param name="affectedSlots">
        ///     受影响的槽位索引列表。<br />
        ///     • 成功时：包含实际被移除的槽位索引。<br />
        ///     • 失败时：为空列表（<c>new List&lt;int&gt;()</c>）。
        /// </param>
        public event Action<string, int, int, RemoveItemResult, List<int>> OnItemRemoveResult;

        /// <summary>
        ///     槽位数量变更事件
        /// </summary>
        /// <param name="slotIndex">变更的槽位索引</param>
        /// <param name="item">变更的物品</param>
        /// <param name="oldCount">原数量</param>
        /// <param name="newCount">新数量</param>
        public event Action<int, IItem, int, int> OnSlotCountChanged;

        /// <summary>
        ///     触发槽位物品数量变更事件
        /// </summary>
        public virtual void OnSlotQuantityChanged(int slotIndex, IItem item, int oldCount, int newCount)
        {
            // 维护可继续堆叠占用槽位计数
            if (item is { IsStackable: true })
            {
                bool oldTracked = oldCount > 0 && (item.MaxStackCount <= 0 || oldCount < item.MaxStackCount);
                bool newTracked = newCount > 0 && (item.MaxStackCount <= 0 || newCount < item.MaxStackCount);

                if (oldTracked != newTracked)
                {
                    _notFullStackSlotsCount += newTracked ? 1 : -1;
                    if (_notFullStackSlotsCount < 0) _notFullStackSlotsCount = 0;
                }
            }

            OnSlotCountChanged?.Invoke(slotIndex, item, oldCount, newCount);
        }

        /// <summary>
        ///     物品总数变更事件
        /// </summary>
        /// <param name="itemId">物品ID</param>
        /// <param name="item">物品引用，可能为null，如果物品已完全移除</param>
        /// <param name="oldTotalCount">旧总数</param>
        /// <param name="newTotalCount">新总数</param>
        public event Action<string, IItem, int, int> OnItemTotalCountChanged;

        private readonly Dictionary<string, int> _itemTotalCounts = new();

        /// <summary>
        ///     触发物品总数变更
        /// </summary>
        public void TriggerItemTotalCountChanged(string itemId, IItem itemRef = null)
        {
            int newTotal = GetItemTotalCount(itemId);

            int oldTotal = _itemTotalCounts.GetValueOrDefault(itemId, 0);

            // 只有总数有变化才继续处理
            if (newTotal == oldTotal) return;

            if (itemRef == null && newTotal > 0) itemRef = GetItemReference(itemId);

            OnItemTotalCountChanged?.Invoke(itemId, itemRef, oldTotal, newTotal);

            if (newTotal > 0)
            {
                _itemTotalCounts[itemId] = newTotal;
            }
            else
            {
                _itemTotalCounts.Remove(itemId);
            }
        }

        #endregion

        #region 批操作

        private readonly HashSet<string> _pendingTotalCountUpdates = new();
        private readonly Dictionary<string, IItem> _itemRefCache = new();
        private int _batchDepth;

        /// <summary>
        ///     开始批量操作模式
        /// </summary>
        protected void BeginBatchUpdate()
        {
            if (_batchDepth == 0)
            {
                _pendingTotalCountUpdates.Clear();
                _itemRefCache.Clear();
            }

            _batchDepth++;
        }

        /// <summary>
        ///     结束批量操作模式并处理所有待更新项
        /// </summary>
        protected void EndBatchUpdate()
        {
            if (_batchDepth <= 0)
            {
                return;
            }

            _batchDepth--;
            if (_batchDepth == 0)
            {
                if (_pendingTotalCountUpdates.Count > 0)
                {
                    foreach (string itemId in _pendingTotalCountUpdates)
                    {
                        TriggerItemTotalCountChanged(itemId,
                            _itemRefCache.GetValueOrDefault(itemId));
                    }

                    _pendingTotalCountUpdates.Clear();
                    _itemRefCache.Clear();
                }
            }
        }

        #endregion

        #region 状态检查

        private int _notFullStackSlotsCount;


        /// <summary>
        ///     检查容器是否已满
        ///     仅当所有槽位都被占用，且每个占用的槽位物品都不可堆叠或已达到堆叠上限时，容器才被认为是满的
        /// </summary>
        public virtual bool Full
        {
            get
            {
                if (Capacity < 0)
                {
                    return false;
                }

                if (_slots.Count < Capacity)
                {
                    return false;
                }

                if (_cacheService.GetEmptySlotIndices().Count > 0)
                {
                    return false;
                }

                return _notFullStackSlotsCount <= 0;
            }
        }

        /// <summary>
        ///     检查物品是否满足容器条件
        /// </summary>
        public bool ValidateItemCondition(IItem item)
        {
            if (item == null)
            {
                Debug.LogWarning("ValidateItemCondition: item is null.");
                return false;
            }

            if (ContainerCondition is { Count: > 0 })
            {
                foreach (IItemCondition condition in ContainerCondition)
                {
                    if (!condition.CheckCondition(item))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        ///     检查是否可以添加物品到容器
        /// </summary>
        /// <param name="item">要添加的物品</param>
        /// <returns>添加结果，如果可以添加返回Success，否则返回对应的错误原因</returns>
        protected virtual AddItemResult CanAddItem(IItem item)
        {
            if (item == null)
            {
                return AddItemResult.ItemIsNull;
            }

            if (!ValidateItemCondition(item))
            {
                return AddItemResult.ItemConditionNotMet;
            }

            // 如果容器已满，需要检查是否有可堆叠的槽位
            if (Full)
            {
                // 如果物品可堆叠，检查是否有相同物品且未达到堆叠上限的槽位
                if (item.IsStackable)
                {
                    if (_cacheService.TryGetItemSlotIndices(item.ID, out var indices))
                    {
                        foreach (int slotIndex in indices)
                        {
                            if (slotIndex < _slots.Count)
                            {
                                ISlot slot = _slots[slotIndex];
                                if (slot.IsOccupied && slot.Item.ID == item.ID &&
                                    slot.Item.IsStackable && (slot.Item.MaxStackCount <= 0 ||
                                                              slot.Item.Count < slot.Item.MaxStackCount))
                                {
                                    return AddItemResult.Success;
                                }
                            }
                        }
                    }

                    return AddItemResult.StackLimitReached;
                }

                return AddItemResult.ContainerIsFull;
            }

            return AddItemResult.Success;
        }

        #endregion

        #region 缓存服务

        /// <summary>
        ///     初始化或重建所有缓存
        /// </summary>
        public void RebuildCaches()
        {
            _cacheService.RebuildCaches(_slots.AsReadOnly());

            // 重建可继续堆叠占用槽位计数
            _notFullStackSlotsCount = 0;
            foreach (ISlot s in _slots)
            {
                if (!s.IsOccupied || s.Item is not { IsStackable: true }) continue;
                if (s.Item.MaxStackCount <= 0 || s.Item.Count < s.Item.MaxStackCount)
                {
                    _notFullStackSlotsCount++;
                }
            }
        }

        /// <summary>
        ///     清除缓存中的无效条目
        /// </summary>
        public bool ValidateCaches()
        {
            try
            {
                _cacheService.ValidateCaches(_slots.AsReadOnly());
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void ApplyBatchCacheUpdates(BatchCacheUpdates updates)
        {
            // 数量缓存只更新一次
            if (updates.TotalCountDelta != 0)
            {
                _cacheService.UpdateItemCountCache(updates.ItemId, updates.TotalCountDelta);
            }

            // 批量更新槽位索引缓存
            foreach ((int slotIndex, bool isAdding) in updates.SlotIndexUpdates)
            {
                _cacheService.UpdateItemSlotIndexCache(updates.ItemId, slotIndex, isAdding);
            }

            // 批量更新类型索引缓存
            foreach ((int slotIndex, bool isAdding) in updates.TypeIndexUpdates)
            {
                _cacheService.UpdateItemTypeCache(updates.ItemType, slotIndex, isAdding);
            }

            // 批量更新空槽位缓存
            foreach ((int slotIndex, bool isEmpty) in updates.EmptySlotUpdates)
            {
                _cacheService.UpdateEmptySlotCache(slotIndex, isEmpty);
            }
        }

        #endregion

        #region 查询服务

        public bool HasItem(string itemId) => _queryService.HasItem(itemId);

        public int GetItemTotalCount(string itemId) => _queryService.GetItemTotalCount(itemId);

        public bool HasEnoughItems(string itemId, int requiredCount) =>
            _queryService.HasEnoughItems(itemId, requiredCount);

        public List<int> FindSlotIndices(string itemId) => _queryService.FindSlotIndices(itemId);

        public int FindFirstSlotIndex(string itemId) => _queryService.FindFirstSlotIndex(itemId);

        public List<(int slotIndex, IItem item, int count)> GetItemsByType(string itemType) =>
            _queryService.GetItemsByType(itemType);

        public List<(int slotIndex, IItem item, int count)> GetItemsByAttribute(string attributeName,
            object attributeValue) =>
            _queryService.GetItemsByAttribute(attributeName, attributeValue);

        public List<(int slotIndex, IItem item, int count)> GetItemsByName(string namePattern) =>
            _queryService.GetItemsByName(namePattern);

        public List<(int slotIndex, IItem item, int count)> GetItemsWhere(Func<IItem, bool> condition) =>
            _queryService.GetItemsWhere(condition);

        public Dictionary<string, int> GetAllItemCountsDict() => _queryService.GetAllItemCountsDict();

        public List<(int slotIndex, IItem item, int count)> GetAllItems() => _queryService.GetAllItems();

        public int GetUniqueItemCount() => _queryService.GetUniqueItemCount();

        public bool IsEmpty() => _queryService.IsEmpty();

        public float GetTotalWeight() => _queryService.GetTotalWeight();

        private IItem GetItemReference(string itemId) => _queryService.GetItemReference(itemId);

        #endregion

        #region 移除物品

        /// <summary>
        ///     移除指定ID的物品
        /// </summary>
        /// <param name="itemId">物品ID</param>
        /// <param name="count">移除数量</param>
        /// <returns>移除结果</returns>
        public virtual RemoveItemResult RemoveItem(string itemId, int count = 1)
        {
            var emptySlots = new List<int>();
            if (string.IsNullOrEmpty(itemId))
            {
                OnItemRemoveResult?.Invoke(itemId, count, 0, RemoveItemResult.InvalidItemId, emptySlots);
                return RemoveItemResult.InvalidItemId;
            }

            int totalCount = GetItemTotalCount(itemId);
            if (totalCount < count && totalCount != 0)
            {
                OnItemRemoveResult?.Invoke(itemId, count, 0, RemoveItemResult.InsufficientQuantity, emptySlots);
                return RemoveItemResult.InsufficientQuantity;
            }

            if (totalCount == 0)
            {
                OnItemRemoveResult?.Invoke(itemId, count, 0, RemoveItemResult.ItemNotFound, emptySlots);
                return RemoveItemResult.ItemNotFound;
            }

            int remainingCount = count;
            List<(ISlot slot, int removeAmount, int slotIndex)> removals = new();

            // 使用缓存的槽位索引集合
            if (_cacheService.TryGetItemSlotIndices(itemId, out var indices) && indices is { Count: > 0 })
            {
                foreach (int i in indices)
                {
                    if (remainingCount <= 0) break;
                    if (i < 0 || i >= _slots.Count) continue;

                    ISlot slot = _slots[i];
                    if (!slot.IsOccupied || slot.Item == null || slot.Item.ID != itemId) continue;

                    int removeAmount = Mathf.Min(slot.Item.Count, remainingCount);
                    if (removeAmount <= 0) continue;

                    removals.Add((slot, removeAmount, i));
                    remainingCount -= removeAmount;
                }
            }

            var affectedSlots = new List<int>();
            // 第二步：确认可以完全移除指定数量后，执行实际的移除操作
            if (remainingCount == 0)
            {
                bool itemCompletelyRemoved = false;

                foreach ((ISlot slot, int removeAmount, int slotIndex) in removals)
                {
                    int oldCount = slot.Item.Count;
                    IItem item = slot.Item;
                    string itemType = item.Type;

                    if (removeAmount == slot.Item.Count)
                    {
                        slot.ClearSlot();
                        _cacheService.UpdateEmptySlotCache(slotIndex, true);
                        _cacheService.UpdateItemSlotIndexCache(itemId, slotIndex, false);
                        _cacheService.UpdateItemTypeCache(itemType, slotIndex, false);
                        if (!itemCompletelyRemoved) itemCompletelyRemoved = !_cacheService.HasItemInCache(itemId);
                    }
                    else
                    {
                        slot.Item.Count -= removeAmount;
                    }

                    // 更新数量缓存
                    _cacheService.UpdateItemCountCache(itemId, -removeAmount);

                    affectedSlots.Add(slotIndex);

                    // 槽位物品数量变更事件
                    OnSlotQuantityChanged(slotIndex, item, oldCount, slot.Item?.Count ?? 0);
                }

                // 移除成功事件
                OnItemRemoveResult?.Invoke(itemId, count, count, RemoveItemResult.Success, affectedSlots);
                TriggerItemTotalCountChanged(itemId);
                return RemoveItemResult.Success;
            }

            // 发生未知错误，才能悲惨得走到了这一步
            OnItemRemoveResult?.Invoke(itemId, count, 0, RemoveItemResult.Failed, emptySlots);
            return RemoveItemResult.Failed;
        }

        /// <summary>
        ///     从指定槽位移除物品
        /// </summary>
        /// <param name="index">槽位索引</param>
        /// <param name="count">移除数量</param>
        /// <param name="expectedItemId">预期物品ID，用于验证</param>
        /// <returns>移除结果</returns>
        public virtual RemoveItemResult RemoveItemAtIndex(int index, int count = 1, string expectedItemId = null)
        {
            var emptySlots = new List<int>();
            // 检查槽位索引是否有效
            if (index < 0 || index >= _slots.Count)
            {
                OnItemRemoveResult?.Invoke(expectedItemId ?? "unknown", count, 0, RemoveItemResult.SlotNotFound,
                    emptySlots);
                return RemoveItemResult.SlotNotFound;
            }

            ISlot slot = _slots[index];

            // 检查槽位是否有物品
            if (!slot.IsOccupied || slot.Item == null)
            {
                OnItemRemoveResult?.Invoke(expectedItemId ?? "unknown", count, 0, RemoveItemResult.ItemNotFound,
                    emptySlots);
                return RemoveItemResult.ItemNotFound;
            }

            // 保存物品引用和ID
            IItem item = slot.Item;
            string itemId = item.ID;
            string itemType = item.Type;

            // 如果提供了预期的物品ID，则验证
            if (!string.IsNullOrEmpty(expectedItemId) && itemId != expectedItemId)
            {
                OnItemRemoveResult?.Invoke(expectedItemId, count, 0, RemoveItemResult.InvalidItemId, emptySlots);
                return RemoveItemResult.InvalidItemId;
            }

            // 检查物品数量是否足够
            if (slot.Item.Count < count)
            {
                OnItemRemoveResult?.Invoke(itemId, count, 0, RemoveItemResult.InsufficientQuantity, emptySlots);
                return RemoveItemResult.InsufficientQuantity;
            }

            // 记录旧数量
            int oldCount = slot.Item.Count;

            // 所有检查都通过，执行移除操作
            if (slot.Item.Count - count <= 0)
            {
                slot.ClearSlot();
                _cacheService.UpdateEmptySlotCache(index, true);
                _cacheService.UpdateItemSlotIndexCache(itemId, index, false);
                _cacheService.UpdateItemTypeCache(itemType, index, false);
            }
            else
            {
                slot.Item.Count -= count;
            }

            // 更新数量缓存
            _cacheService.UpdateItemCountCache(itemId, -count);

            // 触发物品数量变更事件
            OnSlotQuantityChanged(index, item, oldCount, slot.Item?.Count ?? 0);

            // 触发物品移除事件
            var affectedSlots = new List<int> { index };
            OnItemRemoveResult?.Invoke(itemId, count, count, RemoveItemResult.Success, affectedSlots);
            TriggerItemTotalCountChanged(itemId, item);

            return RemoveItemResult.Success;
        }

        #endregion

        #region 物品拆分

        /// <summary>
        ///     拆分物品堆，返回新的Item实例
        /// </summary>
        /// <param name="slotIndex">要拆分的槽位索引</param>
        /// <param name="splitCount">拆分数量</param>
        /// <returns>拆分出的新Item实例（拥有新UID和独立RuntimeMetadata），若失败返回null</returns>
        public virtual IItem SplitItem(int slotIndex, int splitCount)
        {
            // 验证槽位索引
            if (slotIndex < 0 || slotIndex >= _slots.Count)
            {
                Debug.LogWarning($"SplitItem: 槽位索引无效 {slotIndex}");
                return null;
            }

            ISlot slot = _slots[slotIndex];

            // 验证槽位有物品
            if (!slot.IsOccupied || slot.Item == null)
            {
                Debug.LogWarning($"SplitItem: 槽位 {slotIndex} 为空");
                return null;
            }

            IItem sourceItem = slot.Item;

            // 验证拆分数量
            if (splitCount <= 0)
            {
                Debug.LogWarning($"SplitItem: 拆分数量必须大于0，当前: {splitCount}");
                return null;
            }

            if (splitCount > sourceItem.Count)
            {
                Debug.LogWarning($"SplitItem: 拆分数量 {splitCount} 超出物品数量 {sourceItem.Count}");
                return null;
            }

            // 创建克隆物品
            IItem splitItem;
            if (ItemFactory != null)
            {
                splitItem = ItemFactory.CloneItem(sourceItem, splitCount);
            }
            else
            {
                // 降级使用Item.Clone()
                splitItem = sourceItem.Clone();
                splitItem.Count = splitCount;
                
                // 分配新UID
                if (splitItem.ItemUID == -1 && InventoryService != null)
                {
                    InventoryService.AssignItemUID(splitItem);
                }
            }

            if (splitItem == null)
            {
                Debug.LogError($"SplitItem: 克隆物品失败");
                return null;
            }

            // 记录旧数量
            int oldCount = sourceItem.Count;
            string itemId = sourceItem.ID;

            // 减少原物品数量
            sourceItem.Count -= splitCount;

            // 如果原物品数量为0，清空槽位
            if (sourceItem.Count <= 0)
            {
                slot.ClearSlot();
                _cacheService.UpdateEmptySlotCache(slotIndex, true);
                _cacheService.UpdateItemSlotIndexCache(itemId, slotIndex, false);
                _cacheService.UpdateItemTypeCache(sourceItem.Type, slotIndex, false);
            }

            // 更新缓存（拆分不改变总数，因为返回的item未添加到容器）
            // 注意：此处不更新_itemCountCache，因为返回的物品未加入任何容器

            // 触发槽位数量变更事件
            OnSlotQuantityChanged(slotIndex, sourceItem, oldCount, sourceItem.Count);

            // 触发物品总数变更（仅当槽位还有物品时）
            TriggerItemTotalCountChanged(itemId, sourceItem);

            return splitItem;
        }

        #endregion

        #region 物品转移

        /// <summary>
        ///     将物品从一个槽位移动到目标容器
        /// </summary>
        /// <param name="fromSlot">源槽位索引</param>
        /// <param name="toContainer">目标容器</param>
        /// <param name="toSlot">目标槽位索引，-1表示自动查找</param>
        /// <param name="autoStack">是否自动堆叠到目标容器中的相同物品</param>
        /// <returns>操作结果</returns>
        public virtual MoveItemResult MoveItem(int fromSlot, IContainer toContainer, int toSlot = -1, bool autoStack = true)
        {
            // 验证源槽位
            if (fromSlot < 0 || fromSlot >= _slots.Count)
            {
                Debug.LogWarning($"MoveItem: 源槽位索引无效 {fromSlot}");
                return MoveItemResult.SourceSlotNotFound;
            }

            ISlot sourceSlot = _slots[fromSlot];
            if (!sourceSlot.IsOccupied || sourceSlot.Item == null)
            {
                Debug.LogWarning($"MoveItem: 源槽位 {fromSlot} 为空");
                return MoveItemResult.SourceSlotEmpty;
            }

            // 验证目标容器
            if (toContainer == null)
            {
                Debug.LogWarning("MoveItem: 目标容器为null");
                return MoveItemResult.TargetContainerNull;
            }

            IItem itemToMove = sourceSlot.Item;
            string itemId = itemToMove.ID;
            string itemType = itemToMove.Type;
            int itemCount = itemToMove.Count;
            int oldCount = itemCount;

            // 检查目标容器条件
            if (toContainer is Container targetContainer)
            {
                if (!targetContainer.ValidateItemCondition(itemToMove))
                {
                    Debug.LogWarning($"MoveItem: 物品不满足目标容器条件");
                    return MoveItemResult.ConditionNotMet;
                }
            }

            // 如果指定了目标槽位，检查是否有效
            if (toSlot >= 0 && toSlot >= toContainer.Slots.Count)
            {
                Debug.LogWarning($"MoveItem: 目标槽位索引无效 {toSlot}");
                return MoveItemResult.TargetSlotNotFound;
            }

            // 同一容器内移动的特殊处理
            if (ReferenceEquals(this, toContainer))
            {
                return MoveItemWithinContainer(fromSlot, toSlot, autoStack);
            }

            // 尝试添加到目标容器（直接传递引用，保持UID）
            (AddItemResult addResult, int addedCount) = toContainer.AddItems(itemToMove, toSlot, autoStack);

            if (addResult == AddItemResult.Success && addedCount == itemCount)
            {
                // 完全移动成功，清空源槽位
                sourceSlot.ClearSlot();
                _cacheService.UpdateEmptySlotCache(fromSlot, true);
                _cacheService.UpdateItemSlotIndexCache(itemId, fromSlot, false);
                _cacheService.UpdateItemTypeCache(itemType, fromSlot, false);
                _cacheService.UpdateItemCountCache(itemId, -itemCount);

                // 触发事件
                OnSlotQuantityChanged(fromSlot, itemToMove, oldCount, 0);
                TriggerItemTotalCountChanged(itemId);

                return MoveItemResult.Success;
            }
            else if (addedCount > 0)
            {
                // 部分成功
                sourceSlot.Item.Count -= addedCount;
                _cacheService.UpdateItemCountCache(itemId, -addedCount);

                OnSlotQuantityChanged(fromSlot, itemToMove, oldCount, sourceSlot.Item.Count);
                TriggerItemTotalCountChanged(itemId, itemToMove);

                return MoveItemResult.PartialSuccess;
            }
            else if (addResult == AddItemResult.ContainerIsFull)
            {
                return MoveItemResult.TargetContainerFull;
            }

            return MoveItemResult.Failed;
        }

        /// <summary>
        ///     在同一容器内移动物品
        /// </summary>
        private MoveItemResult MoveItemWithinContainer(int fromSlot, int toSlot, bool autoStack)
        {
            if (toSlot < 0)
            {
                // 自动查找槽位时，对于同容器内移动没有意义
                return MoveItemResult.Success;
            }

            if (fromSlot == toSlot)
            {
                return MoveItemResult.Success;
            }

            if (toSlot >= _slots.Count)
            {
                return MoveItemResult.TargetSlotNotFound;
            }

            ISlot sourceSlot = _slots[fromSlot];
            ISlot targetSlot = _slots[toSlot];
            IItem sourceItem = sourceSlot.Item;
            string itemId = sourceItem.ID;
            string itemType = sourceItem.Type;
            int sourceCount = sourceItem.Count;

            if (targetSlot.IsOccupied)
            {
                if (autoStack && sourceItem.CanStack(targetSlot.Item))
                {
                    // 堆叠到目标槽位
                    int targetOldCount = targetSlot.Item.Count;
                    int canStack = sourceItem.MaxStackCount > 0
                        ? Math.Min(sourceCount, sourceItem.MaxStackCount - targetSlot.Item.Count)
                        : sourceCount;

                    if (canStack <= 0)
                    {
                        return MoveItemResult.TargetSlotOccupied;
                    }

                    targetSlot.Item.Count += canStack;
                    sourceSlot.Item.Count -= canStack;

                    OnSlotQuantityChanged(toSlot, targetSlot.Item, targetOldCount, targetSlot.Item.Count);
                    OnSlotQuantityChanged(fromSlot, sourceItem, sourceCount, sourceSlot.Item.Count);

                    if (sourceSlot.Item.Count <= 0)
                    {
                        sourceSlot.ClearSlot();
                        _cacheService.UpdateEmptySlotCache(fromSlot, true);
                        _cacheService.UpdateItemSlotIndexCache(itemId, fromSlot, false);
                        _cacheService.UpdateItemTypeCache(itemType, fromSlot, false);
                    }

                    return canStack == sourceCount ? MoveItemResult.Success : MoveItemResult.PartialSuccess;
                }
                else
                {
                    // 交换物品
                    IItem targetItem = targetSlot.Item;
                    int targetCount = targetItem.Count;
                    string targetItemId = targetItem.ID;
                    string targetItemType = targetItem.Type;

                    // 交换
                    targetSlot.SetItem(sourceItem);
                    sourceSlot.SetItem(targetItem);

                    // 更新缓存
                    _cacheService.UpdateItemSlotIndexCache(itemId, fromSlot, false);
                    _cacheService.UpdateItemSlotIndexCache(itemId, toSlot, true);
                    _cacheService.UpdateItemSlotIndexCache(targetItemId, toSlot, false);
                    _cacheService.UpdateItemSlotIndexCache(targetItemId, fromSlot, true);

                    _cacheService.UpdateItemTypeCache(itemType, fromSlot, false);
                    _cacheService.UpdateItemTypeCache(itemType, toSlot, true);
                    _cacheService.UpdateItemTypeCache(targetItemType, toSlot, false);
                    _cacheService.UpdateItemTypeCache(targetItemType, fromSlot, true);

                    OnSlotQuantityChanged(fromSlot, targetItem, 0, targetCount);
                    OnSlotQuantityChanged(toSlot, sourceItem, 0, sourceCount);

                    return MoveItemResult.Success;
                }
            }
            else
            {
                // 目标槽位为空，直接移动
                if (!targetSlot.CheckSlotCondition(sourceItem))
                {
                    return MoveItemResult.ConditionNotMet;
                }

                targetSlot.SetItem(sourceItem);
                sourceSlot.ClearSlot();

                // 更新缓存
                _cacheService.UpdateEmptySlotCache(fromSlot, true);
                _cacheService.UpdateEmptySlotCache(toSlot, false);
                _cacheService.UpdateItemSlotIndexCache(itemId, fromSlot, false);
                _cacheService.UpdateItemSlotIndexCache(itemId, toSlot, true);
                _cacheService.UpdateItemTypeCache(itemType, fromSlot, false);
                _cacheService.UpdateItemTypeCache(itemType, toSlot, true);

                OnSlotQuantityChanged(fromSlot, sourceItem, sourceCount, 0);
                OnSlotQuantityChanged(toSlot, sourceItem, 0, sourceCount);

                return MoveItemResult.Success;
            }
        }

        #endregion

        #region AddItems辅助方法

        /// <summary>
        ///     验证物品添加的基本条件
        /// </summary>
        private (bool isValid, AddItemResult result) ValidateAddItemRequest(IItem item, int count)
        {
            if (item == null)
            {
                return (false, AddItemResult.ItemIsNull);
            }

            if (count <= 0)
            {
                return (false, AddItemResult.AddNothingLOL);
            }

            // 确保物品有UID
            if (item.ItemUID == -1 && InventoryService != null)
            {
                InventoryService.AssignItemUID(item);
            }

            if (!ValidateItemCondition(item))
            {
                return (false, AddItemResult.ItemConditionNotMet);
            }

            return (true, AddItemResult.Success);
        }

        /// <summary>
        ///     尝试将物品堆叠到现有槽位
        /// </summary>
        private (int addedCount, List<int> affectedSlots, BatchCacheUpdates cacheUpdates) ProcessStackableItems(
            IItem item, int count)
        {
            var cacheUpdates = new BatchCacheUpdates(item.ID, item.Type);
            var affectedSlots = new List<int>();

            if (!item.IsStackable)
            {
                return (0, affectedSlots, cacheUpdates);
            }

            (int stackedCount, var stackedSlots, var slotChanges) = TryStackItems(item, count);

            if (stackedCount > 0)
            {
                affectedSlots.AddRange(stackedSlots);
                cacheUpdates.TotalCountDelta += stackedCount;

                // 触发槽位变更事件
                foreach (var change in slotChanges)
                {
                    int slotIdx = change.Key;
                    ISlot slot = _slots[slotIdx];
                    OnSlotQuantityChanged(slotIdx, slot.Item, change.Value.oldCount, change.Value.newCount);
                }
            }

            return (stackedCount, affectedSlots, cacheUpdates);
        }

        /// <summary>
        ///     处理指定槽位添加
        /// </summary>
        private (bool success, int addedCount, int remaining, BatchCacheUpdates cacheUpdates)
            ProcessSpecificSlotAddition(
                IItem item, int slotIndex, int count)
        {
            var cacheUpdates = new BatchCacheUpdates(item.ID, item.Type);

            if (slotIndex < 0)
            {
                return (false, 0, count, cacheUpdates);
            }

            (bool success, int addedCount, int newRemaining) = TryAddToSpecificSlot(item, slotIndex, count);

            if (success)
            {
                cacheUpdates.TotalCountDelta += addedCount;
                cacheUpdates.SlotIndexUpdates.Add((slotIndex, true));
                cacheUpdates.TypeIndexUpdates.Add((slotIndex, true));
                cacheUpdates.EmptySlotUpdates.Add((slotIndex, false));
            }

            return (success, addedCount, newRemaining, cacheUpdates);
        }

        /// <summary>
        ///     处理空槽位和新槽位填充
        /// </summary>
        private (int addedCount, List<int> affectedSlots, BatchCacheUpdates cacheUpdates) ProcessEmptyAndNewSlots(
            IItem item, int count)
        {
            var cacheUpdates = new BatchCacheUpdates(item.ID, item.Type);
            var affectedSlots = new List<int>();
            int totalAdded = 0;
            int remainingCount = count;

            while (remainingCount > 0)
            {
                // 尝试空槽位
                (bool emptySuccess, int emptyAdded, int emptyRemaining, int emptySlotIndex) =
                    TryAddToEmptySlot(item, remainingCount);

                if (emptySuccess)
                {
                    totalAdded += emptyAdded;
                    remainingCount = emptyRemaining;
                    affectedSlots.Add(emptySlotIndex);

                    cacheUpdates.TotalCountDelta += emptyAdded;
                    cacheUpdates.SlotIndexUpdates.Add((emptySlotIndex, true));
                    cacheUpdates.TypeIndexUpdates.Add((emptySlotIndex, true));

                    if (remainingCount <= 0)
                    {
                        break;
                    }

                    continue;
                }

                // 尝试新槽位
                (bool newSuccess, int newAdded, int newRemaining, int newSlotIndex) =
                    TryAddToNewSlot(item, remainingCount);

                if (newSuccess)
                {
                    totalAdded += newAdded;
                    remainingCount = newRemaining;
                    affectedSlots.Add(newSlotIndex);

                    cacheUpdates.TotalCountDelta += newAdded;
                    cacheUpdates.SlotIndexUpdates.Add((newSlotIndex, true));
                    cacheUpdates.TypeIndexUpdates.Add((newSlotIndex, true));

                    if (remainingCount <= 0)
                    {
                        break;
                    }

                    continue;
                }

                // 无法继续添加
                break;
            }

            return (totalAdded, affectedSlots, cacheUpdates);
        }

        /// <summary>
        ///     添加指定数量的物品到容器（内部实现，带超出数量返回）
        /// </summary>
        private (AddItemResult result, int actualCount)
            AddItemsWithCount(IItem item, out int exceededCount, int count = 1, int slotIndex = -1, bool autoStack = true)
        {
            exceededCount = 0;
            var emptySlots = new List<int>();

            // 1. 基本验证
            (bool isValid, AddItemResult validationResult) = ValidateAddItemRequest(item, count);
            if (!isValid)
            {
                OnItemAddResult?.Invoke(item, count, 0, validationResult, emptySlots);
                return (validationResult, 0);
            }

            // 开始批量更新模式
            BeginBatchUpdate();

            try
            {
                int totalAdded = 0;
                int remainingCount = count;
                var affectedSlots = new List<int>(12);
                var aggregatedCacheUpdates = new BatchCacheUpdates(item.ID, item.Type);

                // 将物品添加到待更新列表并缓存物品引用
                _pendingTotalCountUpdates.Add(item.ID);
                _itemRefCache[item.ID] = item;

                // 2. 堆叠处理（仅在未指定槽位且启用自动堆叠时）
                if (slotIndex == -1 && autoStack)
                {
                    (int stackedCount, var stackedSlots, BatchCacheUpdates stackCacheUpdates) =
                        ProcessStackableItems(item, remainingCount);

                    if (stackedCount > 0)
                    {
                        totalAdded += stackedCount;
                        remainingCount -= stackedCount;
                        affectedSlots.AddRange(stackedSlots);
                        aggregatedCacheUpdates.Merge(stackCacheUpdates);

                        if (remainingCount <= 0)
                        {
                            ApplyBatchCacheUpdates(aggregatedCacheUpdates);
                            OnItemAddResult?.Invoke(item, count, totalAdded, AddItemResult.Success, affectedSlots);
                            return (AddItemResult.Success, totalAdded);
                        }
                    }
                }

                // 3. 指定槽位处理
                if (slotIndex >= 0 && remainingCount > 0)
                {
                    (bool success, int addedCount, int newRemaining, BatchCacheUpdates slotCacheUpdates) =
                        ProcessSpecificSlotAddition(item, slotIndex, remainingCount);

                    if (success)
                    {
                        totalAdded += addedCount;
                        remainingCount = newRemaining;
                        affectedSlots.Add(slotIndex);
                        aggregatedCacheUpdates.Merge(slotCacheUpdates);

                        if (remainingCount <= 0)
                        {
                            ApplyBatchCacheUpdates(aggregatedCacheUpdates);
                            OnItemAddResult?.Invoke(item, count, totalAdded, AddItemResult.Success, affectedSlots);
                            return (AddItemResult.Success, totalAdded);
                        }
                    }
                    else
                    {
                        // 指定槽位失败，部分成功时也要应用缓存
                        if (totalAdded > 0)
                        {
                            ApplyBatchCacheUpdates(aggregatedCacheUpdates);
                        }

                        OnItemAddResult?.Invoke(item, totalAdded > 0 ? totalAdded : count,
                            totalAdded > 0 ? totalAdded : 0,
                            AddItemResult.NoSuitableSlotFound, affectedSlots);
                        return (AddItemResult.NoSuitableSlotFound, totalAdded);
                    }
                }

                // 4. 空槽位和新槽位处理
                if (remainingCount > 0)
                {
                    (int emptyAddedCount, var emptySlots2, BatchCacheUpdates emptyCacheUpdates) =
                        ProcessEmptyAndNewSlots(item, remainingCount);

                    totalAdded += emptyAddedCount;
                    remainingCount -= emptyAddedCount;
                    affectedSlots.AddRange(emptySlots2);
                    aggregatedCacheUpdates.Merge(emptyCacheUpdates);
                }

                // 5. 应用所有缓存更新并返回结果
                ApplyBatchCacheUpdates(aggregatedCacheUpdates);

                if (remainingCount > 0)
                {
                    // 部分成功
                    exceededCount = remainingCount;
                    OnItemAddResult?.Invoke(item, totalAdded, totalAdded, AddItemResult.Success, affectedSlots);
                    OnItemAddResult?.Invoke(item, remainingCount, 0, AddItemResult.ContainerIsFull, emptySlots);
                    return (AddItemResult.ContainerIsFull, totalAdded);
                }

                // 完全成功
                OnItemAddResult?.Invoke(item, count, totalAdded, AddItemResult.Success, affectedSlots);
                return (AddItemResult.Success, totalAdded);
            }
            finally
            {
                // 结束批量更新，统一处理所有待更新项
                EndBatchUpdate();
            }
        }

        /// <summary>
        ///     添加物品到容器
        /// </summary>
        /// <param name="item">要添加的物品</param>
        /// <param name="slotIndex">指定的槽位索引，-1表示自动寻找合适的槽位</param>
        /// <param name="autoStack">是否自动堆叠到现有相同物品槽位</param>
        /// <returns>添加结果和成功添加的数量</returns>
        public virtual (AddItemResult result, int actualCount)
            AddItems(IItem item, int slotIndex = -1, bool autoStack = true)
        {
            var emptySlots = new List<int>();
            
            if (item == null)
            {
                OnItemAddResult?.Invoke(null, 0, 0, AddItemResult.ItemIsNull, emptySlots);
                return (AddItemResult.ItemIsNull, 0);
            }
            
            if (item.Count <= 0)
            {
                OnItemAddResult?.Invoke(item, item.Count, 0, AddItemResult.InvalidCount, emptySlots);
                return (AddItemResult.InvalidCount, 0);
            }
            
            // 使用item.Count作为数量，传递给内部实现
            return AddItemsWithCount(item, out _, item.Count, slotIndex, autoStack);
        }

        /// <summary>
        ///     添加指定数量的物品到容器（已废弃）
        /// </summary>
        /// <param name="item">要添加的物品</param>
        /// <param name="count">添加数量</param>
        /// <returns>添加结果和成功添加的数量</returns>
        /// <remarks>
        ///     请迁移到新API：设置 item.Count 后调用 AddItems(item)
        /// </remarks>
        [Obsolete("请使用 item.Count = count; AddItems(item) 替代。此重载将在下一个版本移除。")]
        public virtual (AddItemResult result, int actualCount)
            AddItems(IItem item, int count) 
        {
            if (item != null) item.Count = count;
            return AddItemsWithCount(item, out _, count, -1, true);
        }

        /// <summary>
        ///     通过ItemData模板添加物品到容器
        /// </summary>
        /// <param name="itemData">物品模板数据</param>
        /// <param name="count">添加数量</param>
        /// <param name="slotIndex">指定槽位索引，-1 表示自动查找</param>
        /// <param name="autoStack">是否自动堆叠到现有相同物品槽位</param>
        /// <returns>操作结果和实际添加数量</returns>
        public virtual (AddItemResult result, int actualCount)
            AddItems(ItemData itemData, int count = 1, int slotIndex = -1, bool autoStack = true)
        {
            if (itemData == null)
            {
                return (AddItemResult.ItemIsNull, 0);
            }

            if (ItemFactory == null)
            {
                Debug.LogError("Container未关联ItemFactory，无法通过ItemData创建物品");
                return (AddItemResult.FactoryNotAvailable, 0);
            }

            IItem item = ItemFactory.CreateItem(itemData, count);
            if (item == null)
            {
                return (AddItemResult.FactoryCreateFailed, 0);
            }

            return AddItems(item, slotIndex, autoStack);
        }

        /// <summary>
        ///     通过物品ID添加物品到容器（从注册的ItemData创建）
        /// </summary>
        /// <param name="itemId">物品ID</param>
        /// <param name="count">添加数量</param>
        /// <param name="slotIndex">指定槽位索引，-1 表示自动查找</param>
        /// <param name="autoStack">是否自动堆叠到现有相同物品槽位</param>
        /// <returns>操作结果和实际添加数量</returns>
        public virtual (AddItemResult result, int actualCount)
            AddItems(string itemId, int count = 1, int slotIndex = -1, bool autoStack = true)
        {
            // 注意：这里的itemId是用于通过ItemFactory创建物品的，而不是移除物品的itemId
            // RemoveItem使用的是物品的ID属性，而不是ItemData的itemId
            if (string.IsNullOrEmpty(itemId))
            {
                return (AddItemResult.ItemIsNull, 0);
            }

            if (ItemFactory == null)
            {
                Debug.LogError("Container未关联ItemFactory，无法通过itemId创建物品");
                return (AddItemResult.FactoryNotAvailable, 0);
            }

            IItem item = ItemFactory.CreateItem(itemId, count);
            if (item == null)
            {
                Debug.LogWarning($"物品未注册: {itemId}");
                return (AddItemResult.ItemNotFound, 0);
            }

            return AddItems(item, slotIndex, autoStack);
        }

        /// <summary>
        ///     异步添加物品
        /// </summary>
        public async Task<(AddItemResult result, int addedCount)> AddItemsAsync(
            IItem item, int count, CancellationToken cancellationToken = default)
        {
            if (item != null) item.Count = count;
            if (count > 10000 || _slots.Count > 100000)
            {
                return await Task.Run(() => AddItems(item), cancellationToken);
            }

            return AddItems(item);
        }

        /// <summary>
        ///     批量添加多种物品
        /// </summary>
        /// <param name="itemsToAdd">要添加的物品和数量列表</param>
        /// <returns>每个物品的添加结果</returns>
        public virtual List<(IItem item, AddItemResult result, int addedCount, int exceededCount)> AddItemsBatch(
            List<(IItem item, int count)> itemsToAdd)
        {
            var results = new List<(IItem item, AddItemResult result, int addedCount, int exceededCount)>();

            if (itemsToAdd == null || itemsToAdd.Count == 0)
            {
                return results;
            }

            // 开始批量更新模式
            BeginBatchUpdate();

            try
            {
                foreach ((IItem item, int count) in itemsToAdd)
                {
                    (AddItemResult result, int addedCount) = AddItemsWithCount(item, out int exceededCount, count);
                    results.Add((item, result, addedCount, exceededCount));
                }
            }
            finally
            {
                // 结束批量更新，统一处理所有待更新项
                EndBatchUpdate();
            }

            return results;
        }

        #endregion

        #region 中间处理API

        /// <summary>
        ///     尝试将物品堆叠到已有相同物品的槽位中
        ///     使用 item.CanStack() 进行比较（ID+Type+RuntimeMetadata）
        /// </summary>
        protected virtual (int stackedCount, List<int> affectedSlots, Dictionary<int, (int oldCount, int newCount)>
            changes)
            TryStackItems(IItem item, int remainingCount)
        {
            // 早期退出
            if (remainingCount <= 0 || !item.IsStackable)
            {
                return (0, new(0), new(0));
            }

            int maxStack = item.MaxStackCount;

            if (maxStack <= 1 || !_cacheService.TryGetItemSlotIndices(item.ID, out var indices) || indices.Count == 0)
            {
                return (0, new(0), new(0));
            }

            // 数组池化
            int estimatedSize = Math.Min(indices.Count, 16);
            var affectedSlots = new List<int>(estimatedSize);
            var slotChanges = new Dictionary<int, (int oldCount, int newCount)>(estimatedSize);

            // 收集有效槽位信息
            var stackableSlots = new List<(int index, int space)>(Math.Min(indices.Count, 64));

            foreach (int idx in indices)
            {
                if (idx >= _slots.Count) continue;

                ISlot slot = _slots[idx];
                if (!slot.IsOccupied || slot.Item == null) continue;
                
                // 深度比较
                if (!item.CanStack(slot.Item)) continue;

                int availSpace = maxStack - slot.Item.Count;
                if (availSpace <= 0) continue;

                stackableSlots.Add((idx, availSpace));
            }

            if (stackableSlots.Count > 20)
                // 按可用空间降序排序，优先填满大空间槽位
            {
                stackableSlots.Sort((a, b) => b.space.CompareTo(a.space));
            }

            // 堆叠实现
            int stackedCount = 0;
            int currentRemaining = remainingCount;

            for (int i = 0; i < stackableSlots.Count && currentRemaining > 0; i++)
            {
                (int slotIndex, int availSpace) = stackableSlots[i];
                ISlot slot = _slots[slotIndex];

                int oldCount = slot.Item.Count;
                int actualAdd = Math.Min(availSpace, currentRemaining);

                // 使用新API：直接修改item.Count
                slot.Item.Count = oldCount + actualAdd;
                currentRemaining -= actualAdd;
                stackedCount += actualAdd;
                affectedSlots.Add(slotIndex);
                slotChanges[slotIndex] = (oldCount, slot.Item.Count);
            }

            return (stackedCount, affectedSlots, slotChanges);
        }

        /// <summary>
        ///     准备用于槽位的物品实例
        ///     始终克隆物品
        /// </summary>
        /// <param name="item">原物品</param>
        /// <param name="addCount">要添加到槽位的数量</param>
        /// <param name="remainingCount">剩余要添加的总数量</param>
        /// <returns>用于槽位的物品实例</returns>
        protected virtual IItem PrepareItemForSlot(IItem item, int addCount, int remainingCount)
        {
            IItem itemToAdd = ItemFactory?.CloneItem(item, addCount) ?? item.Clone();
            if (ItemFactory == null)
            {
                // 如果没有ItemFactory，需要手动设置Count、InventoryService并分配UID
                itemToAdd.Count = addCount;
                if (InventoryService != null)
                {
                    if (itemToAdd is Item concreteItem)
                    {
                        concreteItem.InventoryService = InventoryService;
                    }
                    if (itemToAdd.ItemUID == -1)
                    {
                        InventoryService.AssignItemUID(itemToAdd);
                    }
                }
            }
            
            return itemToAdd;
        }

        /// <summary>
        ///     尝试将物品添加到指定槽位
        /// </summary>
        protected virtual (bool success, int addedCount, int remainingCount)
            TryAddToSpecificSlot(IItem item, int slotIndex, int remainingCount)
        {
            if (slotIndex >= _slots.Count) return (false, 0, remainingCount);

            ISlot targetSlot = _slots[slotIndex];

            // 如果槽位已被占用，检查是否可以堆叠物品
            if (targetSlot.IsOccupied)
            {
                if (targetSlot.Item.ID != item.ID || !item.IsStackable) return (false, 0, remainingCount);

                // 计算可添加数量
                int oldCount = targetSlot.Item.Count;
                int canAddCount;

                if (item.MaxStackCount <= 0)
                {
                    canAddCount = remainingCount; // 无限堆叠
                }
                else
                {
                    // 考虑槽位已有数量，确保不超过最大堆叠数
                    canAddCount = Mathf.Min(remainingCount, item.MaxStackCount - targetSlot.Item.Count);
                    if (canAddCount <= 0) return (false, 0, remainingCount); // 已达到最大堆叠数
                }

                targetSlot.Item.Count = oldCount + canAddCount;
                // 触发数量变更
                OnSlotQuantityChanged(slotIndex, targetSlot.Item, oldCount, targetSlot.Item.Count);
                return (true, canAddCount, remainingCount - canAddCount);
            }
            else
            {
                // 槽位为空，直接添加
                if (!targetSlot.CheckSlotCondition(item)) return (false, 0, remainingCount);

                int addCount = item.IsStackable && item.MaxStackCount > 0
                    ? Mathf.Min(remainingCount, item.MaxStackCount)
                    : remainingCount;

                // 根据情况决定是使用原物品引用还是克隆
                IItem itemToAdd = PrepareItemForSlot(item, addCount, remainingCount);
                
                if (targetSlot.SetItem(itemToAdd))
                {
                    // 触发槽位数量变更事件
                    OnSlotQuantityChanged(slotIndex, itemToAdd, 0, addCount);
                    return (true, addCount, remainingCount - addCount);
                }
            }

            return (false, 0, remainingCount);
        }

        protected virtual (bool success, int addedCount, int remainingCount, int slotIndex)
            TryAddToEmptySlot(IItem item, int remainingCount)
        {
            bool isStackable = item.IsStackable;

            int maxStack = item.MaxStackCount;

            // 可以添加的数量
            int addCount = isStackable && maxStack > 0
                ? Math.Min(remainingCount, maxStack)
                : isStackable
                    ? remainingCount
                    : 1;

            var emptySlotIndices = _cacheService.GetEmptySlotIndices();

            // 使用空槽位缓存
            foreach (int i in emptySlotIndices)
            {
                if (i >= _slots.Count) continue;

                ISlot slot = _slots[i];
                if (slot.IsOccupied) continue;

                if (!slot.CheckSlotCondition(item)) continue;

                // 根据情况决定是使用原物品引用还是克隆
                IItem itemToAdd = PrepareItemForSlot(item, addCount, remainingCount);
                
                if (slot.SetItem(itemToAdd))
                {
                    // 批量更新缓存
                    _cacheService.UpdateEmptySlotCache(i, false);
                    _cacheService.UpdateItemSlotIndexCache(itemToAdd.ID, i, true);
                    _cacheService.UpdateItemTypeCache(itemToAdd.Type, i, true);

                    // 触发槽位数量变更事件（从0到addCount）
                    OnSlotQuantityChanged(i, itemToAdd, 0, addCount);
                    return (true, addCount, remainingCount - addCount, i);
                }
            }

            var emptySlotSet = new HashSet<int>(emptySlotIndices);

            for (int i = 0; i < _slots.Count; i++)
            {
                if (emptySlotSet.Contains(i)) continue; // 已在刚才检查过的槽位跳过

                ISlot slot = _slots[i];
                if (slot.IsOccupied || !slot.CheckSlotCondition(item)) continue;

                // 根据情况决定是使用原物品引用还是克隆
                IItem itemToAdd = PrepareItemForSlot(item, addCount, remainingCount);
                
                if (slot.SetItem(itemToAdd))
                {
                    // 更新缓存状态
                    _cacheService.UpdateItemSlotIndexCache(itemToAdd.ID, i, true);
                    _cacheService.UpdateItemTypeCache(itemToAdd.Type, i, true);

                    // 触发槽位数量变更事件
                    OnSlotQuantityChanged(i, itemToAdd, 0, addCount);
                    return (true, addCount, remainingCount - addCount, i);
                }
            }

            return (false, 0, remainingCount, -1);
        }

        /// <summary>
        ///     尝试创建新槽位并添加物品
        /// </summary>
        protected virtual (bool success, int addedCount, int remainingCount, int slotIndex)
            TryAddToNewSlot(IItem item, int remainingCount)
        {
            if (Capacity <= 0 || _slots.Count < Capacity)
            {
                int newSlotIndex = _slots.Count;
                var newSlot = new Slot { Index = newSlotIndex, Container = this };

                int addCount = item.IsStackable && item.MaxStackCount > 0
                    ? Mathf.Min(remainingCount, item.MaxStackCount)
                    : 1; // 不可堆叠物品

                // 根据情况决定是使用原物品引用还是克隆
                IItem itemToAdd = PrepareItemForSlot(item, addCount, remainingCount);
                
                if (newSlot.CheckSlotCondition(itemToAdd) && newSlot.SetItem(itemToAdd))
                {
                    _slots.Add(newSlot);

                    // 更新缓存
                    _cacheService.UpdateItemSlotIndexCache(itemToAdd.ID, newSlotIndex, true);
                    _cacheService.UpdateItemTypeCache(itemToAdd.Type, newSlotIndex, true);

                    // 触发槽位数量变更事件
                    OnSlotQuantityChanged(newSlotIndex, itemToAdd, 0, addCount);
                    return (true, addCount, remainingCount - addCount, newSlotIndex);
                }
            }

            return (false, 0, remainingCount, -1);
        }

        #endregion

        #region IContainer 接口实现补充

        /// <summary>
        ///     移除物品（IContainer 接口实现）
        /// </summary>
        public virtual (RemoveItemResult result, int actualCount) RemoveItems(string itemId, int count = 1)
        {
            RemoveItemResult result = RemoveItem(itemId, count);
            int actualRemoved = count;

            // 如果移除失败，计算实际移除的数量
            if (result != RemoveItemResult.Success) actualRemoved = 0;

            return (result, actualRemoved);
        }

        /// <summary>
        ///     获取指定物品的所有槽位
        /// </summary>
        public virtual List<ISlot> GetItemSlots(string itemId)
        {
            return _slots.Where(s => s.IsOccupied && s.Item?.ID == itemId).ToList();
        }

        /// <summary>
        ///     获取指定索引的槽位
        /// </summary>
        public virtual ISlot GetSlot(int index)
        {
            if (index < 0 || index >= _slots.Count)
            {
                return null;
            }

            return _slots[index];
        }

        /// <summary>
        ///     获取所有被占用的槽位
        /// </summary>
        public virtual List<ISlot> GetOccupiedSlots()
        {
            return _slots.Where(s => s.IsOccupied).ToList();
        }

        /// <summary>
        ///     获取所有空闲槽位
        /// </summary>
        public virtual List<ISlot> GetFreeSlots()
        {
            return _slots.Where(s => !s.IsOccupied).ToList();
        }

        /// <summary>
        ///     清空指定槽位
        /// </summary>
        public virtual bool ClearSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Count)
            {
                return false;
            }

            ISlot slot = _slots[slotIndex];
            if (!slot.IsOccupied)
            {
                return false;
            }

            IItem item = slot.Item;
            int oldCount = slot.Item.Count;
            long itemUID = item.ItemUID;

            if (InventoryService?.CategoryManager != null && itemUID >= 0)
            {
                InventoryService.CategoryManager.DeleteEntity(itemUID);
            }

            slot.ClearSlot();

            // 更新缓存
            _cacheService.UpdateItemSlotIndexCache(item.ID, slotIndex, false);
            _cacheService.UpdateItemTypeCache(item.Type, slotIndex, false);

            // 触发事件
            OnSlotQuantityChanged(slotIndex, item, oldCount, 0);

            return true;
        }

        /// <summary>
        ///     清空所有槽位
        /// </summary>
        public virtual void ClearAllSlots()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].IsOccupied)
                {
                    ClearSlot(i);
                }
            }
        }

        /// <summary>
        ///     根据条件获取符合的槽位
        /// </summary>
        public virtual List<ISlot> GetSlotsByCondition(IItemCondition condition) =>
            condition == null
                ? new()
                : _slots.Where(s => s.IsOccupied && condition.CheckCondition(s.Item)).ToList();

        /// <summary>
        ///     检查物品是否满足容器条件
        /// </summary>
        public virtual bool CheckContainerCondition(IItem item)
        {
            if (item == null || ContainerCondition == null || ContainerCondition.Count == 0)
            {
                return true;
            }

            return ContainerCondition.All(c => c.CheckCondition(item));
        }

        /// <summary>
        ///     设置物品到指定槽位
        /// </summary>
        /// <param name="item">要设置的物品</param>
        /// <param name="slotIndex">目标槽位索引</param>
        /// <returns>是否设置成功</returns>
        public virtual bool SetSlot(IItem item, int slotIndex)
        {
            if (item == null || slotIndex < 0) return false;
            
            // 确保物品有InventoryService引用
            if (item is Item concreteItem && concreteItem.InventoryService == null)
            {
                concreteItem.InventoryService = InventoryService;
            }
            
            // 确保槽位存在
            while (_slots.Count <= slotIndex)
            {
                var newSlot = new Slot { Index = _slots.Count, Container = this };
                _slots.Add(newSlot);
            }
            
            ISlot targetSlot = _slots[slotIndex];
            if (targetSlot.IsOccupied)
            {
                return false; // 槽位已被占用
            }
            
            // 设置物品
            if (targetSlot.SetItem(item))
            {
                // 更新缓存
                _cacheService.UpdateEmptySlotCache(slotIndex, false);
                _cacheService.UpdateItemSlotIndexCache(item.ID, slotIndex, true);
                _cacheService.UpdateItemTypeCache(item.Type, slotIndex, true);
                
                // 触发槽位数量变更事件
                OnSlotQuantityChanged(slotIndex, item, 0, item.Count);
                return true;
            }
            
            return false;
        }

        #endregion
    }
} // namespace EasyPack.InventorySystem