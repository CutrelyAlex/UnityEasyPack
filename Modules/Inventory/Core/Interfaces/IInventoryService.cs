using System;
using System.Collections.Generic;
using EasyPack.Category;
using EasyPack.ENekoFramework;

namespace EasyPack.InventorySystem
{
    /// <summary>
    ///     库存服务接口，定义库存系统的核心功能
    /// </summary>
    public interface IInventoryService : IService
    {
        // CategoryManager 和 ItemFactory 集成
        /// <summary>
        ///     物品分类管理器，管理Item的Category、Tags和RuntimeMetadata
        /// </summary>
        ICategoryManager<IItem, long> CategoryManager { get; }

        /// <summary>
        ///     物品工厂，负责ItemData注册和Item实例创建
        /// </summary>
        IItemFactory ItemFactory { get; }

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

        // ItemUID 管理
        long AssignItemUID(IItem item);
        IItem GetItemByUID(long uid);
        void UnregisterItemUID(long uid);
        bool IsUIDRegistered(long uid);
        
        /// <summary>
        /// 分配或恢复物品UID（用于反序列化场景）
        /// </summary>
        /// <param name="item">要处理的物品</param>
        /// <param name="preserveUID">是否尝试保留原UID，若为false则总是分配新UID</param>
        /// <returns>最终分配的UID</returns>
        long AssignOrRestoreItemUID(IItem item, bool preserveUID = false);

        // 服务管理
        void Reset();

        // 跨容器操作
        MoveResult MoveItem(string fromContainerId, int fromSlot, string toContainerId,
                                             int toSlot = -1);

        (MoveResult result, int transferredCount) TransferItems(string itemId, int count,
            string fromContainerId, string toContainerId);

        (MoveResult result, int transferredCount) AutoMoveItem(string itemId, string fromContainerId,
            string toContainerId);

        List<(InventoryService.MoveRequest request, MoveResult result, int movedCount)> BatchMoveItems(
            List<InventoryService.MoveRequest> requests);

        Dictionary<string, int> DistributeItems(IItem item, int totalCount, List<string> targetContainerIds);

        // 全局搜索
        List<InventoryService.GlobalItemResult> FindItemGlobally(string itemId);
        int GetGlobalItemCount(string itemId);
        Dictionary<string, int> FindContainersWithItem(string itemId);
        List<InventoryService.GlobalItemResult> SearchItemsByCondition(Func<IItem, bool> condition);
        List<InventoryService.GlobalItemResult> SearchItemsByType(string itemType);
        List<InventoryService.GlobalItemResult> SearchItemsByName(string namePattern);
        List<InventoryService.GlobalItemResult> SearchItemsByAttribute(string attributeName, object attributeValue);

        // 事件
        event Action<Container> OnContainerRegistered;
        event Action<Container> OnContainerUnregistered;
        event Action<string, int> OnContainerPriorityChanged;
        event Action<string, string, string> OnContainerCategoryChanged;
        event Action<IItemCondition> OnGlobalConditionAdded;
        event Action<IItemCondition> OnGlobalConditionRemoved;
        event Action OnGlobalCacheRefreshed;
        event Action OnGlobalCacheValidated;
        event Action<string, int, string, IItem, int> OnItemMoved;
        event Action<string, string, string, int> OnItemsTransferred;

        event Action<List<(InventoryService.MoveRequest request, MoveResult result, int
            movedCount)>> OnBatchMoveCompleted;

        event Action<IItem, int, Dictionary<string, int>, int> OnItemsDistributed;
    }
}