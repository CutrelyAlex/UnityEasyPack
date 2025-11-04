using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyPack.InventorySystem
{
    /// <summary>
    /// 多个容器管理的系统
    /// </summary>
    public partial class InventoryManager
    {
        #region 跨容器物品操作

        /// <summary>
        /// 移动操作请求结构
        /// </summary>
        public struct MoveRequest
        {
            public string FromContainerId;
            public int FromSlot;
            public string ToContainerId;
            public int ToSlot;
            public int Count;
            public string ExpectedItemId;

            public MoveRequest(string fromContainerId, int fromSlot, string toContainerId, int toSlot = -1, int count = -1, string expectedItemId = null)
            {
                FromContainerId = fromContainerId;
                FromSlot = fromSlot;
                ToContainerId = toContainerId;
                ToSlot = toSlot;
                Count = count;
                ExpectedItemId = expectedItemId;
            }
        }

        /// <summary>
        /// 移动操作结果
        /// </summary>
        public enum MoveResult
        {
            Success,
            SourceContainerNotFound,
            TargetContainerNotFound,
            SourceSlotEmpty,
            SourceSlotNotFound,
            TargetSlotNotFound,
            ItemNotFound,
            InsufficientQuantity,
            TargetContainerFull,
            ItemConditionNotMet,
            Failed
        }

        /// <summary>
        /// 容器间物品移动
        /// </summary>
        /// <param name="fromContainerId">源容器ID</param>
        /// <param name="fromSlot">源槽位索引</param>
        /// <param name="toContainerId">目标容器ID</param>
        /// <param name="toSlot">目标槽位索引，-1表示自动寻找</param>
        /// <returns>移动结果</returns>
        public MoveResult MoveItem(string fromContainerId, int fromSlot, string toContainerId, int toSlot = -1)
        {
            try
            {
                var sourceContainer = GetContainer(fromContainerId);
                if (sourceContainer == null)
                    return MoveResult.SourceContainerNotFound;

                var targetContainer = GetContainer(toContainerId);
                if (targetContainer == null)
                    return MoveResult.TargetContainerNotFound;

                if (fromSlot < 0 || fromSlot >= sourceContainer.Slots.Count)
                    return MoveResult.SourceSlotNotFound;

                var sourceSlot = sourceContainer.Slots[fromSlot];
                if (!sourceSlot.IsOccupied || sourceSlot.Item == null)
                    return MoveResult.SourceSlotEmpty;

                var item = sourceSlot.Item;
                int itemCount = sourceSlot.ItemCount;

                // 检查全局条件
                if (!ValidateGlobalItemConditions(item))
                    return MoveResult.ItemConditionNotMet;

                // 尝试添加到目标容器
                var (addResult, addedCount) = targetContainer.AddItems(item, itemCount, toSlot);

                if (addResult == AddItemResult.Success && addedCount > 0)
                {
                    // 从源容器移除
                    var removeResult = sourceContainer.RemoveItemAtIndex(fromSlot, addedCount, item.ID);

                    if (removeResult == RemoveItemResult.Success)
                    {
                        OnItemMoved?.Invoke(fromContainerId, fromSlot, toContainerId, item, addedCount);
                        return MoveResult.Success;
                    }
                }

                return addResult switch
                {
                    AddItemResult.ContainerIsFull => MoveResult.TargetContainerFull,
                    AddItemResult.ItemConditionNotMet => MoveResult.ItemConditionNotMet,
                    AddItemResult.SlotNotFound => MoveResult.TargetSlotNotFound,
                    _ => MoveResult.Failed
                };
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[InventoryManager] 物品移动失败：{ex.Message}");
                return MoveResult.Failed;
            }
        }

        /// <summary>
        /// 指定数量物品转移
        /// </summary>
        /// <param name="itemId">物品ID</param>
        /// <param name="count">转移数量</param>
        /// <param name="fromContainerId">源容器ID</param>
        /// <param name="toContainerId">目标容器ID</param>
        /// <returns>转移结果和实际转移数量</returns>
        public (MoveResult result, int transferredCount) TransferItems(string itemId, int count, string fromContainerId, string toContainerId)
        {
            try
            {
                if (string.IsNullOrEmpty(itemId))
                    return (MoveResult.ItemNotFound, 0);

                var sourceContainer = GetContainer(fromContainerId);
                if (sourceContainer == null)
                    return (MoveResult.SourceContainerNotFound, 0);

                var targetContainer = GetContainer(toContainerId);
                if (targetContainer == null)
                    return (MoveResult.TargetContainerNotFound, 0);

                if (!sourceContainer.HasItem(itemId))
                    return (MoveResult.ItemNotFound, 0);

                int availableCount = sourceContainer.GetItemTotalCount(itemId);
                if (availableCount < count)
                    return (MoveResult.InsufficientQuantity, 0);

                // 获取物品引用
                IItem item = null;
                foreach (var slot in sourceContainer.Slots)
                {
                    if (slot.IsOccupied && slot.Item?.ID == itemId)
                    {
                        item = slot.Item;
                        break;
                    }
                }

                if (item == null)
                    return (MoveResult.ItemNotFound, 0);

                // 检查全局条件
                if (!ValidateGlobalItemConditions(item))
                    return (MoveResult.ItemConditionNotMet, 0);

                // 尝试添加到目标容器
                var (addResult, addedCount) = targetContainer.AddItems(item, count);

                if (addResult == AddItemResult.Success && addedCount > 0)
                {
                    // 从源容器移除
                    var removeResult = sourceContainer.RemoveItem(itemId, addedCount);

                    if (removeResult == RemoveItemResult.Success)
                    {
                        OnItemsTransferred?.Invoke(fromContainerId, toContainerId, itemId, addedCount);
                        return (MoveResult.Success, addedCount);
                    }
                }

                return addResult switch
                {
                    AddItemResult.ContainerIsFull => (MoveResult.TargetContainerFull, 0),
                    AddItemResult.ItemConditionNotMet => (MoveResult.ItemConditionNotMet, 0),
                    _ => (MoveResult.Failed, 0)
                };
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[InventoryManager] 物品转移失败：{ex.Message}");
                return (MoveResult.Failed, 0);
            }
        }

        /// <summary>
        /// 自动寻找最佳位置转移物品
        /// </summary>
        /// <param name="itemId">物品ID</param>
        /// <param name="fromContainerId">源容器ID</param>
        /// <param name="toContainerId">目标容器ID</param>
        /// <returns>转移结果和实际转移数量</returns>
        public (MoveResult result, int transferredCount) AutoMoveItem(string itemId, string fromContainerId, string toContainerId)
        {
            if (string.IsNullOrEmpty(itemId))
                return (MoveResult.ItemNotFound, 0);

            var sourceContainer = GetContainer(fromContainerId);
            if (sourceContainer == null)
                return (MoveResult.SourceContainerNotFound, 0);

            var targetContainer = GetContainer(toContainerId);
            if (targetContainer == null)
                return (MoveResult.TargetContainerNotFound, 0);

            if (!sourceContainer.HasItem(itemId))
                return (MoveResult.ItemNotFound, 0);

            int totalCount = sourceContainer.GetItemTotalCount(itemId);
            return TransferItems(itemId, totalCount, fromContainerId, toContainerId);
        }

        /// <summary>
        /// 批量移动操作
        /// </summary>
        /// <param name="requests">移动请求列表</param>
        /// <returns>每个请求的执行结果</returns>
        public List<(MoveRequest request, MoveResult result, int movedCount)> BatchMoveItems(List<MoveRequest> requests)
        {
            var results = new List<(MoveRequest request, MoveResult result, int movedCount)>();

            try
            {
                if (requests == null)
                {
                    return results;
                }

                foreach (var request in requests)
                {
                    if (request.Count > 0)
                    {
                        // 指定数量移动
                        if (!string.IsNullOrEmpty(request.ExpectedItemId))
                        {
                            var (result, transferredCount) = TransferItems(request.ExpectedItemId, request.Count,
                                request.FromContainerId, request.ToContainerId);
                            results.Add((request, result, transferredCount));
                        }
                        else
                        {
                            // 需要先获取槽位中的物品ID
                            var sourceContainer = GetContainer(request.FromContainerId);
                            if (sourceContainer != null && request.FromSlot >= 0 && request.FromSlot < sourceContainer.Slots.Count)
                            {
                                var slot = sourceContainer.Slots[request.FromSlot];
                                if (slot.IsOccupied && slot.Item != null)
                                {
                                    var (result, transferredCount) = TransferItems(slot.Item.ID, request.Count,
                                        request.FromContainerId, request.ToContainerId);
                                    results.Add((request, result, transferredCount));
                                }
                                else
                                {
                                    results.Add((request, MoveResult.SourceSlotEmpty, 0));
                                }
                            }
                            else
                            {
                                results.Add((request, MoveResult.SourceContainerNotFound, 0));
                            }
                        }
                    }
                    else
                    {
                        // 整个槽位移动
                        var result = MoveItem(request.FromContainerId, request.FromSlot,
                            request.ToContainerId, request.ToSlot);

                        // 获取移动的数量
                        int movedCount = 0;
                        if (result == MoveResult.Success)
                        {
                            var sourceContainer = GetContainer(request.FromContainerId);
                            if (sourceContainer != null && request.FromSlot >= 0 && request.FromSlot < sourceContainer.Slots.Count)
                            {
                                var slot = sourceContainer.Slots[request.FromSlot];
                                movedCount = slot.IsOccupied ? slot.ItemCount : 0;
                            }
                        }

                        results.Add((request, result, movedCount));
                    }
                }

                OnBatchMoveCompleted?.Invoke(results);
            }
            catch
            {
                while (results.Count < requests.Count)
                {
                    results.Add((requests[results.Count], MoveResult.Failed, 0));
                }
            }

            return results;
        }

        /// <summary>
        /// 分配物品到多个容器
        /// </summary>
        /// <param name="item">要分配的物品</param>
        /// <param name="totalCount">总数量</param>
        /// <param name="targetContainerIds">目标容器ID列表</param>
        /// <returns>分配结果：容器ID和分配到的数量</returns>
        public Dictionary<string, int> DistributeItems(IItem item, int totalCount, List<string> targetContainerIds)
        {
            var results = new Dictionary<string, int>();

            try
            {
                if (item == null || totalCount <= 0 || targetContainerIds?.Count == 0)
                    return results;

                // 检查全局条件
                if (!ValidateGlobalItemConditions(item))
                    return results;

                int remainingCount = totalCount;
                var sortedContainers = new List<(string id, Container container, int priority)>();

                // 准备容器列表并按优先级排序
                foreach (string containerId in targetContainerIds)
                {
                    var container = GetContainer(containerId);
                    if (container != null)
                    {
                        int priority = GetContainerPriority(containerId);
                        sortedContainers.Add((containerId, container, priority));
                    }
                }

                // 按优先级降序排序
                sortedContainers.Sort((a, b) => b.priority.CompareTo(a.priority));

                // 按优先级分配物品
                foreach (var (containerId, container, _) in sortedContainers)
                {
                    if (remainingCount <= 0) break;

                    var (addResult, addedCount) = container.AddItems(item, remainingCount);

                    if (addResult == AddItemResult.Success && addedCount > 0)
                    {
                        results[containerId] = addedCount;
                        remainingCount -= addedCount;
                    }
                    else if (addResult == AddItemResult.ContainerIsFull && addedCount > 0)
                    {
                        // 部分添加成功
                        results[containerId] = addedCount;
                        remainingCount -= addedCount;
                    }
                }

                OnItemsDistributed?.Invoke(item, totalCount, results, remainingCount);
            }
            catch
            {
            }

            return results;
        }

        #endregion

        #region 跨容器操作事件

        /// <summary>
        /// 物品移动事件
        /// </summary>
        public event System.Action<string, int, string, IItem, int> OnItemMoved;

        /// <summary>
        /// 物品转移事件
        /// </summary>
        public event System.Action<string, string, string, int> OnItemsTransferred;

        /// <summary>
        /// 批量移动完成事件
        /// </summary>
        public event System.Action<List<(MoveRequest request, MoveResult result, int movedCount)>> OnBatchMoveCompleted;

        /// <summary>
        /// 物品分配事件
        /// </summary>
        public event System.Action<IItem, int, Dictionary<string, int>, int> OnItemsDistributed;

        #endregion

    }
}