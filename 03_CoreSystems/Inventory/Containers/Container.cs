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
        public string Type { get; set; } = "";
        public int Capacity { get; set; } // -1表示无限容量

        /// <summary>
        /// 已使用的槽位数量
        /// </summary>
        public int UsedSlots => _slots.Count(s => s.IsOccupied);

        /// <summary>
        /// 剩余空闲槽位数量
        /// </summary>
        public int FreeSlots => Capacity < 0 ? int.MaxValue : Capacity - UsedSlots;

        public abstract bool IsGrid { get; } // 子类实现，决定是否为网格容器
        public abstract Vector2 Grid { get; } // 网格容器形状

        public List<IItemCondition> ContainerCondition { get; set; }
        protected List<ISlot> _slots = new();
        public IReadOnlyList<ISlot> Slots => _slots.AsReadOnly();

        // 缓存管理器
        protected readonly ContainerCacheService _cacheService;

        // 查询服务
        private readonly IItemQueryService _queryService;

        public Container(string id, string name, string type, int capacity = -1)
        {
            ID = id;
            Name = name;
            Type = type;
            Capacity = capacity;
            ContainerCondition = new List<IItemCondition>();

            _cacheService = new ContainerCacheService(capacity);
            _queryService = new ItemQueryService(_slots.AsReadOnly(), _cacheService);
            RebuildCaches();
        }
        #endregion

        #region 容器事件

        /// <summary>
        /// 添加物品操作结果事件（统一处理成功和失败）
        /// </summary>
        /// <param name="item">操作的物品</param>
        /// <param name="requestedCount">请求添加的数量</param>
        /// <param name="actualCount">实际添加的数量</param>
        /// <param name="result">操作结果</param>
        /// <param name="affectedSlots">涉及的槽位索引列表（失败时为空列表）</param>
        public event System.Action<IItem, int, int, AddItemResult, List<int>> OnItemAddResult;

        /// <summary>
        /// 移除物品操作结果事件（统一处理成功和失败）
        /// </summary>
        /// <param name="itemId">操作的物品ID</param>
        /// <param name="requestedCount">请求移除的数量</param>
        /// <param name="actualCount">实际移除的数量</param>
        /// <param name="result">操作结果</param>
        /// <param name="affectedSlots">涉及的槽位索引列表（失败时为空列表）</param>
        public event System.Action<string, int, int, RemoveItemResult, List<int>> OnItemRemoveResult;

        /// <summary>
        /// 槽位数量变更事件
        /// </summary>
        /// <param name="slotIndex">变更的槽位索引</param>
        /// <param name="item">变更的物品</param>
        /// <param name="oldCount">原数量</param>
        /// <param name="newCount">新数量</param>
        public event System.Action<int, IItem, int, int> OnSlotCountChanged;

        /// <summary>
        /// 触发槽位物品数量变更事件
        /// </summary>
        protected virtual void OnSlotQuantityChanged(int slotIndex, IItem item, int oldCount, int newCount)
        {
            // 维护可继续堆叠占用槽位计数
            if (item != null && item.IsStackable)
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
        /// 物品总数变更事件
        /// </summary>
        /// <param name="itemId">物品ID</param>
        /// <param name="item">物品引用（可能为null，如果物品已完全移除）</param>
        /// <param name="oldTotalCount">旧总数</param>
        /// <param name="newTotalCount">新总数</param>
        public event System.Action<string, IItem, int, int> OnItemTotalCountChanged;

        private readonly Dictionary<string, int> _itemTotalCounts = new();

        /// <summary>
        /// 触发物品总数变更
        /// </summary>
        protected void TriggerItemTotalCountChanged(string itemId, IItem itemRef = null)
        {
            int newTotal = GetItemTotalCount(itemId);

            int oldTotal = _itemTotalCounts.TryGetValue(itemId, out int value) ? value : 0;

            // 只有总数有变化才继续处理
            if (newTotal == oldTotal) return;

            if (itemRef == null && newTotal > 0)
            {
                itemRef = GetItemReference(itemId);
            }

            OnItemTotalCountChanged?.Invoke(itemId, itemRef, oldTotal, newTotal);

            if (newTotal > 0)
                _itemTotalCounts[itemId] = newTotal;
            else
                _itemTotalCounts.Remove(itemId);
        }

        #endregion

        #region 批操作
        private readonly HashSet<string> _pendingTotalCountUpdates = new();
        private readonly Dictionary<string, IItem> _itemRefCache = new();
        private int _batchDepth = 0;

        /// <summary>
        /// 开始批量操作模式
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
        /// 结束批量操作模式并处理所有待更新项
        /// </summary>
        protected void EndBatchUpdate()
        {
            if (_batchDepth <= 0)
                return;

            _batchDepth--;
            if (_batchDepth == 0)
            {
                if (_pendingTotalCountUpdates.Count > 0)
                {
                    foreach (string itemId in _pendingTotalCountUpdates)
                    {
                        TriggerItemTotalCountChanged(itemId,
                            _itemRefCache.TryGetValue(itemId, out var itemRef) ? itemRef : null);
                    }

                    _pendingTotalCountUpdates.Clear();
                    _itemRefCache.Clear();
                }
            }
        }
        #endregion

        #region 状态检查
        private int _notFullStackSlotsCount = 0;
        // <summary>
        /// 检查容器是否已满
        /// 仅当所有槽位都被占用，且每个占用的槽位物品都不可堆叠或已达到堆叠上限时，容器才被认为是满的
        /// </summary>
        public virtual bool Full
        {
            get
            {
                if (Capacity < 0)
                    return false;

                if (_slots.Count < Capacity)
                    return false;

                if (_cacheService.GetEmptySlotIndices().Count > 0)
                    return false;

                if (_notFullStackSlotsCount > 0)
                    return false;

                return true;
            }
        }

        /// <summary>
        /// 检查物品是否满足容器条件
        /// </summary>
        public bool ValidateItemCondition(IItem item)
        {
            if (item == null)
            {
                Debug.LogWarning("ValidateItemCondition: item is null.");
                return false;
            }

            if (ContainerCondition != null && ContainerCondition.Count > 0)
            {
                foreach (var condition in ContainerCondition)
                {
                    if (!condition.CheckCondition(item))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 检查是否可以添加物品到容器
        /// </summary>
        /// <param name="item">要添加的物品</param>
        /// <returns>添加结果，如果可以添加返回Success，否则返回对应的错误原因</returns>
        protected virtual AddItemResult CanAddItem(IItem item)
        {
            if (item == null)
                return AddItemResult.ItemIsNull;

            if (!ValidateItemCondition(item))
                return AddItemResult.ItemConditionNotMet;

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
                                var slot = _slots[slotIndex];
                                if (slot.IsOccupied && slot.Item.ID == item.ID &&
                                    slot.Item.IsStackable && (slot.Item.MaxStackCount <= 0 || slot.ItemCount < slot.Item.MaxStackCount))
                                {
                                    return AddItemResult.Success;
                                }
                            }
                        }
                    }
                    return AddItemResult.StackLimitReached;
                }
                else
                {
                    return AddItemResult.ContainerIsFull;
                }
            }

            return AddItemResult.Success;
        }
        #endregion

        #region 缓存服务
        /// <summary>
        /// 初始化或重建所有缓存
        /// </summary>
        public void RebuildCaches()
        {
            _cacheService.RebuildCaches(_slots.AsReadOnly());

            // 重建可继续堆叠占用槽位计数
            _notFullStackSlotsCount = 0;
            for (int i = 0; i < _slots.Count; i++)
            {
                var s = _slots[i];
                if (s.IsOccupied && s.Item != null && s.Item.IsStackable)
                {
                    if (s.Item.MaxStackCount <= 0 || s.ItemCount < s.Item.MaxStackCount)
                        _notFullStackSlotsCount++;
                }
            }
        }

        /// <summary>
        /// 清除缓存中的无效条目
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
            foreach (var (slotIndex, isAdding) in updates.SlotIndexUpdates)
            {
                _cacheService.UpdateItemSlotIndexCache(updates.ItemId, slotIndex, isAdding);
            }

            // 批量更新类型索引缓存
            foreach (var (slotIndex, isAdding) in updates.TypeIndexUpdates)
            {
                _cacheService.UpdateItemTypeCache(updates.ItemType, slotIndex, isAdding);
            }

            // 批量更新空槽位缓存
            foreach (var (slotIndex, isEmpty) in updates.EmptySlotUpdates)
            {
                _cacheService.UpdateEmptySlotCache(slotIndex, isEmpty);
            }
        }
        #endregion

        #region 查询服务

        public bool HasItem(string itemId) => _queryService.HasItem(itemId);
        public int GetItemTotalCount(string itemId) => _queryService.GetItemTotalCount(itemId);
        public bool HasEnoughItems(string itemId, int requiredCount) => _queryService.HasEnoughItems(itemId, requiredCount);
        public List<int> FindSlotIndices(string itemId) => _queryService.FindSlotIndices(itemId);
        public int FindFirstSlotIndex(string itemId) => _queryService.FindFirstSlotIndex(itemId);

        public List<(int slotIndex, IItem item, int count)> GetItemsByType(string itemType)
            => _queryService.GetItemsByType(itemType);
        public List<(int slotIndex, IItem item, int count)> GetItemsByAttribute(string attributeName, object attributeValue)
            => _queryService.GetItemsByAttribute(attributeName, attributeValue);
        public List<(int slotIndex, IItem item, int count)> GetItemsByName(string namePattern)
            => _queryService.GetItemsByName(namePattern);
        public List<(int slotIndex, IItem item, int count)> GetItemsWhere(Func<IItem, bool> condition)
            => _queryService.GetItemsWhere(condition);

        public Dictionary<string, int> GetAllItemCountsDict() => _queryService.GetAllItemCountsDict();
        public List<(int slotIndex, IItem item, int count)> GetAllItems() => _queryService.GetAllItems();
        public int GetUniqueItemCount() => _queryService.GetUniqueItemCount();
        public bool IsEmpty() => _queryService.IsEmpty();
        public float GetTotalWeight() => _queryService.GetTotalWeight();

        private IItem GetItemReference(string itemId) => _queryService.GetItemReference(itemId);

        #endregion

        #region 移除物品
        /// <summary>
        /// 移除指定ID的物品
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
            if (_cacheService.TryGetItemSlotIndices(itemId, out var indices) && indices != null && indices.Count > 0)
            {
                foreach (int i in indices)
                {
                    if (remainingCount <= 0) break;
                    if (i < 0 || i >= _slots.Count) continue;

                    var slot = _slots[i];
                    if (!slot.IsOccupied || slot.Item == null || slot.Item.ID != itemId) continue;

                    int removeAmount = Mathf.Min(slot.ItemCount, remainingCount);
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

                foreach (var (slot, removeAmount, slotIndex) in removals)
                {
                    int oldCount = slot.ItemCount;
                    var item = slot.Item;
                    string itemType = item.Type;

                    if (removeAmount == slot.ItemCount)
                    {
                        slot.ClearSlot();
                        _cacheService.UpdateEmptySlotCache(slotIndex, true);
                        _cacheService.UpdateItemSlotIndexCache(itemId, slotIndex, false);
                        _cacheService.UpdateItemTypeCache(itemType, slotIndex, false);
                        if (!itemCompletelyRemoved)
                        {
                            itemCompletelyRemoved = !_cacheService.HasItemInCache(itemId);
                        }
                    }
                    else
                    {
                        slot.SetItem(slot.Item, slot.ItemCount - removeAmount);
                    }

                    // 更新数量缓存
                    _cacheService.UpdateItemCountCache(itemId, -removeAmount);

                    affectedSlots.Add(slotIndex);

                    // 槽位物品数量变更事件
                    OnSlotQuantityChanged(slotIndex, item, oldCount, slot.ItemCount);
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
        /// 从指定槽位移除物品
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
                OnItemRemoveResult?.Invoke(expectedItemId ?? "unknown", count, 0, RemoveItemResult.SlotNotFound, emptySlots);
                return RemoveItemResult.SlotNotFound;
            }

            var slot = _slots[index];

            // 检查槽位是否有物品
            if (!slot.IsOccupied || slot.Item == null)
            {
                OnItemRemoveResult?.Invoke(expectedItemId ?? "unknown", count, 0, RemoveItemResult.ItemNotFound, emptySlots);
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
            if (slot.ItemCount < count)
            {
                OnItemRemoveResult?.Invoke(itemId, count, 0, RemoveItemResult.InsufficientQuantity, emptySlots);
                return RemoveItemResult.InsufficientQuantity;
            }

            // 记录旧数量
            int oldCount = slot.ItemCount;

            // 所有检查都通过，执行移除操作
            if (slot.ItemCount - count <= 0)
            {
                slot.ClearSlot();
                _cacheService.UpdateEmptySlotCache(index, true);
                _cacheService.UpdateItemSlotIndexCache(itemId, index, false);
                _cacheService.UpdateItemTypeCache(itemType, index, false);
            }
            else
            {
                slot.SetItem(item, slot.ItemCount - count);
            }

            // 更新数量缓存
            _cacheService.UpdateItemCountCache(itemId, -count);

            // 触发物品数量变更事件
            OnSlotQuantityChanged(index, item, oldCount, slot.ItemCount);

            // 触发物品移除事件
            var affectedSlots = new List<int> { index };
            OnItemRemoveResult?.Invoke(itemId, count, count, RemoveItemResult.Success, affectedSlots);
            TriggerItemTotalCountChanged(itemId, item);

            return RemoveItemResult.Success;
        }
        #endregion

        #region 添加物品
        /// <summary>
        /// 添加指定数量的物品到容器
        /// </summary>
        /// <param name="item">要添加的物品</param>
        /// <param name="count">要添加的数量</param>
        /// <param name="slotIndex">指定的槽位索引，-1表示自动寻找合适的槽位</param>
        /// <param name="exceededCount">超出堆叠上限的数量</param>
        /// <returns>添加结果和成功添加的数量</returns>
        public virtual (AddItemResult result, int addedCount)
        AddItemsWithCount(IItem item, out int exceededCount, int count = 1, int slotIndex = -1)
        {
            exceededCount = 0;
            List<int> affectedSlots = new(12);
            var emptySlots = new List<int>();

            // 基本验证
            if (item == null)
            {
                OnItemAddResult?.Invoke(item, count, 0, AddItemResult.ItemIsNull, emptySlots);
                return (AddItemResult.ItemIsNull, 0);
            }

            if (count <= 0)
                return (AddItemResult.AddNothingLOL, 0);

            if (!ValidateItemCondition(item))
            {
                OnItemAddResult?.Invoke(item, count, 0, AddItemResult.ItemConditionNotMet, emptySlots);
                return (AddItemResult.ItemConditionNotMet, 0);
            }
            // 开始批量更新模式
            BeginBatchUpdate();

            try
            {
                int totalAdded = 0;
                int remainingCount = count;

                // 批量缓存更新数据
                var cacheUpdates = new BatchCacheUpdates(item.ID, item.Type);

                // 将物品添加到待更新列表并缓存物品引用
                _pendingTotalCountUpdates.Add(item.ID);
                _itemRefCache[item.ID] = item;

                // 1. 堆叠处理
                if (item.IsStackable && slotIndex == -1)
                {
                    var (stackedCount, stackedSlots, slotChanges) = TryStackItems(item, remainingCount);

                    if (stackedCount > 0)
                    {
                        totalAdded += stackedCount;
                        remainingCount -= stackedCount;
                        affectedSlots.AddRange(stackedSlots);

                        cacheUpdates.TotalCountDelta += stackedCount;

                        // 批量事件触发
                        foreach (var change in slotChanges)
                        {
                            int slotIdx = change.Key;
                            var slot = _slots[slotIdx];
                            OnSlotQuantityChanged(slotIdx, slot.Item, change.Value.oldCount, change.Value.newCount);
                        }

                        if (remainingCount <= 0)
                        {
                            ApplyBatchCacheUpdates(cacheUpdates);
                            OnItemAddResult?.Invoke(item, count, totalAdded, AddItemResult.Success, affectedSlots);
                            return (AddItemResult.Success, totalAdded);
                        }
                    }
                }

                // 2. 指定槽位处理
                if (slotIndex >= 0 && remainingCount > 0)
                {
                    var (success, addedCount, newRemaining) = TryAddToSpecificSlot(item, slotIndex, remainingCount);

                    if (success)
                    {
                        totalAdded += addedCount;
                        remainingCount = newRemaining;
                        affectedSlots.Add(slotIndex);

                        // 缓存更新
                        cacheUpdates.TotalCountDelta += addedCount;
                        cacheUpdates.SlotIndexUpdates.Add((slotIndex, true));
                        cacheUpdates.TypeIndexUpdates.Add((slotIndex, true));
                        cacheUpdates.EmptySlotUpdates.Add((slotIndex, false));

                        if (remainingCount <= 0)
                        {
                            ApplyBatchCacheUpdates(cacheUpdates);
                            OnItemAddResult?.Invoke(item, count, totalAdded, AddItemResult.Success, affectedSlots);
                            return (AddItemResult.Success, totalAdded);
                        }
                    }
                    else
                    {
                        if (totalAdded > 0)
                        {
                            ApplyBatchCacheUpdates(cacheUpdates);
                            OnItemAddResult?.Invoke(item, count, totalAdded, AddItemResult.Success, affectedSlots);
                        }
                        OnItemAddResult?.Invoke(item, remainingCount, 0, AddItemResult.NoSuitableSlotFound, emptySlots);
                        return (AddItemResult.NoSuitableSlotFound, totalAdded);
                    }
                }

                // 3. 空槽位和新槽位处理
                while (remainingCount > 0)
                {
                    var (emptySlotSuccess, emptyAddedCount, emptyRemaining, emptySlotIndex) =
                        TryAddToEmptySlot(item, remainingCount);

                    if (emptySlotSuccess)
                    {
                        totalAdded += emptyAddedCount;
                        remainingCount = emptyRemaining;
                        affectedSlots.Add(emptySlotIndex);

                        cacheUpdates.TotalCountDelta += emptyAddedCount;
                        cacheUpdates.SlotIndexUpdates.Add((emptySlotIndex, true));
                        cacheUpdates.TypeIndexUpdates.Add((emptySlotIndex, true));

                        if (remainingCount <= 0)
                        {
                            ApplyBatchCacheUpdates(cacheUpdates);
                            OnItemAddResult?.Invoke(item, count, totalAdded, AddItemResult.Success, affectedSlots);
                            return (AddItemResult.Success, totalAdded);
                        }
                        continue;
                    }

                    var (newSlotSuccess, newAddedCount, newRemaining, newSlotIndex) =
                        TryAddToNewSlot(item, remainingCount);

                    if (newSlotSuccess)
                    {
                        totalAdded += newAddedCount;
                        remainingCount = newRemaining;
                        affectedSlots.Add(newSlotIndex);

                        cacheUpdates.TotalCountDelta += newAddedCount;
                        cacheUpdates.SlotIndexUpdates.Add((newSlotIndex, true));
                        cacheUpdates.TypeIndexUpdates.Add((newSlotIndex, true));

                        if (remainingCount <= 0)
                        {
                            ApplyBatchCacheUpdates(cacheUpdates);
                            OnItemAddResult?.Invoke(item, count, totalAdded, AddItemResult.Success, affectedSlots);
                            return (AddItemResult.Success, totalAdded);
                        }
                        continue;
                    }

                    // 无法继续添加
                    if (totalAdded > 0)
                    {
                        exceededCount = remainingCount;
                        ApplyBatchCacheUpdates(cacheUpdates);
                        OnItemAddResult?.Invoke(item, count, totalAdded, AddItemResult.Success, affectedSlots);
                        OnItemAddResult?.Invoke(item, remainingCount, 0, AddItemResult.ContainerIsFull, emptySlots);
                        return (AddItemResult.ContainerIsFull, totalAdded);
                    }
                    else
                    {
                        exceededCount = count;
                        bool noEmptySlots = _cacheService.GetEmptySlotIndices().Count == 0;
                        AddItemResult result = noEmptySlots ? AddItemResult.ContainerIsFull : AddItemResult.NoSuitableSlotFound;
                        OnItemAddResult?.Invoke(item, count, 0, result, emptySlots);
                        return (result, 0);
                    }
                }

                ApplyBatchCacheUpdates(cacheUpdates);
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
        /// 添加指定数量的物品到容器
        /// </summary>
        /// <param name="item">要添加的物品</param>
        /// <param name="count">要添加的数量</param>
        /// <param name="slotIndex">指定的槽位索引，-1表示自动寻找合适的槽位</param>
        /// <returns>添加结果和成功添加的数量</returns>
        public virtual (AddItemResult result, int actualCount) AddItems(IItem item, int count = 1, int slotIndex = -1)
        {
            return AddItemsWithCount(item, out _, count, slotIndex);
        }

        /// <summary>
        /// 异步添加物品
        /// </summary>
        public async Task<(AddItemResult result, int addedCount)> AddItemsAsync(
            IItem item, int count, CancellationToken cancellationToken = default)
        {
            if (count > 10000 || _slots.Count > 100000)
            {
                return await Task.Run(() => AddItems(item, count), cancellationToken);
            }

            return AddItems(item, count);
        }

        /// <summary>
        /// 批量添加多种物品
        /// </summary>
        /// <param name="itemsToAdd">要添加的物品和数量列表</param>
        /// <returns>每个物品的添加结果</returns>
        public virtual List<(IItem item, AddItemResult result, int addedCount, int exceededCount)> AddItemsBatch(
            List<(IItem item, int count)> itemsToAdd)
        {
            var results = new List<(IItem item, AddItemResult result, int addedCount, int exceededCount)>();

            if (itemsToAdd == null || itemsToAdd.Count == 0)
                return results;

            // 开始批量更新模式
            BeginBatchUpdate();

            try
            {
                foreach (var (item, count) in itemsToAdd)
                {
                    var (result, addedCount) = AddItemsWithCount(item, out int exceededCount, count);
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
        /// 尝试将物品堆叠到已有相同物品的槽位中 - 极致优化版本
        /// </summary>
        protected virtual (int stackedCount, List<int> affectedSlots, Dictionary<int, (int oldCount, int newCount)> changes)
            TryStackItems(IItem item, int remainingCount)
        {
            // 早期退出
            if (remainingCount <= 0 || !item.IsStackable)
                return (0, new List<int>(0), new Dictionary<int, (int oldCount, int newCount)>(0));

            int maxStack = item.MaxStackCount;

            if (maxStack <= 1 || !_cacheService.TryGetItemSlotIndices(item.ID, out var indices) || indices.Count == 0)
                return (0, new List<int>(0), new Dictionary<int, (int oldCount, int newCount)>(0));

            // 数组池化
            int estimatedSize = Math.Min(indices.Count, 16);
            var affectedSlots = new List<int>(estimatedSize);
            var slotChanges = new Dictionary<int, (int oldCount, int newCount)>(estimatedSize);

            // 收集有效槽位信息
            bool isInfiniteStack = maxStack <= 0;
            var stackableSlots = new List<(int index, int space)>(Math.Min(indices.Count, 64));

            foreach (int idx in indices)
            {
                if (idx >= _slots.Count) continue;

                var slot = _slots[idx];
                if (!slot.IsOccupied || slot.Item == null) continue;

                int availSpace = isInfiniteStack ? remainingCount : (maxStack - slot.ItemCount);
                if (availSpace <= 0) continue;

                stackableSlots.Add((idx, availSpace));
            }

            if (stackableSlots.Count > 20)
            {
                // 按可用空间降序排序，优先填满大空间槽位
                stackableSlots.Sort((a, b) => b.space.CompareTo(a.space));
            }

            // 堆叠实现
            int stackedCount = 0;
            int currentRemaining = remainingCount;

            for (int i = 0; i < stackableSlots.Count && currentRemaining > 0; i++)
            {
                var (slotIndex, availSpace) = stackableSlots[i];
                var slot = _slots[slotIndex];

                int oldCount = slot.ItemCount;
                int actualAdd = Math.Min(availSpace, currentRemaining);

                if (slot.SetItem(slot.Item, oldCount + actualAdd))
                {
                    currentRemaining -= actualAdd;
                    stackedCount += actualAdd;
                    affectedSlots.Add(slotIndex);
                    slotChanges[slotIndex] = (oldCount, slot.ItemCount);
                }
            }

            return (stackedCount, affectedSlots, slotChanges);
        }

        /// <summary>
        /// 尝试将物品添加到指定槽位
        /// </summary>
        protected virtual (bool success, int addedCount, int remainingCount)
        TryAddToSpecificSlot(IItem item, int slotIndex, int remainingCount)
        {
            if (slotIndex >= _slots.Count)
            {
                return (false, 0, remainingCount);
            }

            var targetSlot = _slots[slotIndex];

            // 如果槽位已被占用，检查是否可以堆叠物品
            if (targetSlot.IsOccupied)
            {
                if (targetSlot.Item.ID != item.ID)
                {
                    return (false, 0, remainingCount);
                }

                if (!item.IsStackable)
                {
                    return (false, 0, remainingCount);
                }

                // 计算可添加数量
                int oldCount = targetSlot.ItemCount;
                int canAddCount;

                if (item.MaxStackCount <= 0)
                {
                    canAddCount = remainingCount; // 无限堆叠
                }
                else
                {
                    // 考虑槽位已有数量，确保不超过最大堆叠数
                    canAddCount = Mathf.Min(remainingCount, item.MaxStackCount - targetSlot.ItemCount);
                    if (canAddCount <= 0)
                    {
                        return (false, 0, remainingCount); // 已达到最大堆叠数
                    }
                }

                // 设置物品
                if (targetSlot.SetItem(targetSlot.Item, targetSlot.ItemCount + canAddCount))
                {
                    // 触发数量变更
                    OnSlotQuantityChanged(slotIndex, targetSlot.Item, oldCount, targetSlot.ItemCount);
                    return (true, canAddCount, remainingCount - canAddCount);
                }
            }
            else
            {
                // 槽位为空，直接添加
                if (!targetSlot.CheckSlotCondition(item))
                {
                    return (false, 0, remainingCount);
                }

                int addCount = item.IsStackable && item.MaxStackCount > 0 ?
                               Mathf.Min(remainingCount, item.MaxStackCount) :
                               remainingCount;

                if (targetSlot.SetItem(item, addCount))
                {
                    // 触发数量变更
                    OnSlotQuantityChanged(slotIndex, targetSlot.Item, 0, targetSlot.ItemCount);
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
                : (isStackable ? remainingCount : 1);

            var emptySlotIndices = _cacheService.GetEmptySlotIndices();

            // 使用空槽位缓存
            foreach (int i in emptySlotIndices)
            {
                if (i >= _slots.Count) continue;

                var slot = _slots[i];
                if (slot.IsOccupied) continue;

                if (!slot.CheckSlotCondition(item)) continue;

                if (slot.SetItem(item, addCount))
                {
                    // 批量更新缓存
                    _cacheService.UpdateEmptySlotCache(i, false);
                    _cacheService.UpdateItemSlotIndexCache(item.ID, i, true);
                    _cacheService.UpdateItemTypeCache(item.Type, i, true);

                    OnSlotQuantityChanged(i, slot.Item, 0, slot.ItemCount);
                    return (true, addCount, remainingCount - addCount, i);
                }
            }

            var emptySlotSet = new HashSet<int>(emptySlotIndices);

            for (int i = 0; i < _slots.Count; i++)
            {
                if (emptySlotSet.Contains(i)) continue; // 已在刚才检查过的槽位跳过

                var slot = _slots[i];
                if (slot.IsOccupied || !slot.CheckSlotCondition(item)) continue;

                if (slot.SetItem(item, addCount))
                {
                    // 更新缓存状态
                    _cacheService.UpdateItemSlotIndexCache(item.ID, i, true);
                    _cacheService.UpdateItemTypeCache(item.Type, i, true);

                    OnSlotQuantityChanged(i, slot.Item, 0, slot.ItemCount);
                    return (true, addCount, remainingCount - addCount, i);
                }
            }

            return (false, 0, remainingCount, -1);
        }

        /// <summary>
        /// 尝试创建新槽位并添加物品
        /// </summary>
        protected virtual (bool success, int addedCount, int remainingCount, int slotIndex)
            TryAddToNewSlot(IItem item, int remainingCount)
        {
            if (Capacity <= 0 || _slots.Count < Capacity)
            {
                int newSlotIndex = _slots.Count;
                var newSlot = new Slot
                {
                    Index = newSlotIndex,
                    Container = this
                };

                int addCount = item.IsStackable && item.MaxStackCount > 0 ?
                              Mathf.Min(remainingCount, item.MaxStackCount) :
                              1; // 不可堆叠物品

                if (newSlot.CheckSlotCondition(item) && newSlot.SetItem(item, addCount))
                {
                    _slots.Add(newSlot);

                    // 更新缓存
                    _cacheService.UpdateItemSlotIndexCache(item.ID, newSlotIndex, true);
                    _cacheService.UpdateItemTypeCache(item.Type, newSlotIndex, true);

                    // 触发数量变更
                    OnSlotQuantityChanged(newSlotIndex, newSlot.Item, 0, newSlot.ItemCount);
                    return (true, addCount, remainingCount - addCount, newSlotIndex);
                }
            }

            return (false, 0, remainingCount, -1);
        }
        #endregion

        #region IContainer 接口实现补充

        /// <summary>
        /// 移除物品（IContainer 接口实现）
        /// </summary>
        public virtual (RemoveItemResult result, int actualCount) RemoveItems(string itemId, int count = 1)
        {
            var result = RemoveItem(itemId, count);
            int actualRemoved = count;

            // 如果移除失败，计算实际移除的数量
            if (result != RemoveItemResult.Success)
            {
                actualRemoved = 0;
            }

            return (result, actualRemoved);
        }

        /// <summary>
        /// 获取指定物品的所有槽位
        /// </summary>
        public virtual List<ISlot> GetItemSlots(string itemId)
        {
            return _slots.Where(s => s.IsOccupied && s.Item?.ID == itemId).ToList();
        }

        /// <summary>
        /// 获取指定索引的槽位
        /// </summary>
        public virtual ISlot GetSlot(int index)
        {
            if (index < 0 || index >= _slots.Count)
                return null;
            return _slots[index];
        }

        /// <summary>
        /// 获取所有被占用的槽位
        /// </summary>
        public virtual List<ISlot> GetOccupiedSlots()
        {
            return _slots.Where(s => s.IsOccupied).ToList();
        }

        /// <summary>
        /// 获取所有空闲槽位
        /// </summary>
        public virtual List<ISlot> GetFreeSlots()
        {
            return _slots.Where(s => !s.IsOccupied).ToList();
        }

        /// <summary>
        /// 清空指定槽位
        /// </summary>
        public virtual bool ClearSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Count)
                return false;

            var slot = _slots[slotIndex];
            if (!slot.IsOccupied)
                return false;

            var item = slot.Item;
            var oldCount = slot.ItemCount;

            slot.ClearSlot();

            // 更新缓存
            _cacheService.UpdateItemSlotIndexCache(item.ID, slotIndex, false);
            _cacheService.UpdateItemTypeCache(item.Type, slotIndex, false);

            // 触发事件
            OnSlotQuantityChanged(slotIndex, item, oldCount, 0);

            return true;
        }

        /// <summary>
        /// 清空所有槽位
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
        /// 根据条件获取符合的槽位
        /// </summary>
        public virtual List<ISlot> GetSlotsByCondition(IItemCondition condition)
        {
            if (condition == null)
                return new List<ISlot>();

            return _slots.Where(s => s.IsOccupied && condition.CheckCondition(s.Item)).ToList();
        }

        /// <summary>
        /// 检查物品是否满足容器条件
        /// </summary>
        public virtual bool CheckContainerCondition(IItem item)
        {
            if (item == null || ContainerCondition == null || ContainerCondition.Count == 0)
                return true;

            return ContainerCondition.All(c => c.CheckCondition(item));
        }

        #endregion
    }
} // namespace EasyPack.InventorySystem
