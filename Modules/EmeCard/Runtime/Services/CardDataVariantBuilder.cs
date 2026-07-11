using System;
using System.Collections.Generic;
using EasyPack.CustomData;
using UnityEngine;

namespace EasyPack.EmeCardSystem
{
    public static class CardDataVariantBuilder
    {
        private readonly struct ChildKey : IEquatable<ChildKey>
        {
            public readonly string ChildID;
            public readonly bool Intrinsic;

            public ChildKey(string childID, bool intrinsic) { ChildID = childID ?? string.Empty; Intrinsic = intrinsic; }
            public bool Equals(ChildKey other) => ChildID == other.ChildID && Intrinsic == other.Intrinsic;
            public override bool Equals(object obj) => obj is ChildKey other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(ChildID, Intrinsic);
        }

        public static CardData BuildVariant(CardData baseData, SerializableCardDataVariant variant)
        {
            if (baseData == null) throw new ArgumentNullException(nameof(baseData));
            if (variant == null) throw new ArgumentNullException(nameof(variant));
            if (string.IsNullOrEmpty(variant.ID)) throw new ArgumentException("Variant ID is required", nameof(variant));

            CardData result = baseData.Clone(variant.ID);

            ApplyChildrenDiff(result, baseData, variant);
            ApplyMetaDataDiff(result, baseData, variant.ModifiedMetaData);

            return result;
        }

        private static void ApplyChildrenDiff(CardData result, CardData baseData, SerializableCardDataVariant variant)
        {
            var children = new List<ChildKey>(baseData.DefaultChildren.Count);
            foreach (var (childId, intrinsic) in baseData.DefaultChildren)
                children.Add(new ChildKey(childId, intrinsic));

            if (variant.ModifiedChildren != null)
            {
                foreach (var diff in variant.ModifiedChildren)
                {
                    if (diff == null || diff.Count <= 0 || string.IsNullOrEmpty(diff.FromChildID) || string.IsNullOrEmpty(diff.ToChildID)) continue;
                    var from = new ChildKey(diff.FromChildID, diff.FromIntrinsic);
                    var to = new ChildKey(diff.ToChildID, diff.ToIntrinsic);
                    int consumed = Replace(children, from, to, diff.Count);
                    if (consumed < diff.Count)
                        Debug.LogWarning($"[MCardDataVariantBuilder] Variant '{variant.ID}' modifies {diff.Count} x {from.ChildID}, base only has {consumed}.");
                }
            }

            if (variant.RemovedChildren != null)
            {
                foreach (var diff in variant.RemovedChildren)
                {
                    if (diff == null || diff.Count <= 0 || string.IsNullOrEmpty(diff.ChildID)) continue;
                    var key = new ChildKey(diff.ChildID, diff.Intrinsic);
                    int consumed = Remove(children, key, diff.Count);
                    if (consumed < diff.Count)
                        Debug.LogWarning($"[CardDataVariantBuilder] Variant '{variant.ID}' removes {diff.Count} x {diff.ChildID}, base only has {consumed}.");
                }
            }

            if (variant.AddedChildren != null)
            {
                foreach (var diff in variant.AddedChildren)
                {
                    if (diff == null || diff.Count <= 0 || string.IsNullOrEmpty(diff.ChildID)) continue;
                    for (int i = 0; i < diff.Count; i++) children.Add(new ChildKey(diff.ChildID, diff.Intrinsic));
                }
            }

            children = ApplyOrderedLayout(children, variant.OrderedChildren);

            result.ClearChildren();
            foreach (ChildKey child in children)
                result.WithChild(child.ChildID, child.Intrinsic);
        }

        private static List<ChildKey> ApplyOrderedLayout(
            IReadOnlyList<ChildKey> children,
            IReadOnlyList<SerializableDefaultChildDiff> orderedChildren)
        {
            if (orderedChildren == null || orderedChildren.Count == 0)
                return new List<ChildKey>(children);

            var remaining = new List<ChildKey>(children);
            var ordered = new List<ChildKey>(children.Count);

            foreach (SerializableDefaultChildDiff entry in orderedChildren)
            {
                if (entry == null || entry.Count <= 0 || string.IsNullOrEmpty(entry.ChildID)) continue;
                var key = new ChildKey(entry.ChildID, entry.Intrinsic);
                for (int i = 0; i < entry.Count; i++)
                {
                    int index = remaining.FindIndex(child => child.Equals(key));
                    if (index < 0) break;
                    ordered.Add(remaining[index]);
                    remaining.RemoveAt(index);
                }
            }

            ordered.AddRange(remaining);
            return ordered;
        }

        private static void ApplyMetaDataDiff(CardData result, CardData baseData, IReadOnlyList<CustomDataEntry> modifiedMetaData)
        {
            if (modifiedMetaData == null || modifiedMetaData.Count == 0) return;

            foreach (var entry in modifiedMetaData)
            {
                if (entry == null || string.IsNullOrEmpty(entry.Key)) continue;

                if (baseData.DefaultMetaData == null || !baseData.DefaultMetaData.HasValue(entry.Key))
                {
                    Debug.LogWarning($"[CardDataVariantBuilder] Base '{baseData.ID}' has no metadata key '{entry.Key}'. Skipped.");
                    continue;
                }

                CustomDataEntry baseEntry = baseData.DefaultMetaData[entry.Key];
                if (baseEntry.Type != entry.Type)
                {
                    Debug.LogWarning($"[CardDataVariantBuilder] Metadata '{entry.Key}' type mismatch on '{result.ID}'. Skipped.");
                    continue;
                }

                if (!IsSupportedSimpleType(entry.Type))
                {
                    Debug.LogWarning($"[CardDataVariantBuilder] Metadata '{entry.Key}' type '{entry.Type}' not supported. Skipped.");
                    continue;
                }

                result.DefaultMetaData.Set(entry.Key, entry.GetValue());
            }
        }

        private static bool IsSupportedSimpleType(CustomDataType type) =>
            type is CustomDataType.Int or CustomDataType.Long or CustomDataType.Float or CustomDataType.Bool or CustomDataType.String
                 or CustomDataType.Vector2 or CustomDataType.Vector3
                 or CustomDataType.Vector3Int or CustomDataType.Color;

        private static int Replace(List<ChildKey> children, ChildKey from, ChildKey to, int count)
        {
            int replaced = 0;
            for (int i = 0; i < children.Count && replaced < count; i++)
            {
                if (!children[i].Equals(from)) continue;
                children[i] = to;
                replaced++;
            }

            return replaced;
        }

        private static int Remove(List<ChildKey> children, ChildKey key, int count)
        {
            int removed = 0;
            for (int i = 0; i < children.Count && removed < count;)
            {
                if (!children[i].Equals(key))
                {
                    i++;
                    continue;
                }

                children.RemoveAt(i);
                removed++;
            }

            return removed;
        }
    }
}
