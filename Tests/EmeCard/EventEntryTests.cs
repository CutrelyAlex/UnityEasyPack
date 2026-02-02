using System;
using EasyPack.EmeCardSystem;
using NUnit.Framework;

namespace EasyPack.EmeCardTests
{
    /// <summary>
    ///     IEventEntry 和 EventEntry 实现测试。
    ///     验证 FR-006: IEventEntry 抽象、多种事件源类型。
    /// </summary>
    [TestFixture]
    public class EventEntryTests
    {
        #region CardEventEntry 测试

        [Test]
        public void CardEventEntry_Constructor_SetsAllProperties()
        {
            // Arrange
            var source = new Card(null);
            var evt = new CardEvent<string>(CardEventTypes.USE, "TestData", "TestEvent");
            var effectRoot = new Card(null);

            // Act
            var entry = new CardEventEntry(source, evt, effectRoot, 5);

            // Assert
            Assert.AreEqual(EventSourceType.Card, entry.SourceType);
            Assert.AreSame(source, entry.SourceCard);
            Assert.IsNull(entry.SourceRuleUID);
            Assert.AreSame(effectRoot, entry.EffectRoot);
            Assert.AreEqual(5, entry.Priority);
            Assert.AreEqual(CardEventTypes.USE, entry.Event.EventType);
            Assert.AreEqual("TestEvent", entry.Event.EventId);
        }

        [Test]
        public void CardEventEntry_Constructor_NullSource_ThrowsException()
        {
            // Arrange
            var evt = CardEventTypes.Use.CreateEvent(null);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new CardEventEntry(null, evt));
        }

        [Test]
        public void CardEventEntry_EffectRoot_DefaultsToSource()
        {
            // Arrange
            var source = new Card(null);
            var evt = CardEventTypes.Use.CreateEvent(null);

            // Act
            var entry = new CardEventEntry(source, evt);

            // Assert
            Assert.AreSame(source, entry.EffectRoot, "EffectRoot 默认应为 Source");
        }

        #endregion

        #region RuleEventEntry 测试

        [Test]
        public void RuleEventEntry_Constructor_SetsAllProperties()
        {
            // Arrange
            var evt = new CardEvent<string>("RuleTriggered", "RuleData");
            var sourceCard = new Card(null);
            var effectRoot = new Card(null);

            // Act
            var entry = new RuleEventEntry(1001, evt,
                sourceCard, effectRoot, 10);

            // Assert
            Assert.AreEqual(EventSourceType.Rule, entry.SourceType);
            Assert.AreEqual(1001, entry.SourceRuleUID);
            Assert.AreSame(sourceCard, entry.SourceCard);
            Assert.AreSame(effectRoot, entry.EffectRoot);
            Assert.AreEqual(10, entry.Priority);
        }

        [Test]
        public void RuleEventEntry_SourceCard_IsOptional()
        {
            // Arrange
            var evt = new CardEvent<object>("RuleEvent", null);

            // Act
            var entry = new RuleEventEntry(2001, evt);

            // Assert
            Assert.IsNull(entry.SourceCard, "RuleEventEntry 的 SourceCard 可为 null");
        }

        #endregion

        #region SystemEventEntry 测试

        [Test]
        public void SystemEventEntry_Constructor_SetsProperties()
        {
            // Arrange
            var evt = CardEventTypes.Tick.CreateEvent(0.016f);

            // Act
            var entry = new SystemEventEntry(evt, 100);

            // Assert
            Assert.AreEqual(EventSourceType.System, entry.SourceType);
            Assert.IsNull(entry.SourceCard);
            Assert.IsNull(entry.SourceRuleUID);
            Assert.IsNull(entry.EffectRoot);
            Assert.AreEqual(100, entry.Priority);
            Assert.AreEqual(CardEventTypes.TICK, entry.Event.EventType);
        }

        [Test]
        public void SystemEventEntry_TickEvent_CarriesDeltaTime()
        {
            // Arrange
            var evt = CardEventTypes.Tick.CreateEvent(0.033f);

            // Act
            var entry = new SystemEventEntry(evt);

            // Assert
            Assert.AreEqual(0.033f, entry.Event.DataObject);
        }

        #endregion

        #region ExternalEventEntry 测试

        [Test]
        public void ExternalEventEntry_Constructor_SetsProperties()
        {
            // Arrange
            var evt = new CardEvent<string>("ExternalCommand", "CommandData");
            var sourceCard = new Card(null);

            // Act
            var entry = new ExternalEventEntry(evt, sourceCard, priority: 50);

            // Assert
            Assert.AreEqual(EventSourceType.External, entry.SourceType);
            Assert.AreSame(sourceCard, entry.SourceCard);
            Assert.IsNull(entry.SourceRuleUID);
            Assert.AreEqual(50, entry.Priority);
        }

        [Test]
        public void ExternalEventEntry_AllFieldsOptional()
        {
            // Arrange
            var evt = new CardEvent<object>("Broadcast", null);

            // Act
            var entry = new ExternalEventEntry(evt);

            // Assert
            Assert.IsNull(entry.SourceCard);
            Assert.IsNull(entry.EffectRoot);
            Assert.AreEqual(0, entry.Priority);
        }

        #endregion

        #region IEventEntry 多态测试

        [Test]
        public void IEventEntry_Polymorphism_WorksCorrectly()
        {
            // Arrange
            var source = new Card(null);
            var evt = CardEventTypes.Use.CreateEvent(null);

            IEventEntry cardEntry = new CardEventEntry(source, evt);
            IEventEntry ruleEntry = new RuleEventEntry(1001, evt);
            IEventEntry sysEntry = new SystemEventEntry(evt);
            IEventEntry extEntry = new ExternalEventEntry(evt);

            // Act & Assert
            Assert.AreEqual(EventSourceType.Card, cardEntry.SourceType);
            Assert.AreEqual(EventSourceType.Rule, ruleEntry.SourceType);
            Assert.AreEqual(EventSourceType.System, sysEntry.SourceType);
            Assert.AreEqual(EventSourceType.External, extEntry.SourceType);
        }

        [Test]
        public void IEventEntry_ToString_ReturnsDescriptiveString()
        {
            // Arrange
            var source = new Card(null);
            var evt = new CardEvent<string>(CardEventTypes.USE, "data", "TestEvent");
            var entry = new CardEventEntry(source, evt);

            // Act
            string str = entry.ToString();

            // Assert
            Assert.IsTrue(str.Contains("CardEventEntry"), "ToString 应包含类型名");
            Assert.IsTrue(str.Contains("TestEvent"), "ToString 应包含事件 ID");
        }

        #endregion
    }
}