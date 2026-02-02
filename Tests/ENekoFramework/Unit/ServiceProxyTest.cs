using NUnit.Framework;
using System;
using System.Threading.Tasks;
using EasyPack.ENekoFramework;

namespace EasyPack.ENekoFrameworkTest.Unit
{
    /// <summary>
    /// 测试 ServiceProxy 的跨架构服务代理功能
    /// </summary>
    [TestFixture]
    public class ServiceProxyTest
    {
        private ServiceContainer _sourceContainer;
        private ServiceProxy<ITestService> _proxy;

        [SetUp]
        public void Setup()
        {
            _sourceContainer = new ServiceContainer();
        }

        [TearDown]
        public void TearDown()
        {
            _proxy?.Dispose();
            _sourceContainer?.Dispose();
        }

        [Test]
        public void GetService_ShouldResolveFromSourceContainer()
        {
            // Arrange
            var testService = new TestService { Value = 42 };
            _sourceContainer.RegisterSingleton<ITestService>(testService);

            _proxy = new ServiceProxy<ITestService>(_sourceContainer);

            // Act
            var service = _proxy.GetService();

            // Assert
            Assert.IsNotNull(service);
            Assert.AreEqual(42, service.Value);
        }

        [Test]
        public void GetService_MultipleCalls_ShouldReturnCachedInstance()
        {
            // Arrange
            var testService = new TestService { Value = 42 };
            _sourceContainer.RegisterSingleton<ITestService>(testService);

            _proxy = new ServiceProxy<ITestService>(_sourceContainer);

            // Act
            var service1 = _proxy.GetService();
            var service2 = _proxy.GetService();

            // Assert
            Assert.AreSame(service1, service2, "Should return cached instance");
        }

        [Test]
        public void GetService_WithLazyRegistration_ShouldResolve()
        {
            // Arrange
            _sourceContainer.RegisterLazy<ITestService>(c => new TestService { Value = 99 });

            _proxy = new ServiceProxy<ITestService>(_sourceContainer);

            // Act
            var service = _proxy.GetService();

            // Assert
            Assert.IsNotNull(service);
            Assert.AreEqual(99, service.Value);
        }

        [Test]
        public void GetService_ServiceNotRegistered_ShouldThrow()
        {
            // Arrange - 不注册任何服务
            _proxy = new ServiceProxy<ITestService>(_sourceContainer);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                _proxy.GetService();
            });
        }

        [Test]
        public void GetService_AfterDispose_ShouldThrow()
        {
            // Arrange
            var testService = new TestService { Value = 42 };
            _sourceContainer.RegisterSingleton<ITestService>(testService);

            _proxy = new ServiceProxy<ITestService>(_sourceContainer);
            _proxy.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() =>
            {
                _proxy.GetService();
            });
        }

        [Test]
        public void GetService_ParallelCalls_ShouldResolveOnce()
        {
            // Arrange
            int creationCount = 0;
            _sourceContainer.RegisterLazy<ITestService>(c =>
            {
                creationCount++;
                return new TestService { Value = 42 };
            });

            _proxy = new ServiceProxy<ITestService>(_sourceContainer);

            // Act - 并行调用
            var tasks = new[]
            {
                _proxy.GetServiceAsync(),
                _proxy.GetServiceAsync(),
                _proxy.GetServiceAsync()
            };

            Task.WaitAll(tasks);
            var services = new[] { tasks[0].Result, tasks[1].Result, tasks[2].Result };

            // Assert
            Assert.AreEqual(1, creationCount, "Service should only be created once");
            Assert.AreSame(services[0], services[1]);
            Assert.AreSame(services[1], services[2]);
        }

        [Test]
        public void Dispose_ShouldClearCachedService()
        {
            // Arrange
            var testService = new TestService { Value = 42 };
            _sourceContainer.RegisterSingleton<ITestService>(testService);

            _proxy = new ServiceProxy<ITestService>(_sourceContainer);

            // Act
            _proxy.Dispose();

            // Assert - 调用同步方法应该抛出 ObjectDisposedException
            Assert.Throws<ObjectDisposedException>(() =>
            {
                _proxy.GetService();
            });
        }

        // 测试接口和类
        private interface ITestService
        {
            int Value { get; set; }
        }

        private class TestService : ITestService
        {
            public int Value { get; set; }
        }
    }
}
