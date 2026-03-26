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
        private readonly CardJsonSerializer _cardSerializer = new();

        /// <summary>
        ///     获取可序列化的状态对象
        /// </summary>
        public CardEngineDTO GetSerializableState()
        {
            var dto = new CardEngineDTO();

            // 确保序列化器使用当前的工厂
            CardJsonSerializer.Factory = _cardFactory;

            var cards = new List<SerializableCard>(_cardsByUID.Count);
            foreach (Card card in _cardsByUID.Values)
            {
                if (card == null) continue;

                SerializableCard cardDto = _cardSerializer.ToSerializable(card);
                if (cardDto != null)
                {
                    cards.Add(cardDto);
                }
            }

            dto.Cards = cards.Count == 0 ? Array.Empty<SerializableCard>() : cards.ToArray();

            var metadataEntries = new List<SerializableCardMetadata>();
            foreach (Card card in _cardsByUID.Values)
            {
                if (card == null || card.UID < 0) continue;

                CustomDataCollection metadata = CategoryManager.GetMetadata(card.UID);
                if (metadata == null || metadata.Count == 0) continue;

                string metadataJson = JsonUtility.ToJson(new CustomDataCollectionWrapper
                {
                    Entries = new List<CustomDataEntry>(metadata),
                });

                metadataEntries.Add(new SerializableCardMetadata
                {
                    UID = card.UID,
                    MetadataJson = metadataJson,
                });
            }

            dto.Metadata = metadataEntries.Count == 0
                ? Array.Empty<SerializableCardMetadata>()
                : metadataEntries.ToArray();

            return dto;
        }

        /// <summary>
        ///     从状态对象加载
        /// </summary>
        public void LoadState(CardEngineDTO dto)
        {
            if (dto == null) return;

            // 确保序列化器使用当前的工厂
            CardJsonSerializer.Factory = _cardFactory;

            // 1. 清除当前状态
            ClearAllCards();

            // 2. 重建 CategoryManager
            if (CategoryManager is IDisposable disposable) disposable.Dispose();
            var newManager = new CategoryManager<Card, long>(card => card.UID);
            CategoryManager = newManager;

            // 3. Entities (同时恢复 CardEngine 内部状态)
            // 使用 Identity Map 确保同一 UID 只对应一个实例
            var identityMap = new Dictionary<long, Card>();
            var restoredUids = new HashSet<long>();
            long maxUID = 0;

            if (dto.Cards != null)
            {
                foreach (SerializableCard cardDto in dto.Cards)
                {
                    Card card = cardDto != null ? _cardSerializer.FromSerializable(cardDto, identityMap) : null;

                    if (card != null)
                    {

                        if (!restoredUids.Add(card.UID))
                        {
                            continue;
                        }

                        if (card.UID > maxUID) maxUID = card.UID;

                        // 恢复 CardEngine 内部状态
                        // 注意：对于同一个实例（如子卡），这里可能会被调用多次（一次作为父卡的子卡，一次作为独立实体）
                        // RestoreCardToEngine 内部操作（字典赋值）是幂等的，或者是安全的覆盖
                        RestoreCardToEngine(card);

                        // 注册到 CategoryManager用于动态 metadata
                        // 如果有 DefaultMetaData，先应用它（创建副本以避免修改共享数据）
                        if (card.Data?.DefaultMetaData != null && card.Data.DefaultMetaData.Count > 0)
                        {
                            var initialMetadata = new CustomDataCollection(card.Data.DefaultMetaData);
                            newManager.RegisterEntityWithMetadata(card.UID, card, CardData.DEFAULT_CATEGORY,
                                initialMetadata);
                        }
                        else
                        {
                            newManager.RegisterEntity(card.UID, card, CardData.DEFAULT_CATEGORY);
                        }
                    }
                }
            }

            // 同步 UID 计数器，防止后续分配冲突
            CardFactory.SyncUID(maxUID);

            // 3.5. Runtime Metadata
            RestoreRuntimeMetadata(newManager, dto.Metadata);

            // 3.6. 建立父子关系（第二阶段）
            RestoreHierarchy(dto.Cards, identityMap);

            // 3.7. 在层级重建完成后统一恢复位置缓存，避免子卡覆盖根卡位置索引
            RebuildPositionCaches(identityMap.Values.ToList());
        }

        /// <summary>
        ///     将反序列化的卡牌恢复到引擎内部缓存中
        /// </summary>
        private void RestoreCardToEngine(Card card)
        {
            if (card == null) return;

            card.Engine = this;
            EnsureTemplateDataRegistered(card);

            // 恢复索引
            string id = card.Id;
            if (!_idIndexes.TryGetValue(id, out var indexes))
            {
                indexes = new();
                _idIndexes[id] = indexes;
                _idMaxIndexes[id] = -1;
            }

            indexes.Add(card.Index);
            if (card.Index > _idMaxIndexes[id])
            {
                _idMaxIndexes[id] = card.Index;
            }

            // 恢复查找表
            _cardsByUID[card.UID] = card;
            _cardsByKey[(id, card.Index)] = card;

            if (!_cardsById.TryGetValue(id, out var cardList))
            {
                cardList = new();
                _cardsById[id] = cardList;
            }

            cardList.Add(card);

            // 订阅事件
            card.OnEvent += OnCardEvent;
        }

        /// <summary>
        ///    恢复父子关系
        /// </summary>
        /// <param name="cardDtos"></param>
        /// <param name="identityMap"></param>
        private void RestoreHierarchy(IReadOnlyList<SerializableCard> cardDtos, Dictionary<long, Card> identityMap)
        {
            if (cardDtos == null || cardDtos.Count == 0 || identityMap == null || identityMap.Count == 0)
            {
                return;
            }

            var dtoByUid = new Dictionary<long, SerializableCard>(cardDtos.Count);
            var childrenWithParents = new HashSet<long>();

            foreach (SerializableCard cardDto in cardDtos)
            {
                if (cardDto == null) continue;

                dtoByUid[cardDto.UID] = cardDto;

                if (cardDto.ChildrenUIDs == null) continue;
                foreach (long childUid in cardDto.ChildrenUIDs)
                {
                    childrenWithParents.Add(childUid);
                }
            }

            var visited = new HashSet<long>();

            foreach (SerializableCard cardDto in cardDtos)
            {
                if (cardDto == null || childrenWithParents.Contains(cardDto.UID)) continue;
                RestoreHierarchyRecursive(cardDto, dtoByUid, identityMap, visited);
            }

            foreach (SerializableCard cardDto in cardDtos)
            {
                if (cardDto == null || visited.Contains(cardDto.UID)) continue;
                RestoreHierarchyRecursive(cardDto, dtoByUid, identityMap, visited);
            }
        }

        private void RestoreHierarchyRecursive(SerializableCard parentDto,
                                              IReadOnlyDictionary<long, SerializableCard> dtoByUid,
                                              IReadOnlyDictionary<long, Card> identityMap,
                                              ISet<long> visited)
        {
            if (parentDto == null || !visited.Add(parentDto.UID))
            {
                return;
            }

            if (!identityMap.TryGetValue(parentDto.UID, out Card parentCard) ||
                parentDto.ChildrenUIDs == null ||
                parentDto.ChildrenUIDs.Length == 0)
            {
                return;
            }

            long[] intrinsicChildren = parentDto.IntrinsicChildrenUIDs ?? Array.Empty<long>();

            foreach (long childUid in parentDto.ChildrenUIDs)
            {
                if (!identityMap.TryGetValue(childUid, out Card childCard))
                {
                    Debug.LogWarning($"[CardEngine] 无法找到子卡牌 UID={childUid}，父卡牌 UID={parentDto.UID}");
                    continue;
                }

                if (childCard.Owner != null && !ReferenceEquals(childCard.Owner, parentCard))
                {
                    Debug.LogWarning(
                        $"[CardEngine] 子卡牌 UID={childUid} 已有父卡牌 UID={childCard.Owner.UID}，忽略重复父级 UID={parentDto.UID}");
                    continue;
                }

                if (!parentCard.IsChild(childCard))
                {
                    bool isIntrinsic = Array.IndexOf(intrinsicChildren, childUid) >= 0;
                    parentCard.RestoreChild(childCard, isIntrinsic);
                }

                if (dtoByUid.TryGetValue(childUid, out SerializableCard childDto))
                {
                    RestoreHierarchyRecursive(childDto, dtoByUid, identityMap, visited);
                }
            }
        }

        private static void RestoreRuntimeMetadata(ICategoryManager<Card, long> categoryManager,
                                                   IReadOnlyList<SerializableCardMetadata> metadataDtos)
        {
            if (categoryManager == null || metadataDtos == null || metadataDtos.Count == 0)
            {
                return;
            }

            foreach (SerializableCardMetadata metadataDto in metadataDtos)
            {
                if (metadataDto == null || string.IsNullOrEmpty(metadataDto.MetadataJson)) continue;

                long uid = metadataDto.UID;
                var wrapper = JsonUtility.FromJson<CustomDataCollectionWrapper>(metadataDto.MetadataJson);
                if (wrapper == null || wrapper.Entries == null) continue;

                CustomDataCollection mergedMetadata = categoryManager.GetMetadata(uid) ?? new CustomDataCollection();
                foreach (CustomDataEntry entry in wrapper.Entries)
                {
                    mergedMetadata.Set(entry.Key, entry.GetValue());
                }

                categoryManager.UpdateMetadata(uid, mergedMetadata);
            }
        }

        public string SerializeToJson(bool prettyPrint = true) => JsonUtility.ToJson(GetSerializableState(), prettyPrint);

        public void DeserializeFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return;

            CardEngineDTO dto = JsonUtility.FromJson<CardEngineDTO>(json);
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