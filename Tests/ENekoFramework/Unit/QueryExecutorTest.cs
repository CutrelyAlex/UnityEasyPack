using EasyPack.ENekoFramework;
using NUnit.Framework;
using System;

namespace EasyPack.ENekoFrameworkTest.Unit
{
    /// <summary>
    /// 查询执行器 (QueryExecutor) 单元测试
    ///
    /// QueryExecutor 是什么？
    /// ====================
    /// QueryExecutor 是 ENekoFramework 的核心组件之一，负责：
    /// 1. 同步查询执行 - 执行只读查询并返回结果
    /// 2. 异常传播 - 查询中的异常会直接抛出给调用者
    /// 3. 查询历史记录 - 跟踪查询执行的统计信息
    /// 4. 类型安全 - 泛型支持，编译时类型检查
    ///
    /// 设计模式：Command Query Separation (CQS) - Query 部分
    /// 作用：处理读取操作，与 Command（写操作）分离
    ///
    /// 关键特性：
    /// • 同步执行：查询立即返回结果
    /// • 异常透明：查询异常直接传播
    /// • 历史记录：记录执行时间、状态等
    /// • 线程安全：可以在任何线程执行
    ///
    /// 测试覆盖范围：
    /// ============
    /// 1. 查询执行 - 验证基本查询功能
    /// 2. 异常处理 - 测试错误传播机制
    /// 3. 多种查询类型 - 测试不同返回类型的查询
    /// 4. 查询历史 - 验证执行统计和诊断信息
    /// </summary>
    [TestFixture]
    public class QueryExecutorTest
    {
        private QueryExecutor _executor;

        [SetUp]
        public void Setup()
        {
            _executor = new QueryExecutor();
        }

        [TearDown]
        public void TearDown()
        {
            _executor = null;
        }

        #region 查询执行测试 - 测试基本查询功能

        /// <summary>
        /// 测试：执行有效的查询应该返回正确结果
        ///
        /// 验证内容：
        /// 1. QueryExecutor 能正确执行查询
        /// 2. 查询结果能正确返回
        /// 3. 查询的 Execute 方法被调用
        ///
        /// 场景：业务逻辑需要读取数据，如获取用户信息、计算结果等
        /// </summary>
        [Test]
        public void Execute_ValidQuery_ShouldReturnResult()
        {
            // Arrange - 准备一个返回固定结果的查询
            var query = new MockQuery { ExpectedResult = 42 };

            // Act - 执行查询
            var result = _executor.Execute(query);

            // Assert - 验证结果正确且查询被执行
            Assert.AreEqual(42, result);
            Assert.IsTrue(query.WasExecuted);
        }

        /// <summary>
        /// 测试：查询抛出异常时应该传播给调用者
        ///
        /// 验证内容：
        /// 1. 查询中的异常不会被吞掉
        /// 2. 异常类型和消息保持不变
        /// 3. 调用者能正确处理查询错误
        ///
        /// 场景：查询数据不存在、权限不足等业务异常需要抛出
        /// </summary>
        [Test]
        public void Execute_QueryWithException_ShouldPropagateError()
        {
            // Arrange - 准备一个会抛出异常的查询
            var query = new MockQuery { ShouldThrow = true };

            // Act & Assert - 执行查询，应该抛出预期的异常
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                _executor.Execute(query);
            });

            Assert.AreEqual("Mock query error", ex.Message);
        }

        /// <summary>
        /// 测试：连续执行多个查询应该返回各自的正确结果
        ///
        /// 验证内容：
        /// 1. 每个查询独立执行
        /// 2. 结果不互相干扰
        /// 3. QueryExecutor 能处理连续请求
        ///
        /// 场景：业务流程中需要执行一系列读取操作
        /// </summary>
        [Test]
        public void Execute_MultipleQueries_ShouldReturnCorrectResults()
        {
            // Arrange - 准备三个不同的查询
            var query1 = new MockQuery { ExpectedResult = 10 };
            var query2 = new MockQuery { ExpectedResult = 20 };
            var query3 = new MockQuery { ExpectedResult = 30 };

            // Act - 依次执行所有查询
            var result1 = _executor.Execute(query1);
            var result2 = _executor.Execute(query2);
            var result3 = _executor.Execute(query3);

            // Assert - 验证每个结果都正确
            Assert.AreEqual(10, result1);
            Assert.AreEqual(20, result2);
            Assert.AreEqual(30, result3);
        }

        #endregion

        #region 查询类型测试 - 测试不同返回类型

        /// <summary>
        /// 测试：字符串类型查询应该正确返回字符串结果
        ///
        /// 验证内容：
        /// 1. 泛型查询支持不同返回类型
        /// 2. 字符串查询能正确处理
        /// 3. 类型安全得到保证
        ///
        /// 场景：查询用户名、配置字符串等文本数据
        /// </summary>
        [Test]
        public void Execute_StringQuery_ShouldReturnString()
        {
            // Arrange - 准备一个字符串查询
            var query = new MockStringQuery { ExpectedResult = "Test String" };

            // Act - 执行查询
            var result = _executor.Execute(query);

            // Assert - 验证字符串结果正确
            Assert.AreEqual("Test String", result);
        }

        /// <summary>
        /// 测试：复杂对象查询应该正确返回对象实例
        ///
        /// 验证内容：
        /// 1. 支持复杂对象作为查询结果
        /// 2. 对象属性正确传递
        /// 3. 引用完整性保持
        ///
        /// 场景：查询用户对象、配置对象等复杂数据结构
        /// </summary>
        [Test]
        public void Execute_ComplexObjectQuery_ShouldReturnObject()
        {
            // Arrange - 准备一个返回复杂对象的查询
            var expectedData = new MockData { Id = 123, Name = "Test" };
            var query = new MockDataQuery { ExpectedResult = expectedData };

            // Act - 执行查询
            var result = _executor.Execute(query);

            // Assert - 验证对象及其属性正确
            Assert.IsNotNull(result);
            Assert.AreEqual(123, result.Id);
            Assert.AreEqual("Test", result.Name);
        }

        #endregion

        #region 查询诊断测试 - 测试历史记录和监控

        /// <summary>
        /// 测试：查询执行后应该记录到历史中
        ///
        /// 验证内容：
        /// 1. 每次查询执行都被记录
        /// 2. 历史记录包含查询类型信息
        /// 3. 历史记录按执行顺序保存
        ///
        /// 场景：系统监控、性能分析、调试查询执行
        /// </summary>
        [Test]
        public void Execute_ShouldRecordQueryHistory()
        {
            // Arrange - 准备两个不同的查询
            var query1 = new MockQuery { ExpectedResult = 1 };
            var query2 = new MockQuery { ExpectedResult = 2 };

            // Act - 执行两个查询
            _executor.Execute(query1);
            _executor.Execute(query2);

            // Assert - 验证历史记录正确
            var history = _executor.GetQueryHistory();
            Assert.AreEqual(2, history.Count);
            Assert.AreEqual(typeof(MockQuery), history[0].QueryType);
            Assert.AreEqual(typeof(MockQuery), history[1].QueryType);
        }

        /// <summary>
        /// 测试：查询执行时间和状态应该被正确记录
        ///
        /// 验证内容：
        /// 1. 记录查询开始和完成时间
        /// 2. 计算执行时间（毫秒）
        /// 3. 记录查询执行状态（成功/失败）
        ///
        /// 场景：性能监控、SLA 跟踪、故障排查
        /// </summary>
        [Test]
        public void Execute_ShouldRecordExecutionTime()
        {
            // Arrange - 准备一个查询
            var query = new MockQuery { ExpectedResult = 100 };

            // Act - 执行查询
            _executor.Execute(query);

            // Assert - 验证时间和状态记录
            var history = _executor.GetQueryHistory();
            Assert.AreEqual(1, history.Count);
            Assert.IsNotNull(history[0].StartedAt);
            Assert.IsNotNull(history[0].CompletedAt);
            Assert.IsTrue(history[0].ExecutionTimeMs >= 0);
            Assert.AreEqual(QueryStatus.Succeeded, history[0].Status);
        }

        #endregion

        #region Mock Query Implementations

        /// <summary>
        /// Mock 查询实现 - 用于测试的整数查询类
        ///
        /// 实现 IQuery&lt;int&gt; 接口，支持：
        /// • ExpectedResult: 设置期望的返回结果
        /// • ShouldThrow: 控制是否抛出异常
        /// • WasExecuted: 跟踪是否被执行
        ///
        /// 用于验证基本查询执行和异常处理
        /// </summary>
        private class MockQuery : IQuery<int>
        {
            /// <summary>期望的查询结果</summary>
            public int ExpectedResult { get; set; }

            /// <summary>是否应该抛出异常</summary>
            public bool ShouldThrow { get; set; }

            /// <summary>是否已被执行（用于验证）</summary>
            public bool WasExecuted { get; private set; }

            /// <summary>执行查询</summary>
            public int Execute()
            {
                if (ShouldThrow)
                {
                    throw new InvalidOperationException("Mock query error");
                }

                WasExecuted = true;
                return ExpectedResult;
            }
        }

        /// <summary>
        /// Mock 字符串查询实现 - 用于测试的字符串查询类
        ///
        /// 实现 IQuery&lt;string&gt; 接口：
        /// • ExpectedResult: 设置期望的字符串结果
        ///
        /// 用于验证不同返回类型的查询支持
        /// </summary>
        public class MockStringQuery : IQuery<string>
        {
            /// <summary>期望的字符串结果</summary>
            public string ExpectedResult { get; set; }

            /// <summary>执行查询</summary>
            public string Execute()
            {
                return ExpectedResult ?? "Default";
            }
        }

        /// <summary>
        /// Mock 数据类 - 用于测试的复杂对象
        ///
        /// 包含基本属性：
        /// • Id: 整数标识符
        /// • Name: 字符串名称
        ///
        /// 用于验证复杂对象查询
        /// </summary>
        public class MockData
        {
            /// <summary>数据标识符</summary>
            public int Id { get; set; }

            /// <summary>数据名称</summary>
            public string Name { get; set; }
        }

        /// <summary>
        /// Mock 数据查询实现 - 用于测试的复杂对象查询类
        ///
        /// 实现 IQuery&lt;MockData&gt; 接口：
        /// • ExpectedResult: 设置期望的对象结果
        ///
        /// 用于验证复杂对象作为查询结果
        /// </summary>
        public class MockDataQuery : IQuery<MockData>
        {
            /// <summary>期望的对象结果</summary>
            public MockData ExpectedResult { get; set; }

            /// <summary>执行查询</summary>
            public MockData Execute()
            {
                return ExpectedResult;
            }
        }

        #endregion
    }
}
