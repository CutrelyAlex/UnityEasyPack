using System.Collections.Generic;
using UnityEngine;

namespace EasyPack.EmeCardSystem
{
    public sealed partial class CardEngine
    {
        public void RegisterMapCardDataVariants(SerializableCardDataVariant[] variants)
        {
            if (variants == null || variants.Length == 0) return;

            foreach (var variant in variants)
            {
                if (variant == null || string.IsNullOrEmpty(variant.ID) || string.IsNullOrEmpty(variant.BaseID)) continue;

                if (!TryGetTemplateData(variant.BaseID, out CardData baseData))
                {
                    Debug.LogWarning($"[CardEngine] Cannot find base CardData '{variant.BaseID}' for variant '{variant.ID}'");
                    continue;
                }

                CardData variantData = CardDataVariantBuilder.BuildVariant(baseData, variant);
                _cardDataTemplates[variant.ID] = variantData;
                _cardDataLogicalIds[variant.ID] = variant.BaseID;
                (_cardFactory as ICardFactoryRegistry)?.RegisterData(variant.ID, variantData);
            }
        }

        public void RebuildTemplateChildrenForAllRootCards()
        {
            var rootCards = new List<Card>();
            foreach (Card card in _cardsByUID.Values)
            {
                if (card != null && card.Owner == null) rootCards.Add(card);
            }

            foreach (Card root in rootCards)
            {
                // 反序列化的 root cards 没有 children，直接构建即可
                //ClearTemplateChildren(root);
                BuildTemplateChildren(root);
            }

            RebuildPositionCaches(new List<Card>(_cardsByUID.Values));
        }

        private void ClearTemplateChildren(Card parent)
        {
            if (parent == null || parent.Children.Count == 0) return;

            var children = new List<Card>(parent.Children);
            foreach (Card child in children)
            {
                if (child == null) continue;
                parent.RemoveChild(child, force: true);
                RemoveCard(child);
            }
        }

        private void BuildTemplateChildren(Card parent)
        {
            //if (parent == null) return;

            CardData templateData = GetTemplateData(parent.DataId);
            if (templateData?.DefaultChildren is not { Count: > 0 }) return;

            foreach (var (childId, intrinsic) in templateData.DefaultChildren)
            {
                if (string.IsNullOrEmpty(childId)) continue;
                Card child = CreateCard(childId);
                if (child == null) continue;
                parent.AddChild(child, intrinsic);
            }
        }
    }
}
