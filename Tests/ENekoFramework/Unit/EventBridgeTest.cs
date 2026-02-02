using NUnit.Framework;
using EasyPack.ENekoFramework;

namespace EasyPack.ENekoFrameworkTest.Unit
{
    /// <summary>
    /// 测试 EventBridge 的跨架构事件转发功能
    /// </summary>
    [TestFixture]
    public class EventBridgeTests
    {
        private EventBridge _bridge;

        [SetUp]
        public void Setup()
        {
            _bridge = new EventBridge();
        }

        [TearDown]
        public void TearDown()
        {
            _bridge?.Dispose();
        }

        [Test]
        public void RegisterAndForward_ShouldTriggerListener()
        {
            // Arrange
            string receivedEvent = null;
            object receivedData = null;

            _bridge.Register<TestEventData>("TestEvent", (eventName, data) =>
            {
                receivedEvent = eventName;
                receivedData = data;
            });

            var testData = new TestEventData { Value = 42 };

            // Act
            _bridge.Forward("TestEvent", testData);

            // Assert
            Assert.AreEqual("TestEvent", receivedEvent);
            Assert.IsNotNull(receivedData);
            Assert.AreEqual(42, ((TestEventData)receivedData).Value);
        }

        [Test]
        public void Unregister_ShouldStopReceivingEvents()
        {
            // Arrange
            int callCount = 0;

            _bridge.Register<TestEventData>("TestEvent", (eventName, data) =>
            {
                callCount++;
            });

            var testData = new TestEventData { Value = 42 };

            // Act
            _bridge.Forward("TestEvent", testData);
            _bridge.Unregister("TestEvent");
            _bridge.Forward("TestEvent", testData);

            // Assert
            Assert.AreEqual(1, callCount, "Should only receive one event before unregister");
        }

        [Test]
        public void MultipleListeners_ShouldAllReceiveEvent()
        {
            // Arrange
            int listener1Calls = 0;
            int listener2Calls = 0;

            _bridge.Register<TestEventData>("TestEvent", (e, d) => listener1Calls++);
            _bridge.Register<TestEventData>("TestEvent", (e, d) => listener2Calls++);

            // Act
            _bridge.Forward("TestEvent", new TestEventData { Value = 42 });

            // Assert
            Assert.AreEqual(1, listener1Calls);
            Assert.AreEqual(1, listener2Calls);
        }

        [Test]
        public void Forward_WithNoListeners_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                _bridge.Forward("NonExistentEvent", new TestEventData { Value = 42 });
            });
        }

        [Test]
        public void Forward_WithNullData_ShouldWork()
        {
            // Arrange
            object receivedData = new object(); // 非null初始值

            _bridge.Register<TestEventData>("TestEvent", (e, d) =>
            {
                receivedData = d;
            });

            // Act
            _bridge.Forward<TestEventData>("TestEvent", null);

            // Assert
            Assert.IsNull(receivedData);
        }

        [Test]
        public void Dispose_ShouldClearAllListeners()
        {
            // Arrange
            int callCount = 0;

            _bridge.Register<TestEventData>("TestEvent", (e, d) => callCount++);

            // Act
            _bridge.Dispose();
            _bridge.Forward("TestEvent", new TestEventData { Value = 42 });

            // Assert
            Assert.AreEqual(0, callCount, "Should not receive events after dispose");
        }

        [Test]
        public void TypeMismatch_ShouldLogWarning()
        {
            // Arrange
            _bridge.Register<string>("TestEvent", (e, d) => { });

            // Act & Assert - 不应抛出异常，但会记录警告
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Warning,
                new System.Text.RegularExpressions.Regex(".*类型不匹配.*"));

            _bridge.Forward("TestEvent", new TestEventData { Value = 42 });
        }

        private class TestEventData
        {
            public int Value { get; set; }
        }
    }
}
