using System;
using UnityEngine;

namespace EasyPack
{

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