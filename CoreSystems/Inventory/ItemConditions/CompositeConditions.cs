using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// 所有子条件全部成立则返回 true；空子集视为真。
    /// </summary>
    public sealed class AllCondition : IItemCondition
    {
        public List<IItemCondition> Children { get; } = new List<IItemCondition>();
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
    }

    /// <summary>
    /// 任意一个子条件成立则返回 true；空子集视为假。
    /// </summary>
    public sealed class AnyCondition : IItemCondition
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
    }

    /// <summary>
    /// 对单一子条件取反：子条件不成立时为真；如果 Inner 为 null，视为真。
    /// </summary>
    public sealed class NotCondition : IItemCondition
    {
        public IItemCondition Inner { get; set; }

        public NotCondition() { }

        public NotCondition(IItemCondition inner)
        {
            Inner = inner;
        }

        /// <summary>
        /// 如果 Inner 为 null 则返回 true；否则返回 !Inner.IsCondition(item)。
        /// </summary>
        public bool CheckCondition(IItem item)
        {
            if (Inner == null) return true;
            return !Inner.CheckCondition(item);
        }

        public NotCondition Set(IItemCondition inner)
        {
            Inner = inner;
            return this;
        }
    }
}