using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack.InventorySystem
{
    /// <summary>
    /// Container类型的JSON序列化器
    /// </summary>
    public class ContainerJsonSerializer : JsonSerializerBase<Container>
    {
        public override string SerializeToJson(Container obj)
        {
            if (obj == null) return null;

            var dto = new SerializedContainer
            {
                ContainerKind = obj.GetType().Name,
                ID = obj.ID,
                Name = obj.Name,
                Type = obj.Type,
                Capacity = obj.Capacity,
                IsGrid = obj.IsGrid,
                Grid = obj.IsGrid ? obj.Grid : new Vector2(-1, -1)
            };

            // 序列化容器条件
            if (obj.ContainerCondition != null)
            {
                foreach (var cond in obj.ContainerCondition)
                {
                    if (cond != null)
                    {
                        // 使用实际类型序列化条件（而不是接口类型）
                        var condJson = SerializationServiceManager.SerializeToJson(cond, cond.GetType());
                        if (!string.IsNullOrEmpty(condJson))
                        {
                            var serializedCond = JsonUtility.FromJson<SerializedCondition>(condJson);
                            if (serializedCond != null)
                            {
                                dto.ContainerConditions.Add(serializedCond);
                            }
                        }
                    }
                }
            }

            // 序列化槽位
            foreach (var slot in obj.Slots)
            {
                if (slot == null || !slot.IsOccupied || slot.Item == null) continue;

                string itemJson = null;
                if (slot.Item is Item concrete)
                {
                    // 使用实际类型序列化物品
                    itemJson = SerializationServiceManager.SerializeToJson(concrete, concrete.GetType());
                }

                dto.Slots.Add(new SerializedSlot
                {
                    Index = slot.Index,
                    ItemJson = itemJson,
                    ItemCount = slot.ItemCount,
                    SlotCondition = null
                });
            }

            return JsonUtility.ToJson(dto);
        }

        public override Container DeserializeFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;

            SerializedContainer dto;
            try
            {
                dto = JsonUtility.FromJson<SerializedContainer>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ContainerJsonSerializer] 反序列化失败: {e.Message}");
                return null;
            }

            if (dto == null) return null;

            // 根据ContainerKind创建容器实例
            Container container = CreateContainerInstance(dto);
            if (container == null)
            {
                Debug.LogError($"创建容器失败: {dto.ContainerKind}");
                return null;
            }

            // 还原容器条件
            var conds = new List<IItemCondition>();
            if (dto.ContainerConditions != null)
            {
                foreach (var c in dto.ContainerConditions)
                {
                    if (c == null || string.IsNullOrEmpty(c.Kind)) continue;

                    var condType = ConditionTypeRegistry.GetConditionType(c.Kind);
                    if (condType == null)
                    {
                        Debug.LogWarning($"[ContainerJsonSerializer] 未注册的条件类型: {c.Kind}");
                        continue;
                    }

                    var condJson = JsonUtility.ToJson(c);
                    var cond = SerializationServiceManager.DeserializeFromJson(condJson, condType) as IItemCondition;
                    if (cond != null)
                    {
                        conds.Add(cond);
                    }
                }
            }
            container.ContainerCondition = conds;

            // 还原物品到指定槽位
            if (dto.Slots != null)
            {
                foreach (var s in dto.Slots)
                {
                    if (string.IsNullOrEmpty(s.ItemJson)) continue;

                    var item = SerializationServiceManager.DeserializeFromJson<Item>(s.ItemJson);
                    if (item == null) continue;

                    var (res, added) = container.AddItems(item, s.ItemCount, s.Index >= 0 ? s.Index : -1);
                    if (res != AddItemResult.Success || added <= 0)
                    {
                        Debug.LogWarning($"反序列化槽位失败: idx={s.Index}, item={item?.ID ?? "null"}, count={s.ItemCount}, res={res}, added={added}");
                    }
                }
            }

            return container;
        }

        /// <summary>
        /// 根据DTO创建容器实例
        /// </summary>
        private Container CreateContainerInstance(SerializedContainer dto)
        {
            switch (dto.ContainerKind)
            {
                case "LinerContainer":
                    return new LinerContainer(dto.ID, dto.Name, dto.Type, dto.Capacity);

                // 可以在这里添加其他容器类型
                // case "GridContainer":
                //     return new GridContainer(dto.ID, dto.Name, dto.Type, dto.Grid);

                default:
                    Debug.LogWarning($"未知容器类型: {dto.ContainerKind}，使用LinerContainer作为默认");
                    return new LinerContainer(dto.ID, dto.Name, dto.Type, dto.Capacity);
            }
        }
    }
}

