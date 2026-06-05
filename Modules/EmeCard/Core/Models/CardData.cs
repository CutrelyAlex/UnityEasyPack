using System;
using System.Collections.Generic;
using EasyPack.CustomData;
using UnityEngine;

namespace EasyPack.EmeCardSystem
{
    public class CardData
    {
        public const string DEFAULT_CATEGORY = "Default";

        public string ID { get; }
        public string Name { get; }
        public string Description { get; }
        public string Category { get; }
        public Sprite Sprite { get; set; }

        private readonly HashSet<string> _defaultTags = new();
        public IReadOnlyCollection<string> DefaultTags => _defaultTags;

        private readonly List<(string id, float value)> _defaultProperties = new();
        public IReadOnlyList<(string id, float value)> DefaultProperties => _defaultProperties;

        private readonly List<(string childId, bool intrinsic)> _defaultChildren = new();
        public IReadOnlyList<(string childId, bool intrinsic)> DefaultChildren => _defaultChildren;

        public CustomDataCollection DefaultMetaData { get; } = new();

        public CardData(string id, string name = "Default", string desc = "",
                        string category = DEFAULT_CATEGORY, IEnumerable<string> defaultTags = null, Sprite sprite = null)
        {
            ID = id;
            Name = name;
            Description = desc;
            Category = category ?? DEFAULT_CATEGORY;
            if (defaultTags != null)
            {
                foreach (string tag in defaultTags) WithTag(tag);
            }
            Sprite = sprite ?? Resources.Load<Sprite>(ID);
        }

        public CardData WithTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return this;
            _defaultTags.Add(tag);
            return this;
        }

        public CardData WithTags(params string[] tags) => WithTags((IEnumerable<string>)tags);

        public CardData WithTags(IEnumerable<string> tags)
        {
            if (tags == null) return this;
            foreach (string tag in tags) WithTag(tag);
            return this;
        }

        public bool HasDefaultTag(string tag) => _defaultTags.Contains(tag);

        public CardData WithMetaData(Action<CustomDataCollection> action)
        {
            action?.Invoke(DefaultMetaData);
            return this;
        }

        public CardData WithProperty(string id, float value)
        {
            if (string.IsNullOrEmpty(id)) return this;
            _defaultProperties.Add((id, value));
            return this;
        }

        public CardData WithChild(string childId, bool intrinsic = false)
        {
            if (string.IsNullOrEmpty(childId)) return this;
            _defaultChildren.Add((childId, intrinsic));
            return this;
        }

        public CardData ClearChildren()
        {
            _defaultChildren.Clear();
            return this;
        }

        public CardData RemoveChild(string childId, bool intrinsic, int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                int idx = _defaultChildren.FindIndex(c => c.childId == childId && c.intrinsic == intrinsic);
                if (idx < 0) break;
                _defaultChildren.RemoveAt(idx);
            }
            return this;
        }

        public CardData ModifyChild(string fromChildId, bool fromIntrinsic, string toChildId, bool toIntrinsic, int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                int idx = _defaultChildren.FindIndex(c => c.childId == fromChildId && c.intrinsic == fromIntrinsic);
                if (idx < 0) break;
                _defaultChildren[idx] = (toChildId, toIntrinsic);
            }
            return this;
        }

        public CardData Clone(string newId)
        {
            var clone = new CardData(newId, Name, Description, Category, _defaultTags, Sprite);

            if (DefaultMetaData != null) clone.DefaultMetaData.Merge(DefaultMetaData);
            foreach (var prop in _defaultProperties) clone._defaultProperties.Add(prop);
            foreach (var child in _defaultChildren) clone._defaultChildren.Add(child);

            return clone;
        }
    }
}
