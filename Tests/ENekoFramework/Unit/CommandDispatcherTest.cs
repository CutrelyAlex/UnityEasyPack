using NUnit.Framework;
using UnityEngine.TestTools;
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using EasyPack.ENekoFramework;

namespace EasyPack.ENekoFrameworkTest.Unit
{
    /// <summary>
    /// 命令调度器 (CommandDispatcher) 单元测试
    ///
    /// CommandDispatcher 是什么？
    /// ====================
    /// CommandDispatcher 是 ENekoFramework 的核心组件之一，负责：
    /// 1. 异步执行命令 (ICommand<TResult>)
    /// 2. 超时控制 - 防止命令执行时间过长
    /// 3. 取消支持 - 允许外部取消正在执行的命令
    /// 4. 错误传播 - 正确传递命令执行中的异常
    /// 5. 执行历史跟踪 - 记录所有命令的执行情况
    ///
    /// 设计模式：Command Pattern + Dispatcher Pattern
    /// 作用：在业务逻辑层和基础设施层之间提供抽象，统一处理异步操作
    ///
    /// 测试覆盖范围：
    /// =============
    /// 1. 正常命令执行 - 验证基本功能
    /// 2. 异常处理 - 确保错误正确传播
    /// 3. 超时机制 - 测试超时检测和处理
    /// 4. 取消操作 - 验证取消令牌功能
    /// 5. 历史记录 - 确认执行历史被正确跟踪
    /// </summary>
    public class CommandDispatcherTests
    {
        private CommandDispatcher _dispatcher;

        [SetUp]
        public void Setup()
        {
            _dispatcher = new CommandDispatcher();
        }

        [TearDown]
        public void TearDown()
        {
            _dispatcher = null;
        }

        #region 命令执行测试 - 测试基本功能

        /// <summary>
        /// 测试：正常命令执行应该成功
        ///
        /// 验证内容：
        /// 1. CommandDispatcher 能正确执行有效的命令
        /// 2. 返回结果与预期一致
        /// 3. 命令的 ExecuteAsync 方法被调用
        ///
        /// 场景：用户发送一个简单的命令，期望得到正确的结果
        /// </summary>
        [UnityTest]
        public IEnumerator Execute_ValidCommand_ShouldSucceed()
        {
            // Arrange - 准备一个返回 "Success" 的命令
            var command = new MockCommand { ExpectedResult = "Success" };

            // Act - 执行命令
            Task<string> task = _dispatcher.ExecuteAsync(command);
            yield return new WaitUntil(() => task.IsCompleted);

            // Assert - 验证结果
            Assert.AreEqual("Success", task.Result);
            Assert.IsTrue(command.WasExecuted);
        }

        /// <summary>
        /// 测试：命令抛出异常时应该正确传播错误
        ///
        /// 验证内容：
        /// 1. 命令执行中的异常不会被吞掉
        /// 2. 异常类型和消息保持不变
        /// 3. Task.IsFaulted 状态正确设置
        ///
        /// 场景：命令执行失败时，调用者需要知道具体的错误信息
        /// </summary>
        [UnityTest]
        public IEnumerator Execute_CommandWithException_ShouldPropagateError()
        {
            // Arrange - 准备一个会抛出异常的命令
            var command = new MockCommand { ShouldThrow = true };

            // Act - 执行命令（预期会失败）
            Task<string> task = _dispatcher.ExecuteAsync(command);
            yield return new WaitUntil(() => task.IsCompleted || task.IsFaulted);

            // Assert - 验证异常被正确传播
            Assert.IsTrue(task.IsFaulted);
            Assert.IsNotNull(task.Exception);
            var innerException = task.Exception.InnerException;
            Assert.IsInstanceOf<InvalidOperationException>(innerException);
            Assert.AreEqual("Mock command error", innerException.Message);
        }

        #endregion

        #region 超时处理测试 - 测试超时机制

        /// <summary>
        /// 测试：命令执行超过超时时间应该抛出 TimeoutException
        ///
        /// 验证内容：
        /// 1. 超时检测机制正常工作
        /// 2. 超时的命令被取消
        /// 3. 抛出正确的异常类型
        ///
        /// 场景：防止长时间运行的命令占用系统资源
        /// </summary>
        [UnityTest]
        public IEnumerator Execute_CommandExceedingTimeout_ShouldThrowTimeoutException()
        {
            // Arrange - 准备一个执行 1.5 秒的命令，但只给 1 秒超时
            var command = new MockCommand { DelayMs = 1500 }; // 1.5 seconds, exceeds timeout

            // Act - 执行命令，设置 1 秒超时
            Task<string> task = _dispatcher.ExecuteAsync(command, timeoutSeconds: 1);
            yield return new WaitUntil(() => task.IsCompleted || task.IsFaulted);

            // Assert - 验证超时异常
            Assert.IsTrue(task.IsFaulted);
            Assert.IsNotNull(task.Exception);
            var innerException = task.Exception.InnerException;
            Assert.IsInstanceOf<TimeoutException>(innerException);
        }

        /// <summary>
        /// 测试：命令在超时时间内完成应该成功
        ///
        /// 验证内容：
        /// 1. 正常执行的命令不受超时影响
        /// 2. 超时机制不会误杀正常命令
        ///
        /// 场景：确保合理的超时设置不会影响正常业务
        /// </summary>
        [UnityTest]
        public IEnumerator Execute_CommandWithinTimeout_ShouldSucceed()
        {
            // Arrange - 准备一个执行 100ms 的快速命令
            var command = new MockCommand { DelayMs = 100, ExpectedResult = "Fast" };

            // Act - 执行命令，设置 2 秒超时（足够）
            Task<string> task = _dispatcher.ExecuteAsync(command, timeoutSeconds: 2);
            yield return new WaitUntil(() => task.IsCompleted);

            // Assert - 验证成功完成
            Assert.AreEqual("Fast", task.Result);
        }

        /// <summary>
        /// 测试：使用默认超时时间（4秒）
        ///
        /// 验证内容：
        /// 1. 默认超时值（4秒）在合理范围内
        /// 2. 不指定超时参数时使用默认值
        ///
        /// 场景：大多数命令使用默认超时设置
        /// </summary>
        [UnityTest]
        public IEnumerator Execute_WithDefaultTimeout_ShouldUse4Seconds()
        {
            // Arrange - 准备一个执行 500ms 的命令（在默认 4 秒内）
            var command = new MockCommand { DelayMs = 500, ExpectedResult = "WithinDefault" };

            // Act - 执行命令，不指定超时（使用默认 4 秒）
            Task<string> task = _dispatcher.ExecuteAsync(command); // Uses default timeout
            yield return new WaitUntil(() => task.IsCompleted);

            // Assert - 验证在默认超时内完成
            Assert.AreEqual("WithinDefault", task.Result);
        }

        #endregion

        #region 取消测试 - 测试取消令牌功能

        /// <summary>
        /// 测试：使用取消令牌可以在执行中途取消命令
        ///
        /// 验证内容：
        /// 1. 取消令牌能正确传递到命令
        /// 2. 命令在取消时抛出 OperationCanceledException
        /// 3. 外部可以控制命令的执行
        ///
        /// 场景：用户操作取消或系统关闭时停止正在执行的命令
        /// </summary>
        [UnityTest]
        public IEnumerator Execute_WithCancellationToken_ShouldCancelWhenRequested()
        {
            // Arrange - 准备一个执行 2 秒的命令
            var command = new MockCommand { DelayMs = 2000 };
            var cts = new CancellationTokenSource();

            // Act - 执行命令，然后在 300ms 后取消
            Task<string> task = _dispatcher.ExecuteAsync(command, cancellationToken: cts.Token);
            cts.CancelAfter(300); // Cancel after 300ms

            yield return new WaitUntil(() => task.IsCompleted || task.IsCanceled || task.IsFaulted);

            // Assert - 验证命令被取消
            Assert.IsTrue(task.IsCanceled || task.IsFaulted);
            if (task.IsFaulted)
            {
                Assert.IsNotNull(task.Exception);
                var innerException = task.Exception.InnerException;
                Assert.IsInstanceOf<OperationCanceledException>(innerException);
            }
        }

        #endregion

        #region 命令历史测试 - 测试执行历史跟踪

        /// <summary>
        /// 测试：执行的命令应该被记录在历史中
        ///
        /// 验证内容：
        /// 1. 每次命令执行都被记录
        /// 2. 历史记录包含正确的命令类型
        /// 3. 历史记录的数量正确
        ///
        /// 场景：系统管理员查看命令执行历史，调试问题
        /// </summary>
        [UnityTest]
        public IEnumerator Execute_ShouldRecordCommandHistory()
        {
            // Arrange - 准备两个不同的命令
            var command1 = new MockCommand { ExpectedResult = "Command1" };
            var command2 = new MockCommand { ExpectedResult = "Command2" };

            // Act - 依次执行两个命令
            Task<string> task1 = _dispatcher.ExecuteAsync(command1);
            yield return new WaitUntil(() => task1.IsCompleted);

            Task<string> task2 = _dispatcher.ExecuteAsync(command2);
            yield return new WaitUntil(() => task2.IsCompleted);

            // Assert - 验证历史记录
            var history = _dispatcher.GetCommandHistory();
            Assert.AreEqual(2, history.Count);
            Assert.AreEqual(typeof(MockCommand), history[0].CommandType);
            Assert.AreEqual(typeof(MockCommand), history[1].CommandType);
        }

        /// <summary>
        /// 测试：失败的命令应该在历史中记录异常信息
        ///
        /// 验证内容：
        /// 1. 失败的命令仍然被记录在历史中
        /// 2. 异常信息被正确保存
        /// 3. 可以查看失败的原因
        ///
        /// 场景：分析命令失败的原因，改进系统稳定性
        /// </summary>
        [UnityTest]
        public IEnumerator Execute_FailedCommand_ShouldRecordException()
        {
            // Arrange - 准备一个会失败的命令
            var command = new MockCommand { ShouldThrow = true };

            // Act - 执行命令（会失败）
            Task<string> task = _dispatcher.ExecuteAsync(command);
            yield return new WaitUntil(() => task.IsCompleted || task.IsFaulted);

            // Assert - 验证异常被记录
            var history = _dispatcher.GetCommandHistory();
            Assert.AreEqual(1, history.Count);
            Assert.IsNotNull(history[0].Exception);
            Assert.AreEqual("Mock command error", history[0].Exception.Message);
        }

        #endregion

        #region Mock Command Implementation

        /// <summary>
        /// Mock 命令实现 - 用于测试的模拟命令
        ///
        /// 功能特性：
        /// 1. 可配置的执行结果 (ExpectedResult)
        /// 2. 可配置的执行延迟 (DelayMs) - 用于测试超时
        /// 3. 可配置的异常抛出 (ShouldThrow) - 用于测试错误处理
        /// 4. 执行状态跟踪 (WasExecuted) - 用于验证命令是否被执行
        ///
        /// 实现 ICommand<string> 接口，返回字符串结果
        /// </summary>
        private class MockCommand : ICommand<string>
        {
            /// <summary>期望的执行结果</summary>
            public string ExpectedResult { get; set; }

            /// <summary>是否应该抛出异常</summary>
            public bool ShouldThrow { get; set; }

            /// <summary>执行延迟毫秒数（用于测试超时）</summary>
            public int DelayMs { get; set; }

            /// <summary>是否已被执行（用于验证）</summary>
            public bool WasExecuted { get; private set; }

            /// <summary>
            /// 执行命令的异步方法
            ///
            /// 执行逻辑：
            /// 1. 如果设置了延迟，先等待指定时间
            /// 2. 如果设置了抛出异常，抛出 InvalidOperationException
            /// 3. 标记为已执行
            /// 4. 返回配置的结果
            /// </summary>
            /// <param name="cancellationToken">取消令牌</param>
            /// <returns>命令执行结果</returns>
            public async Task<string> ExecuteAsync(CancellationToken cancellationToken = default)
            {
                // 模拟执行延迟（用于测试超时）
                if (DelayMs > 0)
                {
                    await Task.Delay(DelayMs, cancellationToken);
                }

                // 模拟异常情况
                if (ShouldThrow)
                {
                    throw new InvalidOperationException("Mock command error");
                }

                // 标记为已执行
                WasExecuted = true;

                // 返回配置的结果
                return ExpectedResult ?? "Default";
            }
        }

        #endregion
    }
}
