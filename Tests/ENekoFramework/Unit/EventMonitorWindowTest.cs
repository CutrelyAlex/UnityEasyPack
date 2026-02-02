using NUnit.Framework;
using EasyPack.ENekoFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using TestScripts.ENekoFramework.Mocks;

namespace EasyPack.ENekoFrameworkTest.Unit
{
    /// <summary>
    /// 测试事件监控窗口功能
    /// </summary>
    [TestFixture]
    public class EventMonitorWindowTest
    {
        private TestArchitecture _architecture;
        private List<IEvent> _capturedEvents;

        [SetUp]
        public void Setup()
        {
            TestArchitecture.ResetInstance();
            _architecture = TestArchitecture.Instance;
            _capturedEvents = new List<IEvent>();
        }

        [TearDown]
        public void Teardown()
        {
            TestArchitecture.ResetInstance();
            _capturedEvents.Clear();
        }

        [Test]
        public void EventCapture_ShouldRecordPublishedEvents()
        {
            // Arrange
            var testEvent = new TestEvent { Message = "Test event" };

            // Setup event capture
            _architecture.SubscribeEvent<TestEvent>(e => _capturedEvents.Add(e));

            // Act
            _architecture.PublishEvent(testEvent);

            // Assert
            Assert.That(_capturedEvents.Count, Is.EqualTo(1));
            Assert.That(_capturedEvents[0], Is.TypeOf<TestEvent>());
            Assert.That(((TestEvent)_capturedEvents[0]).Message, Is.EqualTo("Test event"));
        }

        [Test]
        public void EventHistory_ShouldMaintainChronologicalOrder()
        {
            // Arrange
            var event1 = new TestEvent { Message = "First" };
            var event2 = new TestEvent { Message = "Second" };
            var event3 = new TestEvent { Message = "Third" };

            _architecture.SubscribeEvent<TestEvent>(e => _capturedEvents.Add(e));

            // Act
            _architecture.PublishEvent(event1);
            _architecture.PublishEvent(event2);
            _architecture.PublishEvent(event3);

            // Assert
            Assert.That(_capturedEvents.Count, Is.EqualTo(3));
            Assert.That(((TestEvent)_capturedEvents[0]).Message, Is.EqualTo("First"));
            Assert.That(((TestEvent)_capturedEvents[1]).Message, Is.EqualTo("Second"));
            Assert.That(((TestEvent)_capturedEvents[2]).Message, Is.EqualTo("Third"));
        }

        [Test]
        public void EventFilter_ShouldFilterByEventType()
        {
            // Arrange
            var testEvent = new TestEvent { Message = "Test" };
            var buffsEvent = new BuffsChangedEvent { BuffCount = 5 };

            _architecture.SubscribeEvent<TestEvent>(e => _capturedEvents.Add(e));
            _architecture.SubscribeEvent<BuffsChangedEvent>(e => _capturedEvents.Add(e));

            // Act
            _architecture.PublishEvent(testEvent);
            _architecture.PublishEvent(buffsEvent);

            // Assert
            Assert.That(_capturedEvents.Count, Is.EqualTo(2));

            var testEvents = _capturedEvents.Where(e => e is TestEvent).ToList();
            var buffEvents = _capturedEvents.Where(e => e is BuffsChangedEvent).ToList();

            Assert.That(testEvents.Count, Is.EqualTo(1));
            Assert.That(buffEvents.Count, Is.EqualTo(1));
        }

        [Test]
        public void EventTimestamp_ShouldBeRecorded()
        {
            // Arrange
            var testEvent = new TestEvent { Message = "Timestamped" };
            DateTime beforePublish = DateTime.UtcNow;

            _architecture.SubscribeEvent<TestEvent>(e => _capturedEvents.Add(e));

            // Act
            _architecture.PublishEvent(testEvent);
            DateTime afterPublish = DateTime.UtcNow;

            // Assert
            Assert.That(_capturedEvents.Count, Is.EqualTo(1));
            var capturedEvent = _capturedEvents[0];

            // Verify timestamp is within reasonable range
            Assert.That(capturedEvent.Timestamp, Is.GreaterThanOrEqualTo(beforePublish));
            Assert.That(capturedEvent.Timestamp, Is.LessThanOrEqualTo(afterPublish));
        }

        [Test]
        public void SubscriptionCount_ShouldTrackActiveSubscribers()
        {
            // Arrange
            int callCount1 = 0;
            int callCount2 = 0;

            Action<TestEvent> handler1 = e => callCount1++;
            Action<TestEvent> handler2 = e => callCount2++;

            // Act
            _architecture.SubscribeEvent(handler1);
            _architecture.SubscribeEvent(handler2);

            _architecture.PublishEvent(new TestEvent { Message = "Test" });

            // Assert
            Assert.That(callCount1, Is.EqualTo(1));
            Assert.That(callCount2, Is.EqualTo(1));
        }

        [Test]
        public void EventMonitor_ShouldHandleHighFrequencyEvents()
        {
            // Arrange
            _architecture.SubscribeEvent<TestEvent>(e => _capturedEvents.Add(e));

            // Act - Publish 100 events rapidly
            for (int i = 0; i < 100; i++)
            {
                _architecture.PublishEvent(new TestEvent { Message = $"Event {i}" });
            }

            // Assert
            Assert.That(_capturedEvents.Count, Is.EqualTo(100));

            // Verify all events were captured in order
            for (int i = 0; i < 100; i++)
            {
                Assert.That(((TestEvent)_capturedEvents[i]).Message, Is.EqualTo($"Event {i}"));
            }
        }

        [Test]
        public void EventDetails_ShouldIncludeEventData()
        {
            // Arrange
            var complexEvent = new BuffsChangedEvent
            {
                BuffCount = 42,
                RemovedBuffs = new List<string> { "Buff1", "Buff2" }
            };

            _architecture.SubscribeEvent<BuffsChangedEvent>(e => _capturedEvents.Add(e));

            // Act
            _architecture.PublishEvent(complexEvent);

            // Assert
            Assert.That(_capturedEvents.Count, Is.EqualTo(1));
            var captured = (BuffsChangedEvent)_capturedEvents[0];
            Assert.That(captured.BuffCount, Is.EqualTo(42));
            Assert.That(captured.RemovedBuffs.Count, Is.EqualTo(2));
            Assert.That(captured.RemovedBuffs, Contains.Item("Buff1"));
            Assert.That(captured.RemovedBuffs, Contains.Item("Buff2"));
        }
    }
}
