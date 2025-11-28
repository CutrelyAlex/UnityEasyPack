using System;
using System.Collections.Generic;
using EasyPack.Serialization;
using UnityEngine;

namespace EasyPack.InventorySystem
{
    /// <summary>
    ///     Container类型的JSON序列化器
    /// </summary>
    public class ContainerJsonSerializer : JsonSerializerBase<Container>
    {
        private readonly ISerializationService _serializationService;

        /// <summary>
        ///     构造函数,注入序列化服务
        /// </summary>
        /// <param name="serializationService">序列化服务实例</param>
        public ContainerJsonSerializer(ISerializationService serializationService) =>
            _serializationService =
                serializationService ?? throw new ArgumentNullException(nameof(serializationService));

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
                Grid = obj.IsGrid ? obj.Grid : new(-1, -1),
            };

            // 序列化容器条件
            if (obj.ContainerCondition != null)
                foreach (IItemCondition cond in obj.ContainerCondition)
                {
                    if (cond != null)
                    {
                        // 使用注入的序列化服务序列化条件（而不是接口类型）
                        string condJson = _serializationService.SerializeToJson(cond, cond.GetType());
                        if (!string.IsNullOrEmpty(condJson))
                        {
                            var serializedCond = JsonUtility.FromJson<SerializedCondition>(condJson);
                            if (serializedCond != null) dto.ContainerConditions.Add(serializedCond);
                        }
                    }
                }

            // 序列化槽位
            foreach (ISlot slot in obj.Slots)
            {
                if (slot == null || !slot.IsOccupied || slot.Item == null) continue;

                string itemJson = null;
                if (slot.Item is Item concrete)
                    // 使用注入的序列化服务序列化物品
                    itemJson = _serializationService.SerializeToJson(concrete, concrete.GetType());

                dto.Slots.Add(new()
                {
                    Index = slot.Index, ItemJson = itemJson, ItemCount = slot.ItemCount, SlotCondition = null,
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
                foreach (SerializedCondition c in dto.ContainerConditions)
                {
                    if (c == null || string.IsNullOrEmpty(c.Kind)) continue;

                    string condJson = JsonUtility.ToJson(c);
                    var cond = _serializationService.DeserializeFromJson<IItemCondition>(condJson);
                    if (cond != null) conds.Add(cond);
                }

            container.ContainerCondition = conds;

            // 还原物品到指定槽位
            if (dto.Slots != null)
                foreach (SerializedSlot s in dto.Slots)
                {
                    if (string.IsNullOrEmpty(s.ItemJson)) continue;

                    var item = _serializationService.DeserializeFromJson<Item>(s.ItemJson);
                    if (item == null) continue;

                    (AddItemResult res, int added) = container.AddItems(item, s.ItemCount, s.Index >= 0 ? s.Index : -1);
                    if (res != AddItemResult.Success || added <= 0)
                        Debug.LogWarning(
                            $"反序列化槽位失败: idx={s.Index}, item={item?.ID ?? "null"}, count={s.ItemCount}, res={res}, added={added}");
                }

            return container;
        }

        /// <summary>
        ///     根据DTO创建容器实例
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