using System;
using EasyPack.EmeCardSystem;
using EasyPack.GamePropertySystem;
using EasyPack.Serialization;
using NUnit.Framework;
using UnityEngine;

namespace EasyPack.EmeCardTests
{
    /// <summary>
    ///     CardJsonSerializer 新接口实现单元测试
    ///     测试目标：
    ///     =========
    ///     验证 CardJsonSerializer 实现的 ITypeSerializer
    ///     <Card, SerializableCard>
    ///         接口的所有方法：
    ///         1. ToSerializable(Card) - 对象到 DTO 的转换
    ///         2. FromSerializable(SerializableCard) - DTO 到对象的转换
    ///         3. ToJson(SerializableCard) - DTO 到 JSON 字符串
    ///         4. FromJson(string) - JSON 字符串到 DTO
    ///         5. SerializeToJson(Card) - 直接序列化（语法糖）
    ///         6. DeserializeFromJson(string) - 直接反序列化（语法糖）
    ///         测试覆盖范围：
    ///         ============
    ///         • 基本转换功能
    ///         • 数据完整性验证
    ///         • 往返转换一致性
    ///         • 边界条件处理
    ///         • 错误处理机制
    /// </summary>
    [TestFixture]
    public class CardJsonSerializerTest
    {
        private CardJsonSerializer _serializer;
        private CardData _testCardData;
        private Card _testCard;
        private CardEngine _engine;
        private CardFactory _factory;

        [SetUp]
        public void Setup()
        {
            // 初始化序列化器
            _serializer = new();

            // 创建测试卡牌数据
            _testCardData = new(
                "test_warrior",
                "测试战士",
                "这是一个测试战士卡牌",
                "CardCategory.Object",
                new[] { "Combat", "Melee" }
            );

            // 使用 Engine 创建卡牌以确保标签正确管理
            _factory = new CardFactory();
            _factory.Register("test_warrior", () => 
            {
                var card = new Card(_testCardData, "Player");
                card.Properties.Add(new GamePropertySystem.GameProperty("Strength", 100f));
                card.Properties.Add(new GamePropertySystem.GameProperty("Health", 50f));
                return card;
            });
            _engine = new CardEngine(_factory);
            _testCard = _engine.CreateCard("test_warrior");
        }

        [TearDown]
        public void TearDown()
        {
            _serializer = null;
            _testCard = null;
            _testCardData = null;
            _engine = null;
            _factory = null;
        }

        #region ToSerializable 测试

        /// <summary>
        ///     测试：ToSerializable 应该将 Card 转换为 SerializableCard DTO
        /// </summary>
        [Test]
        public void Test_ToSerializable_ConvertsCardToDTO()
        {
            // Act
            SerializableCard dto = _serializer.ToSerializable(_testCard);

            // Assert
            Assert.IsNotNull(dto, "DTO 不应为 null");
            Assert.IsInstanceOf<SerializableCard>(dto, "应返回 SerializableCard 类型");
            Assert.AreEqual("test_warrior", dto.ID, "ID 应匹配");
            Assert.AreEqual("测试战士", dto.Name, "名称应匹配");
            Assert.AreEqual("这是一个测试战士卡牌", dto.Description, "描述应匹配");
            Assert.AreEqual("CardCategory.Object", dto.DefaultCategory, "分类应匹配");
        }

        /// <summary>
        ///     测试：ToSerializable 应该转换所有属性
        /// </summary>
        [Test]
        public void Test_ToSerializable_ConvertsAllProperties()
        {
            // Act
            SerializableCard dto = _serializer.ToSerializable(_testCard);

            // Assert
            Assert.IsNotNull(dto.Properties, "属性列表不应为 null");
            Assert.AreEqual(2, dto.Properties.Length, "应该有 2 个属性");

            // 验证属性存在
            SerializableGameProperty strengthProp = Array.Find(dto.Properties, p => p.ID == "Strength");
            SerializableGameProperty healthProp = Array.Find(dto.Properties, p => p.ID == "Health");

            Assert.IsNotNull(strengthProp, "应该包含 Strength 属性");
            Assert.IsNotNull(healthProp, "应该包含 Health 属性");
        }

        /// <summary>
        ///     测试：ToSerializable 应该仅序列化 CardData.DefaultTags（运行时 Tags 由 CategoryManager 管理，不在 SerializableCard 中保存）
        /// </summary>
        [Test]
        public void Test_ToSerializable_SerializesDefaultTagsOnly()
        {
            // Act
            SerializableCard dto = _serializer.ToSerializable(_testCard);

            // Assert
            Assert.IsNotNull(dto.DefaultTags, "DefaultTags 不应为 null");
            Assert.GreaterOrEqual(dto.DefaultTags.Length, 0, "DefaultTags 数组应可用");
            Assert.Contains("Combat", dto.DefaultTags, "应该包含 Combat 默认标签");
            Assert.Contains("Melee", dto.DefaultTags, "应该包含 Melee 默认标签");

            // 运行时额外标签（如 Player）来自 CategoryManager，不应被写入 SerializableCard
            Assert.IsFalse(Array.Exists(dto.DefaultTags, t => t == "Player"), "DefaultTags 不应包含运行时标签 Player");
        }

        /// <summary>
        ///     测试：ToSerializable 处理 null 应返回 null
        /// </summary>
        [Test]
        public void Test_ToSerializable_HandlesNull()
        {
            // Act
            SerializableCard dto = _serializer.ToSerializable(null);

            // Assert
            Assert.IsNull(dto, "null 输入应返回 null");
        }

        #endregion

        #region FromSerializable 测试

        /// <summary>
        ///     测试：FromSerializable 应该将 DTO 转换回 Card 对象
        /// </summary>
        [Test]
        public void Test_FromSerializable_ConvertsDTOToCard()
        {
            // Arrange
            SerializableCard dto = _serializer.ToSerializable(_testCard);

            // Act
            Card card = _serializer.FromSerializable(dto);

            // Assert
            Assert.IsNotNull(card, "Card 不应为 null");
            Assert.IsInstanceOf<Card>(card, "应返回 Card 类型");
            Assert.AreEqual(_testCard.Id, card.Id, "ID 应匹配");
            Assert.AreEqual(_testCard.Name, card.Name, "名称应匹配");
            Assert.AreEqual(_testCard.Description, card.Description, "描述应匹配");
        }

        /// <summary>
        ///     测试：FromSerializable 应该恢复所有属性
        /// </summary>
        [Test]
        public void Test_FromSerializable_RestoresAllProperties()
        {
            // Arrange
            SerializableCard dto = _serializer.ToSerializable(_testCard);

            // Act
            Card card = _serializer.FromSerializable(dto);

            // Assert
            Assert.AreEqual(2, card.Properties.Count, "应该恢复 2 个属性");
            Assert.IsNotNull(card.GetProperty("Strength"), "应该恢复 Strength 属性");
            Assert.IsNotNull(card.GetProperty("Health"), "应该恢复 Health 属性");
        }

        /// <summary>
        ///     测试：FromSerializable 不应恢复运行时标签（由 CategoryManager 管理），但在注册到 Engine 后应应用 CardData.DefaultTags
        /// </summary>
        [Test]
        public void Test_FromSerializable_DoesNotRestoreRuntimeTags_ButAppliesDefaultTagsAfterEngineRegistration()
        {
            // Arrange
            SerializableCard dto = _serializer.ToSerializable(_testCard);

            // Act
            Card card = _serializer.FromSerializable(dto);

            // 将卡牌注册到 Engine，使 CategoryManager 应用 CardData.DefaultTags
            _engine.AddCard(card);

            // Assert
            // DefaultTags 应在注册时被应用
            Assert.IsTrue(card.HasTag("Combat"), "应该应用 Combat 默认标签");
            Assert.IsTrue(card.HasTag("Melee"), "应该应用 Melee 默认标签");

            // 运行时额外标签（Player）不会随 CardJsonSerializer 序列化/反序列化保存
            Assert.IsFalse(card.HasTag("Player"), "不应恢复运行时标签 Player（应由 CategoryManager 状态恢复）");
        }

        #endregion

        #region ToJson 测试

        /// <summary>
        ///     测试：ToJson 应该将 DTO 序列化为 JSON 字符串
        /// </summary>
        [Test]
        public void Test_ToJson_SerializesDTOToJsonString()
        {
            // Arrange
            SerializableCard dto = _serializer.ToSerializable(_testCard);

            // Act
            string json = _serializer.ToJson(dto);

            // Assert
            Assert.IsNotNull(json, "JSON 不应为 null");
            Assert.IsNotEmpty(json, "JSON 不应为空字符串");
            Assert.IsTrue(json.Contains("test_warrior"), "JSON 应包含卡牌 ID");
            Assert.IsTrue(json.Contains("测试战士"), "JSON 应包含卡牌名称");
        }

        /// <summary>
        ///     测试：ToJson 处理 null 应返回 null
        /// </summary>
        [Test]
        public void Test_ToJson_HandlesNull()
        {
            // Act
            string json = _serializer.ToJson(null);

            // Assert
            Assert.IsNull(json, "null 输入应返回 null");
        }

        #endregion

        #region FromJson 测试

        /// <summary>
        ///     测试：FromJson 应该从 JSON 字符串反序列化为 DTO
        /// </summary>
        [Test]
        public void Test_FromJson_DeserializesJsonToDTO()
        {
            // Arrange
            SerializableCard dto = _serializer.ToSerializable(_testCard);
            string json = _serializer.ToJson(dto);

            // Act
            SerializableCard deserializedDto = _serializer.FromJson(json);

            // Assert
            Assert.IsNotNull(deserializedDto, "DTO 不应为 null");
            Assert.IsInstanceOf<SerializableCard>(deserializedDto, "应返回 SerializableCard 类型");
            Assert.AreEqual("test_warrior", deserializedDto.ID, "ID 应匹配");
            Assert.AreEqual("测试战士", deserializedDto.Name, "名称应匹配");
        }

        /// <summary>
        ///     测试：FromJson 处理空字符串应返回 null
        /// </summary>
        [Test]
        public void Test_FromJson_HandlesEmptyString()
        {
            // Act
            SerializableCard dto = _serializer.FromJson(string.Empty);

            // Assert
            Assert.IsNull(dto, "空字符串应返回 null");
        }

        /// <summary>
        ///     测试：FromJson 处理无效 JSON 应抛出异常
        /// </summary>
        [Test]
        public void Test_FromJson_ThrowsOnInvalidJson()
        {
            // Act & Assert
            Assert.Throws<SerializationException>(() => { _serializer.FromJson("{ invalid json"); },
                "无效 JSON 应抛出 SerializationException");
        }

        #endregion

        #region SerializeToJson 测试（语法糖）

        /// <summary>
        ///     测试：SerializeToJson 应该直接将 Card 序列化为 JSON
        /// </summary>
        [Test]
        public void Test_SerializeToJson_DirectlySerializesCard()
        {
            // Act
            string json = _serializer.SerializeToJson(_testCard);

            // Assert
            Assert.IsNotNull(json, "JSON 不应为 null");
            Assert.IsNotEmpty(json, "JSON 不应为空字符串");
            Assert.IsTrue(json.Contains("test_warrior"), "JSON 应包含卡牌 ID");
            Assert.IsTrue(json.Contains("测试战士"), "JSON 应包含卡牌名称");
        }

        #endregion

        #region DeserializeFromJson 测试（语法糖）

        /// <summary>
        ///     测试：DeserializeFromJson 应该直接从 JSON 反序列化为 Card
        /// </summary>
        [Test]
        public void Test_DeserializeFromJson_DirectlyDeserializesCard()
        {
            // Arrange
            string json = _serializer.SerializeToJson(_testCard);

            // Act
            Card card = _serializer.DeserializeFromJson(json);

            // Assert
            Assert.IsNotNull(card, "Card 不应为 null");
            Assert.IsInstanceOf<Card>(card, "应返回 Card 类型");
            Assert.AreEqual(_testCard.Id, card.Id, "ID 应匹配");
            Assert.AreEqual(_testCard.Name, card.Name, "名称应匹配");
        }

        #endregion

        #region 往返转换测试

        /// <summary>
        ///     测试：完整往返转换应保持数据一致性
        /// </summary>
        [Test]
        public void Test_RoundTrip_PreservesDataIntegrity()
        {
            // Act: Card -> DTO -> JSON -> DTO -> Card
            SerializableCard dto1 = _serializer.ToSerializable(_testCard);
            string json = _serializer.ToJson(dto1);
            SerializableCard dto2 = _serializer.FromJson(json);
            Card card = _serializer.FromSerializable(dto2);
            // 将卡牌注册到 Engine 以恢复完整功能
            _engine.AddCard(card);

            // Assert
            Assert.AreEqual(_testCard.Id, card.Id, "ID 应保持一致");
            Assert.AreEqual(_testCard.Name, card.Name, "名称应保持一致");
            Assert.AreEqual(_testCard.Description, card.Description, "描述应保持一致");
            Assert.AreEqual(_testCard.Data?.Category, card.Data?.Category, "分类应保持一致");
            Assert.AreEqual(_testCard.Properties.Count, card.Properties.Count, "属性数量应保持一致");
        }

        /// <summary>
        ///     测试：使用语法糖的往返转换应保持数据一致性
        /// </summary>
        [Test]
        public void Test_RoundTrip_WithSyntaxSugar_PreservesDataIntegrity()
        {
            // Act: Card -> JSON -> Card (使用语法糖方法)
            string json = _serializer.SerializeToJson(_testCard);
            Card card = _serializer.DeserializeFromJson(json);
            // 将卡牌注册到 Engine 以恢复完整功能
            _engine.AddCard(card);

            // Assert
            Assert.AreEqual(_testCard.Id, card.Id, "ID 应保持一致");
            Assert.AreEqual(_testCard.Name, card.Name, "名称应保持一致");
            Assert.AreEqual(_testCard.Description, card.Description, "描述应保持一致");
            Assert.AreEqual(_testCard.Data?.Category, card.Data?.Category, "分类应保持一致");
            Assert.AreEqual(_testCard.Properties.Count, card.Properties.Count, "属性数量应保持一致");
        }


        [Test]
        public void Test_Mutipule_SameIdDifferentCards_IndexPreserved()
        {
            // Arrange
            var card1 = new Card(new("card", "卡牌一"));
            var card2 = new Card(new("card", "卡牌二"));
            var card3 = new Card(new("card", "卡牌三"));
            _engine.AddCard(card1);
            _engine.AddCard(card2);
            _engine.AddCard(card3);
            // Act
            _engine.RemoveCard(card1);
            _engine.RemoveCard(card2);
            _engine.RemoveCard(card3);
            string json1 = _serializer.SerializeToJson(card1);
            string json2 = _serializer.SerializeToJson(card2);
            string json3 = _serializer.SerializeToJson(card3);
            var restored1 = _serializer.DeserializeFromJson(json1);
            var restored2 = _serializer.DeserializeFromJson(json2);
            var restored3 = _serializer.DeserializeFromJson(json3);
            _engine.AddCard(restored1);
            _engine.AddCard(restored2);
            _engine.AddCard(restored3);
            var card4 = new Card(new("card", "卡牌四"));
            _engine.AddCard(card4);
            // Assert
            Assert.AreEqual(0, restored1.Index, "第一个卡牌索引应为 0");
            Assert.AreEqual(1, restored2.Index, "第二个卡牌索引应为 1");
            Assert.AreEqual(2, restored3.Index, "第三个卡牌索引应为 2");
            Assert.AreEqual(3, card4.Index, "第四个卡牌索引应为 3");
        }

        #endregion

        #region 子卡层级测试

        /// <summary>
        ///     测试：往返转换应保持子卡层级结构
        /// </summary>
        [Test]
        public void Test_RoundTrip_PreservesChildHierarchy()
        {
            // Arrange: 创建带子卡的卡牌
            var weapon = new Card(new("weapon_sword", "剑", "一把锋利的剑"));
            var shield = new Card(new("weapon_shield", "盾", "一面坚固的盾"));
            
            // 先将子卡添加到 Engine 以分配 UID
            _engine.AddCard(weapon);
            _engine.AddCard(shield);
            
            // 然后建立父子关系
            _testCard.AddChild(weapon);
            _testCard.AddChild(shield, true);

            // Act: 使用 Engine 序列化整个状态（包括子卡关系）
            string json = _engine.SerializeToJson();
            
            // 创建新的 Engine 并加载状态
            var newFactory = new CardFactory();
            newFactory.Register("test_warrior", () => 
            {
                var card = new Card(_testCardData, "Player");
                card.Properties.Add(new GamePropertySystem.GameProperty("Strength", 100f));
                return card;
            });
            newFactory.Register("weapon_sword", () => new Card(new("weapon_sword", "剑", "一把锋利的剑")));
            newFactory.Register("weapon_shield", () => new Card(new("weapon_shield", "盾", "一面坚固的盾")));
            
            CardJsonSerializer.Factory = newFactory;
            var newEngine = new CardEngine(newFactory);
            newEngine.DeserializeFromJson(json);

            // 通过 UID 获取恢复后的卡牌
            Card restoredCard = newEngine.GetCardByUID(_testCard.UID);

            // Assert: 验证子卡已恢复
            Assert.IsNotNull(restoredCard, "应该能找到恢复的卡牌");
            Assert.AreEqual(2, restoredCard.Children.Count, "应该恢复 2 个子卡");
            Assert.AreEqual("weapon_sword", restoredCard.Children[0].Id, "第一个子卡 ID 应匹配");
            Assert.AreEqual("weapon_shield", restoredCard.Children[1].Id, "第二个子卡 ID 应匹配");
            Assert.IsFalse(restoredCard.IsIntrinsic(restoredCard.Children[0]), "第一个子卡不应是固有的");
            Assert.IsTrue(restoredCard.IsIntrinsic(restoredCard.Children[1]), "第二个子卡应是固有的");
        }

        #endregion
    }
}