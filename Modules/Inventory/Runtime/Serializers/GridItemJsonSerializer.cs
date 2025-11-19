using EasyPack.CustomData;
using EasyPack.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack.InventorySystem
{
    /// <summary>
    /// GridItem 的序列化数据传输对象
    /// </summary>
    [Serializable]
    public class SerializedGridItem : ISerializable
    {
        public string ID;
        public string Name;
        public string Type;
        public string Description;
        public int MaxStackCount;
        public bool IsStackable;
        public float Weight;
        public bool isContanierItem;

        /// <summary>自定义数据列表</summary>
        public List<CustomDataEntry> CustomData;

        public List<string> ContainerIds;

        // GridItem 特有属性
        public List<SerializedCell> Shape; // 形状的单元格坐标列表
        public bool CanRotate;
        public int Rotation; // 旋转角度 (0=0°, 1=90°, 2=180°, 3=270°)
    }

    /// <summary>
    /// 单元格坐标的序列化对象
    /// </summary>
    [Serializable]
    public class SerializedCell
    {
        public int x;
        public int y;
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
                isContanierItem = item.IsContainerItem,

                CustomData = item.CustomData != null && item.CustomData.Count > 0
                    ? new List<CustomDataEntry>(item.CustomData)
                    : null,
                ContainerIds = (item.IsContainerItem && item.ContainerIds != null && item.ContainerIds.Count > 0)
                    ? new List<string>(item.ContainerIds)
                    : null,
                Shape = item.Shape != null
                    ? item.Shape.ConvertAll(cell => new SerializedCell { x = cell.x, y = cell.y })
                    : new List<SerializedCell> { new() { x = 0, y = 0 } },
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
                IsContainerItem = dto.isContanierItem,
                Shape = dto.Shape != null && dto.Shape.Count > 0
                    ? dto.Shape.ConvertAll(cell => (cell.x, cell.y))
                    : new List<(int x, int y)> { (0, 0) },
                CanRotate = dto.CanRotate,
                Rotation = (RotationAngle)dto.Rotation
            };

            // 反序列化 CustomData
            if (dto.CustomData != null && dto.CustomData.Count > 0)
            {
                item.CustomData = new List<CustomDataEntry>(dto.CustomData);
            }
            else
            {
                item.CustomData = new List<CustomDataEntry>();
            }

            // 反序列化容器ID列表
            if (dto.ContainerIds != null && dto.ContainerIds.Count > 0)
            {
                item.IsContainerItem = true;
                item.ContainerIds = new List<string>(dto.ContainerIds);
            }

            return item;
        }
    }
}

