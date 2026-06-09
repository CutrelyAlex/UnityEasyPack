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
            var remaining = new Dictionary<ChildKey, int>();
            foreach (var (childId, intrinsic) in baseData.DefaultChildren)
                Increment(remaining, new ChildKey(childId, intrinsic), 1);

            var products = new List<ChildKey>();

            if (variant.ModifiedChildren != null)
            {
                foreach (var diff in variant.ModifiedChildren)
                {
                    if (diff == null || diff.Count <= 0 || string.IsNullOrEmpty(diff.FromChildID) || string.IsNullOrEmpty(diff.ToChildID)) continue;
                    var from = new ChildKey(diff.FromChildID, diff.FromIntrinsic);
                    var to = new ChildKey(diff.ToChildID, diff.ToIntrinsic);
                    int consumed = Consume(remaining, from, diff.Count);
                    if (consumed < diff.Count)
                        Debug.LogWarning($"[MCardDataVariantBuilder] Variant '{variant.ID}' modifies {diff.Count} x {from.ChildID}, base only has {consumed}.");
                    for (int i = 0; i < consumed; i++) products.Add(to);
                }
            }

            if (variant.RemovedChildren != null)
            {
                foreach (var diff in variant.RemovedChildren)
                {
                    if (diff == null || diff.Count <= 0 || string.IsNullOrEmpty(diff.ChildID)) continue;
                    var key = new ChildKey(diff.ChildID, diff.Intrinsic);
                    int consumed = Consume(remaining, key, diff.Count);
                    if (consumed < diff.Count)
                        Debug.LogWarning($"[CardDataVariantBuilder] Variant '{variant.ID}' removes {diff.Count} x {diff.ChildID}, base only has {consumed}.");
                }
            }

            result.ClearChildren();

            foreach (var pair in remaining)
                for (int i = 0; i < pair.Value; i++)
                    result.WithChild(pair.Key.ChildID, pair.Key.Intrinsic);

            foreach (var key in products)
                result.WithChild(key.ChildID, key.Intrinsic);

            if (variant.AddedChildren != null)
            {
                foreach (var diff in variant.AddedChildren)
                {
                    if (diff == null || diff.Count <= 0 || string.IsNullOrEmpty(diff.ChildID)) continue;
                    for (int i = 0; i < diff.Count; i++) result.WithChild(diff.ChildID, diff.Intrinsic);
                }
            }
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

        private static void Increment(IDictionary<ChildKey, int> counts, ChildKey key, int amount)
        {
            if (amount <= 0) return;
            counts.TryGetValue(key, out int current);
            counts[key] = current + amount;
        }

        private static int Consume(IDictionary<ChildKey, int> counts, ChildKey key, int requested)
        {
            if (requested <= 0 || !counts.TryGetValue(key, out int available) || available <= 0) return 0;
            int consumed = Mathf.Min(available, requested);
            int remaining = available - consumed;
            if (remaining <= 0) counts.Remove(key);
            else counts[key] = remaining;
            return consumed;
        }
    }
}
