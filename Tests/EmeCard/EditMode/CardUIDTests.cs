using System.Collections.Generic;
using System.Diagnostics;
using EasyPack.EmeCardSystem;
using NUnit.Framework;

namespace EasyPack.EmeCardTests
{
    /// <summary>
    ///     Card UID 系统测试。
    ///     验证 FR-001: UID 属性、分配、O(1) 查找。
    /// </summary>
    [TestFixture]
    public class CardUIDTests
    {
        private CardEngine _engine;
        private CardFactory _factory;

        [SetUp]
        public void Setup()
        {
            _factory = new();
            _engine = new(_factory);
        }

        [TearDown]
        public void TearDown()
        {
            _engine = null;
            _factory = null;
        }

        #region UID 属性测试

        [Test]
        public void Card_UID_DefaultValue_IsMinusOne()
        {
            // Arrange & Act
            var card = new Card(null);

            // Assert
            Assert.AreEqual(-1, card.UID, "新创建的卡牌 UID 应为 -1");
        }

        [Test]
        public void Card_UID_AfterAddToEngine_IsPositive()
        {
            // Arrange
            var card = new Card(null);

            // Act
            _engine.AddCard(card);

            // Assert
            Assert.Greater(card.UID, 0, "添加到引擎后 UID 应为正数");
        }

        [Test]
        public void Card_UID_MultipleCards_AreUnique()
        {
            // Arrange
            var card1 = new Card(null);
            var card2 = new Card(null);
            var card3 = new Card(null);

            // Act
            _engine.AddCard(card1);
            _engine.AddCard(card2);
            _engine.AddCard(card3);

            // Assert
            Assert.AreNotEqual(card1.UID, card2.UID, "卡牌 UID 应唯一");
            Assert.AreNotEqual(card2.UID, card3.UID, "卡牌 UID 应唯一");
            Assert.AreNotEqual(card1.UID, card3.UID, "卡牌 UID 应唯一");
        }

        [Test]
        public void Card_UID_SameIdCards_HaveUniqueUIDs()
        {
            // Arrange
            var data = new CardData("Monster", "Monster");

            var card1 = new Card(data);
            var card2 = new Card(data);
            var card3 = new Card(data);

            // Act
            _engine.AddCard(card1);
            _engine.AddCard(card2);
            _engine.AddCard(card3);

            // Assert
            Assert.AreEqual("Monster", card1.Id);
            Assert.AreEqual("Monster", card2.Id);
            Assert.AreEqual("Monster", card3.Id);

            var uids = new HashSet<long> { card1.UID, card2.UID, card3.UID };
            Assert.AreEqual(3, uids.Count, "同 ID 卡牌应有不同的 UID");
        }

        #endregion

        #region UID 查询测试

        [Test]
        public void GetCardByUID_ExistingCard_ReturnsCard()
        {
            // Arrange
            var card = new Card(null);
            _engine.AddCard(card);
            long uid = card.UID;

            // Act
            Card result = _engine.GetCardByUID(uid);

            // Assert
            Assert.AreSame(card, result, "通过 UID 应能获取到同一卡牌");
        }

        [Test]
        public void GetCardByUID_NonExistingUID_ReturnsNull()
        {
            // Arrange
            var card = new Card(null);
            _engine.AddCard(card);

            // Act
            Card result = _engine.GetCardByUID(99999);

            // Assert
            Assert.IsNull(result, "不存在的 UID 应返回 null");
        }

        [Test]
        public void GetCardByUID_AfterRemove_ReturnsNull()
        {
            // Arrange
            var card = new Card(null);
            _engine.AddCard(card);
            long uid = card.UID;
            _engine.RemoveCard(card);

            // Act
            Card result = _engine.GetCardByUID(uid);

            // Assert
            Assert.IsNull(result, "移除后通过 UID 应返回 null");
        }

        #endregion

        #region UID 与 ID/Index 兼容性测试

        [Test]
        public void Card_IdAndIndex_StillWorkAfterUID()
        {
            // Arrange
            var data = new CardData("TestCard", "TestCard");
            var card = new Card(data);

            // Act
            _engine.AddCard(card);

            // Assert
            Assert.AreEqual("TestCard", card.Id, "ID 应保持不变");
            Assert.GreaterOrEqual(card.Index, 0, "Index 应被分配");
            Assert.Greater(card.UID, 0, "UID 应被分配");
        }

        [Test]
        public void Card_AllThreeIdentifiers_AreIndependent()
        {
            // Arrange
            var data1 = new CardData("Card", "Card");
            var data2 = new CardData("Card", "Card");

            var card1 = new Card(data1);
            var card2 = new Card(data2);

            // Act
            _engine.AddCard(card1);
            _engine.AddCard(card2);

            // Assert
            Assert.AreEqual(card1.Id, card2.Id, "同 ID 卡牌 ID 相同");
            Assert.AreNotEqual(card1.Index, card2.Index, "同 ID 卡牌 Index 不同");
            Assert.AreNotEqual(card1.UID, card2.UID, "同 ID 卡牌 UID 不同");
        }

        #endregion
    }
}