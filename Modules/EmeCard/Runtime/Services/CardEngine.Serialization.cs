using System;
using System.Collections.Generic;
using System.Linq;
using EasyPack.Category;
using EasyPack.CustomData;
using EasyPack.Serialization;
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

            if (CategoryManager is CategoryManager<Card, long> concreteManager)
            {
                // 确保序列化器使用当前的工厂
                CardJsonSerializer.Factory = _cardFactory;

                dto.CategoryState = concreteManager.GetSerializableState(
                    card => _cardSerializer.SerializeToJson(card),
                    uid => uid.ToString(),
                    metadata => JsonUtility.ToJson(new CustomDataCollectionWrapper { Entries = metadata.ToList() })
                );
            }

            return dto;
        }

        /// <summary>
        ///     从状态对象加载
        /// </summary>
        public void LoadState(CardEngineDTO dto)
        {
            if (dto == null || dto.CategoryState == null) return;

            // 确保序列化器使用当前的工厂
            CardJsonSerializer.Factory = _cardFactory;

            // 1. 清除当前状态
            ClearAllCards();

            // 2. 重建 CategoryManager
            if (CategoryManager is IDisposable disposable) disposable.Dispose();
            var newManager = new CategoryManager<Card, long>(card => card.UID);
            CategoryManager = newManager;

            var state = dto.CategoryState;

            // 3. Entities (同时恢复 CardEngine 内部状态)
            // 使用 Identity Map 确保同一 UID 只对应一个实例
            var identityMap = new Dictionary<long, Card>();
            var restoredUids = new HashSet<long>();
            long maxUID = 0;

            if (state.Entities != null)
            {
                foreach (SerializableCategoryManagerState<Card, long>.SerializedEntity entityDto in state.Entities)
                {
                    if (string.IsNullOrEmpty(entityDto.EntityJson)) continue;

                    // 从 JSON 字符串解析为 SerializableCard DTO，然后转换为 Card 实例
                    // 传入 identityMap，如果该 UID 已存在于 map 中，则直接返回现有实例
                    SerializableCard cardDto = _cardSerializer.FromJson(entityDto.EntityJson);
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

                        // 注册到分类
                        // 如果有 DefaultMetaData，先应用它（创建副本以避免修改共享数据）
                        if (card.Data?.DefaultMetaData != null && card.Data.DefaultMetaData.Count > 0)
                        {
                            var initialMetadata = new CustomDataCollection(card.Data.DefaultMetaData);
                            newManager.RegisterEntityWithMetadata(card.UID, card, entityDto.Category, initialMetadata);
                        }
                        else
                        {
                            newManager.RegisterEntity(card.UID, card, entityDto.Category);
                        }
                    }
                }
            }

            // 同步 UID 计数器，防止后续分配冲突
            CardFactory.SyncUID(maxUID);

            // 3.5. 建立父子关系（第二阶段）
            // 首先建立 UID -> SerializableCard 的映射，以便获取 IsIntrinsic 标记
            var dtoMap = new Dictionary<long, SerializableCard>();
            if (state.Entities != null)
            {
                foreach (SerializableCategoryManagerState<Card, long>.SerializedEntity entityDto in state.Entities)
                {
                    if (string.IsNullOrEmpty(entityDto.EntityJson)) continue;
                    SerializableCard cardDto = _cardSerializer.FromJson(entityDto.EntityJson);
                    if (cardDto != null)
                    {
                        dtoMap[cardDto.UID] = cardDto;
                    }
                }
            }

            // 现在所有卡牌都已创建并缓存，可以安全地建立父子关系
            if (state.Entities != null)
            {
                foreach (SerializableCategoryManagerState<Card, long>.SerializedEntity entityDto in state.Entities)
                {
                    if (string.IsNullOrEmpty(entityDto.EntityJson)) continue;

                    SerializableCard cardDto = _cardSerializer.FromJson(entityDto.EntityJson);
                    if (cardDto == null) continue;

                    // 从缓存中获取父卡牌
                    if (!identityMap.TryGetValue(cardDto.UID, out Card parentCard)) continue;

                    // 建立父子关系
                    if (cardDto.ChildrenUIDs != null && cardDto.ChildrenUIDs.Length > 0)
                    {
                        foreach (long childUID in cardDto.ChildrenUIDs)
                        {
                            // 从缓存中查找子卡牌
                            if (identityMap.TryGetValue(childUID, out Card childCard))
                            {
                                // 避免重复添加
                                if (!parentCard.Children.Contains(childCard))
                                {
                                    // 检查是否是固有子卡
                                    bool isIntrinsic = cardDto.IntrinsicChildrenUIDs != null 
                                        && Array.IndexOf(cardDto.IntrinsicChildrenUIDs, childUID) >= 0;
                                    
                                    parentCard.AddChild(childCard, isIntrinsic);
                                }
                            }
                            else
                            {
                                Debug.LogWarning($"[CardEngine] 无法找到子卡牌 UID={childUID}，父卡牌 UID={parentCard.UID}");
                            }
                        }
                    }
                }
            }

            // 4. Tags
            if (state.Tags != null)
            {
                foreach (SerializableCategoryManagerState<Card, long>.SerializedTag tagDto in state.Tags)
                {
                    if (tagDto.EntityKeyJsons != null)
                    {
                        foreach (string keyJson in tagDto.EntityKeyJsons)
                        {
                            if (long.TryParse(keyJson, out long uid))
                            {
                                newManager.AddTag(uid, tagDto.TagName);
                            }
                        }
                    }
                }
            }

            // 5. Metadata
            if (state.Metadata != null)
            {
                foreach (SerializableCategoryManagerState<Card, long>.SerializedMetadata metaDto in state.Metadata)
                {
                    if (long.TryParse(metaDto.EntityKeyJson, out long uid))
                    {
                        var wrapper = JsonUtility.FromJson<CustomDataCollectionWrapper>(metaDto.MetadataJson);
                        if (wrapper != null && wrapper.Entries != null)
                        {
                            // 合并并持久化元数据：
                            // 注意：CategoryManager.GetMetadata(key) 在未命中时会返回 new()，并不会自动写回 _metadataStore。
                            // 所以这里无论是否命中，都必须在合并后调用 UpdateMetadata 写回。

                            CustomDataCollection mergedMetadata =
                                newManager.GetMetadata(uid) ?? new CustomDataCollection();
                            foreach (CustomDataEntry entry in wrapper.Entries)
                            {
                                // 使用 SetValue 确保覆盖同名 key，且缓存一致
                                mergedMetadata.Set(entry.Key, entry.GetValue());
                            }

                            // 写回（即使 mergedMetadata 是从 store 取出的引用，也允许幂等写回；
                            // 若此前不存在元数据，则确保落到 _metadataStore）
                            newManager.UpdateMetadata(uid, mergedMetadata);
                        }
                    }
                }
            }

            // 6. 重新初始化缓存
            InitializeTargetSelectorCache();
        }

        /// <summary>
        ///     将反序列化的卡牌恢复到引擎内部缓存中
        /// </summary>
        private void RestoreCardToEngine(Card card)
        {
            if (card == null) return;

            card.Engine = this;

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

            // 恢复位置缓存
            if (card.Position.HasValue)
            {
                _cardsByPosition[card.Position.Value] = card;
                _positionByUID[card.UID] = card.Position.Value;
            }

            // 订阅事件
            card.OnEvent += OnCardEvent;
        }

        public string SerializeToJson(bool prettyPrint = true) => JsonUtility.ToJson(GetSerializableState(), prettyPrint);

        public void DeserializeFromJson(string json) => LoadState(JsonUtility.FromJson<CardEngineDTO>(json));

        [Serializable]
        private class CustomDataCollectionWrapper
        {
            public List<CustomDataEntry> Entries;
        }
    }
}