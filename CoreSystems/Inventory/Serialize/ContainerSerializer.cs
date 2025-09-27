using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace EasyPack
{
    public static class ContainerSerializer
    {
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

            // 容器条件（条件自序列化）
            if (container.ContainerCondition != null)
            {
                foreach (var cond in container.ContainerCondition)
                {
                    if (cond is ISerializableCondition sc)
                    {
                        var c = sc.ToDto();
                        if (c != null) dto.ContainerConditions.Add(c);
                    }
                    else
                    {
                        Debug.LogWarning($"条件未实现序列化接口: {cond?.GetType().Name}");
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
        // TODO : 支持其他容器
        public static Container Deserialize(SerializedContainer data)
        {
            if (data == null) return null;

            Container container;
            switch (data.ContainerKind)
            {
                case "LinerContainer":
                    container = new LinerContainer(data.ID, data.Name, data.Type, data.Capacity);
                    break;
                default:
                    container = new LinerContainer(data.ID, data.Name, data.Type, data.Capacity);
                    break;
            }

            // 还原容器条件（自动分发到各条件类型的 FromDto 实例方法）
            var conds = new List<IItemCondition>();
            if (data.ContainerConditions != null)
            {
                foreach (var c in data.ContainerConditions)
                {
                    if (c == null || string.IsNullOrEmpty(c.Kind)) continue;
                    IItemCondition cond = null;
                    switch (c.Kind)
                    {
                        case "ItemType":
                            cond = new ItemTypeCondition("").FromDto(c);
                            break;
                        case "Attr":
                            cond = new AttributeCondition("", null).FromDto(c);
                            break;
                        // 可扩展其它条件类型
                        default:
                            Debug.LogWarning($"未知条件 Kind: {c.Kind}，已跳过。");
                            break;
                    }
                    if (cond != null) conds.Add(cond);
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
    }
}