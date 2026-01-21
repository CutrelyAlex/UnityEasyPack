using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EasyPack.Architecture;
using EasyPack.Category;
using EasyPack.ENekoFramework;
using EasyPack.Serialization;
using UnityEngine;

namespace EasyPack.InventorySystem
{
    /// <summary>
    ///     多个容器管理的系统，实现 IService 接口以支持 ENekoFramework
    /// </summary>
    public partial class InventoryService : BaseService, IInventoryService
    {
        #region IService 实现

        /// <summary>
        ///     线程锁，用于保证线程安全
        /// </summary>
        private readonly object _lock = new();

        /// <summary>
        ///     服务初始化钩子方法
        ///     派生类应重写此方法以实现自定义初始化逻辑
        /// </summary>
        protected override async Task OnInitializeAsync()
        {
            await base.OnInitializeAsync();

            // 执行必要的初始化逻辑
            await Task.Run(() =>
            {
                lock (_lock)
                {
                    // 初始化CategoryManager
                    CategoryManager = new CategoryManager<IItem, long>(item => item.ItemUID);
                    
                    // 初始化ItemFactory
                    ItemFactory = new ItemFactory(this);
                    
                    // 初始化内部数据结构（已在字段声明时初始化）
                    _enableGlobalConditions = false;
                }
            });

            // 注册序列化器
            await RegisterSerializers();

            Debug.Log("[InventoryService] 服务初始化完成");
        }

        /// <summary>
        ///     注册 Inventory 系统的序列化器
        /// </summary>
        private async Task RegisterSerializers()
        {
            try
            {
                // 获取序列化服务
                var serializationService = await EasyPackArchitecture.Instance.ResolveAsync<ISerializationService>();

                // 注册物品和容器的序列化器
                serializationService.RegisterSerializer(new ItemJsonSerializer());
                serializationService.RegisterSerializer(new GridItemJsonSerializer());
                serializationService.RegisterSerializer(new ContainerJsonSerializer(serializationService));
                serializationService.RegisterSerializer(new GridContainerJsonSerializer(serializationService));

                // 注册条件序列化器
                serializationService.RegisterSerializer(new ConditionJsonSerializer());

                Debug.Log("[InventoryService] 序列化器注册成功");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[InventoryService] 序列化器注册失败: {ex.Message}");
            }
        }

        /// <summary>
        ///     服务暂停钩子方法
        /// </summary>
        protected override void OnPause()
        {
            lock (_lock)
            {
                Debug.Log("[InventoryService] 服务已暂停");
            }

            base.OnPause();
        }

        /// <summary>
        ///     服务恢复钩子方法
        /// </summary>
        protected override void OnResume()
        {
            lock (_lock)
            {
                Debug.Log("[InventoryService] 服务已恢复");
            }

            base.OnResume();
        }

        /// <summary>
        ///     服务释放钩子方法
        /// </summary>
        protected override async Task OnDisposeAsync()
        {
            lock (_lock)
            {
                // 清理所有容器
                foreach (Container container in _containers.Values)
                    // 容器可能有自己的清理逻辑
                {
                    container?.ClearAllSlots();
                }

                _containers.Clear();
                _containersByType.Clear();
                _containerPriorities.Clear();
                _containerCategories.Clear();
                _globalItemConditions.Clear();

                Debug.Log("[InventoryService] 服务已释放");
            }

            await base.OnDisposeAsync();
        }

        /// <summary>
        ///     重置服务状态
        ///     清空所有容器、条件和缓存，但保留服务的初始化状态
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                if (State == ServiceLifecycleState.Disposed)
                {
                    Debug.LogWarning("[InventoryService] 服务已释放，无法重置");
                    return;
                }

                // 清理所有容器
                foreach (Container container in _containers.Values)
                {
                    container?.ClearAllSlots();
                }

                _containers.Clear();
                _containersByType.Clear();
                _containerPriorities.Clear();
                _containerCategories.Clear();
                _globalItemConditions.Clear();
                _enableGlobalConditions = false;

                // 重置UID分配器
                _nextItemUID = 1;
                _itemsByUID.Clear();

                // 重置CategoryManager
                CategoryManager?.Clear();

                Debug.Log("[InventoryService] 服务状态已重置");
            }
        }

        /// <summary>
        ///     检查服务是否可用
        /// </summary>
        private bool IsServiceAvailable() => State == ServiceLifecycleState.Ready;

        #endregion

        #region 存储

        /// <summary>
        ///     物品分类管理器，管理Item的Category、Tags和RuntimeMetadata
        /// </summary>
        public ICategoryManager<IItem, long> CategoryManager { get; private set; }

        /// <summary>
        ///     物品工厂，负责ItemData注册和Item实例创建
        /// </summary>
        public IItemFactory ItemFactory { get; private set; }

        /// <summary>
        ///     按ID索引的容器字典
        /// </summary>
        private readonly Dictionary<string, Container> _containers = new();

        /// <summary>
        ///     按类型分组的容器索引，功能导向
        /// </summary>
        private readonly Dictionary<string, HashSet<string>> _containersByType = new();

        /// <summary>
        ///     容器优先级设置
        /// </summary>
        private readonly Dictionary<string, int> _containerPriorities = new();


        /// <summary>
        ///     容器分类设置，业务导向
        /// </summary>
        /// 类型表示"是什么"，分类表示"属于谁/用于什么"
        /// 例如：类型为"背包""装备"，分类为"玩家""临时"之类
        private readonly Dictionary<string, string> _containerCategories = new();

        /// <summary>
        ///     全局物品条件列表
        /// </summary>
        private readonly List<IItemCondition> _globalItemConditions = new();

        /// <summary>
        ///     是否启用全局物品条件检查
        /// </summary>
        private bool _enableGlobalConditions;

        #endregion

        #region ItemUID 管理

        /// <summary>
        ///     ItemUID生成器，自增长ID
        /// </summary>
        private long _nextItemUID = 1;

        /// <summary>
        ///     ItemUID到物品实例的映射（可选，用于快速查找）
        /// </summary>
        private readonly Dictionary<long, IItem> _itemsByUID = new();

        /// <summary>
        ///     为物品分配唯一UID
        /// </summary>
        /// <param name="item">要分配UID的物品</param>
        /// <returns>分配的UID</returns>
        public long AssignItemUID(IItem item)
        {
            if (item == null) return -1;

            lock (_lock)
            {
                // 如果已经有UID，不重复分配
                if (item.ItemUID != -1)
                {
                    return item.ItemUID;
                }

                long uid = _nextItemUID++;
                item.ItemUID = uid;

                // 注册到全局映射
                _itemsByUID[uid] = item;

                return uid;
            }
        }

        /// <summary>
        ///     通过UID查找物品实例
        /// </summary>
        /// <param name="uid">物品UID</param>
        /// <returns>物品实例，未找到返回null</returns>
        public IItem GetItemByUID(long uid)
        {
            if (uid == -1) return null;

            lock (_lock)
            {
                return _itemsByUID.GetValueOrDefault(uid);
            }
        }

        /// <summary>
        ///     注销物品UID
        /// </summary>
        /// <param name="uid">要注销的UID</param>
        public void UnregisterItemUID(long uid)
        {
            if (uid == -1) return;

            lock (_lock)
            {
                _itemsByUID.Remove(uid);
            }
        }

        /// <summary>
        ///     检查UID是否已被使用
        /// </summary>
        public bool IsUIDRegistered(long uid)
        {
            if (uid == -1) return false;

            lock (_lock)
            {
                return _itemsByUID.ContainsKey(uid);
            }
        }

        /// <summary>
        ///     分配或恢复物品UID（用于反序列化场景）
        /// </summary>
        /// <param name="item">要处理的物品</param>
        /// <param name="preserveUID">是否尝试保留原UID，若为false则总是分配新UID</param>
        /// <returns>最终分配的UID</returns>
        public long AssignOrRestoreItemUID(IItem item, bool preserveUID = false)
        {
            if (item == null) return -1;

            lock (_lock)
            {
                long requestedUID = item.ItemUID;

                // 如果不保留UID或UID为-1，直接分配新UID
                if (!preserveUID || requestedUID == -1)
                {
                    long newUID = _nextItemUID++;
                    item.ItemUID = newUID;
                    _itemsByUID[newUID] = item;
                    return newUID;
                }

                // 尝试恢复原UID
                if (_itemsByUID.TryGetValue(requestedUID, out IItem existingItem))
                {
                    // UID冲突
                    if (!ReferenceEquals(existingItem, item))
                    {
                        // 分配新UID
                        long newUID = _nextItemUID++;
                        item.ItemUID = newUID;
                        _itemsByUID[newUID] = item;
                        return newUID;
                    }
                    // 同一个对象，保持原UID
                    return requestedUID;
                }

                // 无冲突，使用原UID
                _itemsByUID[requestedUID] = item;

                // 更新_nextItemUID以避免将来冲突
                if (requestedUID >= _nextItemUID)
                {
                    _nextItemUID = requestedUID + 1;
                }

                return requestedUID;
            }
        }

        #endregion

        #region 容器注册与查询

        /// <summary>
        ///     注册容器到管理器中
        /// </summary>
        /// <param name="container">要注册的容器</param>
        /// <param name="priority">容器优先级，数值越高优先级越高</param>
        /// <param name="category">容器分类</param>
        /// <returns>注册是否成功</returns>
        public bool RegisterContainer(Container container, int priority = 0, string category = "Default")
        {
            if (!IsServiceAvailable())
            {
                Debug.LogWarning("[InventoryService] 服务未就绪，无法注册容器");
                return false;
            }

            if (container?.ID == null) return false;

            lock (_lock)
            {
                if (_containers.ContainsKey(container.ID)) UnregisterContainerInternal(container.ID);

                // 设置容器的InventoryService引用
                container.InventoryService = this;

                // 注册容器
                _containers[container.ID] = container;
                _containerPriorities[container.ID] = priority;
                _containerCategories[container.ID] = category ?? "Default";

                // 按类型建立索引
                string containerType = container.Type ?? "Unknown";
                if (!_containersByType.ContainsKey(containerType))
                {
                    _containersByType[containerType] = new();
                }

                _containersByType[containerType].Add(container.ID);

                // 如果全局条件已启用，添加到新容器
                if (_enableGlobalConditions) ApplyGlobalConditionsToContainer(container);
            }

            OnContainerRegistered?.Invoke(container);
            return true;
        }

        /// <summary>
        ///     注销指定ID的容器
        /// </summary>
        /// <param name="containerId">容器ID</param>
        /// <returns>注销是否成功</returns>
        public bool UnregisterContainer(string containerId)
        {
            if (!IsServiceAvailable())
            {
                Debug.LogWarning("[InventoryService] 服务未就绪，无法注销容器");
                return false;
            }

            lock (_lock)
            {
                return UnregisterContainerInternal(containerId);
            }
        }

        /// <summary>
        ///     内部注销容器实现（不检查服务状态，不加锁）
        /// </summary>
        private bool UnregisterContainerInternal(string containerId)
        {
            try
            {
                if (string.IsNullOrEmpty(containerId) || !_containers.TryGetValue(containerId, out Container container))
                {
                    return false;
                }

                // 移除全局条件
                if (_enableGlobalConditions) RemoveGlobalConditionsFromContainer(container);

                // 从主字典移除
                _containers.Remove(containerId);

                // 从类型索引移除
                string containerType = container.Type ?? "Unknown";
                if (_containersByType.TryGetValue(containerType, out var typeSet))
                {
                    typeSet.Remove(containerId);
                    if (typeSet.Count == 0)
                    {
                        _containersByType.Remove(containerType);
                    }
                }

                // 清理其他相关数据
                _containerPriorities.Remove(containerId);
                _containerCategories.Remove(containerId);

                OnContainerUnregistered?.Invoke(container);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InventoryService] 注销容器失败：{ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        ///     获取指定ID的容器
        /// </summary>
        /// <param name="containerId">容器ID</param>
        /// <returns>找到的容器，未找到返回null</returns>
        public Container GetContainer(string containerId)
        {
            if (!IsServiceAvailable()) return null;

            lock (_lock)
            {
                return string.IsNullOrEmpty(containerId) ? null : _containers.GetValueOrDefault(containerId);
            }
        }

        /// <summary>
        ///     获取所有已注册的容器
        /// </summary>
        /// <returns>所有容器的只读列表</returns>
        public IReadOnlyList<Container> GetAllContainers()
        {
            if (!IsServiceAvailable()) return new List<Container>().AsReadOnly();

            lock (_lock)
            {
                return _containers.Values.ToList().AsReadOnly();
            }
        }

        /// <summary>
        ///     按类型获取容器
        /// </summary>
        /// <param name="containerType">容器类型</param>
        /// <returns>指定类型的容器列表</returns>
        public List<Container> GetContainersByType(string containerType)
        {
            if (!IsServiceAvailable()) return new();

            lock (_lock)
            {
                if (string.IsNullOrEmpty(containerType) ||
                    !_containersByType.TryGetValue(containerType, out var containerIds))
                {
                    return new();
                }

                var result = new List<Container>();
                foreach (string containerId in containerIds)
                {
                    if (_containers.TryGetValue(containerId, out Container container))
                    {
                        result.Add(container);
                    }
                }

                return result;
            }
        }

        /// <summary>
        ///     按分类获取容器
        /// </summary>
        /// <param name="category">分类名称</param>
        /// <returns>指定分类的容器列表</returns>
        public List<Container> GetContainersByCategory(string category)
        {
            if (!IsServiceAvailable()) return new();

            lock (_lock)
            {
                if (string.IsNullOrEmpty(category))
                {
                    return new();
                }

                var result = new List<Container>();
                foreach (var kvp in _containerCategories)
                {
                    if (kvp.Value == category && _containers.TryGetValue(kvp.Key, out Container container))
                    {
                        result.Add(container);
                    }
                }

                return result;
            }
        }

        /// <summary>
        ///     按优先级排序获取容器
        /// </summary>
        /// <param name="descending">是否降序排列（优先级高的在前）</param>
        /// <returns>按优先级排序的容器列表</returns>
        public List<Container> GetContainersByPriority(bool descending = true)
        {
            if (!IsServiceAvailable()) return new();

            lock (_lock)
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
        }

        /// <summary>
        ///     检查容器是否已注册
        /// </summary>
        /// <param name="containerId">容器ID</param>
        /// <returns>是否已注册</returns>
        public bool IsContainerRegistered(string containerId)
        {
            if (!IsServiceAvailable()) return false;

            lock (_lock)
            {
                return !string.IsNullOrEmpty(containerId) && _containers.ContainsKey(containerId);
            }
        }

        /// <summary>
        ///     获取已注册容器的数量
        /// </summary>
        public int ContainerCount
        {
            get
            {
                if (!IsServiceAvailable()) return 0;

                lock (_lock)
                {
                    return _containers.Count;
                }
            }
        }

        #endregion

        #region 配置

        /// <summary>
        ///     设置容器优先级
        /// </summary>
        /// <param name="containerId">容器ID</param>
        /// <param name="priority">优先级数值</param>
        /// <returns>设置是否成功</returns>
        public bool SetContainerPriority(string containerId, int priority)
        {
            if (!IsServiceAvailable()) return false;

            lock (_lock)
            {
                if (!IsContainerRegistered(containerId))
                {
                    return false;
                }

                _containerPriorities[containerId] = priority;
            }

            OnContainerPriorityChanged?.Invoke(containerId, priority);
            return true;
        }

        /// <summary>
        ///     获取容器优先级
        /// </summary>
        /// <param name="containerId">容器ID</param>
        /// <returns>容器优先级，未找到返回0</returns>
        public int GetContainerPriority(string containerId)
        {
            if (!IsServiceAvailable() || string.IsNullOrEmpty(containerId)) return 0;

            lock (_lock)
            {
                return _containerPriorities.GetValueOrDefault(containerId, 0);
            }
        }

        /// <summary>
        ///     设置容器分类
        /// </summary>
        /// <param name="containerId">容器ID</param>
        /// <param name="category">分类名称</param>
        /// <returns>设置是否成功</returns>
        public bool SetContainerCategory(string containerId, string category)
        {
            if (!IsServiceAvailable()) return false;

            string oldCategory;
            lock (_lock)
            {
                if (!IsContainerRegistered(containerId))
                {
                    return false;
                }

                oldCategory = _containerCategories.GetValueOrDefault(containerId, "Default");
                _containerCategories[containerId] = category ?? "Default";
            }

            OnContainerCategoryChanged?.Invoke(containerId, oldCategory, category);
            return true;
        }

        /// <summary>
        ///     获取容器分类
        /// </summary>
        /// <param name="containerId">容器ID</param>
        /// <returns>容器分类，未找到返回"Default"</returns>
        public string GetContainerCategory(string containerId)
        {
            if (!IsServiceAvailable() || string.IsNullOrEmpty(containerId)) return "Default";

            lock (_lock)
            {
                return _containerCategories.GetValueOrDefault(containerId, "Default");
            }
        }

        #endregion

        #region 全局条件

        /// <summary>
        ///     检查物品是否满足全局条件
        /// </summary>
        /// <param name="item">要检查的物品</param>
        /// <returns>是否满足所有全局条件</returns>
        public bool ValidateGlobalItemConditions(IItem item)
        {
            if (item == null) return false;

            if (!IsServiceAvailable()) return false;

            lock (_lock)
            {
                if (!_enableGlobalConditions)
                {
                    return true;
                }

                // 用户提供的条件检查可能抛异常，需要保护
                try
                {
                    foreach (IItemCondition condition in _globalItemConditions)
                    {
                        if (!condition.CheckCondition(item))
                        {
                            return false;
                        }
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[InventoryService] 全局条件检查失败：{ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        ///     添加全局物品条件
        /// </summary>
        /// <param name="condition">物品条件</param>
        public void AddGlobalItemCondition(IItemCondition condition)
        {
            if (!IsServiceAvailable()) return;

            try
            {
                if (condition == null) return;

                lock (_lock)
                {
                    if (_globalItemConditions.Contains(condition))
                    {
                        return;
                    }

                    _globalItemConditions.Add(condition);

                    // 如果全局条件已启用，添加到所有容器
                    if (_enableGlobalConditions)
                    {
                        foreach (Container container in _containers.Values)
                        {
                            if (!container.ContainerCondition.Contains(condition))
                            {
                                container.ContainerCondition.Add(condition);
                            }
                        }
                    }
                }

                OnGlobalConditionAdded?.Invoke(condition);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InventoryService] 操作失败：{ex.Message}"); // 静默处理异常
            }
        }

        /// <summary>
        ///     移除全局物品条件
        /// </summary>
        /// <param name="condition">物品条件</param>
        /// <returns>移除是否成功</returns>
        public bool RemoveGlobalItemCondition(IItemCondition condition)
        {
            if (!IsServiceAvailable() || condition == null) return false;

            bool removed;
            lock (_lock)
            {
                removed = _globalItemConditions.Remove(condition);
                if (removed)
                    // 从所有容器中移除此条件
                {
                    foreach (Container container in _containers.Values)
                    {
                        container.ContainerCondition.Remove(condition);
                    }
                }
            }

            if (removed) OnGlobalConditionRemoved?.Invoke(condition);

            return removed;
        }

        /// <summary>
        ///     设置是否启用全局条件
        /// </summary>
        /// <param name="enable">是否启用</param>
        public void SetGlobalConditionsEnabled(bool enable)
        {
            if (!IsServiceAvailable()) return;

            lock (_lock)
            {
                if (_enableGlobalConditions == enable) return;

                _enableGlobalConditions = enable;

                if (enable)
                {
                    foreach (Container container in _containers.Values)
                    {
                        ApplyGlobalConditionsToContainer(container);
                    }
                }
                else
                {
                    foreach (Container container in _containers.Values)
                    {
                        RemoveGlobalConditionsFromContainer(container);
                    }
                }
            }
        }

        /// <summary>
        ///     获取是否启用全局条件
        /// </summary>
        public bool IsGlobalConditionsEnabled
        {
            get
            {
                if (!IsServiceAvailable()) return false;

                lock (_lock)
                {
                    return _enableGlobalConditions;
                }
            }
        }

        /// <summary>
        ///     将全局条件应用到指定容器
        /// </summary>
        /// <param name="container">目标容器</param>
        private void ApplyGlobalConditionsToContainer(Container container)
        {
            foreach (IItemCondition condition in _globalItemConditions)
            {
                if (!container.ContainerCondition.Contains(condition))
                {
                    container.ContainerCondition.Add(condition);
                }
            }
        }

        /// <summary>
        ///     从指定容器移除全局条件
        /// </summary>
        /// <param name="container">目标容器</param>
        private void RemoveGlobalConditionsFromContainer(Container container)
        {
            foreach (IItemCondition condition in _globalItemConditions)
            {
                container.ContainerCondition.Remove(condition);
            }
        }

        #endregion

        #region 事件

        /// <summary>
        ///     容器注册事件
        /// </summary>
        public event Action<Container> OnContainerRegistered;

        /// <summary>
        ///     容器注销事件
        /// </summary>
        public event Action<Container> OnContainerUnregistered;

        /// <summary>
        ///     容器优先级变更事件
        /// </summary>
        public event Action<string, int> OnContainerPriorityChanged;

        /// <summary>
        ///     容器分类变更事件
        /// </summary>
        public event Action<string, string, string> OnContainerCategoryChanged;

        /// <summary>
        ///     全局条件添加事件
        /// </summary>
        public event Action<IItemCondition> OnGlobalConditionAdded;

        /// <summary>
        ///     全局条件移除事件
        /// </summary>
        public event Action<IItemCondition> OnGlobalConditionRemoved;

        /// <summary>
        ///     全局缓存刷新事件
        /// </summary>
        public event Action OnGlobalCacheRefreshed;

        /// <summary>
        ///     全局缓存验证事件
        /// </summary>
        public event Action OnGlobalCacheValidated;

        #endregion

        #region 全局缓存

        /// <summary>
        ///     刷新全局缓存
        /// </summary>
        public void RefreshGlobalCache()
        {
            if (!IsServiceAvailable()) return;

            lock (_lock)
            {
                foreach (Container container in _containers.Values)
                {
                    container?.RebuildCaches();
                }
            }

            OnGlobalCacheRefreshed?.Invoke();
        }

        /// <summary>
        ///     验证全局缓存
        /// </summary>
        public void ValidateGlobalCache()
        {
            if (!IsServiceAvailable()) return;

            lock (_lock)
            {
                foreach (Container container in _containers.Values)
                {
                    container?.ValidateCaches();
                }
            }

            OnGlobalCacheValidated?.Invoke();
        }

        #endregion
    }
}