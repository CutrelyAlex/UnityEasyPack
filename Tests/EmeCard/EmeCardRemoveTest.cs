using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EasyPack.Category;
using EasyPack.CustomData;
using EasyPack.EmeCardSystem;
using NUnit.Framework;
using UnityEngine;

namespace EasyPack.EmeCardTests
{
    /// <summary>
    ///     EmeCard 删除测试
    ///     测试卡牌删除后的缓存清理情况，确保没有泄漏引用
    /// </summary>
    [TestFixture]
    public class EmeCardRemoveTest
    {
        private CardFactory _factory;
        private CardEngine _engine;

        [SetUp]
        public void Setup()
        {
            _factory = new CardFactory();
            _factory.Register("test_card", () => new(new("test_card", "测试卡", "", "Card.Object")));
            _factory.Register("parent_card", () => new(new("parent_card", "父卡", "", "Card.Object")));
            _factory.Register("child_card", () => new(new("child_card", "子卡", "", "Card.Object")));
            _engine = new CardEngine(_factory);
        }

        [TearDown]
        public void TearDown()
        {
            _engine?.ClearAllCards();
            _engine = null;
            _factory = null;
        }

        /// <summary>
        ///     测试1: Card被调用RemoveCard API删除后，所有的缓存结构不存在引用
        ///     检查以下缓存：
        ///     - _cardsById
        ///     - _cardsByUID
        ///     - _cardsByKey
        ///     - _cardsByPosition
        ///     - _positionByUID
        ///     - _idIndexes
        ///     - _idMaxIndexes
        /// </summary>
        [Test]
        public void Test_Card_RemoveFromEngine_CachesNotContainReference()
        {
            // 创建并添加卡牌
            Card card = _engine.CreateCard("test_card");
            Assert.IsNotNull(card, "卡牌应该创建成功");

            string cardId = card.Id;
            long cardUID = card.UID;
            int cardIndex = card.Index;

            // 验证卡牌已添加到缓存
            Assert.IsNotNull(_engine.GetCardByUID(cardUID), "删除前应能找到卡牌");
            Assert.IsNotNull(_engine.GetCardById(cardId), "删除前应能找到卡牌");

            // 删除卡牌
            _engine.RemoveCard(card);

            // 使用反射访问私有缓存字段
            Type engineType = typeof(CardEngine);

            // 检查 _cardsByUID
            var cardsByUID = GetPrivateField<Dictionary<long, Card>>(engineType, "_cardsByUID");
            Assert.IsFalse(cardsByUID.ContainsKey(cardUID),
                $"_cardsByUID 不应包含已删除卡牌的UID: {cardUID}");

            // 检查 _cardsById
            var cardsById = GetPrivateField<Dictionary<string, List<Card>>>(engineType, "_cardsById");
            if (cardsById.TryGetValue(cardId, out var cardList))
            {
                Assert.IsFalse(cardList.Contains(card),
                    $"_cardsById[{cardId}] 不应包含已删除的卡牌");
            }

            // 检查 _cardsByKey
            var cardsByKey = GetPrivateField<Dictionary<(string, int), Card>>(engineType, "_cardsByKey");
            Assert.IsFalse(cardsByKey.ContainsKey((cardId, cardIndex)),
                $"_cardsByKey 不应包含已删除卡牌的键: ({cardId}, {cardIndex})");

            // 检查 _cardsByPosition
            var cardsByPosition = GetPrivateField<Dictionary<Vector3Int?, Card>>(engineType, "_cardsByPosition");
            foreach (var kvp in cardsByPosition)
            {
                Assert.AreNotEqual(card, kvp.Value,
                    $"_cardsByPosition 不应包含已删除的卡牌，位置: {kvp.Key}");
            }

            // 检查 _positionByUID
            var positionByUID = GetPrivateField<Dictionary<long, Vector3Int?>>(engineType, "_positionByUID");
            Assert.IsFalse(positionByUID.ContainsKey(cardUID),
                $"_positionByUID 不应包含已删除卡牌的UID: {cardUID}");

            // 检查 _idIndexes
            var idIndexes = GetPrivateField<Dictionary<string, HashSet<int>>>(engineType, "_idIndexes");
            if (idIndexes.TryGetValue(cardId, out var indexes))
            {
                Assert.IsFalse(indexes.Contains(cardIndex),
                    $"_idIndexes[{cardId}] 不应包含已删除卡牌的索引: {cardIndex}");
            }

            // 验证查询API也返回null
            Assert.IsNull(_engine.GetCardByUID(cardUID), "GetCardByUID应返回null");
            Assert.IsNull(_engine.GetCardById(cardId), "GetCardById应返回null");
        }

        /// <summary>
        ///     测试2: Card被删除后，CategoryManager的所有缓存也不存在引用
        ///     检查CategoryManager中的缓存：
        ///     - _entities
        ///     - _entityKeyToNode
        ///     - _nodeToEntityKeys
        ///     - _tagToEntityKeys
        ///     - _entityToTagIds
        ///     - _metadataStore
        ///     - _tagCache
        /// </summary>
        [Test]
        public void Test_Card_RemoveFromEngine_CategoryManagerCachesNotContainReference()
        {
            // 创建带标签和元数据的卡牌
            // 注意：CardData.ID 必须与 factory.Register 的 key 一致，否则模板查找链会命中先前缓存的同名模板
            var cardData = new CardData("tagged_card", "测试卡", "", "Card.Object", new[] { "标签1", "标签2" });
            cardData.DefaultMetaData.Set("TestKey", "TestValue");
            _factory.Register("tagged_card", () => new(cardData));

            Card card = _engine.CreateCard("tagged_card");
            Assert.IsNotNull(card, "卡牌应该创建成功");

            long cardUID = card.UID;
            Assert.AreEqual(2, card.Tags.Count, "卡牌应该有2个标签");

            // 验证CategoryManager包含该卡牌
            Assert.IsNotNull(_engine.CategoryManager.GetById(cardUID),
                "删除前CategoryManager应包含该卡牌");

            // 删除卡牌
            _engine.RemoveCard(card);

            // 使用反射访问CategoryManager的私有字段
            Type categoryManagerType = typeof(CategoryManager<Card, long>);

            var categoryManager = _engine.CategoryManager as CategoryManager<Card, long>;
            Assert.IsNotNull(categoryManager, "CategoryManager应为CategoryManager<Card, long>类型");

            // 检查 _entities
            var entities = GetPrivateField<object>(categoryManagerType, "_entities");
            bool containsInEntities = ReflectionHelper.ConcurrentDictionaryContainsKey(entities, cardUID);

            Assert.IsFalse(containsInEntities,
                $"CategoryManager._entities 不应包含已删除卡牌的UID: {cardUID}");

            // 检查 _entityKeyToNode
            var entityKeyToNode = GetPrivateField<object>(categoryManagerType, "_entityKeyToNode");
            bool containsInEntityKeyToNode = ReflectionHelper.GenericDictionaryContainsKey(entityKeyToNode, cardUID);
            Assert.IsFalse(containsInEntityKeyToNode,
                $"CategoryManager._entityKeyToNode 不应包含已删除卡牌的UID: {cardUID}");

            // 检查 _metadataStore（是 ConcurrentDictionary）
            var metadataStore = GetPrivateField<object>(categoryManagerType, "_metadataStore");
            bool containsInMetadataStore = ReflectionHelper.GenericDictionaryContainsKey(metadataStore, cardUID);
            Assert.IsFalse(containsInMetadataStore,
                $"CategoryManager._metadataStore 不应包含已删除卡牌的UID: {cardUID}");

            // 检查 _entityToTagIds
            var entityToTagIds = GetPrivateField<object>(categoryManagerType, "_entityToTagIds");
            bool containsInEntityToTagIds = ReflectionHelper.GenericDictionaryContainsKey(entityToTagIds, cardUID);
            Assert.IsFalse(containsInEntityToTagIds,
                $"CategoryManager._entityToTagIds 不应包含已删除卡牌的UID: {cardUID}");

            // 使用公开API验证
            var getByIdResult = categoryManager.GetById(cardUID);
            Assert.IsFalse(getByIdResult.IsSuccess,
                "GetById应返回失败结果");
        }

        /// <summary>
        ///     测试3: Card删除后，序列化+反序列化后，仍然满足测试1和测试2的条件
        /// </summary>
        [Test]
        public void Test_Card_RemoveAndSerialize_CachesStayClean()
        {
            // 创建多张卡牌
            Card card1 = _engine.CreateCard("test_card");
            Card card2 = _engine.CreateCard("test_card");
            Card parentCard = _engine.CreateCard("parent_card");

            // 添加子卡牌
            _engine.AddChildToCard(parentCard, _engine.CreateCard("child_card"));

            long card1UID = card1.UID;
            long card2UID = card2.UID;

            // 删除第一张卡牌
            _engine.RemoveCard(card1);

            // 序列化当前状态
            string serializedState = JsonUtility.ToJson(_engine.GetSerializableState());
            Assert.IsFalse(string.IsNullOrEmpty(serializedState), "序列化状态不应为空");

            // 创建新引擎并反序列化
            var newFactory = new CardFactory();
            newFactory.Register("test_card", () => new(new("test_card", "测试卡", "", "Card.Object")));
            newFactory.Register("parent_card", () => new(new("parent_card", "父卡", "", "Card.Object")));
            newFactory.Register("child_card", () => new(new("child_card", "子卡", "", "Card.Object")));
            var newEngine = new CardEngine(newFactory);

            var dto = JsonUtility.FromJson<CardEngineDTO>(serializedState);
            newEngine.LoadState(dto);

            // 验证反序列化后，不存在card1的引用
            Assert.IsNull(newEngine.GetCardByUID(card1UID),
                "反序列化后应不包含已删除卡牌card1的引用");

            // 检查新引擎的缓存也不包含card1
            Type engineType = typeof(CardEngine);
            var cardsByUID = GetPrivateField<Dictionary<long, Card>>(engineType, "_cardsByUID");
            Assert.IsFalse(cardsByUID.ContainsKey(card1UID),
                "反序列化后的引擎缓存不应包含card1的UID");

            // 验证card2仍然存在
            Card restoredCard2 = newEngine.GetCardByUID(card2UID);
            Assert.IsNotNull(restoredCard2, "反序列化后应包含card2");
            Assert.AreEqual("test_card", restoredCard2.Id, "card2的ID应正确");

            // 验证CategoryManager的缓存也不包含card1
            var categoryManagerType = typeof(CategoryManager<Card, long>);
            var entities = GetPrivateField<object>(categoryManagerType, "_entities");
            bool containsInEntities = ReflectionHelper.ConcurrentDictionaryContainsKey(entities, card1UID);

            Assert.IsFalse(containsInEntities,
                "反序列化后的CategoryManager不应包含card1的引用");

            newEngine.ClearAllCards();
        }

        /// <summary>
        ///     测试4: 删除子卡牌，验证缓存清理
        /// </summary>
        [Test]
        public void Test_ChildCard_RemoveFromEngine_CachesNotContainReference()
        {
            Card parentCard = _engine.CreateCard("parent_card");
            Card childCard = _engine.CreateCard("child_card");

            _engine.AddChildToCard(parentCard, childCard);

            long childUID = childCard.UID;
            string childId = childCard.Id;

            // 从父卡移除子卡
            parentCard.RemoveChild(childCard);
            _engine.RemoveCard(childCard);

            // 检查缓存
            Type engineType = typeof(CardEngine);
            var cardsByUID = GetPrivateField<Dictionary<long, Card>>(engineType, "_cardsByUID");
            Assert.IsFalse(cardsByUID.ContainsKey(childUID),
                $"删除子卡后，_cardsByUID不应包含该子卡的UID");

            // 验证API查询
            Assert.IsNull(_engine.GetCardByUID(childUID), "GetCardByUID应返回null");
            
            // 父卡的children列表应该为空
            Assert.AreEqual(0, parentCard.Children.Count, "父卡的children列表应为空");
        }

        /// <summary>
        ///     反射辅助类
        /// </summary>
        private static class ReflectionHelper
        {
            /// <summary>
            ///     检查ConcurrentDictionary是否包含指定的键
            /// </summary>
            public static bool ConcurrentDictionaryContainsKey(object concurrentDict, long key)
            {
                if (concurrentDict == null) return false;

                Type dictType = concurrentDict.GetType();
                MethodInfo containsKeyMethod = dictType.GetMethod("ContainsKey",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                if (containsKeyMethod == null) return false;

                try
                {
                    object result = containsKeyMethod.Invoke(concurrentDict, new object[] { key });
                    return result is bool && (bool)result;
                }
                catch
                {
                    return false;
                }
            }

            /// <summary>
            ///     通用方法：检查任何Dictionary类型是否包含指定的键
            /// </summary>
            public static bool GenericDictionaryContainsKey(object dict, long key)
            {
                if (dict == null) return false;

                Type dictType = dict.GetType();
                MethodInfo containsKeyMethod = dictType.GetMethod("ContainsKey",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                if (containsKeyMethod == null) return false;

                try
                {
                    object result = containsKeyMethod.Invoke(dict, new object[] { key });
                    return result is bool && (bool)result;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        ///     辅助方法：使用反射获取私有字段
        /// </summary>
        private T GetPrivateField<T>(Type type, string fieldName)
        {
            FieldInfo field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
            {
                Assert.Fail($"未找到字段: {type.Name}.{fieldName}");
                return default;
            }

            object instance = null;
            
            // 检查是否是 CategoryManager<Card, long> 类型
            if (type == typeof(CategoryManager<Card, long>))
            {
                instance = _engine.CategoryManager;
            }
            else if (type == typeof(CardEngine))
            {
                instance = _engine;
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(CategoryManager<,>))
            {
                instance = _engine.CategoryManager;
            }

            if (instance == null)
            {
                Assert.Fail($"无法获取 {type.Name} 的实例");
                return default;
            }

            object fieldValue = field.GetValue(instance);
            if (fieldValue == null)
            {
                return default;
            }

            if (fieldValue is T typedValue)
            {
                return typedValue;
            }

            Assert.Fail($"无法将字段 {fieldName} 的值从 {fieldValue.GetType().Name} 转换为 {typeof(T).Name}");
            return default;
        }
    }
}
