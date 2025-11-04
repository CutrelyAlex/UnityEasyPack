
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
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[InventoryManager] 注销容器失败：{ex.Message}\n{ex.StackTrace}");
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
            return string.IsNullOrEmpty(containerId) ? null : _containers.GetValueOrDefault(containerId);
        }

        /// <summary>
        /// 获取所有已注册的容器
        /// </summary>
        /// <returns>所有容器的只读列表</returns>
        public IReadOnlyList<Container> GetAllContainers()
        {
            return _containers.Values.ToList().AsReadOnly();
        }

        /// <summary>
        /// 按类型获取容器
        /// </summary>
        /// <param name="containerType">容器类型</param>
        /// <returns>指定类型的容器列表</returns>
        public List<Container> GetContainersByType(string containerType)
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

        /// <summary>
        /// 按分类获取容器
        /// </summary>
        /// <param name="category">分类名称</param>
        /// <returns>指定分类的容器列表</returns>
        public List<Container> GetContainersByCategory(string category)
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

        /// <summary>
        /// 按优先级排序获取容器
        /// </summary>
        /// <param name="descending">是否降序排列（优先级高的在前）</param>
        /// <returns>按优先级排序的容器列表</returns>
        public List<Container> GetContainersByPriority(bool descending = true)
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

        /// <summary>
        /// 检查容器是否已注册
        /// </summary>
        /// <param name="containerId">容器ID</param>
        /// <returns>是否已注册</returns>
        public bool IsContainerRegistered(string containerId)
        {
            return !string.IsNullOrEmpty(containerId) && _containers.ContainsKey(containerId);
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
            if (!IsContainerRegistered(containerId))
                return false;

            _containerPriorities[containerId] = priority;
            OnContainerPriorityChanged?.Invoke(containerId, priority);
            return true;
        }

        /// <summary>
        /// 获取容器优先级
        /// </summary>
        /// <param name="containerId">容器ID</param>
        /// <returns>容器优先级，未找到返回0</returns>
        public int GetContainerPriority(string containerId)
        {
            if (string.IsNullOrEmpty(containerId))
                return 0;

            return _containerPriorities.GetValueOrDefault(containerId, 0);
        }

        /// <summary>
        /// 设置容器分类
        /// </summary>
        /// <param name="containerId">容器ID</param>
        /// <param name="category">分类名称</param>
        /// <returns>设置是否成功</returns>
        public bool SetContainerCategory(string containerId, string category)
        {
            if (!IsContainerRegistered(containerId))
                return false;

            string oldCategory = _containerCategories.GetValueOrDefault(containerId, "Default");
            _containerCategories[containerId] = category ?? "Default";
            OnContainerCategoryChanged?.Invoke(containerId, oldCategory, category);
            return true;
        }

        /// <summary>
        /// 获取容器分类
        /// </summary>
        /// <param name="containerId">容器ID</param>
        /// <returns>容器分类，未找到返回"Default"</returns>
        public string GetContainerCategory(string containerId)
        {
            if (string.IsNullOrEmpty(containerId))
                return "Default";

            return _containerCategories.GetValueOrDefault(containerId, "Default");
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
            if (item == null) return false;
            if (!_enableGlobalConditions)
                return true;

            // 用户提供的条件检查可能抛异常，需要保护
            try
            {
                foreach (var condition in _globalItemConditions)
                {
                    if (!condition.CheckCondition(item))
                        return false;
                }
                return true;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[InventoryManager] 全局条件检查失败：{ex.Message}");
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
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[InventoryManager] 操作失败：{ex.Message}"); // 静默处理异常
            }
        }

        /// <summary>
        /// 移除全局物品条件
        /// </summary>
        /// <param name="condition">物品条件</param>
        /// <returns>移除是否成功</returns>
        public bool RemoveGlobalItemCondition(IItemCondition condition)
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

        /// <summary>
        /// 设置是否启用全局条件
        /// </summary>
        /// <param name="enable">是否启用</param>
        public void SetGlobalConditionsEnabled(bool enable)
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
            foreach (var condition in _globalItemConditions)
            {
                if (!container.ContainerCondition.Contains(condition))
                {
                    container.ContainerCondition.Add(condition);
                }
            }
        }

        /// <summary>
        /// 从指定容器移除全局条件
        /// </summary>
        /// <param name="container">目标容器</param>
        private void RemoveGlobalConditionsFromContainer(Container container)
        {
            foreach (var condition in _globalItemConditions)
            {
                container.ContainerCondition.Remove(condition);
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

        #region 全局缓存
        /// <summary>
        /// 刷新全局缓存
        /// </summary>
        public void RefreshGlobalCache()
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

        /// <summary>
        /// 验证全局缓存
        /// </summary>
        public void ValidateGlobalCache()
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
        #endregion
    }
}