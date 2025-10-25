using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyPack.InventorySystem
{
    /// <summary>
    /// 所有子条件全部成立则返回 true；空子集视为真。
    /// </summary>
    public sealed class AllCondition : IItemCondition, ISerializableCondition
    {
        public List<IItemCondition> Children { get; } = new List<IItemCondition>();

        public AllCondition()
        {
        }

        public AllCondition(params IItemCondition[] children)
        {
            if (children != null)
                Children.AddRange(children.Where(c => c != null));
        }

        /// <summary>
        /// 若 Children 为空认为是true。
        /// 若不为空且任一子条件为 null 或判定为 false，则整体为 false。
        /// </summary>
        public bool CheckCondition(IItem item)
        {
            if (Children == null || Children.Count == 0) return true; // 真空真
            foreach (var c in Children)
            {
                if (c == null) return false;
                if (!c.CheckCondition(item)) return false;
            }
            return true;
        }

        public AllCondition Add(IItemCondition condition)
        {
            if (condition != null) Children.Add(condition);
            return this;
        }

        public AllCondition AddRange(IEnumerable<IItemCondition> conditions)
        {
            if (conditions != null)
            {
                foreach (var c in conditions) if (c != null) Children.Add(c);
            }
            return this;
        }

        public string Kind => "All";

        // ISerializableCondition 实现
        public SerializedCondition ToDto()
        {
            var dto = new SerializedCondition { Kind = Kind };

            // 序列化子条件
            int childIndex = 0;
            foreach (var child in Children)
            {
                if (child != null && child is ISerializableCondition serializableChild)
                {
                    var childDto = serializableChild.ToDto();
                    if (childDto != null)
                    {
                        var childEntry = new CustomDataEntry { Id = $"Child_{childIndex}" };
                        childEntry.SetValue(JsonUtility.ToJson(childDto), CustomDataType.String);
                        dto.Params.Add(childEntry);
                        childIndex++;
                    }
                }
            }

            // 存储子条件数量
            var countEntry = new CustomDataEntry { Id = "ChildCount" };
            countEntry.SetValue(childIndex, CustomDataType.Int);
            dto.Params.Add(countEntry);

            return dto;
        }

        public ISerializableCondition FromDto(SerializedCondition dto)
        {
            if (dto == null || dto.Params == null)
                return this;

            // 清空现有子条件
            Children.Clear();

            // 使用 ConditionJsonSerializer 反序列化子条件
            int childCount = 0;
            foreach (var p in dto.Params)
            {
                if (p?.Id == "ChildCount")
                {
                    childCount = p.IntValue;
                    break;
                }
            }

            // 反序列化每个子条件
            for (int i = 0; i < childCount; i++)
            {
                string childId = $"Child_{i}";
                foreach (var p in dto.Params)
                {
                    if (p?.Id == childId)
                    {
                        var childJsonStr = p.StringValue ?? p.GetValue() as string;
                        if (!string.IsNullOrEmpty(childJsonStr))
                        {
                            var childDto = JsonUtility.FromJson<SerializedCondition>(childJsonStr);
                            if (childDto != null)
                            {
                                // 递归创建子条件
                                IItemCondition childCondition = CreateConditionFromDto(childDto);
                                if (childCondition != null)
                                {
                                    Children.Add(childCondition);
                                }
                            }
                        }
                        break;
                    }
                }
            }

            return this;
        }

        /// <summary>
        /// 从 DTO 创建条件实例（内部辅助方法）
        /// </summary>
        private static IItemCondition CreateConditionFromDto(SerializedCondition dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.Kind))
                return null;

            ISerializableCondition condition = null;

            switch (dto.Kind)
            {
                case "ItemType":
                    condition = new ItemTypeCondition("");
                    break;
                case "Attr":
                    condition = new AttributeCondition("", null);
                    break;
                case "All":
                    condition = new AllCondition();
                    break;
                    // 后续添加 Any 和 Not
            }

            return condition?.FromDto(dto) as IItemCondition;
        }
    }
}
