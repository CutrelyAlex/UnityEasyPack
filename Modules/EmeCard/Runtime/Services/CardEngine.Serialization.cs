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

                // 获取通用的序列化状态（仍使用字符串序列化器）
                var genericState = concreteManager.GetSerializableState(
                    card => JsonUtility.ToJson(_cardSerializer.ToSerializable(card)),
                    uid => uid.ToString(),
                    metadata => JsonUtility.ToJson(new CustomDataCollectionWrapper { Entries = metadata.ToList() })
                );

                // 转换为 CardCategoryManagerState
                dto.CategoryState = new CardCategoryManagerState
                {
                    IncludeEntities = genericState.IncludeEntities,
                    Entities = new(),
                    Categories = new(),
                    Tags = new(),
                    Metadata = new()
                };

                // 转换 Entities：从 EntityJson (string) 解析为 SerializableCard 对象
                if (genericState.Entities != null)
                {
                    foreach (var entity in genericState.Entities)
                    {
                        // EntityJson 现在包含 Card 的 JSON 序列化字符串
                        // 先解析为 SerializableCard DTO
                        if (string.IsNullOrEmpty(entity.EntityJson))
                        {
                            Debug.LogWarning($"[CardEngine] 实体 JSON 为空，跳过: KeyJson={entity.KeyJson}");
                            continue;
                        }
                        
                        SerializableCard cardDto = _cardSerializer.FromJson(entity.EntityJson);
                        if (cardDto == null)
                        {
                            Debug.LogWarning($"[CardEngine] 无法解析实体 JSON: KeyJson={entity.KeyJson}");
                            continue;
                        }
                        
                        dto.CategoryState.Entities.Add(new CardCategoryManagerState.SerializedEntity
                        {
                            KeyJson = entity.KeyJson,
                            Entity = cardDto,
                            Category = entity.Category
                        });
                    }
                }

                // 转换 Categories
                if (genericState.Categories != null)
                {
                    foreach (var cat in genericState.Categories)
                    {
                        dto.CategoryState.Categories.Add(new CardCategoryManagerState.SerializedCategory
                        {
                            Name = cat.Name
                        });
                    }
                }

                // 转换 Tags
                if (genericState.Tags != null)
                {
                    foreach (var tag in genericState.Tags)
                    {
                        dto.CategoryState.Tags.Add(new CardCategoryManagerState.SerializedTag
                        {
                            TagName = tag.TagName,
                            EntityKeyJsons = tag.EntityKeyJsons
                        });
                    }
                }

                // 转换 Metadata
                if (genericState.Metadata != null)
                {
                    foreach (var meta in genericState.Metadata)
                    {
                        dto.CategoryState.Metadata.Add(new CardCategoryManagerState.SerializedMetadata
                        {
                            EntityKeyJson = meta.EntityKeyJson,
                            MetadataJson = meta.MetadataJson
                        });
                    }
                }
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
                foreach (CardCategoryManagerState.SerializedEntity entityDto in state.Entities)
                {
                    if (entityDto.Entity == null) continue;

                    // 直接使用 SerializableCard 对象转换为 Card 实例
                    Card card = _cardSerializer.FromSerializable(entityDto.Entity, identityMap);

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

            // 4. Tags
            if (state.Tags != null)
            {
                foreach (CardCategoryManagerState.SerializedTag tagDto in state.Tags)
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
                foreach (CardCategoryManagerState.SerializedMetadata metaDto in state.Metadata)
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
    }

    [Serializable]
    internal class CustomDataCollectionWrapper
    {
        public List<CustomDataEntry> Entries;
    }
}