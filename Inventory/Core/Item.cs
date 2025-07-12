using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

namespace EasyPack
{
    public class Item : IItem
    {
        #region 基本属性
        public string ID { get; set; }

        public string Name { get; set; }

        public string Type { get; set; } = "Default";
        public string Description { get; set; } = "";

        public float Weight { get; set; } = 1;

        public bool IsStackable { get; set; } = true;

        public int MaxStackCount { get; set; } = -1; // -1表示无限堆叠

        public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();


        public bool isContanierItem = false;
        public List<IContainer> Containers { get; set; } // 嵌套的容器

        #endregion

        #region 多格物品

        public bool IsMultiSlot { get; set; } = false;
        public Vector2Int Size { get; set; } = new Vector2Int(1, 1);

        #endregion

        /// <summary>
        /// 创建物品的深拷贝
        /// </summary>
        /// <returns>物品的副本</returns>
        public IItem Clone()
        {
            var clone = new Item
            {
                ID = this.ID,
                Name = this.Name,
                Type = this.Type,
                Description = this.Description,
                Weight = this.Weight,
                IsStackable = this.IsStackable,
                MaxStackCount = this.MaxStackCount,
                IsMultiSlot = this.IsMultiSlot,
                Size = this.Size,
                isContanierItem = this.isContanierItem
            };

            // 深度复制属性字典
            if (this.Attributes != null)
            {
                clone.Attributes = new Dictionary<string, object>();
                foreach (var kvp in this.Attributes)
                {
                    clone.Attributes[kvp.Key] = kvp.Value;
                }
            }

            // 深度复制容器列表（如果有的话）
            if (this.Containers != null && this.Containers.Count > 0)
            {
                clone.Containers = new List<IContainer>(this.Containers);
            }

            return clone;
        }
    }
}