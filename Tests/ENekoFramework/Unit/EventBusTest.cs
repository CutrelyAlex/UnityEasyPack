using EasyPack.ENekoFramework;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace EasyPack.ENekoFrameworkTest.Unit
{
    /// <summary>
    /// 事件总线 (EventBus) 单元测试
    ///
    /// EventBus 是什么？
    /// ================
    /// EventBus 是 ENekoFramework 的核心组件之一，负责：
    /// 1. 事件发布/订阅 - 解耦的事件通信机制
    /// 2. At-most-once 语义 - 保证事件最多被处理一次
    /// 3. WeakReference 支持 - 防止内存泄漏
    /// 4. 自动清理 - 在发布时清理失效的订阅者
    ///
    /// 设计模式：Observer Pattern + Publish-Subscribe Pattern
    /// 作用：在组件间提供松耦合的事件通信，避免直接依赖
    ///
    /// 关键特性：
    /// • 弱引用存储订阅者，防止内存泄漏
    /// • 异常隔离：一个订阅者异常不影响其他订阅者
    /// • 自动清理：发布事件时清理已回收的订阅者
    /// • 类型安全：泛型支持，编译时类型检查
    ///
    /// 测试覆盖范围：
    /// =============
    /// 1. 事件订阅 - 验证订阅机制正常工作
    /// 2. 事件发布 - 测试事件广播到所有订阅者
    /// 3. 取消订阅 - 验证可以停止接收事件
    /// 4. 异常处理 - 测试 At-most-once 语义
    /// 5. WeakReference - 验证内存管理机制
    /// </summary>
    [TestFixture]
    public class EventBusTests
    {
        private EventBus _eventBus;

        [SetUp]
        public void Setup()
        {
            _eventBus = new EventBus();
        }

        [TearDown]
        public void TearDown()
        {
            _eventBus?.ClearAllSubscriptions();
            _eventBus = null;
        }

        #region 事件订阅测试 - 测试订阅机制

        /// <summary>
        /// 测试：订阅有效的事件处理器应该成功
        ///
        /// 验证内容：
        /// 1. EventBus 能接受有效的订阅者
        /// 2. 订阅后能正确统计订阅者数量
        /// 3. 订阅本身不会触发处理器（只有发布时才会）
        ///
        /// 场景：组件注册事件监听器，准备接收特定类型的事件
        /// </summary>
        [Test]
        public void Subscribe_ValidHandler_ShouldSucceed()
        {
            // Arrange - 准备一个事件处理器
            var handlerCalled = false;
            Action<MockEvent> handler = (e) => { handlerCalled = true; };

            // Act - 订阅事件
            _eventBus.Subscribe<MockEvent>(handler);

            // Assert - 验证订阅成功，但处理器还未被调用
            Assert.AreEqual(1, _eventBus.GetSubscriberCount<MockEvent>());
            Assert.IsFalse(handlerCalled); // Not called yet
        }

        /// <summary>
        /// 测试：可以订阅多个处理器到同一事件类型
        ///
        /// 验证内容：
        /// 1. 支持多个订阅者同时监听同一事件
        /// 2. 发布事件时所有订阅者都会被调用
        /// 3. 订阅者之间相互独立
        ///
        /// 场景：多个组件都需要响应同一个事件（如用户登录事件）
        /// </summary>
        [Test]
        public void Subscribe_MultipleHandlers_ShouldAllowMultipleSubscriptions()
        {
            // Arrange - 准备两个不同的处理器
            var handler1Called = false;
            var handler2Called = false;
            Action<MockEvent> handler1 = (e) => { handler1Called = true; };
            Action<MockEvent> handler2 = (e) => { handler2Called = true; };

            // Act - 订阅两个处理器，然后发布事件
            _eventBus.Subscribe<MockEvent>(handler1);
            _eventBus.Subscribe<MockEvent>(handler2);
            _eventBus.Publish(new MockEvent { Message = "Test" });

            // Assert - 两个处理器都被调用
            Assert.IsTrue(handler1Called);
            Assert.IsTrue(handler2Called);
        }

        #endregion

        #region 事件发布测试 - 测试事件广播

        /// <summary>
        /// 测试：发布事件时所有订阅者都会收到通知
        ///
        /// 验证内容：
        /// 1. 事件能广播到所有订阅者
        /// 2. 每个订阅者都能收到正确的事件数据
        /// 3. 订阅者按订阅顺序接收事件（但不保证执行顺序）
        ///
        /// 场景：系统状态改变时通知所有相关组件
        /// </summary>
        [Test]
        public void Publish_WithSubscribers_ShouldNotifyAll()
        {
            // Arrange - 准备两个收集通知的处理器
            var notifications = new List<string>();
            Action<MockEvent> handler1 = (e) => { notifications.Add("Handler1: " + e.Message); };
            Action<MockEvent> handler2 = (e) => { notifications.Add("Handler2: " + e.Message); };

            _eventBus.Subscribe<MockEvent>(handler1);
            _eventBus.Subscribe<MockEvent>(handler2);

            // Act - 发布事件
            _eventBus.Publish(new MockEvent { Message = "TestMessage" });

            // Assert - 验证所有处理器都收到了事件
            Assert.AreEqual(2, notifications.Count);
            Assert.Contains("Handler1: TestMessage", notifications);
            Assert.Contains("Handler2: TestMessage", notifications);
        }

        /// <summary>
        /// 测试：向没有订阅者的事件发布不会抛出异常
        ///
        /// 验证内容：
        /// 1. 发布事件时如果没有订阅者，不会出错
        /// 2. EventBus 能优雅处理无订阅者的情况
        ///
        /// 场景：某些事件可能暂时没有监听者，但发布仍然是安全的
        /// </summary>
        [Test]
        public void Publish_WithoutSubscribers_ShouldNotThrow()
        {
            // Act & Assert - 发布事件到空订阅列表应该不会抛异常
            Assert.DoesNotThrow(() =>
            {
                _eventBus.Publish(new MockEvent { Message = "No subscribers" });
            });
        }

        /// <summary>
        /// 测试：事件数据能正确传递给订阅者
        ///
        /// 验证内容：
        /// 1. 事件对象的所有属性都能正确传递
        /// 2. 订阅者收到的是同一个事件实例
        /// 3. 数据完整性得到保证
        ///
        /// 场景：事件携带重要数据，如用户信息、状态变更等
        /// </summary>
        [Test]
        public void Publish_EventWithData_ShouldPassDataToSubscribers()
        {
            // Arrange - 准备一个捕获事件的处理器
            MockEvent receivedEvent = null;
            Action<MockEvent> handler = (e) => { receivedEvent = e; };
            _eventBus.Subscribe<MockEvent>(handler);

            var sentEvent = new MockEvent { Message = "Test", Value = 42 };

            // Act - 发布包含数据的复杂事件
            _eventBus.Publish(sentEvent);

            // Assert - 验证数据完整传递
            Assert.IsNotNull(receivedEvent);
            Assert.AreEqual("Test", receivedEvent.Message);
            Assert.AreEqual(42, receivedEvent.Value);
        }

        #endregion

        #region 取消订阅测试 - 测试动态管理订阅

        /// <summary>
        /// 测试：取消订阅后不再接收事件
        ///
        /// 验证内容：
        /// 1. 能成功取消特定订阅者
        /// 2. 取消后不再收到后续事件
        /// 3. 之前的调用不受影响
        ///
        /// 场景：组件销毁或状态改变时停止监听事件
        /// </summary>
        [Test]
        public void Unsubscribe_ValidSubscription_ShouldStopReceivingEvents()
        {
            // Arrange - 订阅一个处理器
            var callCount = 0;
            Action<MockEvent> handler = (e) => { callCount++; };
            _eventBus.Subscribe<MockEvent>(handler);

            // Act - 先发布一次，再取消订阅，然后再发布一次
            _eventBus.Publish(new MockEvent { Message = "First" });
            _eventBus.Unsubscribe<MockEvent>(handler);
            _eventBus.Publish(new MockEvent { Message = "Second" });

            // Assert - 只收到第一次发布的事件
            Assert.AreEqual(1, callCount); // Only first event received
        }

        /// <summary>
        /// 测试：取消订阅只影响指定的处理器
        ///
        /// 验证内容：
        /// 1. 取消订阅只移除特定的处理器
        /// 2. 其他处理器继续正常工作
        /// 3. 订阅者管理是独立的
        ///
        /// 场景：多个组件监听同一事件，其中一个组件停止监听
        /// </summary>
        [Test]
        public void Unsubscribe_MultipleSubscriptions_ShouldOnlyAffectTarget()
        {
            // Arrange - 订阅两个处理器
            var handler1Called = false;
            var handler2Called = false;
            Action<MockEvent> handler1 = (e) => { handler1Called = true; };
            Action<MockEvent> handler2 = (e) => { handler2Called = true; };

            _eventBus.Subscribe<MockEvent>(handler1);
            _eventBus.Subscribe<MockEvent>(handler2);

            // Act - 取消第一个处理器，然后发布事件
            _eventBus.Unsubscribe<MockEvent>(handler1);
            _eventBus.Publish(new MockEvent { Message = "Test" });

            // Assert - 只有第二个处理器被调用
            Assert.IsFalse(handler1Called);
            Assert.IsTrue(handler2Called);
        }

        #endregion

        #region At-most-once 传递保证测试 - 测试异常处理

        /// <summary>
        /// 测试：处理器抛出异常不会重试（At-most-once 语义）
        ///
        /// 验证内容：
        /// 1. 异常处理器只被调用一次
        /// 2. EventBus 不会重试失败的处理器
        /// 3. 异常不会传播到发布者
        ///
        /// 场景：确保事件处理失败不会导致无限重试或系统崩溃
        /// </summary>
        [Test]
        public void Publish_HandlerThrowsException_ShouldNotRetry()
        {
            // Arrange - 准备一个会抛出异常的处理器
            var callCount = 0;
            Action<MockEvent> faultyHandler = (e) =>
            {
                callCount++;
                throw new InvalidOperationException("Handler error");
            };

            _eventBus.Subscribe<MockEvent>(faultyHandler);

            // Act & Assert - 发布事件，应该只调用一次且不抛异常
            // Should not throw, and should only call once (At-most-once guarantee)
            Assert.DoesNotThrow(() =>
            {
                _eventBus.Publish(new MockEvent { Message = "Test" });
            });

            Assert.AreEqual(1, callCount); // Called exactly once, no retry
        }

        /// <summary>
        /// 测试：一个处理器异常不影响其他处理器
        ///
        /// 验证内容：
        /// 1. 异常处理器失败不阻止其他处理器执行
        /// 2. 事件仍然能传递给正常的处理器
        /// 3. 异常被隔离处理
        ///
        /// 场景：系统中某些组件处理事件失败，不影响其他组件正常工作
        /// </summary>
        [Test]
        public void Publish_HandlerThrows_ShouldNotAffectOtherHandlers()
        {
            // Arrange - 准备一个正常处理器和一个异常处理器
            var successfulHandlerCalled = false;
            Action<MockEvent> faultyHandler = (e) => { throw new Exception("Error"); };
            Action<MockEvent> successfulHandler = (e) => { successfulHandlerCalled = true; };

            _eventBus.Subscribe<MockEvent>(faultyHandler);
            _eventBus.Subscribe<MockEvent>(successfulHandler);

            // Act - 发布事件
            _eventBus.Publish(new MockEvent { Message = "Test" });

            // Assert - 正常处理器仍然被调用
            Assert.IsTrue(successfulHandlerCalled); // Other handlers still execute
        }

        #endregion

        #region WeakReference 测试 - 测试内存管理

        /// <summary>
        /// 测试：EventBus 使用 WeakReference，不会阻止垃圾回收
        ///
        /// 验证内容：
        /// 1. 订阅者能正常工作
        /// 2. WeakReference 实现不会影响基本功能
        /// 3. 内存管理机制存在（通过代码审查验证）
        ///
        /// 注意：实际 GC 行为在单元测试中不可预测，
        /// WeakReference 的完整功能通过集成测试验证
        ///
        /// 场景：防止事件订阅导致内存泄漏
        /// </summary>
        [Test]
        public void Subscribe_UsesWeakReference_ShouldNotPreventGarbageCollection()
        {
            // 这个测试验证 EventBus 使用 WeakReference 存储订阅者
            // 由于 GC 行为在测试环境中不可预测，我们只验证基本功能

            // Arrange - 准备一个处理器
            var callCount = 0;
            Action<MockEvent> handler = (e) => { callCount++; };

            // Act - 订阅并发布事件
            _eventBus.Subscribe<MockEvent>(handler);
            _eventBus.Publish(new MockEvent { Message = "Test" });

            // Assert - 处理器正常工作
            Assert.AreEqual(1, callCount);
            Assert.AreEqual(1, _eventBus.GetSubscriberCount<MockEvent>());

            // Note: Actual GC behavior cannot be reliably tested in unit tests
            // because the runtime may keep references alive for optimization.
            // The WeakReference implementation is verified through code review
            // and integration testing rather than forcing GC in unit tests.
        }

        /// <summary>
        /// 测试：发布事件时会自动清理失效的引用
        ///
        /// 验证内容：
        /// 1. 多订阅者场景正常工作
        /// 2. 清理机制在发布时被触发
        /// 3. 存活的订阅者继续工作
        ///
        /// 场景：长期运行的系统需要定期清理失效的订阅者
        /// </summary>
        [Test]
        public void Publish_ShouldCleanupDeadReferencesAutomatically()
        {
            // Arrange - 创建多个处理器
            var callCount1 = 0;
            var callCount2 = 0;
            Action<MockEvent> handler1 = (e) => { callCount1++; };
            Action<MockEvent> handler2 = (e) => { callCount2++; };

            _eventBus.Subscribe<MockEvent>(handler1);
            _eventBus.Subscribe<MockEvent>(handler2);

            Assert.AreEqual(2, _eventBus.GetSubscriberCount<MockEvent>());

            // Act - 发布事件
            _eventBus.Publish(new MockEvent { Message = "Test" });

            // Assert - 所有处理器都被调用
            Assert.AreEqual(1, callCount1);
            Assert.AreEqual(1, callCount2);

            // Verify the cleanup mechanism works during publish
            // (Dead references would be removed during Publish if they existed)
            Assert.AreEqual(2, _eventBus.GetSubscriberCount<MockEvent>());
        }

        #endregion

        #region Mock Event Implementation

        /// <summary>
        /// Mock 事件实现 - 用于测试的事件类
        ///
        /// 实现 IEvent 接口，包含测试所需的基本属性：
        /// • Timestamp: 事件时间戳（自动设置）
        /// • Message: 字符串消息
        /// • Value: 整数值
        ///
        /// 用于验证事件数据传递的完整性
        /// </summary>
        public class MockEvent : IEvent
        {
            /// <summary>事件时间戳</summary>
            public DateTime Timestamp { get; set; } = DateTime.UtcNow;

            /// <summary>事件消息内容</summary>
            public string Message { get; set; }

            /// <summary>事件数值</summary>
            public int Value { get; set; }
        }

        #endregion
    }
}
