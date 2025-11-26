using System;
using UnityEngine;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     卡牌的静态数据
    ///     该类型不包含运行时状态
    ///     运行时应由 <see cref="Card" /> 持有一份 <see cref="CardData" />，并在实例化时基于此进行初始化。
    /// </summary>
    public partial class CardData
    {
        /// <summary>
        ///     通用默认分类路径，用于 CategoryManager 注册。
        /// </summary>
        public const string DEFAULT_CATEGORY = "Default";

        /// <summary>
        ///     卡牌唯一标识
        /// </summary>
        public string ID { get; }

        /// <summary>
        ///     展示名
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     文本描述
        /// </summary>
        public string Description { get; }

        /// <summary>
        ///     默认分类路径，用于 CategoryManager 注册。
        ///     示例："Card.Object"、"Card.Action"、"Equipment.Weapon"
        /// </summary>
        public string DefaultCategory { get; }

        /// <summary>
        ///     卡牌类别（物品/动作/环境等），用于规则匹配或统计
        /// </summary>
        [Obsolete("使用 DefaultCategory 和 CategoryManager 进行分类管理。此属性将在未来版本移除。")]
        public CardCategory Category { get; }

        /// <summary>
        ///     卡牌图标
        /// </summary>
        public Sprite Sprite { get; set; }

        /// <summary>
        ///     默认标签集合：
        ///     - 通常在基于 <see cref="CardData" /> 创建 <see cref="Card" /> 实例时拷贝到实例的标签集中；
        ///     - 本数组应视为只读元数据，不建议在运行时直接修改该数组内容（修改应作用于实例）。
        /// </summary>
        public string[] DefaultTags { get; }

        /// <summary>
        ///     创建一条卡牌静态数据。
        /// </summary>
        /// <param name="id">逻辑ID</param>
        /// <param name="name">展示名。默认为 "Default"</param>
        /// <param name="desc">描述文本</param>
        /// <param name="defaultCategory">分类路径，用于 CategoryManager（默认为 "Default"）</param>
        /// <param name="defaultTags">默认标签集合；null 时使用空数组。</param>
        /// <param name="sprite">卡牌图标。</param>
        public CardData(string id, string name = "Default", string desc = "",
                        string defaultCategory = DEFAULT_CATEGORY, string[] defaultTags = null, Sprite sprite = null)
        {
            ID = id;
            Name = name;
            Description = desc;
            DefaultCategory = defaultCategory ?? DEFAULT_CATEGORY;
            Category = CardCategory.Object; // 保持向后兼容
            DefaultTags = defaultTags ?? Array.Empty<string>();
            Sprite = sprite ?? Resources.Load<Sprite>(ID);
        }

        /// <summary>
        ///     创建一条卡牌静态数据（旧版构造函数，保持向后兼容）。
        /// </summary>
        /// <param name="id">逻辑ID</param>
        /// <param name="name">展示名。默认为 "Default"</param>
        /// <param name="desc">描述文本</param>
        /// <param name="category">类别（默认 Object）</param>
        /// <param name="defaultTags">默认标签集合；null 时使用空数组。</param>
        /// <param name="sprite">卡牌图标。</param>
        [Obsolete("使用新的构造函数 CardData(id, name, desc, defaultCategory, defaultTags, sprite)。此构造函数将在未来版本移除。")]
        public CardData(string id, string name, string desc,
                        CardCategory category, string[] defaultTags = null, Sprite sprite = null)
        {
            ID = id;
            Name = name;
            Description = desc;
            Category = category;
            // 从旧枚举转换为新的字符串分类
            DefaultCategory = ConvertCategoryEnumToPath(category);
            DefaultTags = defaultTags ?? Array.Empty<string>();
            Sprite = sprite ?? Resources.Load<Sprite>(ID);
        }

        /// <summary>
        ///     将旧的 CardCategory 枚举转换为新的分类路径字符串
        /// </summary>
        private static string ConvertCategoryEnumToPath(CardCategory category)
        {
            return category switch
            {
                CardCategory.Object => "Card.Object",
                CardCategory.Attribute => "Card.Attribute",
                CardCategory.Action => "Card.Action",
                CardCategory.Environment => "Card.Environment",
                _ => DEFAULT_CATEGORY
            };
        }
    }
}