using System.Collections.Generic;
using System.Linq;

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
}