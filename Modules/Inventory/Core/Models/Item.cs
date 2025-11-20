using EasyPack.CustomData;
using System.Collections.Generic;

namespace EasyPack.InventorySystem
{
    public interface IItem
    {
        string ID { get; }
        string Name { get; }
        string Type { get; }
        string Description { get; }
        bool IsStackable { get; }

        float Weight { get; set; }
        int MaxStackCount { get; }

        /// <summary>自定义数据列表，支持多种类型的键值对存储</summary>
        CustomDataCollection CustomData { get; set; }

        IItem Clone();
    }

    /// <summary>
    /// IItem 接口的扩展方法，提供 CustomData 操作的便利方法
    /// </summary>
    public static class IItemExtensions
    {
        /// <summary>获取自定义数据值</summary>
        /// <typeparam name="T">期望的值类型</typeparam>
        /// <param name="item">物品实例</param>
        /// <param name="id">数据键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>找到的值或默认值</returns>
        public static T GetCustomData<T>(this IItem item, string id, T defaultValue = default)
        {
            return CustomDataUtility.GetValue(item.CustomData, id, defaultValue);
        }

        /// <summary>设置自定义数据值</summary>
        /// <param name="item">物品实例</param>
        /// <param name="id">数据键</param>
        /// <param name="value">要设置的值</param>
        public static void SetCustomData(this IItem item, string id, object value)
        {
            item.CustomData ??= new CustomDataCollection();

            CustomDataUtility.SetValue(item.CustomData, id, value);
        }

        /// <summary>移除自定义数据</summary>
        /// <param name="item">物品实例</param>
        /// <param name="id">数据键</param>
        /// <returns>是否成功移除</returns>
        public static bool RemoveCustomData(this IItem item, string id)
        {
            return CustomDataUtility.RemoveValue(item.CustomData, id);
        }

        /// <summary>检查是否存在自定义数据</summary>
        /// <param name="item">物品实例</param>
        /// <param name="id">数据键</param>
        /// <returns>是否存在</returns>
        public static bool HasCustomData(this IItem item, string id)
        {
            return CustomDataUtility.HasValue(item.CustomData, id);
        }
    }

    public class Item : IItem
    {
        #region 基本属性
        public string ID { get; set; }

        public string Name { get; set; }

        public string Type { get; set; } = "Default";
        public string Description { get; set; } = "";

        public float Weight { get; set; } = 1;

        public bool IsStackable { get; set; } = true;

        public int MaxStackCount { get; set; } = -1; // -1代表无限堆叠

        /// <summary>
        /// 自定义数据列表
        /// </summary>
        public CustomDataCollection CustomData { get; set; } = new CustomDataCollection();


        public bool IsContainerItem = false;
        public List<string> ContainerIds { get; set; } // 容器类型的物品对应的ID区域

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
                Weight = Weight,
                IsStackable = IsStackable,
                MaxStackCount = MaxStackCount,
                IsContainerItem = IsContainerItem
            };

            clone.CustomData = CustomDataUtility.Clone(CustomData);

            if (ContainerIds != null && ContainerIds.Count > 0)
            {
                clone.ContainerIds = new List<string>(ContainerIds);
            }

            return clone;
        }
        #endregion

        #region CustomData 辅助方法

        /// <summary>获取自定义数据值</summary>
        public T GetCustomData<T>(string id, T defaultValue = default) => CustomDataUtility.GetValue(CustomData, id, defaultValue);

        /// <summary>设置自定义数据值</summary>
        public void SetCustomData(string id, object value)
        {
            if (CustomData == null)
                CustomData = new CustomDataCollection();

            CustomDataUtility.SetValue(CustomData, id, value);
        }

        /// <summary>移除自定义数据</summary>
        public bool RemoveCustomData(string id) => CustomDataUtility.RemoveValue(CustomData, id);

        /// <summary>检查是否存在自定义数据</summary>
        public bool HasCustomData(string id) => CustomDataUtility.HasValue(CustomData, id);

        #endregion
    }
}

