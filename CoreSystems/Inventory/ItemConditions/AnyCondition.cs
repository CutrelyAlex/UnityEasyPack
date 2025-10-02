using System.Collections.Generic;
using System.Linq;

namespace EasyPack
{
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
}