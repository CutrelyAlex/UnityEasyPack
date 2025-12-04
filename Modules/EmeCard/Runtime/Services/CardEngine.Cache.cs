using System.Collections.Generic;
using UnityEngine;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     卡牌引擎 - 内部缓存与卡牌管理
    /// </summary>
    public sealed partial class CardEngine
    {
        #region 内部缓存字段

        // 已注册的卡牌模板集合
        private readonly HashSet<Card> _registeredCardsTemplates = new();

        // ID -> Index 集合缓存
        private readonly Dictionary<string, HashSet<int>> _idIndexes = new();

        // ID -> MaxIndex 缓存
        private readonly Dictionary<string, int> _idMaxIndexes = new();

        // ID -> Card 列表缓存，用于快速查找
        private readonly Dictionary<string, List<Card>> _cardsById = new();

        // UID -> Card 缓存，支持 O(1) UID 查询
        private readonly Dictionary<long, Card> _cardsByUID = new();

        // 位置 -> Card 映射（一个位置最多一个卡牌）
        // 仅存储根卡牌（无 Owner 的卡牌），子卡牌的位置通过 RootCard 动态派生
        private readonly Dictionary<Vector3Int?, Card> _cardsByPosition = new();

        // UID -> 位置缓存
        private readonly Dictionary<long, Vector3Int?> _positionByUID = new();

        #endregion

        #region 卡牌缓存处理

        /// <summary>
        ///     添加卡牌到引擎，分配唯一 Index、UID 并订阅事件。
        /// </summary>
        public CardEngine AddCard(Card card)
        {
            if (card == null) return this;

            // 设置卡牌的 Engine 引用
            card.Engine = this;

            string id = card.Id;

            // 第一步: 处理 UID（必须先分配 UID，因为 CategoryManager 使用 UID 作为键）
            if (card.UID < 0)
            {
                // 未分配 UID，分配新的
                EmeCardSystem.CardFactory.AssignUID(card);
            }
            else if (_cardsByUID.ContainsKey(card.UID))
            {
                // UID 冲突，重新分配
                card.UID = -1;
                EmeCardSystem.CardFactory.AssignUID(card);
            }

            // 第二步: 分配索引（如果还未分配）
            if (!_idIndexes.TryGetValue(id, out var indexes))
            {
                indexes = new HashSet<int>();
                _idIndexes[id] = indexes;
                _idMaxIndexes[id] = -1;
            }

            if (card.Index < 0)
            {
                int maxIndex = _idMaxIndexes[id];
                card.Index = maxIndex + 1;
                _idMaxIndexes[id] = card.Index;
            }
            else if (indexes.Contains(card.Index))
            {
                int maxIndex = _idMaxIndexes[id];
                card.Index = maxIndex + 1;
                _idMaxIndexes[id] = card.Index;
            }

            // 第三步: 订阅卡牌事件
            card.OnEvent += OnCardEvent;

            // 第四步: 添加到索引
            indexes.Add(card.Index);
            _cardsByUID[card.UID] = card;

            if (!_cardsById.TryGetValue(id, out var cardList))
            {
                cardList = new List<Card>();
                _cardsById[id] = cardList;
            }
            cardList.Add(card);

            // 第五步: 注册到 CategoryManager
            RegisterToCategoryManager(card);

            // 第六步: 将卡牌的所有标签加入 TargetSelector 缓存
            foreach (string tag in card.Tags)
            {
                TargetSelector.OnCardTagAdded(card, tag);
            }

            // 第七步: 递归注册所有已存在的子卡牌
            if (card.Children is { Count: > 0 })
            {
                foreach (Card child in card.Children)
                {
                    if (child != null && !HasCard(child))
                    {
                        AddCard(child);
                    }
                }
            }

            // 第八步: 注册位置映射（仅对根卡牌）
            _positionByUID[card.UID] = card.Position;

            if (card.RootCard == null)
            {
                card.RootCard = card;
            }

            // 子卡牌不添加到位置索引
            if (card.Owner != null) return this;

            var initialPosition = card.Position;
            if (initialPosition == null) return this;

            if (_cardsByPosition.TryGetValue(initialPosition, out Card existingCard))
            {
                Debug.LogError($"[CardEngine] 位置冲突: {initialPosition} 已被 '{existingCard.Id}' (UID: {existingCard.UID}) 占用");
                return this;
            }

            _cardsByPosition[initialPosition] = card;
            return this;
        }

        /// <summary>
        ///     将子卡牌添加到父卡牌，同时确保子卡牌已注册到引擎。
        /// </summary>
        public CardEngine AddChildToCard(Card parent, Card child, bool intrinsic = false)
        {
            if (parent == null) throw new System.ArgumentNullException(nameof(parent));
            if (child == null) throw new System.ArgumentNullException(nameof(child));

            if (!HasCard(parent))
            {
                throw new System.InvalidOperationException($"父卡牌 '{parent.Id}' 未注册到引擎");
            }

            child.Owner?.RemoveChild(child);

            if (!HasCard(child))
            {
                AddCard(child);
            }

            parent.AddChild(child, intrinsic);
            return this;
        }

        /// <summary>
        ///     批量将多个子卡牌添加到父卡牌。
        /// </summary>
        public CardEngine AddChildrenToCard(Card parent, IEnumerable<Card> children, bool intrinsic = false)
        {
            if (parent == null) throw new System.ArgumentNullException(nameof(parent));
            if (children == null) throw new System.ArgumentNullException(nameof(children));

            foreach (Card child in children)
            {
                if (child != null)
                {
                    AddChildToCard(parent, child, intrinsic);
                }
            }
            return this;
        }

        /// <summary>
        ///     转移根卡牌到新位置。
        /// </summary>
        public CardEngine MoveCardToPosition(Card card, Vector3Int newPosition)
        {
            if (card is not { Owner: null }) return this;

            var oldPosition = card.Position;
            if (oldPosition.HasValue && oldPosition.Value == newPosition) return this;

            // 从旧位置移除
            if (oldPosition != null &&
                _cardsByPosition.TryGetValue(oldPosition, out Card cardAtOldPosition) &&
                cardAtOldPosition == card)
            {
                _cardsByPosition.Remove(oldPosition);
            }

            card.Position = newPosition;
            _cardsByPosition[newPosition] = card;
            _positionByUID[card.UID] = newPosition;

            return this;
        }

        /// <summary>
        ///     清除根卡牌的位置。
        /// </summary>
        public CardEngine ClearCardPosition(Card card)
        {
            if (card is not { Owner: null }) return this;

            var oldPosition = card.Position;
            if (oldPosition == null) return this;

            if (_cardsByPosition.TryGetValue(oldPosition, out Card cardAtPosition) && cardAtPosition == card)
            {
                _cardsByPosition.Remove(oldPosition);
            }

            card.Position = null;
            _positionByUID[card.UID] = null;

            return this;
        }

        /// <summary>
        ///     当卡牌被添加为子卡牌时，从位置索引中移除它。
        /// </summary>
        internal void NotifyChildAddedToParent(Card child)
        {
            if (child?.Position != null &&
                _cardsByPosition.TryGetValue(child.Position, out Card cardAtPosition) &&
                cardAtPosition == child)
            {
                _cardsByPosition.Remove(child.Position);
            }
        }

        /// <summary>
        ///     当子卡牌从父卡牌移除时，重新添加到位置索引。
        /// </summary>
        internal void NotifyChildRemovedFromParent(Card card, Vector3Int newPosition)
        {
            if (card is not { Owner: null }) return;

            if (_cardsByPosition.TryGetValue(newPosition, out Card existingCard) && existingCard != card)
            {
                Debug.LogWarning($"[CardEngine] 位置 {newPosition} 已被 '{existingCard.Id}' 占用");
                return;
            }

            card.Position = newPosition;
            _cardsByPosition[newPosition] = card;
            _positionByUID[card.UID] = newPosition;
        }

        /// <summary>
        ///     移除卡牌，移除事件订阅、UID 映射与索引。
        /// </summary>
        public CardEngine RemoveCard(Card c)
        {
            if (c == null || !_cardsByUID.ContainsKey(c.UID)) return this;

            c.OnEvent -= OnCardEvent;

            if (c.UID >= 0) _cardsByUID.Remove(c.UID);

            if (_idIndexes.TryGetValue(c.Id, out var indexes))
            {
                indexes.Remove(c.Index);
                if (indexes.Count == 0) _idIndexes.Remove(c.Id);
            }

            if (_cardsById.TryGetValue(c.Id, out var cardList))
            {
                cardList.Remove(c);
                if (cardList.Count == 0) _cardsById.Remove(c.Id);
            }

            // 收集标签用于后续清理
            var tags = c.Tags;
            string[] tagArray = null;
            if (tags.Count > 0)
            {
                tagArray = new string[tags.Count];
                int idx = 0;
                foreach (string tag in tags)
                {
                    tagArray[idx++] = tag;
                }
            }

            UnregisterFromCategoryManager(c.UID);

            // 从位置映射中移除
            if (c.Owner == null && c.Position != null &&
                _cardsByPosition.TryGetValue(c.Position, out Card cardAtPosition) &&
                cardAtPosition == c)
            {
                _cardsByPosition.Remove(c.Position);
            }
            _positionByUID.Remove(c.UID);

            // 清理 TargetSelector 标签缓存
            if (tagArray != null)
            {
                foreach (string tag in tagArray)
                {
                    TargetSelector.OnCardTagRemoved(c, tag);
                }
            }

            return this;
        }

        /// <summary>
        ///     清除所有卡牌。
        /// </summary>
        public void ClearAllCards()
        {
            foreach (var uid in _cardsByUID.Keys)
            {
                UnregisterFromCategoryManager(uid);
            }

            _idIndexes.Clear();
            _cardsById.Clear();
            _cardsByUID.Clear();
            _cardsByPosition.Clear();
            _positionByUID.Clear();
        }

        #endregion
    }
}
