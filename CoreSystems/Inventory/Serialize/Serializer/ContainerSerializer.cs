using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace EasyPack
{
    public static class ContainerSerializer
    {
        // 确保注册器已初始化
        static ContainerSerializer()
        {
            SerializationRegistry.EnsureInitialized();
        }

        // Container -> DTO
        public static SerializedContainer Serialize(Container container, bool prettyPrint = false)
        {
            if (container == null) return null;

            var dto = new SerializedContainer
            {
                ContainerKind = container.GetType().Name,
                ID = container.ID,
                Name = container.Name,
                Type = container.Type,
                Capacity = container.Capacity,
                IsGrid = container.IsGrid,
                Grid = container.IsGrid ? container.Grid : new Vector2(-1, -1)
            };

            // 容器条件（使用注册器序列化）
            if (container.ContainerCondition != null)
            {
                foreach (var cond in container.ContainerCondition)
                {
                    if (cond != null)
                    {
                        var c = SerializationRegistry.SerializeCondition(cond);
                        if (c != null)
                        {
                            dto.ContainerConditions.Add(c);
                        }
                        else
                        {
                            Debug.LogWarning($"条件序列化失败: {cond?.GetType().Name}");
                        }
                    }
                }
            }

            // 槽位
            foreach (var slot in container.Slots)
            {
                if (slot == null || !slot.IsOccupied || slot.Item == null) continue;

                string itemJson = null;
                if (slot.Item is Item concrete)
                {
                    itemJson = concrete.ToJson(prettyPrint);
                }

                dto.Slots.Add(new SerializedSlot
                {
                    Index = slot.Index,
                    ItemJson = itemJson,
                    ItemCount = slot.ItemCount,
                    SlotCondition = null
                });
            }

            return dto;
        }

        // DTO -> Container
        public static Container Deserialize(SerializedContainer data)
        {
            if (data == null) return null;

            // 使用注册器创建容器
            Container container = SerializationRegistry.CreateContainer(data);
            
            if (container == null)
            {
                Debug.LogError($"创建容器失败: {data.ContainerKind}");
                return null;
            }

            // 还原容器条件（使用注册器反序列化）
            var conds = new List<IItemCondition>();
            if (data.ContainerConditions != null)
            {
                foreach (var c in data.ContainerConditions)
                {
                    if (c == null || string.IsNullOrEmpty(c.Kind)) continue;
                    
                    var cond = SerializationRegistry.DeserializeCondition(c);
                    if (cond != null)
                    {
                        conds.Add(cond);
                    }
                }
            }
            container.ContainerCondition = conds;

            // 还原物品到指定槽位
            if (data.Slots != null)
            {
                foreach (var s in data.Slots)
                {
                    if (string.IsNullOrEmpty(s.ItemJson)) continue;
                    var item = ItemSerializer.FromJson(s.ItemJson);
                    if (item == null) continue;

                    var (res, added) = container.AddItems(item, s.ItemCount, s.Index >= 0 ? s.Index : -1);
                    if (res != AddItemResult.Success || added <= 0)
                    {
                        Debug.LogWarning($"反序列化槽位失败: idx={s.Index}, item={(item?.ID ?? "null")}, count={s.ItemCount}, res={res}, added={added}");
                    }
                }
            }

            return container;
        }

        public static string ToJson(Container container, bool prettyPrint = false)
        {
            var dto = Serialize(container, prettyPrint: prettyPrint);
            return dto != null ? JsonUtility.ToJson(dto, prettyPrint) : null;
        }

        public static Container FromJson(string json, Func<string, Item> itemFromJson = null)
        {
            if (string.IsNullOrEmpty(json)) return null;
            var dto = JsonUtility.FromJson<SerializedContainer>(json);
            return Deserialize(dto);
        }

        /// <summary>
        /// 序列化条件（暴露给外部使用）
        /// </summary>
        public static SerializedCondition SerializeCondition(IItemCondition condition)
        {
            return SerializationRegistry.SerializeCondition(condition);
        }

        /// <summary>
        /// 反序列化条件（暴露给外部使用）
        /// </summary>
        public static IItemCondition DeserializeCondition(SerializedCondition dto)
        {
            return SerializationRegistry.DeserializeCondition(dto);
        }
    }
}