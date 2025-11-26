using System;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     卡牌类别。
    /// </summary>
    [Obsolete("使用 CategoryManager 的层级分类路径（如 \"Card.Object\"）代替。此枚举将在未来版本移除。")]
    public enum CardCategory
    {
        /// <summary>物品/实体类。</summary>
        Object,

        /// <summary>属性/状态类。</summary>
        Attribute,

        /// <summary>行为/动作类。</summary>
        Action,

        /// <summary>环境类。</summary>
        Environment,
    }
}