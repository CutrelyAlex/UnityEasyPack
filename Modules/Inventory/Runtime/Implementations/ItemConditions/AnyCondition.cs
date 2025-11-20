using EasyPack.CustomData;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyPack.InventorySystem
{
    /// <summary>
    /// 任意一个子条件成立则返回 true；空子集视为假。
    /// </summary>
    public sealed class AnyCondition : ISerializableCondition
    {
        public List<IItemCondition> Children { get; } = new List<IItemCondition>();

        public AnyCondition() { }

        public AnyCondition(params IItemCondition[] children)
        {
            if (children != null)
                Children.AddRange(children.Where(c => c != null));
        }

        /// <summary>
        /// 若 Children 为空，返回 false（真空假）。
        /// 忽略为 null 的子条件，至少有一个子条件返回 true 则整体为 true。
        /// </summary>
        public bool CheckCondition(IItem item)
        {
            if (Children == null || Children.Count == 0) return false;
            bool any = false;
            foreach (var c in Children)
            {
                if (c == null) continue;
                if (c.CheckCondition(item))
                {
                    any = true;
                    break;
                }
            }
            return any;
        }

        public AnyCondition Add(IItemCondition condition)
        {
            if (condition != null) Children.Add(condition);
            return this;
        }

        public AnyCondition AddRange(IEnumerable<IItemCondition> conditions)
        {
            if (conditions != null)
            {
                foreach (var c in conditions) if (c != null) Children.Add(c);
            }
            return this;
        }

        public string Kind => "Any";

        // ISerializableCondition 实现
        public SerializedCondition ToDto()
        {
            var dto = new SerializedCondition { Kind = Kind };

            // 序列化子条件
            int childIndex = 0;
            foreach (var child in Children)
            {
                if (child is ISerializableCondition serializableChild)
                {
                    var childDto = serializableChild.ToDto();
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
            foreach (var p in dto.Params)
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
                foreach (var p in dto.Params)
                {
                    if (p?.Key == childId)
                    {
                        var childJsonStr = p.StringValue ?? p.GetValue() as string;
                        if (!string.IsNullOrEmpty(childJsonStr))
                        {
                            var childDto = JsonUtility.FromJson<SerializedCondition>(childJsonStr);
                            if (childDto != null)
                            {
                                var serializer = new ConditionJsonSerializer();
                                var childJson = JsonUtility.ToJson(childDto);
                                var childCondition = serializer.DeserializeFromJson(childJson);
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
    }
}
