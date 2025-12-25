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

            if (state.Entities != null)
            {
                foreach (var entityDto in state.Entities)
                {
                    if (string.IsNullOrEmpty(entityDto.EntityJson)) continue;

                    // 传入 identityMap，如果该 UID 已存在于 map 中，则直接返回现有实例
                    // 这样可以自动合并父子关系中的重复序列化数据
                    Card card = _cardSerializer.DeserializeFromJson(entityDto.EntityJson, identityMap);

                    if (card != null)
                    {
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

            // 4. Tags
            if (state.Tags != null)
            {
                foreach (var tagDto in state.Tags)
                {
                    if (tagDto.EntityKeyJsons != null)
                    {
                        foreach (var keyJson in tagDto.EntityKeyJsons)
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
                foreach (var metaDto in state.Metadata)
                {
                    if (long.TryParse(metaDto.EntityKeyJson, out long uid))
                    {
                        var wrapper = JsonUtility.FromJson<CustomDataCollectionWrapper>(metaDto.MetadataJson);
                        if (wrapper != null && wrapper.Entries != null)
                        {
                            // 合并并持久化元数据：
                            // 注意：CategoryManager.GetMetadata(key) 在未命中时会返回 new()，并不会自动写回 _metadataStore。
                            // 所以这里无论是否命中，都必须在合并后调用 UpdateMetadata 写回。

                            var mergedMetadata = newManager.GetMetadata(uid) ?? new CustomDataCollection();
                            foreach (var entry in wrapper.Entries)
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
                cardList = new List<Card>();
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

        public string SerializeToJson() => JsonUtility.ToJson(GetSerializableState());

        public void DeserializeFromJson(string json) => LoadState(JsonUtility.FromJson<CardEngineDTO>(json));

        [Serializable]
        private class CustomDataCollectionWrapper
        {
            public List<CustomDataEntry> Entries;
        }
    }
}
