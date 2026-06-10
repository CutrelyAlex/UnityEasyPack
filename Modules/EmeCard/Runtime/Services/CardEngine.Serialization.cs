using System;
using System.Collections.Generic;
using System.Linq;
using EasyPack.Category;
using EasyPack.CustomData;
using UnityEngine;

namespace EasyPack.EmeCardSystem
{
    public sealed partial class CardEngine
    {
        private readonly CardJsonSerializer _cardSerializer;

        public CardEngineDTO GetSerializableState()
        {
            var dto = new CardEngineDTO();

            // 只序列化 root cards（子卡由 CardData 重建，不再作为权威数据保存）
            var cards = new List<SerializableCard>();
            foreach (Card card in _cardsByUID.Values)
            {
                if (card == null || card.Owner != null) continue;

                SerializableCard cardDto = _cardSerializer.ToSerializable(card);
                if (cardDto != null) cards.Add(cardDto);
            }

            dto.Cards = cards.Count == 0 ? Array.Empty<SerializableCard>() : cards.ToArray();

            var metadataEntries = new List<SerializableCardMetadata>();
            foreach (Card card in _cardsByUID.Values)
            {
                if (card == null || card.UID < 0) continue;

                CustomDataCollection metadata = ICategoryManager.GetMetadata(card.UID);
                if (metadata == null || metadata.Count == 0) continue;

                string metadataJson = UnityEngine.JsonUtility.ToJson(new CustomDataCollectionWrapper
                {
                    Entries = new List<CustomDataEntry>(metadata),
                });

                metadataEntries.Add(new SerializableCardMetadata
                {
                    UID = card.UID,
                    MetadataJson = metadataJson,
                });
            }

            dto.Metadata = metadataEntries.Count == 0 ? Array.Empty<SerializableCardMetadata>() : metadataEntries.ToArray();

            return dto;
        }

        public void LoadState(CardEngineDTO dto)
        {
            if (dto == null) return;

            ClearAllCards();

            if (ICategoryManager is IDisposable disposable) disposable.Dispose();
            var newManager = new CategoryManager<Card, long>(card => card.UID);
            ICategoryManager = newManager;

            var identityMap = new Dictionary<long, Card>();
            var restoredUids = new HashSet<long>();
            ICardFactory.ResetMaxUID();
            long maxUID = 0;

            if (dto.Cards != null)
            {
                foreach (SerializableCard cardDto in dto.Cards)
                {
                    Card card = cardDto != null ? _cardSerializer.FromSerializable(cardDto, identityMap) : null;
                    if (card == null) continue;

                    if (!restoredUids.Add(card.UID)) continue;
                    
                    if (card.UID > maxUID) maxUID = card.UID;

                    RestoreCardToEngine(card);

                    if (card.Data?.DefaultMetaData != null && card.Data.DefaultMetaData.Count > 0)
                        newManager.RegisterEntityWithMetadata(card.UID, card, CardData.DEFAULT_CATEGORY, card.Data.DefaultMetaData.Clone());
                    else
                        newManager.RegisterEntity(card.UID, card, CardData.DEFAULT_CATEGORY);
                }
            }

            ICardFactory.SyncUID(maxUID);

            RestoreRuntimeMetadata(newManager, dto.Metadata);

            // 注意：子卡层级由 World.Load 调用 RebuildTemplateChildrenForAllRootCards 重建
            // 不再在此处调用 RestoreHierarchy

            RebuildPositionCaches(identityMap.Values.ToList());
        }

        private void RestoreCardToEngine(Card card)
        {
            if (card == null) return;

            card.Engine = this;
            EnsureTemplateDataRegistered(card);

            string dataId = card.DataId;
            string logicalId = GetLogicalIdForDataId(dataId);
            if (!string.IsNullOrEmpty(logicalId) && card.Id != logicalId)
            {
                card.Id = logicalId;
                card.DataId = dataId;
            }

            string id = card.Id;
            if (!_idIndexes.TryGetValue(id, out var indexes))
            {
                indexes = new();
                _idIndexes[id] = indexes;
                _idMaxIndexes[id] = -1;
            }

            indexes.Add(card.Index);
            if (card.Index > _idMaxIndexes[id]) _idMaxIndexes[id] = card.Index;

            _cardsByUID[card.UID] = card;
            _cardsByKey[(id, card.Index)] = card;

            if (!_cardsById.TryGetValue(id, out var cardList))
            {
                cardList = new();
                _cardsById[id] = cardList;
            }

            cardList.Add(card);
            card.OnEvent += OnCardEvent;
        }

        private static void RestoreRuntimeMetadata(ICategoryManager<Card, long> categoryManager,
                                                   IReadOnlyList<SerializableCardMetadata> metadataDtos)
        {
            if (categoryManager == null || metadataDtos == null || metadataDtos.Count == 0) return;

            foreach (SerializableCardMetadata metadataDto in metadataDtos)
            {
                if (metadataDto == null || string.IsNullOrEmpty(metadataDto.MetadataJson)) continue;

                long uid = metadataDto.UID;
                var wrapper = UnityEngine.JsonUtility.FromJson<CustomDataCollectionWrapper>(metadataDto.MetadataJson);
                if (wrapper == null || wrapper.Entries == null) continue;

                CustomDataCollection mergedMetadata = categoryManager.GetMetadata(uid) ?? new CustomDataCollection();
                foreach (CustomDataEntry entry in wrapper.Entries)
                    mergedMetadata.Set(entry.Key, entry.GetValue());

                categoryManager.UpdateMetadata(uid, mergedMetadata);
            }
        }

        public string SerializeToJson(bool prettyPrint = true) => UnityEngine.JsonUtility.ToJson(GetSerializableState(), prettyPrint);

        public void DeserializeFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return;

            CardEngineDTO dto = UnityEngine.JsonUtility.FromJson<CardEngineDTO>(json);
            bool isLikelyLegacyPayload = dto == null || (dto.Cards == null && dto.Metadata == null);
            if (isLikelyLegacyPayload &&
                CardEngineLegacyCompatibility.TryConvertFromLegacyJson(json, _cardSerializer, out CardEngineDTO legacyDto))
            {
                dto = legacyDto;
            }

            LoadState(dto);
        }

        [Serializable]
        private class CustomDataCollectionWrapper
        {
            public List<CustomDataEntry> Entries;
        }
    }
}
