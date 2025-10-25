using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// Item类型的JSON序列化器
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
                isContanierItem = obj.IsContanierItem,
                Attributes = CustomDataUtility.ToEntries(obj.Attributes),
                ContainerIds = (obj.IsContanierItem && obj.ContainerIds != null && obj.ContainerIds.Count > 0)
                    ? new List<string>(obj.ContainerIds)
                    : null
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
                IsContanierItem = dto.isContanierItem,
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
