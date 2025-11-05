using System.Collections.Generic;

namespace EasyPack.InventorySystem
{
    /// <summary>
    /// 多个容器管理的系统
    /// </summary>
    public partial class InventoryService
    {
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

            if (!IsServiceAvailable() || string.IsNullOrEmpty(itemId))
            {
                return results;
            }

            try
            {
                lock (_lock)
                {
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
            }
            catch
            {
                results.Clear();
                lock (_lock)
                {
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
            if (!IsServiceAvailable() || string.IsNullOrEmpty(itemId))
            {
                return 0;
            }

            int totalCount = 0;
            lock (_lock)
            {
                foreach (var container in _containers.Values)
                {
                    totalCount += container.GetItemTotalCount(itemId);
                }
            }

            return totalCount;
        }

        /// <summary>
        /// 查找包含指定物品的容器
        /// </summary>
        /// <param name="itemId">物品ID</param>
        /// <returns>包含该物品的容器列表和数量</returns>
        public Dictionary<string, int> FindContainersWithItem(string itemId)
        {
            var results = new Dictionary<string, int>();

            if (!IsServiceAvailable() || string.IsNullOrEmpty(itemId))
            {
                return results;
            }

            try
            {
                lock (_lock)
                {
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
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[InventoryService] 操作失败：{ex.Message}");
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

            if (!IsServiceAvailable() || condition == null)
            {
                return results;
            }

            try
            {
                lock (_lock)
                {
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
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[InventoryService] 操作失败：{ex.Message}");
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

            if (!IsServiceAvailable() || string.IsNullOrEmpty(itemType))
            {
                return results;
            }

            try
            {
                lock (_lock)
                {
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

            if (!IsServiceAvailable() || string.IsNullOrEmpty(namePattern))
            {
                return results;
            }

            try
            {
                lock (_lock)
                {
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

            if (!IsServiceAvailable() || string.IsNullOrEmpty(attributeName))
            {
                return results;
            }

            try
            {
                lock (_lock)
                {
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
    }
}