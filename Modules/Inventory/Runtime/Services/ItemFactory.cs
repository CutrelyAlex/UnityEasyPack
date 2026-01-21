using System.Collections.Generic;
using EasyPack.CustomData;
using UnityEngine;

namespace EasyPack.InventorySystem
{
    /// <summary>
    ///     物品工厂实现，负责ItemData注册和Item实例创建
    /// </summary>
    public class ItemFactory : IItemFactory
    {
        /// <summary>ItemData注册表</summary>
        private readonly Dictionary<string, ItemData> _itemDataRegistry = new();

        /// <summary>UID分配器，单调递增</summary>
        private long _nextUID = 1;

        /// <summary>InventoryService引用，所有创建的Item都通过浅引用指向它</summary>
        private IInventoryService _inventoryService;

        /// <summary>
        ///     构造函数
        /// </summary>
        /// <param name="inventoryService">InventoryService实例</param>
        public ItemFactory(IInventoryService inventoryService = null)
        {
            _inventoryService = inventoryService;
        }

        /// <summary>
        ///     注册ItemData模板
        /// </summary>
        public void Register(string itemId, ItemData itemData)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                Debug.LogError("物品ID不能为空");
                return;
            }

            if (itemData == null)
            {
                Debug.LogError($"物品数据不能为空: {itemId}");
                return;
            }

            _itemDataRegistry[itemId] = itemData;
        }

        /// <summary>
        ///     根据ItemData创建Item实例
        /// </summary>
        public IItem CreateItem(ItemData itemData, int count = 1)
        {
            if (itemData == null)
            {
                Debug.LogError("物品数据不能为空");
                return null;
            }

            var item = new Item
            {
                ID = itemData.ID,
                Name = itemData.Name,
                Description = itemData.Description,
                Weight = itemData.Weight,
                IsStackable = itemData.IsStackable,
                MaxStackCount = itemData.MaxStackCount,
                Count = count,
                ItemUID = -1, // 未分配，由InventoryService分配
                CustomData = new CustomDataCollection(),
                InventoryService = _inventoryService
            };

            // 自动分配UID并注册到CategoryManager
            if (_inventoryService != null)
            {
                // 分配UID
                long uid = _inventoryService.AssignItemUID(item);
                
                // 注册到CategoryManager（使用 ItemData.Category）
                if (_inventoryService.CategoryManager != null && !string.IsNullOrEmpty(itemData.Category))
                {
                    // 准备Metadata
                    CustomDataCollection runtimeMetadata = null;
                    if (itemData.DefaultMetadata != null && itemData.DefaultMetadata.Count > 0)
                    {
                        runtimeMetadata = new CustomDataCollection();
                        foreach (var entry in itemData.DefaultMetadata)
                        {
                            runtimeMetadata.Set(entry.Key, entry.GetValue());
                        }
                    }

                    if (runtimeMetadata != null)
                    {
                        _inventoryService.CategoryManager.RegisterEntityWithMetadata(uid, item, itemData.Category, runtimeMetadata);
                    }
                    else
                    {
                        _inventoryService.CategoryManager.RegisterEntity(uid, item, itemData.Category);
                    }
                    
                    // 添加默认Tags
                    if (itemData.DefaultTags != null && itemData.DefaultTags.Length > 0)
                    {
                        foreach (var tag in itemData.DefaultTags)
                        {
                            _inventoryService.CategoryManager.AddTag(uid, tag);
                        }
                    }
                }
            }

            return item;
        }

        /// <summary>
        ///     根据物品ID创建Item实例
        /// </summary>
        public IItem CreateItem(string itemId, int count = 1)
        {
            if (!_itemDataRegistry.TryGetValue(itemId, out var itemData))
            {
                Debug.LogError($"物品未注册: {itemId}");
                return null;
            }

            return CreateItem(itemData, count);
        }

        /// <summary>
        ///     克隆物品，生成新的Item实例
        /// </summary>
        public IItem CloneItem(IItem sourceItem, int count = -1)
        {
            if (sourceItem == null)
            {
                Debug.LogError("源物品不能为空");
                return null;
            }

            var clonedItem = sourceItem.Clone() as Item;
            if (clonedItem != null)
            {
                clonedItem.ItemUID = -1; // 克隆的物品需要重新分配UID
                if (count > 0)
                {
                    clonedItem.Count = count;
                }

                //  自动分配UID并注册到CategoryManager
                if (_inventoryService != null)
                {
                    // 设置克隆物品的InventoryService引用
                    clonedItem.InventoryService = _inventoryService;
                    
                    // 分配新的UID
                    _inventoryService.AssignItemUID(clonedItem);
                    
                    // 如果原Item有Category或Type，注册到CategoryManager
                    var category = sourceItem.Category;
                    if (string.IsNullOrEmpty(category) && !string.IsNullOrEmpty(sourceItem.Type))
                    {
                        // 如果没有明确Category，使用Type作为默认分类
                        category = sourceItem.Type;
                    }
                    
                    if (_inventoryService.CategoryManager != null && !string.IsNullOrEmpty(category))
                    {
                        _inventoryService.CategoryManager.RegisterEntity(clonedItem.ItemUID, clonedItem, category);
                        
                        // 复制Tags
                        var tags = sourceItem.Tags;
                        if (tags != null && tags.Length > 0)
                        {
                            foreach (var tag in tags)
                            {
                                _inventoryService.CategoryManager.AddTag(clonedItem.ItemUID, tag);
                            }
                        }
                        
                        // 深拷贝RuntimeMetadata
                        var sourceMetadata = sourceItem.RuntimeMetadata;
                        if (sourceMetadata != null && sourceMetadata.Count > 0)
                        {
                            // 确保注册了实体后再获取元数据
                            var regMetadata = _inventoryService.CategoryManager.GetOrAddMetadata(clonedItem.ItemUID);
                            if (regMetadata != null)
                            {
                                foreach (var entry in sourceMetadata)
                                {
                                    regMetadata.Set(entry.Key, entry.GetValue());
                                }
                            }
                        }
                    }
                }
            }

            return clonedItem;
        }

        /// <summary>
        ///     分配新的唯一ItemUID
        /// </summary>
        public long AllocateItemUID()
        {
            return _nextUID++;
        }

        /// <summary>
        ///     重置UID计数器（用于反序列化或重新初始化）
        /// </summary>
        public void ResetUIDCounter(long maxUID)
        {
            _nextUID = maxUID + 1;
        }
    }
}