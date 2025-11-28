using System.Collections.Generic;
using System.Linq;
using EasyPack.CustomData;
using UnityEngine;

namespace EasyPack.InventorySystem
{
    /// <summary>
    ///     所有子条件全部成立则返回 true；空子集视为真。
    /// </summary>
    public sealed class AllCondition : ISerializableCondition
    {
        public List<IItemCondition> Children { get; } = new();

        public AllCondition() { }

        public AllCondition(params IItemCondition[] children)
        {
            if (children != null)
                Children.AddRange(children.Where(c => c != null));
        }

        /// <summary>
        ///     若 Children 为空认为是true。
        ///     若不为空且任一子条件为 null 或判定为 false，则整体为 false。
        /// </summary>
        public bool CheckCondition(IItem item)
        {
            if (Children == null || Children.Count == 0) return true; // 真空真
            foreach (IItemCondition c in Children)
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
                foreach (IItemCondition c in conditions)
                {
                    if (c != null)
                        Children.Add(c);
                }
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
            foreach (IItemCondition child in Children)
            {
                if (child is ISerializableCondition serializableChild)
                {
                    SerializedCondition childDto = serializableChild.ToDto();
                    if (childDto != null)
                    {
                        var childEntry = new CustomDataEntry { Key = $"Child_{childIndex}" };
                        childEntry.SetValue(JsonUtility.ToJson(childDto), CustomDataType.String);
                        dto.Params.Add(childEntry);
                        childIndex++;
                    }
                }
            }

            // 存储子条件数量
            var countEntry = new CustomDataEntry { Key = "ChildCount" };
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

            // 获取子条件数量
            int childCount = 0;
            foreach (CustomDataEntry p in dto.Params)
            {
                if (p?.Key == "ChildCount")
                {
                    childCount = p.IntValue;
                    break;
                }
            }

            // 反序列化每个子条件
            for (int i = 0; i < childCount; i++)
            {
                string childId = $"Child_{i}";
                foreach (CustomDataEntry p in dto.Params)
                {
                    if (p?.Key == childId)
                    {
                        string childJsonStr = p.StringValue ?? p.GetValue() as string;
                        if (!string.IsNullOrEmpty(childJsonStr))
                        {
                            var childDto = JsonUtility.FromJson<SerializedCondition>(childJsonStr);
                            if (childDto != null)
                            {
                                var serializer = new ConditionJsonSerializer();
                                string childJson = JsonUtility.ToJson(childDto);
                                IItemCondition childCondition = serializer.DeserializeFromJson(childJson);
                                if (childCondition != null) Children.Add(childCondition);
                            }
                        }

                        break;
                    }
                }
            }

            return this;
        }
    }
}