using NUnit.Framework;
using EasyPack.ENekoFramework;

namespace EasyPack.ENekoFrameworkTest.Unit
{
    /// <summary>
    /// 测试 ServiceContainer 的循环依赖检测
    /// </summary>
    [TestFixture]
    public class CircularDependencyTests
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
            _container?.Dispose();
        }

        [Test]
        public void DirectCircularDependency_ShouldThrowException()
        {
            // Arrange - ServiceA -> ServiceB -> ServiceA
            _container.RegisterLazy<ServiceA>(c => new ServiceA(c.Resolve<ServiceB>()));
            _container.RegisterLazy<ServiceB>(c => new ServiceB(c.Resolve<ServiceA>()));

            // Act & Assert
            var ex = Assert.Throws<CircularDependencyException>(() =>
            {
                _container.Resolve<ServiceA>();
            });

            Assert.That(ex.Message, Does.Contain("ServiceA"));
            Assert.That(ex.Message, Does.Contain("ServiceB"));
            Assert.That(ex.DependencyPath, Does.Contain("→"));
        }

        [Test]
        public void IndirectCircularDependency_ShouldThrowException()
        {
            // Arrange - ServiceA -> ServiceB -> ServiceC -> ServiceA
            _container.RegisterLazy<ServiceA>(c => new ServiceA(c.Resolve<ServiceB>()));
            _container.RegisterLazy<ServiceB>(c => new ServiceB(c.Resolve<ServiceC>()));
            _container.RegisterLazy<ServiceC>(c => new ServiceC(c.Resolve<ServiceA>()));

            // Act & Assert
            var ex = Assert.Throws<CircularDependencyException>(() =>
            {
                _container.Resolve<ServiceA>();
            });

            Assert.That(ex.DependencyPath, Does.Match(".*ServiceA.*→.*ServiceB.*→.*ServiceC.*→.*ServiceA.*"));
        }

        [Test]
        public void SelfCircularDependency_ShouldThrowException()
        {
            // Arrange - ServiceA -> ServiceA
            _container.RegisterLazy<ServiceA>(c => new ServiceA(c.Resolve<ServiceA>()));

            // Act & Assert
            var ex = Assert.Throws<CircularDependencyException>(() =>
            {
                _container.Resolve<ServiceA>();
            });

            Assert.That(ex.DependencyPath, Does.Match(".*ServiceA.*→.*ServiceA.*"));
        }

        [Test]
        public void NormalDependencyChain_ShouldResolveSuccessfully()
        {
            // Arrange - ServiceA -> ServiceB -> ServiceC (无循环)
            _container.RegisterLazy<ServiceA>(c => new ServiceA(c.Resolve<ServiceB>()));
            _container.RegisterLazy<ServiceB>(c => new ServiceB(c.Resolve<ServiceC>()));
            _container.RegisterSingleton<ServiceC>(new ServiceC(null));

            // Act
            var service = _container.Resolve<ServiceA>();

            // Assert
            Assert.IsNotNull(service);
            Assert.IsNotNull(service.Dependency);
        }

        [Test]
        public void ParallelResolution_WithCircularDependency_ShouldThrowException()
        {
            // Arrange
            _container.RegisterLazy<ServiceA>(c => new ServiceA(c.Resolve<ServiceB>()));
            _container.RegisterLazy<ServiceB>(c => new ServiceB(c.Resolve<ServiceA>()));

            // Act & Assert - 循环依赖应该在解析时立即抛出
            Assert.Throws<CircularDependencyException>(() =>
            {
                _container.Resolve<ServiceA>();
            });
        }

        [Test]
        public void CircularDependencyException_ShouldContainFullPath()
        {
            // Arrange - A -> B -> C -> D -> B
            _container.RegisterLazy<ServiceA>(c => new ServiceA(c.Resolve<ServiceB>()));
            _container.RegisterLazy<ServiceB>(c => new ServiceB(c.Resolve<ServiceC>()));
            _container.RegisterLazy<ServiceC>(c => new ServiceC(c.Resolve<ServiceD>()));
            _container.RegisterLazy<ServiceD>(c => new ServiceD(c.Resolve<ServiceB>()));

            // Act & Assert
            var ex = Assert.Throws<CircularDependencyException>(() =>
            {
                _container.Resolve<ServiceA>();
            });

            // 验证完整路径: A → B → C → D → B
            StringAssert.Contains("ServiceA", ex.DependencyPath);
            StringAssert.Contains("ServiceB", ex.DependencyPath);
            StringAssert.Contains("ServiceC", ex.DependencyPath);
            StringAssert.Contains("ServiceD", ex.DependencyPath);
        }

        // 测试服务类
        private class ServiceA
        {
            public object Dependency { get; }
            public ServiceA(object dependency) => Dependency = dependency;
        }

        private class ServiceB
        {
            public object Dependency { get; }
            public ServiceB(object dependency) => Dependency = dependency;
        }

        private class ServiceC
        {
            public object Dependency { get; }
            public ServiceC(object dependency) => Dependency = dependency;
        }

        private class ServiceD
        {
            public object Dependency { get; }
            public ServiceD(object dependency) => Dependency = dependency;
        }
    }
}
