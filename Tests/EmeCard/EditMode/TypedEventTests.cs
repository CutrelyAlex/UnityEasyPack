using System;
using EasyPack.EmeCardSystem;
using NUnit.Framework;

namespace EasyPack.EmeCardTests
{
    /// <summary>
    ///     类型化事件系统测试。
    ///     验证 FR-004: ICardEvent<T>、类型安全、CardEvent<TData>。
    /// </summary>
    [TestFixture]
    public class TypedEventTests
    {
        #region CardEvent<TData> 构造测试

        [Test]
        public void CardEvent_Constructor_SetsAllProperties()
        {
            // Arrange & Act
            var evt = new CardEvent<int>("Damage", 10);

            // Assert
            Assert.AreEqual("Damage", evt.EventType);
            Assert.AreEqual("Damage", evt.EventId); // 默认使用 EventType
            Assert.AreEqual(10, evt.Data);
            Assert.AreEqual(10, evt.DataObject);
        }

        [Test]
        public void CardEvent_WithCustomId_SetsEventId()
        {
            // Arrange & Act
            var evt = new CardEvent<int>("Damage", 10, "CustomDamage");

            // Assert
            Assert.AreEqual("Damage", evt.EventType);
            Assert.AreEqual("CustomDamage", evt.EventId);
            Assert.AreEqual(10, evt.Data);
        }

        [Test]
        public void CardEvent_WithReferenceType_WorksCorrectly()
        {
            // Arrange
            var data = new TestEventData { Value = 42, Name = "Test" };

            // Act
            var evt = new CardEvent<TestEventData>("TestEvent", data);

            // Assert
            Assert.AreSame(data, evt.Data);
            Assert.AreEqual(42, evt.Data.Value);
            Assert.AreEqual("Test", evt.Data.Name);
        }

        [Test]
        public void CardEvent_NullEventType_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new CardEvent<int>(null, 10));
        }

        #endregion

        #region CardEventDefinition 测试

        [Test]
        public void CardEventDefinition_Create_ReturnsTypedEvent()
        {
            // Arrange
            var typeDef = new CardEventDefinition<float>("Tick");

            // Act
            var evt = typeDef.CreateEvent(0.016f);

            // Assert
            Assert.AreEqual("Tick", evt.EventType);
            Assert.AreEqual("Tick", evt.EventId);
            Assert.AreEqual(0.016f, evt.Data, 0.0001f);
        }

        [Test]
        public void CardEventDefinition_Create_WithCustomId_UsesCustomId()
        {
            // Arrange
            var typeDef = new CardEventDefinition<string>("Message");

            // Act
            var evt = typeDef.CreateEvent("Hello", "CustomMessage");

            // Assert
            Assert.AreEqual("Message", evt.EventType);
            Assert.AreEqual("CustomMessage", evt.EventId);
            Assert.AreEqual("Hello", evt.Data);
        }

        [Test]
        public void CardEventDefinition_Matches_ReturnsTrue_WhenTypeMatches()
        {
            // Arrange
            var typeDef = new CardEventDefinition<float>("Tick");
            var evt = typeDef.CreateEvent(0.016f);

            // Act & Assert
            Assert.IsTrue(typeDef.Matches(evt));
        }

        [Test]
        public void CardEventDefinition_Matches_ReturnsFalse_WhenTypeDiffers()
        {
            // Arrange
            var tickDef = new CardEventDefinition<float>("Tick");
            var useDef = new CardEventDefinition<Card>("Use");
            var tickEvt = tickDef.CreateEvent(0.016f);

            // Act & Assert
            Assert.IsFalse(useDef.Matches(tickEvt));
        }

        [Test]
        public void CardEventDefinition_TryGetData_ExtractsTypedData()
        {
            // Arrange
            var typeDef = new CardEventDefinition<float>("Tick");
            ICardEvent evt = typeDef.CreateEvent(0.033f);

            // Act
            bool success = typeDef.TryGetData(evt, out float data);

            // Assert
            Assert.IsTrue(success);
            Assert.AreEqual(0.033f, data, 0.0001f);
        }

        [Test]
        public void CardEventDefinition_TryGetData_ReturnsFalse_WhenTypeMismatch()
        {
            // Arrange
            var floatDef = new CardEventDefinition<float>("Tick");
            var intDef = new CardEventDefinition<int>("Tick"); // 同名但类型不同
            ICardEvent floatEvt = floatDef.CreateEvent(0.033f);

            // Act
            bool success = intDef.TryGetData(floatEvt, out int data);

            // Assert
            Assert.IsFalse(success);
            Assert.AreEqual(0, data);
        }

        #endregion

        #region CardEventTypes 注册表测试

        [Test]
        public void CardEventTypes_Tick_CreatesFloatEvent()
        {
            // Act
            var evt = CardEventTypes.Tick.CreateEvent(0.033f);

            // Assert
            Assert.AreEqual(CardEventTypes.TICK, evt.EventType);
            Assert.AreEqual("Tick", evt.EventId);
            Assert.AreEqual(0.033f, evt.Data, 0.0001f);
        }

        [Test]
        public void CardEventTypes_AddedToOwner_CreatesCardEvent()
        {
            // Arrange
            var owner = new Card(null);

            // Act
            var evt = CardEventTypes.AddedToOwner.CreateEvent(owner);

            // Assert
            Assert.AreEqual(CardEventTypes.ADDED_TO_OWNER, evt.EventType);
            Assert.AreSame(owner, evt.Data);
        }

        [Test]
        public void CardEventTypes_Use_CreatesCardEvent()
        {
            // Arrange
            var target = new Card(null);

            // Act
            var evt = CardEventTypes.Use.CreateEvent(target);

            // Assert
            Assert.AreEqual(CardEventTypes.USE, evt.EventType);
            Assert.AreEqual("Use", evt.EventId);
            Assert.AreSame(target, evt.Data);
        }

        [Test]
        public void CardEventTypes_Define_CreatesCustomEventType()
        {
            // Arrange
            var customType = CardEventTypes.Define<DamageEventData>("OnDamage");
            var data = new DamageEventData { Amount = 25, Source = null };

            // Act
            var evt = customType.CreateEvent(data);

            // Assert
            Assert.AreEqual("OnDamage", evt.EventType);
            Assert.AreEqual("OnDamage", evt.EventId);
            Assert.AreEqual(25, evt.Data.Amount);
        }

        [Test]
        public void CardEventTypes_Create_DirectlyCreatesEvent()
        {
            // Arrange & Act
            var evt = CardEventTypes.Create("Custom", 42);

            // Assert
            Assert.AreEqual("Custom", evt.EventType);
            Assert.AreEqual(42, evt.Data);
        }

        #endregion

        #region 事件类型匹配测试

        [Test]
        public void CardEventTypes_IsTick_ReturnsTrue_ForTickEvent()
        {
            // Arrange
            var evt = CardEventTypes.Tick.CreateEvent(0.016f);

            // Act & Assert
            Assert.IsTrue(CardEventTypes.IsTick(evt));
        }

        [Test]
        public void CardEventTypes_IsUse_ReturnsFalse_ForTickEvent()
        {
            // Arrange
            var evt = CardEventTypes.Tick.CreateEvent(0.016f);

            // Act & Assert
            Assert.IsFalse(CardEventTypes.IsUse(evt));
        }

        #endregion

        #region 类型安全测试

        [Test]
        public void ICardEvent_Covariance_WorksCorrectly()
        {
            // Arrange
            var evt = new CardEvent<string>("Message", "Hello");

            // Act - 协变性允许赋值给父类型
            ICardEvent<object> objEvent = evt;

            // Assert
            Assert.AreEqual("Hello", objEvent.Data);
        }

        [Test]
        public void CardEvent_PatternMatching_WorksCorrectly()
        {
            // Arrange
            ICardEvent evt = new CardEvent<int>("Damage", 50);

            // Act & Assert
            if (evt is ICardEvent<int> intEvent)
                Assert.AreEqual(50, intEvent.Data);
            else
                Assert.Fail("Pattern matching should work for typed events");
        }

        [Test]
        public void CardEvent_ImplicitConversion_ToObjectEvent()
        {
            // Arrange
            var intEvent = new CardEvent<int>("Value", 100);

            // Act
            CardEvent<object> objEvent = intEvent;

            // Assert
            Assert.AreEqual("Value", objEvent.EventType);
            Assert.AreEqual(100, objEvent.Data);
        }

        #endregion

        #region 自定义事件场景测试

        [Test]
        public void CustomEvent_CollisionScenario_WorksCorrectly()
        {
            // Arrange - 定义碰撞事件类型
            var collisionDef = CardEventTypes.Define<CollisionData>("Collision");
            var collisionData = new CollisionData { Target = new(null), Force = 10.5f };

            // Act - 创建碰撞事件
            var evt = collisionDef.CreateEvent(collisionData);

            // Assert
            Assert.AreEqual("Collision", evt.EventType);
            Assert.IsNotNull(evt.Data.Target);
            Assert.AreEqual(10.5f, evt.Data.Force, 0.001f);
        }

        [Test]
        public void CustomEvent_DamageScenario_WorksCorrectly()
        {
            // Arrange - 定义伤害事件类型
            var damageDef = CardEventTypes.Define<DamageEventData>("Damage");
            var card = new Card(null);
            var damageData = new DamageEventData { Amount = 25, Source = card };

            // Act
            ICardEvent evt = damageDef.CreateEvent(damageData);

            // Assert - 通过模式匹配获取数据
            if (evt is ICardEvent<DamageEventData> damageEvt)
            {
                Assert.AreEqual(25, damageEvt.Data.Amount);
                Assert.AreSame(card, damageEvt.Data.Source);
            }
            else
                Assert.Fail("Should be able to cast to ICardEvent<DamageEventData>");
        }

        [Test]
        public void CustomEvent_TryGetData_ExtractsCorrectData()
        {
            // Arrange
            var damageDef = CardEventTypes.Define<DamageEventData>("Damage");
            ICardEvent evt = damageDef.CreateEvent(new() { Amount = 50 });

            // Act
            bool success = damageDef.TryGetData(evt, out DamageEventData data);

            // Assert
            Assert.IsTrue(success);
            Assert.AreEqual(50, data.Amount);
        }

        #endregion

        #region 测试辅助类

        private class TestEventData
        {
            public int Value { get; set; }
            public string Name { get; set; }
        }

        private class DamageEventData
        {
            public int Amount { get; set; }
            public Card Source { get; set; }
        }

        private class CollisionData
        {
            public Card Target { get; set; }
            public float Force { get; set; }
        }

        #endregion
    }
}