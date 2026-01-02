using System;
using System.Collections.Generic;
using EasyPack.CustomData;
using EasyPack.Serialization;
using UnityEngine;

namespace EasyPack.InventorySystem
{
    /// <summary>
    ///     Item类型的JSON序列化器
    ///     直接使用 CustomDataEntry，无需中间转换
    /// </summary>
    public class ItemJsonSerializer : ITypeSerializer<Item, SerializedItem>
    {
        public SerializedItem ToSerializable(Item obj)
        {
            if (obj == null) return null;
            return new()
            {
                ID = obj.ID,
                Name = obj.Name,
                Type = obj.Type,
                Description = obj.Description,
                Weight = obj.Weight,
                IsStackable = obj.IsStackable,
                MaxStackCount = obj.MaxStackCount,
                ItemUID = obj.ItemUID,
                isContanierItem = obj.IsContainerItem,
                CustomData = obj.CustomData is { Count: > 0 }
                    ? new List<CustomDataEntry>(obj.CustomData)
                    : null,
                ContainerIds = obj.IsContainerItem && obj.ContainerIds is { Count: > 0 }
                    ? new List<string>(obj.ContainerIds)
                    : null,
            };
        }

        public Item FromSerializable(SerializedItem dto)
        {
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
                ItemUID = dto.ItemUID,
                IsContainerItem = dto.isContanierItem,
            };
            if (dto.CustomData is { Count: > 0 })
            {
                item.CustomData = new(dto.CustomData);
            }
            else
            {
                item.CustomData = new();
            }

            if (dto.ContainerIds is { Count: > 0 })
            {
                item.IsContainerItem = true;
                item.ContainerIds = new(dto.ContainerIds);
            }

            return item;
        }

        public string ToJson(SerializedItem dto) => dto == null ? null : JsonUtility.ToJson(dto);

        public SerializedItem FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                return JsonUtility.FromJson<SerializedItem>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ItemJsonSerializer] 反序列化失败: {e.Message}");
                return null;
            }
        }

        public string SerializeToJson(Item obj)
        {
            SerializedItem dto = ToSerializable(obj);
            return ToJson(dto);
        }

        public Item DeserializeFromJson(string json)
        {
            SerializedItem dto = FromJson(json);
            return FromSerializable(dto);
        }
    }
}