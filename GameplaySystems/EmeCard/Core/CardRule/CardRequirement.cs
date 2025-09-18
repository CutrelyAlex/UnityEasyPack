using System;

namespace EasyPack
{
    /// <summary>
    /// 单个卡牌匹配条件。
    /// </summary>
    public sealed class CardRequirement
    {
        /// <summary>
        /// 匹配方式（标签/ID/类别）。
        /// </summary>
        public MatchKind Kind;

        /// <summary>
        /// 当 <see cref="Kind"/> 为 <see cref="MatchKind.Tag"/> 或 <see cref="MatchKind.Id"/> 时的匹配值。
        /// </summary>
        public string Value;

        /// <summary>
        /// 当 <see cref="Kind"/> 为 <see cref="MatchKind.Category"/> 时指定的类别。
        /// </summary>
        public CardCategory? Category;

        /// <summary>
        /// 该条件需要匹配的卡牌最小数量。
        /// </summary>
        public int MinCount = 1;

        /// <summary>
        /// 是否允许将“触发源”一并纳入候选集合（取决于规则作用域）。
        /// </summary>
        public bool IncludeSelf = false;

        /// <summary>
        /// 判断指定卡牌是否满足该条件。
        /// </summary>
        /// <param name="c">待检查的卡牌。</param>
        /// <returns>满足则返回 true。</returns>
        public bool Matches(Card c)
        {
            switch (Kind)
            {
                case MatchKind.Tag: return !string.IsNullOrEmpty(Value) && c.HasTag(Value);
                case MatchKind.Id: return !string.IsNullOrEmpty(Value) && string.Equals(c.Id, Value, StringComparison.Ordinal);
                case MatchKind.Category: return Category.HasValue && c.Category == Category.Value;
                default: return false;
            }
        }
    }
}