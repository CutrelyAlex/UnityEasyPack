using EasyPack.ENekoFramework;
using System.Collections.Generic;

namespace EasyPack.InventorySystem
{
    /// <summary>
    ///     库存服务接口，定义库存系统的核心功能
    /// </summary>
    public interface IInventoryService : IService
    {
        // 基础容器管理
        bool RegisterContainer(Container container, int priority = 0, string category = "Default");
        bool UnregisterContainer(string containerId);
        Container GetContainer(string containerId);
        IReadOnlyList<Container> GetAllContainers();
        List<Container> GetContainersByType(string containerType);
        List<Container> GetContainersByCategory(string category);
        List<Container> GetContainersByPriority(bool descending = true);
        bool IsContainerRegistered(string containerId);
        int ContainerCount { get; }

        // 容器配置
        bool SetContainerPriority(string containerId, int priority);
        int GetContainerPriority(string containerId);
        bool SetContainerCategory(string containerId, string category);
        string GetContainerCategory(string containerId);

        // 全局条件
        bool ValidateGlobalItemConditions(IItem item);
        void AddGlobalItemCondition(IItemCondition condition);
        bool RemoveGlobalItemCondition(IItemCondition condition);
        void SetGlobalConditionsEnabled(bool enable);
        bool IsGlobalConditionsEnabled { get; }

        // 全局缓存
        void RefreshGlobalCache();
        void ValidateGlobalCache();

        // 服务管理
        void Reset();

        // 跨容器操作
        InventoryService.MoveResult MoveItem(string fromContainerId, int fromSlot, string toContainerId,
                                             int toSlot = -1);

        (InventoryService.MoveResult result, int transferredCount) TransferItems(string itemId, int count,
            string fromContainerId, string toContainerId);

        (InventoryService.MoveResult result, int transferredCount) AutoMoveItem(string itemId, string fromContainerId,
            string toContainerId);

        List<(InventoryService.MoveRequest request, InventoryService.MoveResult result, int movedCount)> BatchMoveItems(
            List<InventoryService.MoveRequest> requests);

        Dictionary<string, int> DistributeItems(IItem item, int totalCount, List<string> targetContainerIds);

        // 全局搜索
        List<InventoryService.GlobalItemResult> FindItemGlobally(string itemId);
        int GetGlobalItemCount(string itemId);
        Dictionary<string, int> FindContainersWithItem(string itemId);
        List<InventoryService.GlobalItemResult> SearchItemsByCondition(System.Func<IItem, bool> condition);
        List<InventoryService.GlobalItemResult> SearchItemsByType(string itemType);
        List<InventoryService.GlobalItemResult> SearchItemsByName(string namePattern);
        List<InventoryService.GlobalItemResult> SearchItemsByAttribute(string attributeName, object attributeValue);

        // 事件
        event System.Action<Container> OnContainerRegistered;
        event System.Action<Container> OnContainerUnregistered;
        event System.Action<string, int> OnContainerPriorityChanged;
        event System.Action<string, string, string> OnContainerCategoryChanged;
        event System.Action<IItemCondition> OnGlobalConditionAdded;
        event System.Action<IItemCondition> OnGlobalConditionRemoved;
        event System.Action OnGlobalCacheRefreshed;
        event System.Action OnGlobalCacheValidated;
        event System.Action<string, int, string, IItem, int> OnItemMoved;
        event System.Action<string, string, string, int> OnItemsTransferred;

        event System.Action<List<(InventoryService.MoveRequest request, InventoryService.MoveResult result, int
            movedCount)>> OnBatchMoveCompleted;

        event System.Action<IItem, int, Dictionary<string, int>, int> OnItemsDistributed;
    }
}