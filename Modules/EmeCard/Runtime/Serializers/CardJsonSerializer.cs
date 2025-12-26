using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using EasyPack.GamePropertySystem;
using EasyPack.Serialization;
using UnityEngine;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     EmeCard 的 JSON 序列化器
    ///     实现双泛型接口，将 Card 与其子层级转换为 JSON，及从 JSON 重建
    /// </summary>
    public class CardJsonSerializer : ITypeSerializer<Card, SerializableCard>
    {
        private readonly GamePropertyJsonSerializer _propertySerializer = new();

        /// <summary>
        ///     可选的卡牌工厂引用，用于在反序列化时重建符合静态数据的原型
        /// </summary>
        public static ICardFactory Factory { get; set; }

        #region ITypeSerializer<Card, SerializableCard> 实现

        /// <summary>
        ///     将 Card 对象转换为可序列化的 DTO
        /// </summary>
        public SerializableCard ToSerializable(Card obj)
        {
            if (obj == null)
            {
                Debug.LogWarning("[CardJsonSerializer] Attempting to serialize null Card");
                return null;
            }

            if (obj.Data == null)
            {
                throw new SerializationException(
                    "无法序列化没有 CardData 的 Card",
                    typeof(Card),
                    SerializationErrorCode.SerializationFailed
                );
            }

            var visited = new HashSet<Card>(ReferenceEqualityComparer<Card>.Default);
            return SerializeCardRecursive(obj, visited, new());
        }

        /// <summary>
        ///     从可序列化 DTO 转换回 Card 对象
        /// </summary>
        public Card FromSerializable(SerializableCard dto) => DeserializeCardRecursive(dto, null);

        /// <summary>
        ///     从可序列化 DTO 转换回 Card 对象
        /// </summary>
        public Card FromSerializable(SerializableCard dto, Dictionary<long, Card> cache) =>
            DeserializeCardRecursive(dto, cache);

        /// <summary>
        ///     将 DTO 序列化为 JSON 字符串
        /// </summary>
        public string ToJson(SerializableCard dto) => dto == null ? null : JsonUtility.ToJson(dto);

        /// <summary>
        ///     从 JSON 字符串反序列化为 DTO
        /// </summary>
        public SerializableCard FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;

            SerializableCard data;
            try
            {
                data = JsonUtility.FromJson<SerializableCard>(json);
            }
            catch (Exception ex)
            {
                throw new SerializationException(
                    $"无效的 JSON 结构：{ex.Message}",
                    typeof(Card),
                    SerializationErrorCode.DeserializationFailed,
                    ex
                );
            }

            if (data == null)
            {
                throw new SerializationException(
                    "JSON 解析结果为空",
                    typeof(Card),
                    SerializationErrorCode.DeserializationFailed
                );
            }

            return data;
        }

        /// <summary>
        ///     将 Card 直接序列化为 JSON
        /// </summary>
        public string SerializeToJson(Card obj)
        {
            SerializableCard dto = ToSerializable(obj);
            return ToJson(dto);
        }

        /// <summary>
        ///     从 JSON 直接反序列化为 Card
        /// </summary>
        public Card DeserializeFromJson(string json) => DeserializeFromJson(json, null);

        /// <summary>
        ///     从 JSON 直接反序列化为 Card，使用缓存避免重复创建
        /// </summary>
        public Card DeserializeFromJson(string json, Dictionary<long, Card> cache)
        {
            SerializableCard dto = FromJson(json);
            return FromSerializable(dto, cache);
        }

        #endregion

        #region 私有辅助方法

        private SerializableCard SerializeCardRecursive(Card card, HashSet<Card> visited, List<Card> path)
        {
            if (!visited.Add(card))
            {
                string message = $"检测到循环引用：{BuildPath(path)} → Card[ID={card.Id}, Index={card.Index}]";
                throw new SerializationException(message, typeof(Card), SerializationErrorCode.SerializationFailed);
            }

            path.Add(card);
            try
            {
                var propertiesList = new List<SerializableGameProperty>();
                var childrenList = new List<SerializableCard>();

                var dto = new SerializableCard
                {
                    ID = card.Data.ID,
                    Name = card.Data.Name,
                    Description = card.Data.Description,
                    // 序列化时使用 CardData.Category，确保一致性
                    DefaultCategory = card.Data?.Category ?? CardData.DEFAULT_CATEGORY,
                    DefaultTags = card.Data.DefaultTags,
                    Index = card.Index,
                    UID = card.UID,
                    Properties = Array.Empty<SerializableGameProperty>(),
                    Tags = card.Tags is { Count: > 0 } tags ? new List<string>(tags).ToArray() : Array.Empty<string>(),
                    ChildrenJson = null, // 默认为空，有子卡时才序列化

                    // 运行时分类由 CategoryManager 的状态（SerializedEntity.Category）负责保存。
                    Category = null,

                    // 位置信息
                    HasPosition = card.Position.HasValue,
                    Position = card.Position ?? Vector3Int.zero,
                };

                // Debug.Log($"[Serialize] ID={dto.ID}, DefaultCategory={dto.DefaultCategory}, Category={dto.Category}");

                // 序列化 GameProperty 列表
                if (card.Properties != null)
                {
                    foreach (GameProperty prop in card.Properties)
                    {
                        try
                        {
                            string propJson = _propertySerializer.SerializeToJson(prop);
                            if (!string.IsNullOrEmpty(propJson))
                            {
                                var sProp = JsonUtility.FromJson<SerializableGameProperty>(propJson);
                                if (sProp != null)
                                {
                                    propertiesList.Add(sProp);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning(
                                $"[CardJsonSerializer] 跳过序列化失败的 GameProperty [ID={prop?.ID}]: {ex.Message}");
                        }
                    }
                }

                // 递归序列化子卡
                if (card.Children is { Count: > 0 })
                {
                    foreach (Card child in card.Children)
                    {
                        SerializableCard childDto = SerializeCardRecursive(child, visited, path);
                        // 标记固有子卡：使用公开的只读访问器
                        childDto.IsIntrinsic = card.IsIntrinsic(child);
                        childrenList.Add(childDto);
                    }

                    // 子卡数组序列化
                    if (childrenList.Count > 0)
                    {
                        var childrenArray = childrenList.ToArray();
                        dto.ChildrenJson = JsonUtility.ToJson(new SerializableCardArray { Cards = childrenArray });
                    }
                }

                // 转换为数组
                dto.Properties = propertiesList.ToArray();

                return dto;
            }
            finally
            {
                path.RemoveAt(path.Count - 1);
                visited.Remove(card);
            }
        }

        private Card DeserializeCardRecursive(SerializableCard data, Dictionary<long, Card> cache)
        {
            if (data == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(data.ID))
            {
                throw new SerializationException("CardData.ID 是必需字段", typeof(Card),
                    SerializationErrorCode.DeserializationFailed);
            }

            // 检查缓存
            if (cache != null && cache.TryGetValue(data.UID, out Card existingCard))
            {
                return existingCard;
            }

            // 反序列化时创建的CardData不应包含DefaultTags
            // DefaultTags仅在新建卡牌时使用，反序列化的卡牌标签完全由序列化数据决定

            // 尝试使用工厂创建原型以获取正确的 CardData
            Card prototype = null;
            if (Factory != null && !string.IsNullOrEmpty(data.ID))
            {
                try
                {
                    // 创建一个临时原型来获取 CardData
                    // 注意：这里可能会有副作用（如果工厂方法里有副作用），但通常工厂方法只负责创建对象
                    prototype = Factory.Create(data.ID);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[CardJsonSerializer] 无法从工厂创建原型 ID={data.ID}: {ex.Message}");
                }
            }

            CardData cardData;
            if (prototype != null && prototype.Data != null)
            {
                // 使用工厂创建的 CardData
                cardData = prototype.Data;
            }
            else
            {
                // 回退
                string category = data.Category;
                if (string.IsNullOrEmpty(category))
                {
                    category = data.DefaultCategory;
                }

                if (string.IsNullOrEmpty(category))
                {
                    category = CardData.DEFAULT_CATEGORY;
                }

                cardData = new(
                    data.ID,
                    data.Name ?? "Default",
                    data.Description ?? string.Empty,
                    category,
                    Array.Empty<string>()
                );
            }

            var card = new Card(cardData)
            {
                Index = data.Index, UID = data.UID, Position = data.HasPosition ? data.Position : null,
            };

            // 加入缓存
            if (cache != null)
            {
                cache[card.UID] = card;
            }

            // 恢复属性
            if (data.Properties != null)
            {
                foreach (SerializableGameProperty sProp in data.Properties)
                {
                    try
                    {
                        string propJson = JsonUtility.ToJson(sProp);
                        GameProperty prop = _propertySerializer.DeserializeFromJson(propJson);
                        if (prop != null) card.Properties.Add(prop);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[CardJsonSerializer] 跳过反序列化失败的 GameProperty: {ex.Message}");
                    }
                }
            }

            // 恢复标签 - 直接保存序列化时的所有标签
            // 注意：反序列化时不应用DefaultTags，这些标签完全由序列化数据决定

            if (data.Tags is { Length: > 0 })
            {
                var allTags = new List<string>();
                foreach (string tag in data.Tags)
                {
                    if (!string.IsNullOrEmpty(tag))
                    {
                        allTags.Add(tag);
                    }
                }

                if (allTags.Count > 0)
                {
                    card.PendingExtraTags = allTags;
                }
            }

            // 恢复子卡
            if (!string.IsNullOrEmpty(data.ChildrenJson))
            {
                try
                {
                    var childrenArray = JsonUtility.FromJson<SerializableCardArray>(data.ChildrenJson);
                    if (childrenArray is { Cards: not null })
                    {
                        foreach (SerializableCard childData in childrenArray.Cards)
                        {
                            Card child = DeserializeCardRecursive(childData, cache);
                            bool intrinsic = childData is { IsIntrinsic: true };
                            card.AddChild(child, intrinsic);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[CardJsonSerializer] 跳过反序列化失败的 Children: {ex.Message}");
                }
            }

            return card;
        }

        private static string BuildPath(List<Card> path)
        {
            if (path == null || path.Count == 0) return string.Empty;
            var sb = new StringBuilder();
            for (int i = 0; i < path.Count; i++)
            {
                Card c = path[i];
                if (i > 0) sb.Append(" → ");
                sb.Append($"Card[ID={c.Id}, Index={c.Index}]");
            }

            return sb.ToString();
        }
    }


    internal sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        public static readonly ReferenceEqualityComparer<T> Default = new();

        public bool Equals(T x, T y) => ReferenceEquals(x, y);

        public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
    }

    #endregion
}