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
        public List<string> ContainerIds { get; set; } // 容器类型的物品对于的ID区域

        #endregion

        #region 克隆
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

            if (this.ContainerIds != null && this.ContainerIds.Count > 0)
            {
                clone.ContainerIds = new List<string>(this.ContainerIds);
            }

            return clone;
        }
        #endregion

        #region 序列化
        public string ToJson(bool prettyPrint = false)
        {
            var dto = new SerializeableItem
            {
                ID = this.ID,
                Name = this.Name,
                Type = this.Type,
                Description = this.Description,
                Weight = this.Weight,
                IsStackable = this.IsStackable,
                MaxStackCount = this.MaxStackCount,
                isContanierItem = this.isContanierItem,
                Attributes = CustomDataUtility.ToEntries(this.Attributes),
                ContainerIds = (this.isContanierItem && this.ContainerIds != null && this.ContainerIds.Count > 0)
                    ? new List<string>(this.ContainerIds)
                    : null
            };

            return JsonUtility.ToJson(dto, prettyPrint);
        }

        public static Item FromJson(string json, ICustomDataSerializer fallbackSerializer = null)
        {
            if (string.IsNullOrEmpty(json)) return null;

            SerializeableItem dto = null;
            try
            {
                dto = JsonUtility.FromJson<SerializeableItem>(json);
            }
            catch
            {
                return null;
            }
            if (dto == null) return null;

            var item = new Item
            {
                ID = dto.ID,
                Name = dto.Name,
                Type = dto.Type,
                Description = dto.Description,
                Weight = dto.Weight,
                IsStackable = dto.IsStackable,
                MaxStackCount = dto.MaxStackCount,
                isContanierItem = dto.isContanierItem,
            };

            if (dto.Attributes != null)
            {
                // 若存在自定义序列化类型，注入回条目以便 GetValue 能正确解析 Custom 类型
                if (fallbackSerializer != null)
                {
                    for (int i = 0; i < dto.Attributes.Count; i++)
                    {
                        var e = dto.Attributes[i];
                        if (e != null) e.Serializer = fallbackSerializer;
                    }
                }
                item.Attributes = CustomDataUtility.ToDictionary(dto.Attributes);
            }
            else
            {
                item.Attributes = new Dictionary<string, object>();
            }

            if (dto.ContainerIds != null && dto.ContainerIds.Count > 0)
            {
                item.isContanierItem = true;
                item.ContainerIds = new List<string>(dto.ContainerIds);
            }

            return item;
        }
        #endregion
    }
}