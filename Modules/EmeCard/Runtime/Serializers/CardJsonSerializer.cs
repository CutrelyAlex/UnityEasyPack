using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using EasyPack.GamePropertySystem;
using EasyPack.Serialization;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    /// EmeCard 的 JSON 序列化器
    /// 实现双泛型接口，将 Card 与其子层级转换为 JSON，及从 JSON 重建
    /// </summary>
    public class CardJsonSerializer : ITypeSerializer<Card, SerializableCard>
    {
        private readonly GamePropertyJsonSerializer _propertySerializer = new();

        #region ITypeSerializer<Card, SerializableCard> 实现

        /// <summary>
        /// 将 Card 对象转换为可序列化的 DTO
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
            return SerializeCardRecursive(obj, visited, new List<Card>());
        }

        /// <summary>
        /// 从可序列化 DTO 转换回 Card 对象
        /// </summary>
        public Card FromSerializable(SerializableCard dto)
        {
            return DeserializeCardRecursive(dto);
        }

        /// <summary>
        /// 将 DTO 序列化为 JSON 字符串
        /// </summary>
        public string ToJson(SerializableCard dto)
        {
            if (dto == null) return null;
            return JsonUtility.ToJson(dto);
        }

        /// <summary>
        /// 从 JSON 字符串反序列化为 DTO
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
        /// 将 Card 直接序列化为 JSON
        /// </summary>
        public string SerializeToJson(Card obj)
        {
            var dto = ToSerializable(obj);
            return ToJson(dto);
        }

        /// <summary>
        /// 从 JSON 直接反序列化为 Card
        /// </summary>
        public Card DeserializeFromJson(string json)
        {
            var dto = FromJson(json);
            return FromSerializable(dto);
        }

        #endregion

        #region 私有辅助方法

        private SerializableCard SerializeCardRecursive(Card card, HashSet<Card> visited, List<Card> path)
        {
            if (visited.Contains(card))
            {
                var message = $"检测到循环引用：{BuildPath(path)} → Card[ID={card.Id}, Index={card.Index}]";
                throw new SerializationException(message, typeof(Card), SerializationErrorCode.SerializationFailed);
            }

            visited.Add(card);
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
                    Category = card.Data.Category,
                    DefaultTags = card.Data.DefaultTags,
                    Index = card.Index,
                    Properties = Array.Empty<SerializableGameProperty>(),
                    Tags = (card.Tags != null && card.Tags.Count > 0) ? new List<string>(card.Tags).ToArray() : Array.Empty<string>(),
                    ChildrenJson = null,  // 默认为空，有子卡时才序列化
                    IsIntrinsic = false
                };

                // 序列化 GameProperty 列表
                if (card.Properties != null)
                {
                    foreach (var prop in card.Properties)
                    {
                        try
                        {
                            string propJson = _propertySerializer.SerializeToJson(prop);
                            if (!string.IsNullOrEmpty(propJson))
                            {
                                var sProp = JsonUtility.FromJson<SerializableGameProperty>(propJson);
                                if (sProp != null)
                                    propertiesList.Add(sProp);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[CardJsonSerializer] 跳过序列化失败的 GameProperty [ID={prop?.ID}]: {ex.Message}");
                        }
                    }
                }

                // 递归序列化子卡
                if (card.Children != null && card.Children.Count > 0)
                {
                    foreach (var child in card.Children)
                    {
                        var childDto = SerializeCardRecursive(child, visited, path);
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

        private Card DeserializeCardRecursive(SerializableCard data)
        {
            if (data == null)
                return null;

            if (string.IsNullOrEmpty(data.ID))
            {
                throw new SerializationException("CardData.ID 是必需字段", typeof(Card), SerializationErrorCode.DeserializationFailed);
            }

            var cardData = new CardData(
                id: data.ID,
                name: data.Name ?? "Default",
                desc: data.Description ?? string.Empty,
                category: data.Category,
                defaultTags: data.DefaultTags ?? Array.Empty<string>(),
                sprite: null
            );

            var card = new Card(cardData)
            {
                Index = data.Index
            };

            // 恢复属性
            if (data.Properties != null)
            {
                foreach (var sProp in data.Properties)
                {
                    try
                    {
                        string propJson = JsonUtility.ToJson(sProp);
                        var prop = _propertySerializer.DeserializeFromJson(propJson);
                        if (prop != null)
                        {
                            card.Properties.Add(prop);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[CardJsonSerializer] 跳过反序列化失败的 GameProperty: {ex.Message}");
                    }
                }
            }

            // 恢复标签
            if (data.Tags != null)
            {
                foreach (var tag in data.Tags)
                {
                    if (!string.IsNullOrEmpty(tag)) card.AddTag(tag);
                }
            }

            // 恢复子卡
            if (!string.IsNullOrEmpty(data.ChildrenJson))
            {
                try
                {
                    var childrenArray = JsonUtility.FromJson<SerializableCardArray>(data.ChildrenJson);
                    if (childrenArray != null && childrenArray.Cards != null)
                    {
                        foreach (var childData in childrenArray.Cards)
                        {
                            var child = DeserializeCardRecursive(childData);
                            bool intrinsic = childData != null && childData.IsIntrinsic;
                            card.AddChild(child, intrinsic: intrinsic);
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

        private string BuildPath(List<Card> path)
        {
            if (path == null || path.Count == 0) return string.Empty;
            var sb = new StringBuilder();
            for (int i = 0; i < path.Count; i++)
            {
                var c = path[i];
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
        public int GetHashCode(T obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
    #endregion
}

