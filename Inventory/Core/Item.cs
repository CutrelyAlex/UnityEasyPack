using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

namespace EasyPack
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
        Dictionary<string, object> Attributes { get; set; }
        IItem Clone();
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

        public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();


        public bool isContanierItem = false;
        public List<IContainer> Containers { get; set; } // 容器类型的物品

        #endregion

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
                isContanierItem = this.isContanierItem
            };

            if (this.Attributes != null)
            {
                clone.Attributes = new Dictionary<string, object>();
                foreach (var kvp in this.Attributes)
                {
                    clone.Attributes[kvp.Key] = kvp.Value;
                }
            }

            if (this.Containers != null && this.Containers.Count > 0)
            {
                clone.Containers = new List<IContainer>(this.Containers);
            }

            return clone;
        }
    }
}