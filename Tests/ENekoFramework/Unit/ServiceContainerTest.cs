using NUnit.Framework;
using UnityEngine.TestTools;
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using EasyPack.ENekoFramework;

namespace EasyPack.ENekoFrameworkTest.Unit
{
    /// <summary>
    /// 服务容器 (ServiceContainer) 单元测试
    ///
    /// ServiceContainer 是什么？
    /// =======================
    /// ServiceContainer 是 ENekoFramework 的核心组件之一，负责：
    /// 1. 服务注册 - 注册服务接口到具体实现的映射
    /// 2. 依赖注入 - 提供服务实例的解析和生命周期管理
    /// 3. 单例模式 - 确保每个服务只有一个实例
    /// 4. 异步初始化 - 支持服务的异步初始化过程
    ///
    /// 设计模式：Service Locator + Singleton + Dependency Injection
    /// 作用：管理应用程序的服务实例，提供集中式的依赖管理
    ///
    /// 关键特性：
    /// • 泛型注册：类型安全的接口到实现映射
    /// • 异步解析：支持异步服务初始化
    /// • 单例保证：每个服务只创建一个实例
    /// • 生命周期管理：自动处理服务初始化和清理
    /// • 线程安全：支持并发访问
    ///
    /// 测试覆盖范围：
    /// ============
    /// 1. 服务注册 - 验证注册机制和约束
    /// 2. 服务解析 - 测试异步解析和实例管理
    /// 3. 生命周期 - 验证初始化和清理过程
    /// 4. 错误处理 - 测试异常情况的处理
    /// </summary>
    public class ServiceContainerTest
    {
        private ServiceContainer _container;

        [SetUp]
        public void Setup()
        {
            _container = new ServiceContainer();
        }

        [TearDown]
        public void TearDown()
        {
            _container?.Clear();
            _container = null;
        }

        #region 服务注册测试 - 测试注册机制

        /// <summary>
        /// 测试：注册有效的服务应该成功
        ///
        /// 验证内容：
        /// 1. ServiceContainer 能接受有效的服务注册
        /// 2. 注册后能正确查询服务是否已注册
        /// 3. 注册计数器正确更新
        ///
        /// 场景：应用程序启动时注册核心服务
        /// </summary>
        [Test]
        public void Register_ValidService_ShouldSucceed()
        {
            // Act - 注册一个服务
            _container.Register<IMockService, MockService>();

            // Assert - 验证注册成功
            Assert.IsTrue(_container.IsRegistered<IMockService>());
            Assert.AreEqual(1, _container.RegisteredServiceCount);
        }

        /// <summary>
        /// 测试：重复注册同一服务应该抛出异常
        ///
        /// 验证内容：
        /// 1. 不允许重复注册相同的服务接口
        /// 2. 抛出适当的异常类型
        /// 3. 防止服务注册冲突
        ///
        /// 场景：防止配置错误导致的服务覆盖
        /// </summary>
        [Test]
        public void Register_DuplicateService_ShouldThrowException()
        {
            // Arrange - 先注册一个服务
            _container.Register<IMockService, MockService>();

            // Act & Assert - 重复注册应该抛异常
            Assert.Throws<InvalidOperationException>(() =>
            {
                _container.Register<IMockService, MockService>();
            });
        }

        /// <summary>
        /// 测试：可以注册多个不同的服务
        ///
        /// 验证内容：
        /// 1. 支持同时注册多个不同的服务
        /// 2. 每个服务独立管理
        /// 3. 注册计数正确累加
        ///
        /// 场景：应用程序有多个独立的服务组件
        /// </summary>
        [Test]
        public void Register_MultipleServices_ShouldSucceed()
        {
            // Act - 注册两个不同的服务
            _container.Register<IMockService, MockService>();
            _container.Register<IMockService2, MockService2>();

            // Assert - 验证都注册成功
            Assert.IsTrue(_container.IsRegistered<IMockService>());
            Assert.IsTrue(_container.IsRegistered<IMockService2>());
            Assert.AreEqual(2, _container.RegisteredServiceCount);
        }

        #endregion

        #region 服务解析测试 - 测试异步解析

        /// <summary>
        /// 测试：解析已注册的服务应该返回实例
        ///
        /// 验证内容：
        /// 1. 能成功解析已注册的服务
        /// 2. 返回正确的实现类型实例
        /// 3. 异步解析过程正常工作
        ///
        /// 场景：组件需要获取依赖的服务实例
        /// </summary>
        [UnityTest]
        public IEnumerator Resolve_RegisteredService_ShouldReturnInstance()
        {
            // Arrange - 注册服务
            _container.Register<IMockService, MockService>();

            // Act - 异步解析服务
            Task<IMockService> task = _container.ResolveAsync<IMockService>();
            yield return new WaitUntil(() => task.IsCompleted);

            // Assert - 验证返回正确的实例
            Assert.IsNotNull(task.Result);
            Assert.IsInstanceOf<MockService>(task.Result);
        }

        /// <summary>
        /// 测试：解析未注册的服务应该抛出异常
        ///
        /// 验证内容：
        /// 1. 未注册的服务无法解析
        /// 2. 抛出适当的异常类型
        /// 3. 异步异常正确传播
        ///
        /// 场景：防止使用未配置的服务导致运行时错误
        /// </summary>
        [UnityTest]
        public IEnumerator Resolve_UnregisteredService_ShouldThrowException()
        {
            // Act - 尝试解析未注册的服务
            Task<IMockService> task = _container.ResolveAsync<IMockService>();
            yield return new WaitUntil(() => task.IsCompleted || task.IsFaulted);

            // Assert - 验证抛出异常
            Assert.IsTrue(task.IsFaulted);
            Assert.IsInstanceOf<InvalidOperationException>(task.Exception.InnerException);
        }

        /// <summary>
        /// 测试：多次解析同一服务应该返回相同实例（单例）
        ///
        /// 验证内容：
        /// 1. 同一服务多次解析返回同一实例
        /// 2. 单例模式正确实现
        /// 3. 实例引用相等
        ///
        /// 场景：确保服务状态在整个应用中保持一致
        /// </summary>
        [UnityTest]
        public IEnumerator Resolve_MultipleTimes_ShouldReturnSameInstance()
        {
            // Arrange - 注册服务
            _container.Register<IMockService, MockService>();

            // Act - 解析两次
            Task<IMockService> task1 = _container.ResolveAsync<IMockService>();
            yield return new WaitUntil(() => task1.IsCompleted);

            Task<IMockService> task2 = _container.ResolveAsync<IMockService>();
            yield return new WaitUntil(() => task2.IsCompleted);

            // Assert - 验证返回同一实例
            Assert.AreSame(task1.Result, task2.Result);
        }

        #endregion

        #region 生命周期测试 - 测试初始化和清理

        /// <summary>
        /// 测试：解析服务时应该自动初始化
        ///
        /// 验证内容：
        /// 1. 服务实例被创建后自动调用初始化
        /// 2. 初始化过程异步完成
        /// 3. 服务状态正确转换为Ready
        ///
        /// 场景：服务需要执行启动逻辑，如连接数据库、加载配置
        /// </summary>
        [UnityTest]
        public IEnumerator Resolve_ShouldAutoInitialize()
        {
            // Arrange - 注册服务
            _container.Register<IMockService, MockService>();

            // Act - 解析服务
            Task<IMockService> task = _container.ResolveAsync<IMockService>();
            yield return new WaitUntil(() => task.IsCompleted);

            var service = task.Result as MockService;

            // Assert - 验证自动初始化
            Assert.IsNotNull(service);
            Assert.IsTrue(service.IsInitialized);
            Assert.AreEqual(ServiceLifecycleState.Ready, service.State);
        }

        /// <summary>
        /// 测试：清理容器应该释放所有服务
        ///
        /// 验证内容：
        /// 1. Clear 方法清理所有已注册的服务
        /// 2. 注册计数归零
        /// 3. 服务查询返回false
        ///
        /// 场景：应用程序关闭或重置时清理资源
        /// </summary>
        [Test]
        public void Clear_ShouldDisposeAllServices()
        {
            // Arrange - 注册多个服务
            _container.Register<IMockService, MockService>();
            _container.Register<IMockService2, MockService2>();

            // Act - 清理容器
            _container.Clear();

            // Assert - 验证所有服务都被清理
            Assert.AreEqual(0, _container.RegisteredServiceCount);
            Assert.IsFalse(_container.IsRegistered<IMockService>());
            Assert.IsFalse(_container.IsRegistered<IMockService2>());
        }

        #endregion

        #region Mock Service Implementation

        /// <summary>
        /// Mock 服务接口 - 用于测试的服务契约
        ///
        /// 定义测试服务的基本属性：
        /// • Name: 服务名称
        /// • IsInitialized: 初始化状态
        ///
        /// 用于验证服务注册和解析功能
        /// </summary>
        public interface IMockService : IService
        {
            string Name { get; set; }
            bool IsInitialized { get; }
        }

        /// <summary>
        /// Mock 服务接口2 - 用于测试的第二个服务契约
        ///
        /// 定义不同的属性：
        /// • Value: 整数值
        ///
        /// 用于验证多服务注册功能
        /// </summary>
        public interface IMockService2 : IService
        {
            int Value { get; set; }
        }

        /// <summary>
        /// Mock 服务实现 - 实现 IMockService 的具体类
        ///
        /// 继承 BaseService，支持：
        /// • 异步初始化
        /// • 生命周期状态管理
        /// • 属性设置和状态跟踪
        ///
        /// 用于验证服务初始化和单例行为
        /// </summary>
        public class MockService : BaseService, IMockService
        {
            public string Name { get; set; } = "MockService";
            public bool IsInitialized { get; private set; }

            protected override async Task OnInitializeAsync()
            {
                await Task.CompletedTask;
                IsInitialized = true;
            }
        }

        /// <summary>
        /// Mock 服务实现2 - 实现 IMockService2 的具体类
        ///
        /// 提供简单的属性设置：
        /// • 默认值为42
        /// • 同步初始化
        ///
        /// 用于验证多服务场景
        /// </summary>
        public class MockService2 : BaseService, IMockService2
        {
            public int Value { get; set; } = 42;

            protected override Task OnInitializeAsync()
            {
                return Task.CompletedTask;
            }
        }

        #endregion
    }
}
