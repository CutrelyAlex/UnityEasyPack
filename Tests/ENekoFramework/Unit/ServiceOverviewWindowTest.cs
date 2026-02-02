using NUnit.Framework;
using EasyPack.ENekoFramework;
using System.Linq;
using TestScripts.ENekoFramework.Mocks;

namespace EasyPack.ENekoFrameworkTest.Unit
{
    /// <summary>
    /// 测试服务总览窗口功能
    /// </summary>
    [TestFixture]
    public class ServiceOverviewWindowTest
    {
        private TestArchitecture _architecture;

        [SetUp]
        public void Setup()
        {
            TestArchitecture.ResetInstance();
            _architecture = TestArchitecture.Instance;
        }

        [TearDown]
        public void Teardown()
        {
            TestArchitecture.ResetInstance();
        }

        [Test]
        public void ServiceList_ShouldIncludeAllRegisteredServices()
        {
            // Arrange
            _architecture.RegisterService<IMockService, MockService>();

            // Act
            var services = _architecture.GetAllServices();

            // Assert
            Assert.That(services, Is.Not.Null);
            Assert.That(services.Count(), Is.EqualTo(1));
            Assert.That(services.Any(s => s.ServiceType == typeof(IMockService)), Is.True);
        }

        [Test]
        public void ServiceState_ShouldReflectLifecycle()
        {
            // Arrange
            _architecture.RegisterService<IMockService, MockService>();

            // Act
            var descriptor = _architecture.GetServiceDescriptor<IMockService>();

            // Assert - Initially Uninitialized
            Assert.That(descriptor, Is.Not.Null);
            Assert.That(descriptor.State, Is.EqualTo(ServiceLifecycleState.Uninitialized));

            // After initialization
            var service = _architecture.ResolveAsync<IMockService>().Result;
            descriptor = _architecture.GetServiceDescriptor<IMockService>();
            Assert.That(descriptor.State, Is.EqualTo(ServiceLifecycleState.Ready));
        }

        [Test]
        public void DependencyGraph_ShouldDetectServiceReferences()
        {
            // Arrange
            _architecture.RegisterService<IMockService, MockService>();
            _architecture.RegisterService<IBuffSystem, MockBuffSystem>();

            // Act - 获取服务以触发初始化
            var mockService = _architecture.ResolveAsync<IMockService>().Result;
            var buffs = _architecture.ResolveAsync<IBuffSystem>().Result;

            // Assert
            var allServices = _architecture.GetAllServices().ToList();
            Assert.That(allServices.Count, Is.EqualTo(2));

            // Verify both services are in Ready state
            Assert.That(allServices.All(s => s.State == ServiceLifecycleState.Ready), Is.True);
        }

        [Test]
        public void ServiceMetadata_ShouldIncludeTimestamps()
        {
            // Arrange
            _architecture.RegisterService<IMockService, MockService>();

            // Act
            var descriptorBefore = _architecture.GetServiceDescriptor<IMockService>();
            var registeredAt = descriptorBefore.RegisteredAt;

            var service = _architecture.ResolveAsync<IMockService>().Result;
            var descriptorAfter = _architecture.GetServiceDescriptor<IMockService>();
            var lastAccessed = descriptorAfter.LastAccessedAt;

            // Assert
            Assert.That(registeredAt, Is.Not.EqualTo(default(System.DateTime)));
            Assert.That(lastAccessed, Is.Not.EqualTo(default(System.DateTime)));
            Assert.That(lastAccessed, Is.GreaterThanOrEqualTo(registeredAt));
        }

        [Test]
        public void ServiceFilter_ShouldFilterByType()
        {
            // Arrange
            _architecture.RegisterService<IMockService, MockService>();
            _architecture.RegisterService<IBuffSystem, MockBuffSystem>();
            _architecture.RegisterService<IInventorySystem, MockInventorySystem>();

            // Act
            var allServices = _architecture.GetAllServices().ToList();
            var buffServices = allServices.Where(s => s.ServiceType.Name.Contains("Buff")).ToList();

            // Assert
            Assert.That(allServices.Count, Is.EqualTo(3));
            Assert.That(buffServices.Count, Is.EqualTo(1));
            Assert.That(buffServices[0].ServiceType, Is.EqualTo(typeof(IBuffSystem)));
        }

        [Test]
        public void ServiceDisplay_ShouldShowImplementationType()
        {
            // Arrange
            _architecture.RegisterService<IMockService, MockService>();

            // Act
            var descriptor = _architecture.GetServiceDescriptor<IMockService>();

            // Assert
            Assert.That(descriptor.ServiceType, Is.EqualTo(typeof(IMockService)));
            Assert.That(descriptor.ImplementationType, Is.EqualTo(typeof(MockService)));
        }
    }
}
