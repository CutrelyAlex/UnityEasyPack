using System;
using System.Collections.Generic;
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
        ///     默认分类路径
        ///     示例："Card.Object"、"Card.Action"、"Equipment.Weapon"
        /// </summary>
        public string Category { get; }

        /// <summary>
        ///     卡牌图标
        /// </summary>
        public Sprite Sprite { get; set; }

        /// <summary>
        ///     默认标签集合
        /// </summary>
        private readonly List<string> _defaultTags = new();

        public string[] DefaultTags => _defaultTags.Count == 0 ? Array.Empty<string>() : _defaultTags.ToArray();

        /// <summary>
        ///     默认属性列表：
        ///     Card 被添加到 Engine 时，若自身 Properties 为空，则从此列表初始化。
        /// </summary>
        private readonly List<(string id, float value)> _defaultProperties = new();

        public IReadOnlyList<(string id, float value)> DefaultProperties => _defaultProperties;

        /// <summary>
        ///     默认子卡列表：(childId, intrinsic)
        ///     Card 被 Factory 创建时，自动添加这些子卡。
        /// </summary>
        private readonly List<(string childId, bool intrinsic)> _defaultChildren = new();

        public IReadOnlyList<(string childId, bool intrinsic)> DefaultChildren => _defaultChildren;

        /// <summary>
        ///     自定义数据集合的默认实例
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
            if (defaultTags is { Length: > 0 })
            {
                foreach (string tag in defaultTags)
                {
                    if (string.IsNullOrWhiteSpace(tag)) continue;
                    if (!_defaultTags.Contains(tag)) _defaultTags.Add(tag);
                }
            }
            Sprite = sprite ?? Resources.Load<Sprite>(ID);
        }

        /// <summary>
        ///     为模板添加默认标签。
        /// </summary>
        public CardData WithTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return this;
            if (!_defaultTags.Contains(tag)) _defaultTags.Add(tag);
            return this;
        }

        /// <summary>
        ///     为模板添加多个默认标签。
        /// </summary>
        public CardData WithTags(params string[] tags)
        {
            if (tags == null || tags.Length == 0) return this;
            foreach (string tag in tags)
            {
                WithTag(tag);
            }

            return this;
        }

        /// <summary>
        ///     配置模板默认元数据。
        /// </summary>
        public CardData WithMetaData(Action<CustomDataCollection> action)
        {
            action?.Invoke(DefaultMetaData);
            return this;
        }

        /// <summary>
        ///     为模板添加默认属性。
        /// </summary>
        public CardData WithProperty(string id, float value)
        {
            if (string.IsNullOrEmpty(id)) return this;
            _defaultProperties.Add((id, value));
            return this;
        }

        /// <summary>
        ///     为模板添加默认子卡。
        ///     Factory 创建卡牌时自动 CreateCard 并 AddChild。
        /// </summary>
        public CardData WithChild(string childId, bool intrinsic = false)
        {
            if (string.IsNullOrEmpty(childId)) return this;
            _defaultChildren.Add((childId, intrinsic));
            return this;
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

            // 深度拷贝默认属性
            if (_defaultProperties.Count > 0)
            {
                foreach (var prop in _defaultProperties)
                {
                    clone._defaultProperties.Add(prop);
                }
            }

            // 深度拷贝默认子卡
            if (_defaultChildren.Count > 0)
            {
                foreach (var child in _defaultChildren)
                {
                    clone._defaultChildren.Add(child);
                }
            }

            return clone;
        }
    }
}