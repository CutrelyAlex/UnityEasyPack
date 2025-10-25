
using UnityEngine;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    /// 卡牌的静态数据
    /// 该类型不包含运行时状态
    /// 运行时应由 <see cref="Card"/> 持有一份 <see cref="CardData"/>，并在实例化时基于此进行初始化。
    /// </summary>
    public partial class CardData
    {
        /// <summary>
        /// 卡牌唯一标识
        /// </summary>
        public string ID { get; }

        /// <summary>
        /// 展示名
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 文本描述
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// 卡牌类别（物品/动作/环境等），用于规则匹配或统计
        /// </summary>
        public CardCategory Category { get; }

        /// <summary>
        /// 卡牌图标
        /// </summary>
        public Sprite Sprite { get; set; }

        /// <summary>
        /// 默认标签集合：
        /// - 通常在基于 <see cref="CardData"/> 创建 <see cref="Card"/> 实例时拷贝到实例的标签集中；
        /// - 本数组应视为只读元数据，不建议在运行时直接修改该数组内容（修改应作用于实例）。
        /// </summary>
        public string[] DefaultTags { get; }

        /// <summary>
        /// 创建一条卡牌静态数据。
        /// </summary>
        /// <param name="id">逻辑ID（建议全局唯一）。</param>
        /// <param name="name">展示名（可本地化）。默认为 "Default"。</param>
        /// <param name="desc">描述文本（可本地化）。</param>
        /// <param name="category">类别（默认 Item）。</param>
        /// <param name="defaultTags">默认标签集合；null 时使用空数组。</param>
        /// <param name="sprite">卡牌图标。</param>
        public CardData(string id, string name = "Default", string desc = "",
                        CardCategory category = CardCategory.Object, string[] defaultTags = null, Sprite sprite = null)
        {
            ID = id;
            Name = name;
            Description = desc;
            Category = category;
            DefaultTags = defaultTags ?? System.Array.Empty<string>();
            Sprite = Resources.Load<Sprite>(ID) == null ? Resources.Load<Sprite>("Default") : Resources.Load<Sprite>(ID); ;
        }
    }
}
