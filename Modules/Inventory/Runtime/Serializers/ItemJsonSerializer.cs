using EasyPack.CustomData;
using EasyPack.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack.InventorySystem
{
    /// <summary>
    ///     Item类型的JSON序列化器
    ///     直接使用 CustomDataEntry，无需中间转换
    /// </summary>
    public class ItemJsonSerializer : JsonSerializerBase<Item>
    {
        public override string SerializeToJson(Item obj)
        {
            if (obj == null) return null;

            var dto = new SerializedItem
            {
                ID = obj.ID,
                Name = obj.Name,
                Type = obj.Type,
                Description = obj.Description,
                Weight = obj.Weight,
                IsStackable = obj.IsStackable,
                MaxStackCount = obj.MaxStackCount,
                isContanierItem = obj.IsContainerItem,
                CustomData = obj.CustomData is { Count: > 0 }
                    ? new List<CustomDataEntry>(obj.CustomData)
                    : null,
                ContainerIds = obj.IsContainerItem && obj.ContainerIds is { Count: > 0 }
                    ? new List<string>(obj.ContainerIds)
                    : null,
            };

            return JsonUtility.ToJson(dto);
        }

        public override Item DeserializeFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;

            SerializedItem dto;
            try
            {
                dto = JsonUtility.FromJson<SerializedItem>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ItemJsonSerializer] 反序列化失败: {e.Message}");
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
                IsContainerItem = dto.isContanierItem,
            };

            // 反序列化 CustomData
            if (dto.CustomData is { Count: > 0 })
                item.CustomData = new(dto.CustomData);
            else
                item.CustomData = new();

            // 反序列化容器ID列表
            if (dto.ContainerIds is { Count: > 0 })
            {
                item.IsContainerItem = true;
                item.ContainerIds = new(dto.ContainerIds);
            }

            return item;
        }
    }
}