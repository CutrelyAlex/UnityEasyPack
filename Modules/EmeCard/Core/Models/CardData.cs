using System;
using EasyPack.CustomData;
using UnityEngine;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     卡牌的静态数据
    ///     该类型不包含运行时状态
    ///     运行时应由 <see cref="Card" /> 持有一份 <see cref="CardData" />，并在实例化时基于此进行初始化。
    /// </summary>
    public class CardData
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
        public string Category { get; }

        /// <summary>
        ///     卡牌图标
        /// </summary>
        public Sprite Sprite { get; set; }

        /// <summary>
        ///     默认标签集合：
        ///     - 通常在基于 <see cref="CardData" /> 创建 <see cref="Card" /> 实例时拷贝到实例的标签集中；
        ///     - 本数组应视为只读原数据，不建议在运行时直接修改该数组内容（修改应作用于实例）。
        /// </summary>
        public string[] DefaultTags { get; }

        /// <summary>
        ///    自定义数据集合的默认实例
        /// </summary>
        public CustomDataCollection DefaultMetaData { get; } = new();

        /// <summary>
        ///     创建一条卡牌静态数据。
        /// </summary>
        /// <param name="id">逻辑ID</param>
        /// <param name="name">展示名。默认为 "Default"</param>
        /// <param name="desc">描述文本</param>
        /// <param name="category">分类路径，用于 CategoryManager（默认为 "Default"）</param>
        /// <param name="defaultTags">默认标签集合；null 时使用空数组。</param>
        /// <param name="sprite">卡牌图标。</param>
        public CardData(string id, string name = "Default", string desc = "",
                        string category = DEFAULT_CATEGORY, string[] defaultTags = null, Sprite sprite = null)
        {
            ID = id;
            Name = name;
            Description = desc;
            Category = category ?? DEFAULT_CATEGORY;
            DefaultTags = defaultTags ?? Array.Empty<string>();
            Sprite = sprite ?? Resources.Load<Sprite>(ID);
        }

        /// <summary>
        ///     克隆当前数据并指定新的 ID。
        /// </summary>
        /// <param name="newId">新的逻辑 ID。</param>
        /// <returns>克隆后的 CardData 实例。</returns>
        public CardData Clone(string newId)
        {
            var clone = new CardData(
                newId,
                Name,
                Description,
                Category,
                DefaultTags is { Length: > 0 } ? (string[])DefaultTags.Clone() : null,
                Sprite
            );

            // 深度拷贝元数据
            if (DefaultMetaData != null)
            {
                clone.DefaultMetaData.Merge(DefaultMetaData);
            }

            return clone;
        }
    }
}