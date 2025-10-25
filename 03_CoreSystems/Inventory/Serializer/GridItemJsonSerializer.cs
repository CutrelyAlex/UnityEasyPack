using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// GridItem 的序列化数据传输对象
    /// </summary>
    [Serializable]
    public class SerializedGridItem
    {
        public string ID;
        public string Name;
        public string Type;
        public string Description;
        public int MaxStackCount;
        public bool IsStackable;
        public float Weight;
        public bool isContanierItem;
        public List<CustomDataEntry> Attributes;
        public List<string> ContainerIds;

        // GridItem 特有属性
        public int GridWidth;
        public int GridHeight;
        public bool CanRotate;
        public int Rotation; // 旋转角度 (0=0°, 1=90°, 2=180°, 3=270°)
    }

    /// <summary>
    /// GridItem 序列化器
    /// </summary>
    public class GridItemJsonSerializer : JsonSerializerBase<GridItem>
    {
        public override string SerializeToJson(GridItem item)
        {
            if (item == null) return null;

            var dto = new SerializedGridItem
            {
                ID = item.ID,
                Name = item.Name,
                Type = item.Type,
                Description = item.Description,
                MaxStackCount = item.MaxStackCount,
                IsStackable = item.IsStackable,
                Weight = item.Weight,
                isContanierItem = item.IsContanierItem,
                Attributes = CustomDataUtility.ToEntries(item.Attributes),
                ContainerIds = (item.IsContanierItem && item.ContainerIds != null && item.ContainerIds.Count > 0)
                    ? new List<string>(item.ContainerIds)
                    : null,
                GridWidth = item.GridWidth,
                GridHeight = item.GridHeight,
                CanRotate = item.CanRotate,
                Rotation = (int)item.Rotation
            };

            return JsonUtility.ToJson(dto, true);
        }

        public override GridItem DeserializeFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;

            SerializedGridItem dto;
            try
            {
                dto = JsonUtility.FromJson<SerializedGridItem>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GridItemJsonSerializer] 反序列化失败: {e.Message}");
                return null;
            }

            if (dto == null) return null;

            var item = new GridItem
            {
                ID = dto.ID,
                Name = dto.Name,
                Type = dto.Type,
                Description = dto.Description,
                MaxStackCount = dto.MaxStackCount,
                IsStackable = dto.IsStackable,
                Weight = dto.Weight,
                IsContanierItem = dto.isContanierItem,
                GridWidth = dto.GridWidth,
                GridHeight = dto.GridHeight,
                CanRotate = dto.CanRotate,
                Rotation = (RotationAngle)dto.Rotation
            };

            // 反序列化自定义属性
            if (dto.Attributes != null && dto.Attributes.Count > 0)
            {
                item.Attributes = CustomDataUtility.ToDictionary(dto.Attributes);
            }
            else
            {
                item.Attributes = new Dictionary<string, object>();
            }

            // 反序列化容器ID列表
            if (dto.ContainerIds != null && dto.ContainerIds.Count > 0)
            {
                item.IsContanierItem = true;
                item.ContainerIds = new List<string>(dto.ContainerIds);
            }

            return item;
        }
    }
}
