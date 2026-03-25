using System;
using System.Collections.Generic;
using EasyPack.Category;
using UnityEngine;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    /// 旧版 CardEngine JSON（CategoryState 结构）兼容层。
    /// 将旧结构转换为当前 CardEngineDTO（Cards + Metadata）。
    /// </summary>
    internal static class CardEngineLegacyCompatibility
    {
        [Serializable]
        private class LegacyCardEngineDTO
        {
            public SerializableCategoryManagerState<Card, long> CategoryState;
        }

        public static bool TryConvertFromLegacyJson(string json, CardJsonSerializer serializer, out CardEngineDTO dto)
        {
            dto = null;
            if (string.IsNullOrEmpty(json) || serializer == null) return false;

            LegacyCardEngineDTO legacyDto;
            try
            {
                legacyDto = JsonUtility.FromJson<LegacyCardEngineDTO>(json);
            }
            catch
            {
                return false;
            }

            SerializableCategoryManagerState<Card, long> state = legacyDto?.CategoryState;
            if (state == null) return false;

            var cardByUid = new Dictionary<long, SerializableCard>();
            var cards = new List<SerializableCard>();

            if (state.Entities != null)
            {
                foreach (SerializableCategoryManagerState<Card, long>.SerializedEntity entity in state.Entities)
                {
                    if (entity == null || string.IsNullOrEmpty(entity.EntityJson)) continue;

                    SerializableCard cardDto;
                    try
                    {
                        cardDto = serializer.FromJson(entity.EntityJson);
                    }
                    catch
                    {
                        continue;
                    }

                    if (cardDto == null) continue;

                    if (cardByUid.ContainsKey(cardDto.UID)) continue;
                    cardByUid[cardDto.UID] = cardDto;
                    cards.Add(cardDto);
                }
            }

            var metadata = new List<SerializableCardMetadata>();
            if (state.Metadata != null)
            {
                foreach (SerializableCategoryManagerState<Card, long>.SerializedMetadata meta in state.Metadata)
                {
                    if (meta == null || string.IsNullOrEmpty(meta.EntityKeyJson) || string.IsNullOrEmpty(meta.MetadataJson))
                        continue;

                    if (!long.TryParse(meta.EntityKeyJson, out long uid)) continue;

                    metadata.Add(new SerializableCardMetadata
                    {
                        UID = uid,
                        MetadataJson = meta.MetadataJson,
                    });
                }
            }

            dto = new CardEngineDTO
            {
                Cards = cards.Count == 0 ? Array.Empty<SerializableCard>() : cards.ToArray(),
                Metadata = metadata.Count == 0 ? Array.Empty<SerializableCardMetadata>() : metadata.ToArray(),
            };

            return true;
        }
    }
}
