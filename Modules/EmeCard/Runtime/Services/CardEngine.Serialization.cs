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

            // 1. 清除当前状态
            ClearAllCards();

            // 2. 重建 CategoryManager
            if (CategoryManager is IDisposable disposable) disposable.Dispose();
            var newManager = new CategoryManager<Card, long>(card => card.UID);
            CategoryManager = newManager;
            
            var state = dto.CategoryState;
            
            // 3. Entities (同时恢复 CardEngine 内部状态)
            if (state.Entities != null)
            {
                foreach (var entityDto in state.Entities)
                {
                    if (string.IsNullOrEmpty(entityDto.EntityJson)) continue;
                    
                    Card card = _cardSerializer.DeserializeFromJson(entityDto.EntityJson);
                    if (card != null)
                    {
                        // 恢复 CardEngine 内部状态
                        RestoreCardToEngine(card);

                        // 注册到分类
                        newManager.RegisterEntity(card.UID, card, entityDto.Category);
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
                            var collection = new CustomDataCollection(wrapper.Entries);
                            newManager.UpdateMetadata(uid, collection);
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
