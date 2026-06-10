using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using EasyPack.GamePropertySystem;
using EasyPack.Serialization;
using UnityEngine;

namespace EasyPack.EmeCardSystem
{
    public class CardJsonSerializer : ITypeSerializer<Card, SerializableCard>
    {
        private readonly GamePropertyJsonSerializer _propertySerializer = new();
        private readonly ICardFactory _factory;

        public CardJsonSerializer(ICardFactory factory = null)
        {
            _factory = factory;
        }

        public SerializableCard ToSerializable(Card obj)
        {
            if (obj == null)
            {
                Debug.LogWarning("[CardJsonSerializer] Attempting to serialize null Card");
                return null;
            }

            return SerializeCard(obj);
        }

        public Card FromSerializable(SerializableCard dto) => DeserializeCard(dto, null);
        public Card FromSerializable(SerializableCard dto, Dictionary<long, Card> cache) => DeserializeCard(dto, cache);
        public string ToJson(SerializableCard dto) => dto == null ? null : JsonUtility.ToJson(dto);

        public SerializableCard FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            SerializableCard data;
            try { data = JsonUtility.FromJson<SerializableCard>(json); }
            catch (Exception ex)
            {
                throw new SerializationException($"无效的 JSON 结构：{ex.Message}", typeof(Card), SerializationErrorCode.DeserializationFailed, ex);
            }
            if (data == null) throw new SerializationException("JSON 解析结果为空", typeof(Card), SerializationErrorCode.DeserializationFailed);
            return data;
        }

        public string SerializeToJson(Card obj) => ToJson(ToSerializable(obj));
        public Card DeserializeFromJson(string json) => DeserializeFromJson(json, null);
        public Card DeserializeFromJson(string json, Dictionary<long, Card> cache) => FromSerializable(FromJson(json), cache);

        private SerializableCard SerializeCard(Card card)
        {
            var dto = new SerializableCard
            {
                ID = card.Id,
                DataID = card.DataId,
                Index = card.Index,
                UID = card.UID,
                HasPosition = card.Position.HasValue,
                Position = card.Position ?? Vector3Int.zero,Properties = Array.Empty<SerializableGameProperty>(),
                // ChildrenUIDs 不再写入——children 由 CardData 重建
                ChildrenUIDs = Array.Empty<long>(),
                IntrinsicChildrenUIDs = Array.Empty<long>(),
            };

            if (card.Properties != null)
            {
                var props = new List<SerializableGameProperty>();
                foreach (GameProperty prop in card.Properties)
                {
                    try
                    {
                        var sProp = _propertySerializer.ToSerializable(prop);
                        if (sProp != null) props.Add(sProp);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[CardJsonSerializer] 跳过序列化失败的 GameProperty [ID={prop?.ID}]: {ex.Message}");
                    }
                }
                dto.Properties = props.ToArray();
            }

            return dto;
        }

        private Card DeserializeCard(SerializableCard data, Dictionary<long, Card> cache)
        {
            if (data == null) return null;
            if (string.IsNullOrEmpty(data.ID))throw new SerializationException("CardData.ID 是必需字段", typeof(Card), SerializationErrorCode.DeserializationFailed);

            if (cache != null && cache.TryGetValue(data.UID, out Card existingCard)) return existingCard;

            var card = new Card(data.ID)
            {
                DataId = string.IsNullOrEmpty(data.DataID) ? data.ID : data.DataID,
                Index = data.Index,
                UID = data.UID,
                Position = data.HasPosition ? data.Position : null,
            };

            if (cache != null) cache[card.UID] = card;

            if (data.Properties != null)
            {
                foreach (SerializableGameProperty sProp in data.Properties)
                {
                    try
                    {
                        GameProperty prop = _propertySerializer.FromSerializable(sProp);
                        if (prop != null) card.Properties.Add(prop);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[CardJsonSerializer] 跳过反序列化失败的 GameProperty: {ex.Message}");
                    }
                }
            }

            return card;
        }
    }

    internal sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        public static readonly ReferenceEqualityComparer<T> Default = new();
        public bool Equals(T x, T y) => ReferenceEquals(x, y);
        public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
