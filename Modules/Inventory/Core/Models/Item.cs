using System.Collections.Generic;
using System.Linq;
using EasyPack.CustomData;

namespace EasyPack.InventorySystem
{
    public interface IItem
    {
        string ID { get; }
        string Name { get; }
        string Type { get; }
        string Description { get; }
        bool IsStackable { get; }

        /// <summary>
        ///     物品的全局唯一标识符
        ///     由InventoryService统一分配和管理
        ///     -1 表示未分配
        /// </summary>
        long ItemUID { get; set; }

        float Weight { get; set; }
        int MaxStackCount { get; }

        /// <summary>
        ///     物品堆的数量（默认值1）
        ///     由Item持有，Container操作时修改此属性
        /// </summary>
        int Count { get; set; }

        /// <summary>
        ///     分类路径（由InventoryService的CategoryManager管理）
        /// </summary>
        string Category { get; }

        /// <summary>
        ///     标签数组（由InventoryService的CategoryManager管理）
        /// </summary>
        string[] Tags { get; }

        /// <summary>
        ///     运行时元数据（由InventoryService的CategoryManager管理）
        ///     堆叠时必须严格匹配
        /// </summary>
        CustomDataCollection RuntimeMetadata { get; }

        /// <summary>
        ///     是否拥有运行时元数据
        /// </summary>
        bool HasRuntimeMetadata { get; }

        /// <summary>
        ///     判断是否可以与另一个物品堆叠
        ///     堆叠条件：ID相同、Type相同、RuntimeMetadata严格匹配
        ///     不比较Category/Tags
        /// </summary>
        /// <param name="other">要判断堆叠的另一个物品</param>
        /// <returns>是否可以堆叠</returns>
        bool CanStack(IItem other);

        /// <summary>自定义数据列表，支持多种类型的键值对存储</summary>
        CustomDataCollection CustomData { get; set; }

        IItem Clone();
    }

    /// <summary>
    ///     IItem 接口的扩展方法，提供 CustomData 操作的便利方法
    /// </summary>
    public static class IItemExtensions
    {
        /// <summary>获取自定义数据值</summary>
        /// <typeparam name="T">期望的值类型</typeparam>
        /// <param name="item">物品实例</param>
        /// <param name="id">数据键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>找到的值或默认值</returns>
        public static T GetCustomData<T>(this IItem item, string id, T defaultValue = default) =>
            item.CustomData.Get(id, defaultValue);

        /// <summary>设置自定义数据值</summary>
        /// <param name="item">物品实例</param>
        /// <param name="id">数据键</param>
        /// <param name="value">要设置的值</param>
        public static void SetCustomData(this IItem item, string id, object value)
        {
            if (item is Item concreteItem && concreteItem.InventoryService?.CategoryManager != null && concreteItem.ItemUID >= 0)
            {
                var metadata = concreteItem.InventoryService.CategoryManager.GetOrAddMetadata(concreteItem.ItemUID);
                if (metadata != null)
                {
                    metadata.Set(id, value);
                    return;
                }
            }
            
            // 回退到本地存储
            item.CustomData.Set(id, value);
        }

        /// <summary>移除自定义数据</summary>
        /// <param name="item">物品实例</param>
        /// <param name="id">数据键</param>
        /// <returns>是否成功移除</returns>
        public static bool RemoveCustomData(this IItem item, string id)
        {
            if (item is Item concreteItem && concreteItem.InventoryService?.CategoryManager != null && concreteItem.ItemUID >= 0)
            {
                if (!concreteItem.InventoryService.CategoryManager.HasMetadata(concreteItem.ItemUID)) return false;
                var metadata = concreteItem.InventoryService.CategoryManager.GetMetadata(concreteItem.ItemUID);
                return metadata.Remove(id);
            }

            return item.CustomData.Remove(id);
        }

        /// <summary>检查是否存在自定义数据</summary>
        /// <param name="item">物品实例</param>
        /// <param name="id">数据键</param>
        /// <returns>是否存在</returns>
        public static bool HasCustomData(this IItem item, string id) => item.CustomData.HasValue(id);
    }

    public class Item : IItem
    {
        #region 基本属性

        public string ID { get; set; }

        public string Name { get; set; }

        public string Type { get; set; } = "Default";
        public string Description { get; set; } = "";

        /// <summary>
        ///     物品的全局唯一标识符
        ///     由InventoryService统一分配和管理
        ///     -1 表示未分配
        /// </summary>
        public long ItemUID { get; set; } = -1;

        public float Weight { get; set; } = 1;

        public bool IsStackable { get; set; } = true;

        public int MaxStackCount { get; set; } = -1; // -1 代表无限堆叠

        /// <summary>
        ///     物品堆的数量（默认值1）
        ///     由Container负责验证和修改
        /// </summary>
        public int Count { get; set; } = 1;

        private CustomDataCollection _localCustomData = new();

        /// <summary>
        ///     自定义数据列表
        ///     优先从 CategoryManager 获取受管数据，否则返回本地数据
        /// </summary>
        public CustomDataCollection CustomData
        {
            get
            {
                // 如果有 CategoryManager 且 ItemUID 有效，返回受管元数据
                if (InventoryService?.CategoryManager != null && ItemUID >= 0)
                {
                    return InventoryService.CategoryManager.GetMetadata(ItemUID) ?? _localCustomData;
                }
                // 否则返回本地数据
                return _localCustomData;
            }
            set
            {
                if (InventoryService?.CategoryManager != null && ItemUID >= 0)
                {
                    InventoryService.CategoryManager.UpdateMetadata(ItemUID, value);
                }
                else
                {
                    _localCustomData = value;
                }
            }
        }

        /// <summary>
        ///     关联的InventoryService（用于访问CategoryManager）
        ///     可选引用，在ItemFactory创建时设置
        /// </summary>
        public IInventoryService InventoryService { get; set; }

        public bool IsContainerItem;
        public List<string> ContainerIds { get; set; } // 容器类型的物品对应的ID区域

        #endregion

        #region CategoryManager 接口

        /// <summary>
        ///     分类路径
        ///     由InventoryService的CategoryManager管理
        /// </summary>
        public string Category
        {
            get
            {
                if (InventoryService?.CategoryManager != null && ItemUID >= 0)
                {
                    return InventoryService.CategoryManager.GetReadableCategoryPath(ItemUID);
                }
                return string.Empty;
            }
        }

        /// <summary>
        ///     标签数组（由InventoryService的CategoryManager管理）
        /// </summary>
        public string[] Tags
        {
            get
            {
                if (InventoryService?.CategoryManager == null || ItemUID < 0)
                {
                    return System.Array.Empty<string>();
                }
                var tags = InventoryService.CategoryManager.GetTags(this);
                return tags != null ? tags.ToArray() : System.Array.Empty<string>();
            }
        }

        /// <summary>
        ///     运行时元数据（由InventoryService的CategoryManager管理）
        /// </summary>
        public CustomDataCollection RuntimeMetadata
        {
            get
            {
                if (InventoryService?.CategoryManager == null || ItemUID < 0)
                {
                    return null;
                }

                return InventoryService.CategoryManager.HasMetadata(ItemUID) 
                    ? InventoryService.CategoryManager.GetMetadata(ItemUID) 
                    : null;
            }
        }

        /// <summary>
        ///     是否拥有运行时元数据
        /// </summary>
        public bool HasRuntimeMetadata
        {
            get
            {
                if (InventoryService?.CategoryManager == null || ItemUID < 0)
                {
                    return false;
                }
                return InventoryService.CategoryManager.HasMetadata(ItemUID);
            }
        }

        #endregion

        #region 堆叠检查

        /// <summary>
        ///     判断是否可以与另一个物品堆叠
        ///     堆叠条件：ID相同、Type相同、RuntimeMetadata严格匹配
        ///     不比较Category/Tags（创建后不可变）
        /// </summary>
        /// <param name="other">要判断堆叠的另一个物品</param>
        /// <returns>是否可以堆叠</returns>
        public bool CanStack(IItem other)
        {
            // 检查ID和Type是否相同
            if (other == null || ID != other.ID || Type != other.Type)
                return false;

            // 检查堆叠状态
            if (!IsStackable || !other.IsStackable)
                return false;

            // 检查RuntimeMetadata是否匹配
            // 两个物品的RuntimeMetadata都为null时，视为相同
            if (RuntimeMetadata == null && other.RuntimeMetadata == null)
                return true;

            // 其中一个为null，另一个不为null，则不匹配
            if (RuntimeMetadata == null || other.RuntimeMetadata == null)
                return false;

            // 都不为null，进行比较 
            return RuntimeMetadata.Equals(other.RuntimeMetadata);
        }

        #endregion

        #region 克隆

        public IItem Clone()
        {
            var clone = new Item
            {
                ID = ID,
                Name = Name,
                Type = Type,
                Description = Description,
                ItemUID = -1, // 克隆的物品需要重新分配UID
                Weight = Weight,
                IsStackable = IsStackable,
                MaxStackCount = MaxStackCount,
                Count = Count, // 克隆时保留Count
                IsContainerItem = IsContainerItem,
                CustomData = CustomData.Clone(),
                InventoryService = InventoryService // 保持对InventoryService的引用（浅引用）
            };

            if (ContainerIds is { Count: > 0 }) clone.ContainerIds = new(ContainerIds);

            return clone;
        }

        #endregion

        #region CustomData 辅助方法

        /// <summary>获取自定义数据值</summary>
        public T GetCustomData<T>(string id, T defaultValue = default) => CustomData.Get(id, defaultValue);

        /// <summary>设置自定义数据值</summary>
        public void SetCustomData(string id, object value)
        {
            if (InventoryService?.CategoryManager != null && ItemUID >= 0)
            {
                var metadata = InventoryService.CategoryManager.GetOrAddMetadata(ItemUID);
                if (metadata != null)
                {
                    metadata.Set(id, value);
                    return;
                }
            }
            // 本地存储
            _localCustomData ??= new();
            _localCustomData.Set(id, value);
        }

        /// <summary>移除自定义数据</summary>
        public bool RemoveCustomData(string id)
        {
            if (InventoryService?.CategoryManager != null && ItemUID >= 0)
            {
                if (!InventoryService.CategoryManager.HasMetadata(ItemUID)) return false;
                var metadata = InventoryService.CategoryManager.GetMetadata(ItemUID);
                return metadata != null && metadata.Remove(id);
            }
            return _localCustomData.Remove(id);
        }

        /// <summary>检查是否存在自定义数据</summary>
        public bool HasCustomData(string id) => CustomData.HasValue(id);

        #endregion
    }
}