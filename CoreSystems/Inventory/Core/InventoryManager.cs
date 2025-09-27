using EasyPack;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 多个容器管理的系统
/// </summary>
public class InventoryManager
{
    #region 存储

    /// <summary>
    /// 按ID索引的容器字典
    /// </summary>
    private readonly Dictionary<string, Container> _containers = new();

    /// <summary>
    /// 按类型分组的容器索引，功能导向
    /// </summary>
    private readonly Dictionary<string, HashSet<string>> _containersByType = new();

    /// <summary>
    /// 容器优先级设置
    /// </summary>
    private readonly Dictionary<string, int> _containerPriorities = new();


    /// <summary>
    /// 容器分类设置，业务导向
    /// </summary>
    /// 类型表示"是什么"，分类表示"属于谁/用于什么"
    /// 例如：类型为"背包""装备"，分类为"玩家""临时"之类
    private readonly Dictionary<string, string> _containerCategories = new();

    /// <summary>
    /// 全局物品条件列表
    /// </summary>
    private readonly List<IItemCondition> _globalItemConditions = new();

    /// <summary>
    /// 是否启用全局物品条件检查
    /// </summary>
    private bool _enableGlobalConditions = false;

    #endregion

    #region 容器注册与查询

    /// <summary>
    /// 注册容器到管理器中
    /// </summary>
    /// <param name="container">要注册的容器</param>
    /// <param name="priority">容器优先级，数值越高优先级越高</param>
    /// <param name="category">容器分类</param>
    /// <returns>注册是否成功</returns>
    public bool RegisterContainer(Container container, int priority = 0, string category = "Default")
    {
        try
        {
            if (container?.ID == null) return false;

            if (_containers.ContainsKey(container.ID))
            {
                UnregisterContainer(container.ID);
            }

            // 注册容器
            _containers[container.ID] = container;
            _containerPriorities[container.ID] = priority;
            _containerCategories[container.ID] = category ?? "Default";

            // 按类型建立索引
            string containerType = container.Type ?? "Unknown";
            if (!_containersByType.ContainsKey(containerType))
                _containersByType[containerType] = new HashSet<string>();

            _containersByType[containerType].Add(container.ID);

            // 如果全局条件已启用，添加到新容器
            if (_enableGlobalConditions)
            {
                ApplyGlobalConditionsToContainer(container);
            }

            OnContainerRegistered?.Invoke(container);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 注销指定ID的容器
    /// </summary>
    /// <param name="containerId">容器ID</param>
    /// <returns>注销是否成功</returns>
    public bool UnregisterContainer(string containerId)
    {
        try
        {
            if (string.IsNullOrEmpty(containerId) || !_containers.TryGetValue(containerId, out var container))
                return false;

            // 移除全局条件
            if (_enableGlobalConditions)
            {
                RemoveGlobalConditionsFromContainer(container);
            }

            // 从主字典移除
            _containers.Remove(containerId);

            // 从类型索引移除
            string containerType = container.Type ?? "Unknown";
            if (_containersByType.TryGetValue(containerType, out var typeSet))
            {
                typeSet.Remove(containerId);
                if (typeSet.Count == 0)
                    _containersByType.Remove(containerType);
            }

            // 清理其他相关数据
            _containerPriorities.Remove(containerId);
            _containerCategories.Remove(containerId);

            OnContainerUnregistered?.Invoke(container);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取指定ID的容器
    /// </summary>
    /// <param name="containerId">容器ID</param>
    /// <returns>找到的容器，未找到返回null</returns>
    public Container GetContainer(string containerId)
    {
        try
        {
            return string.IsNullOrEmpty(containerId) ? null : _containers.GetValueOrDefault(containerId);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 获取所有已注册的容器
    /// </summary>
    /// <returns>所有容器的只读列表</returns>
    public IReadOnlyList<Container> GetAllContainers()
    {
        try
        {
            return _containers.Values.ToList().AsReadOnly();
        }
        catch
        {
            return new List<Container>().AsReadOnly();
        }
    }

    /// <summary>
    /// 按类型获取容器
    /// </summary>
    /// <param name="containerType">容器类型</param>
    /// <returns>指定类型的容器列表</returns>
    public List<Container> GetContainersByType(string containerType)
    {
        try
        {
            if (string.IsNullOrEmpty(containerType) || !_containersByType.TryGetValue(containerType, out var containerIds))
                return new List<Container>();

            var result = new List<Container>();
            foreach (string containerId in containerIds)
            {
                if (_containers.TryGetValue(containerId, out var container))
                {
                    result.Add(container);
                }
            }
            return result;
        }
        catch
        {
            return new List<Container>();
        }
    }

    /// <summary>
    /// 按分类获取容器
    /// </summary>
    /// <param name="category">分类名称</param>
    /// <returns>指定分类的容器列表</returns>
    public List<Container> GetContainersByCategory(string category)
    {
        try
        {
            if (string.IsNullOrEmpty(category))
                return new List<Container>();

            var result = new List<Container>();
            foreach (var kvp in _containerCategories)
            {
                if (kvp.Value == category && _containers.TryGetValue(kvp.Key, out var container))
                {
                    result.Add(container);
                }
            }
            return result;
        }
        catch
        {
            return new List<Container>();
        }
    }

    /// <summary>
    /// 按优先级排序获取容器
    /// </summary>
    /// <param name="descending">是否降序排列（优先级高的在前）</param>
    /// <returns>按优先级排序的容器列表</returns>
    public List<Container> GetContainersByPriority(bool descending = true)
    {
        try
        {
            var sortedContainers = _containers.Values.ToList();
            sortedContainers.Sort((a, b) =>
            {
                int priorityA = _containerPriorities.GetValueOrDefault(a.ID, 0);
                int priorityB = _containerPriorities.GetValueOrDefault(b.ID, 0);
                return descending ? priorityB.CompareTo(priorityA) : priorityA.CompareTo(priorityB);
            });
            return sortedContainers;
        }
        catch
        {
            return new List<Container>();
        }
    }

    /// <summary>
    /// 检查容器是否已注册
    /// </summary>
    /// <param name="containerId">容器ID</param>
    /// <returns>是否已注册</returns>
    public bool IsContainerRegistered(string containerId)
    {
        try
        {
            return !string.IsNullOrEmpty(containerId) && _containers.ContainsKey(containerId);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取已注册容器的数量
    /// </summary>
    public int ContainerCount => _containers.Count;

    #endregion

    #region 配置

    /// <summary>
    /// 设置容器优先级
    /// </summary>
    /// <param name="containerId">容器ID</param>
    /// <param name="priority">优先级数值</param>
    /// <returns>设置是否成功</returns>
    public bool SetContainerPriority(string containerId, int priority)
    {
        try
        {
            if (!IsContainerRegistered(containerId))
                return false;

            _containerPriorities[containerId] = priority;
            OnContainerPriorityChanged?.Invoke(containerId, priority);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取容器优先级
    /// </summary>
    /// <param name="containerId">容器ID</param>
    /// <returns>容器优先级，未找到返回0</returns>
    public int GetContainerPriority(string containerId)
    {
        try
        {
            return _containerPriorities.GetValueOrDefault(containerId, 0);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 设置容器分类
    /// </summary>
    /// <param name="containerId">容器ID</param>
    /// <param name="category">分类名称</param>
    /// <returns>设置是否成功</returns>
    public bool SetContainerCategory(string containerId, string category)
    {
        try
        {
            if (!IsContainerRegistered(containerId))
                return false;

            string oldCategory = _containerCategories.GetValueOrDefault(containerId, "Default");
            _containerCategories[containerId] = category ?? "Default";
            OnContainerCategoryChanged?.Invoke(containerId, oldCategory, category);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取容器分类
    /// </summary>
    /// <param name="containerId">容器ID</param>
    /// <returns>容器分类，未找到返回"Default"</returns>
    public string GetContainerCategory(string containerId)
    {
        try
        {
            return _containerCategories.GetValueOrDefault(containerId, "Default");
        }
        catch
        {
            return "Default";
        }
    }


    #endregion

    #region 全局条件

    /// <summary>
    /// 检查物品是否满足全局条件
    /// </summary>
    /// <param name="item">要检查的物品</param>
    /// <returns>是否满足所有全局条件</returns>
    public bool ValidateGlobalItemConditions(IItem item)
    {
        try
        {
            if (item == null) return false;
            if (!_enableGlobalConditions)
                return true;

            foreach (var condition in _globalItemConditions)
            {
                if (!condition.CheckCondition(item))
                    return false;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 添加全局物品条件
    /// </summary>
    /// <param name="condition">物品条件</param>
    public void AddGlobalItemCondition(IItemCondition condition)
    {
        try
        {
            if (condition != null && !_globalItemConditions.Contains(condition))
            {
                _globalItemConditions.Add(condition);

                // 如果全局条件已启用，添加到所有容器
                if (_enableGlobalConditions)
                {
                    foreach (var container in _containers.Values)
                    {
                        if (!container.ContainerCondition.Contains(condition))
                        {
                            container.ContainerCondition.Add(condition);
                        }
                    }
                }

                OnGlobalConditionAdded?.Invoke(condition);
            }
        }
        catch
        {
            // 静默处理异常
        }
    }

    /// <summary>
    /// 移除全局物品条件
    /// </summary>
    /// <param name="condition">物品条件</param>
    /// <returns>移除是否成功</returns>
    public bool RemoveGlobalItemCondition(IItemCondition condition)
    {
        try
        {
            if (condition == null) return false;

            bool removed = _globalItemConditions.Remove(condition);
            if (removed)
            {
                // 从所有容器中移除此条件
                foreach (var container in _containers.Values)
                {
                    container.ContainerCondition.Remove(condition);
                }

                OnGlobalConditionRemoved?.Invoke(condition);
            }
            return removed;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 设置是否启用全局条件
    /// </summary>
    /// <param name="enable">是否启用</param>
    public void SetGlobalConditionsEnabled(bool enable)
    {
        try
        {
            if (_enableGlobalConditions == enable) return;

            _enableGlobalConditions = enable;

            if (enable)
            {
                foreach (var container in _containers.Values)
                {
                    ApplyGlobalConditionsToContainer(container);
                }
            }
            else
            {
                foreach (var container in _containers.Values)
                {
                    RemoveGlobalConditionsFromContainer(container);
                }
            }
        }
        catch
        {
        }
    }

    /// <summary>
    /// 获取是否启用全局条件
    /// </summary>
    public bool IsGlobalConditionsEnabled => _enableGlobalConditions;
    /// <summary>
    /// 将全局条件应用到指定容器
    /// </summary>
    /// <param name="container">目标容器</param>
    private void ApplyGlobalConditionsToContainer(Container container)
    {
        try
        {
            foreach (var condition in _globalItemConditions)
            {
                if (!container.ContainerCondition.Contains(condition))
                {
                    container.ContainerCondition.Add(condition);
                }
            }
        }
        catch
        {
        }
    }

    /// <summary>
    /// 从指定容器移除全局条件
    /// </summary>
    /// <param name="container">目标容器</param>
    private void RemoveGlobalConditionsFromContainer(Container container)
    {
        try
        {
            foreach (var condition in _globalItemConditions)
            {
                container.ContainerCondition.Remove(condition);
            }
        }
        catch
        {
        }
    }

    #endregion

    #region 事件

    /// <summary>
    /// 容器注册事件
    /// </summary>
    public event System.Action<Container> OnContainerRegistered;

    /// <summary>
    /// 容器注销事件
    /// </summary>
    public event System.Action<Container> OnContainerUnregistered;

    /// <summary>
    /// 容器优先级变更事件
    /// </summary>
    public event System.Action<string, int> OnContainerPriorityChanged;

    /// <summary>
    /// 容器分类变更事件
    /// </summary>
    public event System.Action<string, string, string> OnContainerCategoryChanged;

    /// <summary>
    /// 全局条件添加事件
    /// </summary>
    public event System.Action<IItemCondition> OnGlobalConditionAdded;

    /// <summary>
    /// 全局条件移除事件
    /// </summary>
    public event System.Action<IItemCondition> OnGlobalConditionRemoved;

    /// <summary>
    /// 全局缓存刷新事件
    /// </summary>
    public event System.Action OnGlobalCacheRefreshed;

    /// <summary>
    /// 全局缓存验证事件
    /// </summary>
    public event System.Action OnGlobalCacheValidated;

    #endregion

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
        catch
        {
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
        catch
        {
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

            int totalCount = sourceContainer.GetItemTotalCount(itemId);
            return TransferItems(itemId, totalCount, fromContainerId, toContainerId);
        }
        catch
        {
            return (MoveResult.Failed, 0);
        }
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

    #region 全局物品搜索
    /// <summary>
    /// 全局物品搜索结果
    /// </summary>
    public struct GlobalItemResult
    {
        public string ContainerId;
        public int SlotIndex;
        public IItem Item;
        public int IndexCount;

        public GlobalItemResult(string containerId, int slotIndex, IItem item, int count)
        {
            ContainerId = containerId;
            SlotIndex = slotIndex;
            Item = item;
            IndexCount = count;
        }
    }

    /// <summary>
    /// 全局查找物品
    /// </summary>
    /// <param name="itemId">物品ID</param>
    /// <returns>物品所在的位置列表</returns>
    public List<GlobalItemResult> FindItemGlobally(string itemId)
    {
        var results = new List<GlobalItemResult>();

        try
        {
            if (string.IsNullOrEmpty(itemId))
                return results;

            foreach (Container container in _containers.Values)
            {
                if (container.HasItem(itemId)) // 利用缓存快速检查
                {
                    var slotIndices = container.FindSlotIndices(itemId); // 利用缓存获取槽位索引
                    foreach (int slotIndex in slotIndices)
                    {
                        if (slotIndex < container.Slots.Count)
                        {
                            var slot = container.Slots[slotIndex];
                            if (slot.IsOccupied && slot.Item?.ID == itemId)
                            {
                                results.Add(new GlobalItemResult(container.ID, slotIndex, slot.Item, slot.ItemCount));
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            results.Clear();
            foreach (var container in _containers.Values)
            {
                for (int i = 0; i < container.Slots.Count; i++)
                {
                    var slot = container.Slots[i];
                    if (slot.IsOccupied && slot.Item?.ID == itemId)
                    {
                        results.Add(new GlobalItemResult(container.ID, i, slot.Item, slot.ItemCount));
                    }
                }
            }
        }

        return results;
    }

    /// <summary>
    /// 获取全局物品总数
    /// </summary>
    /// <param name="itemId">物品ID</param>
    /// <returns>全局物品总数量</returns>
    public int GetGlobalItemCount(string itemId)
    {
        try
        {
            if (string.IsNullOrEmpty(itemId))
                return 0;

            int totalCount = 0;
            foreach (var container in _containers.Values)
            {
                totalCount += container.GetItemTotalCount(itemId);
            }

            return totalCount;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 查找包含指定物品的容器
    /// </summary>
    /// <param name="itemId">物品ID</param>
    /// <returns>包含该物品的容器列表和数量</returns>
    public Dictionary<string, int> FindContainersWithItem(string itemId)
    {
        var results = new Dictionary<string, int>();

        try
        {
            if (string.IsNullOrEmpty(itemId))
                return results;

            foreach (var container in _containers.Values)
            {
                if (container.HasItem(itemId))
                {
                    int count = container.GetItemTotalCount(itemId);
                    if (count > 0)
                    {
                        results[container.ID] = count;
                    }
                }
            }
        }
        catch
        {

        }

        return results;
    }

    /// <summary>
    /// 按条件全局搜索物品
    /// </summary>
    /// <param name="condition">搜索条件</param>
    /// <returns>符合条件的物品列表</returns>
    public List<GlobalItemResult> SearchItemsByCondition(System.Func<IItem, bool> condition)
    {
        var results = new List<GlobalItemResult>();

        try
        {
            if (condition == null)
                return results;

            foreach (var container in _containers.Values)
            {
                for (int i = 0; i < container.Slots.Count; i++)
                {
                    var slot = container.Slots[i];
                    if (slot.IsOccupied && slot.Item != null && condition(slot.Item))
                    {
                        results.Add(new GlobalItemResult(container.ID, i, slot.Item, slot.ItemCount));
                    }
                }
            }
        }
        catch
        {

        }

        return results;
    }

    /// <summary>
    /// 按物品类型全局搜索
    /// </summary>
    /// <param name="itemType">物品类型</param>
    /// <returns>指定类型的物品列表</returns>
    public List<GlobalItemResult> SearchItemsByType(string itemType)
    {
        var results = new List<GlobalItemResult>();

        try
        {
            if (string.IsNullOrEmpty(itemType))
                return results;

            foreach (Container container in _containers.Values)
            {
                // 利用容器的类型缓存查询
                var typeItems = container.GetItemsByType(itemType);
                foreach (var (slotIndex, item, count) in typeItems)
                {
                    results.Add(new GlobalItemResult(container.ID, slotIndex, item, count));
                }
            }
        }
        catch
        {
            // 发生异常时回退到条件搜索
            results.Clear();
            results = SearchItemsByCondition(item => item.Type == itemType);
        }

        return results;
    }

    /// <summary>
    /// 按物品名称全局搜索
    /// </summary>
    /// <param name="namePattern">名称模式</param>
    /// <returns>符合名称模式的物品列表</returns>
    public List<GlobalItemResult> SearchItemsByName(string namePattern)
    {
        var results = new List<GlobalItemResult>();

        try
        {
            if (string.IsNullOrEmpty(namePattern))
                return results;

            foreach (Container container in _containers.Values)
            {
                // 利用容器的名称缓存查询
                var nameItems = container.GetItemsByName(namePattern);
                foreach (var (slotIndex, item, count) in nameItems)
                {
                    results.Add(new GlobalItemResult(container.ID, slotIndex, item, count));
                }
            }
        }
        catch
        {
            results.Clear();
            results = SearchItemsByCondition(item => item.Name?.Contains(namePattern) == true);
        }

        return results;
    }
    /// <summary>
    /// 按属性全局搜索物品
    /// </summary>
    /// <param name="attributeName">属性名称</param>
    /// <param name="attributeValue">属性值</param>
    /// <returns>符合属性条件的物品列表</returns>
    public List<GlobalItemResult> SearchItemsByAttribute(string attributeName, object attributeValue)
    {
        var results = new List<GlobalItemResult>();

        try
        {
            if (string.IsNullOrEmpty(attributeName))
                return results;

            foreach (Container container in _containers.Values)
            {
                // 利用容器的属性缓存查询
                var attributeItems = container.GetItemsByAttribute(attributeName, attributeValue);
                foreach (var (slotIndex, item, count) in attributeItems)
                {
                    results.Add(new GlobalItemResult(container.ID, slotIndex, item, count));
                }
            }
        }
        catch
        {
            results.Clear();
            results = SearchItemsByCondition(item =>
                item.Attributes != null &&
                item.Attributes.TryGetValue(attributeName, out var value) &&
                (attributeValue == null || value?.Equals(attributeValue) == true));
        }

        return results;
    }
    #endregion

    #region 全局缓存
    /// <summary>
    /// 刷新全局缓存
    /// </summary>
    public void RefreshGlobalCache()
    {
        try
        {
            foreach (var container in _containers.Values)
            {
                if (container is Container containerImpl)
                {
                    containerImpl.RebuildCaches();
                }
            }

            OnGlobalCacheRefreshed?.Invoke();
        }
        catch
        {

        }
    }

    /// <summary>
    /// 验证全局缓存
    /// </summary>
    public void ValidateGlobalCache()
    {
        try
        {
            foreach (var container in _containers.Values)
            {
                if (container is Container containerImpl)
                {
                    containerImpl.ValidateCaches();
                }
            }

            OnGlobalCacheValidated?.Invoke();
        }
        catch
        {

        }
    }
    #endregion
}