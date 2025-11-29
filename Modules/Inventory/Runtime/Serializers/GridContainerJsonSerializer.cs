using System;
using System.Collections.Generic;
using EasyPack.Serialization;
using UnityEngine;

namespace EasyPack.InventorySystem
{
    /// <summary>
    ///     GridContainer 序列化器
    /// </summary>
    public class GridContainerJsonSerializer : ITypeSerializer<GridContainer, SerializedContainer>
    {
        private readonly ISerializationService _serializationService;

        /// <summary>
        ///     构造函数,注入序列化服务
        /// </summary>
        /// <param name="serializationService">序列化服务实例</param>
        public GridContainerJsonSerializer(ISerializationService serializationService) =>
            _serializationService =
                serializationService ?? throw new ArgumentNullException(nameof(serializationService));

        public SerializedContainer ToSerializable(GridContainer container)
        {
            if (container == null) return null;

            var dto = new SerializedContainer
            {
                ContainerKind = "Grid",
                ID = container.ID,
                Name = container.Name,
                Type = container.Type,
                Capacity = container.Capacity,
                IsGrid = true,
                Grid = new(container.GridWidth, container.GridHeight),
            };

            // 序列化槽位，跳过占位符
            var slotsList = new List<SerializedSlot>();
            for (int i = 0; i < container.Slots.Count; i++)
            {
                ISlot slot = container.Slots[i];
                if (!slot.IsOccupied) continue;

                // 跳过占位符，只序列化实际物品
                if (slot.Item.ID == "__GRID_OCCUPIED__") continue;

                var slotDto = new SerializedSlot
                {
                    Index = slot.Index,
                    ItemJson = _serializationService.SerializeToJson(slot.Item, slot.Item.GetType()),
                    ItemCount = slot.ItemCount,
                };
                slotsList.Add(slotDto);
            }

            dto.Slots = slotsList;

            // 序列化容器条件
            if (container.ContainerCondition is { Count: > 0 })
            {
                foreach (IItemCondition cond in container.ContainerCondition)
                {
                    var concrete = cond as ISerializableCondition;
                    if (concrete != null)
                    {
                        SerializedCondition condDto = concrete.ToDto();
                        dto.ContainerConditions.Add(condDto);
                    }
                }
            }

            return dto;
        }

        public GridContainer FromSerializable(SerializedContainer dto)
        {
            if (dto == null) return null;

            // 从Grid向量中提取宽度和高度
            int gridWidth = (int)dto.Grid.x;
            int gridHeight = (int)dto.Grid.y;

            // 如果Grid向量无效，使用默认值
            if (gridWidth <= 0) gridWidth = 10;
            if (gridHeight <= 0) gridHeight = 10;

            var container = new GridContainer(dto.ID, dto.Name, dto.Type, gridWidth, gridHeight);

            // 反序列化槽位
            if (dto.Slots != null)
            {
                foreach (SerializedSlot slotDto in dto.Slots)
                {
                    if (slotDto.Index < 0 || slotDto.Index >= container.Capacity)
                        continue;

                    // 使用注入的序列化服务反序列化
                    var item = _serializationService.DeserializeFromJson<Item>(slotDto.ItemJson);
                    if (item != null) container.AddItems(item, slotDto.ItemCount, slotDto.Index);
                }
            }

            // 反序列化容器条件
            if (dto.ContainerConditions is { Count: > 0 })
            {
                foreach (SerializedCondition condDto in dto.ContainerConditions)
                {
                    string condJson = JsonUtility.ToJson(condDto);
                    var condition = _serializationService.DeserializeFromJson<IItemCondition>(condJson);
                    if (condition != null) container.ContainerCondition.Add(condition);
                }
            }

            return container;
        }

        public string ToJson(SerializedContainer dto) => dto == null ? null : JsonUtility.ToJson(dto, true);

        public SerializedContainer FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            return JsonUtility.FromJson<SerializedContainer>(json);
        }

        public string SerializeToJson(GridContainer container)
        {
            SerializedContainer dto = ToSerializable(container);
            return ToJson(dto);
        }

        public GridContainer DeserializeFromJson(string json)
        {
            SerializedContainer dto = FromJson(json);
            return FromSerializable(dto);
        }
    }
}