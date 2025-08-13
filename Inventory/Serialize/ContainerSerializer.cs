using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack
{
    public static class ContainerSerializer
    {
        static ContainerSerializer()
        {
            // 注册内置条件序列化器
            ConditionSerializerRegistry.Register(new ItemTypeConditionSerializer());
            ConditionSerializerRegistry.Register(new AttributeConditionSerializer());
        }

        // Container -> DTO
        public static SerializableContainer Serialize(IContainer container, Func<IItem, string> itemToJson = null)
        {
            if (container == null) return null;

            var dto = new SerializableContainer
            {
                ContainerKind = container.GetType().Name,
                ID = container.ID,
                Name = container.Name,
                Type = container.Type,
                Capacity = container.Capacity,
                IsGrid = container.IsGrid,
                Grid = container.IsGrid ? container.Grid : new Vector2(-1, -1)
            };

            // 容器条件
            if (container.ContainerCondition != null)
            {
                foreach (var cond in container.ContainerCondition)
                {
                    var ser = ConditionSerializerRegistry.FindFor(cond);
                    if (ser == null) continue; // 未注册的条件跳过
                    var c = ser.Serialize(cond);
                    if (c != null) dto.ContainerConditions.Add(c);
                }
            }

            // 槽位
            foreach (var slot in container.Slots)
            {
                if (slot == null || !slot.IsOccupied || slot.Item == null) continue;

                string itemJson = null;
                if (itemToJson != null)
                {
                    itemJson = itemToJson(slot.Item);
                }
                else if (slot.Item is Item concrete)
                {
                    itemJson = concrete.ToJson(false);
                }

                dto.Slots.Add(new SerializableSlot
                {
                    Index = slot.Index,
                    ItemJson = itemJson,
                    ItemCount = slot.ItemCount,
                    SlotCondition = null
                });
            }

            return dto;
        }

        // DTO -> Container（当前示例支持 LinerContainer）
        public static IContainer Deserialize(SerializableContainer data, Func<string, Item> itemFromJson = null)
        {
            if (data == null) return null;

            IContainer container;
            switch (data.ContainerKind)
            {
                case "LinerContainer":
                case null:
                case "":
                    container = new LinerContainer(data.ID, data.Name, data.Type, data.Capacity);
                    break;
                default:
                    // 未实现的容器类型可扩展此分支
                    container = new LinerContainer(data.ID, data.Name, data.Type, data.Capacity);
                    break;
            }

            // 还原容器条件
            var conds = new List<IItemCondition>();
            if (data.ContainerConditions != null)
            {
                foreach (var c in data.ContainerConditions)
                {
                    var ser = ConditionSerializerRegistry.Get(c.Kind);
                    var cond = ser?.Deserialize(c);
                    if (cond != null) conds.Add(cond);
                }
            }
            container.ContainerCondition = conds;

            // 还原物品到指定槽位
            var fromJson = itemFromJson ?? (s => Item.FromJson(s));
            if (data.Slots != null)
            {
                foreach (var s in data.Slots)
                {
                    if (string.IsNullOrEmpty(s.ItemJson)) continue;
                    var item = fromJson(s.ItemJson);
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

        public static string ToJson(IContainer container, bool prettyPrint = false)
        {
            var dto = Serialize(container);
            return dto != null ? JsonUtility.ToJson(dto, prettyPrint) : null;
        }

        public static IContainer FromJson(string json, Func<string, Item> itemFromJson = null)
        {
            if (string.IsNullOrEmpty(json)) return null;
            var dto = JsonUtility.FromJson<SerializableContainer>(json);
            return Deserialize(dto, itemFromJson);
        }
    }

    // 内置：ItemTypeCondition <-> DTO
    internal sealed class ItemTypeConditionSerializer : IConditionSerializer
    {
        public string Kind => "ItemType";
        public bool CanHandle(IItemCondition condition) => condition is ItemTypeCondition;

        public ConditionDTO Serialize(IItemCondition condition)
        {
            var c = (ItemTypeCondition)condition;
            var dto = new ConditionDTO { Kind = Kind, Version = 1 };
            var entry = new CustomDataEntry { Id = "ItemType" };
            entry.SetValue(c.ItemType, CustomDataType.String);
            dto.Params.Add(entry);
            return dto;
        }

        public IItemCondition Deserialize(ConditionDTO dto)
        {
            if (dto?.Params == null) return null;
            string itemType = null;
            foreach (var p in dto.Params)
            {
                if (p?.Id == "ItemType")
                {
                    itemType = p.StringValue ?? p.GetValue() as string;
                    break;
                }
            }
            return string.IsNullOrEmpty(itemType) ? null : new ItemTypeCondition(itemType);
        }
    }

    // 内置：AttributeCondition <-> DTO
    internal sealed class AttributeConditionSerializer : IConditionSerializer
    {
        public string Kind => "Attr";
        public bool CanHandle(IItemCondition condition) => condition is AttributeCondition;

        public ConditionDTO Serialize(IItemCondition condition)
        {
            var c = (AttributeCondition)condition;
            var dto = new ConditionDTO { Kind = Kind, Version = 1 };

            var name = new CustomDataEntry { Id = "Name" };
            name.SetValue(c.AttributeName, CustomDataType.String);
            var cmp = new CustomDataEntry { Id = "Cmp" };
            cmp.SetValue((int)c.ComparisonType, CustomDataType.Int);
            var val = new CustomDataEntry { Id = "Value" };
            val.SetValue(c.AttributeValue); // 自动判断类型；复杂类型将走 Json/Custom

            dto.Params.Add(name);
            dto.Params.Add(cmp);
            dto.Params.Add(val);
            return dto;
        }

        public IItemCondition Deserialize(ConditionDTO dto)
        {
            if (dto?.Params == null) return null;

            string name = null;
            object value = null;
            int cmp = 0;

            foreach (var p in dto.Params)
            {
                if (p == null) continue;
                switch (p.Id)
                {
                    case "Name": name = p.StringValue ?? p.GetValue() as string; break;
                    case "Cmp": cmp = p.IntValue; break;
                    case "Value": value = p.GetValue(); break;
                }
            }

            if (string.IsNullOrEmpty(name)) return null;
            return new AttributeCondition(name, value, (AttributeComparisonType)cmp);
        }
    }
}