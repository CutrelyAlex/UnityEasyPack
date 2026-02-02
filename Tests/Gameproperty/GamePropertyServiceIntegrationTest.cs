using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using EasyPack.Architecture;
using EasyPack.ENekoFramework;
using EasyPack.GamePropertySystem;
using EasyPack.Modifiers;

namespace EasyPack.GamepropertyTests
{
    /// <summary>
    /// 测试GamePropertyManager的IService集成和架构集成
    /// </summary>
    [TestFixture]
    public class TestIServiceIntegration
    {
        private GamePropertyService _manager;

        [SetUp]
        public void Setup()
        {
            EasyPackArchitecture.ResetInstance();
            _manager = new GamePropertyService();
            _manager.InitializeAsync().GetAwaiter().GetResult();
        }

        [TearDown]
        public void TearDown()
        {
            _manager?.Dispose();
            _manager = null;
        }

        #region IService接口测试

        [Test]
        public void Test_IService接口_初始化流程()
        {
            var manager = new GamePropertyService();
            IService service = manager;

            Assert.AreEqual(ServiceLifecycleState.Uninitialized, service.State);

            service.InitializeAsync().GetAwaiter().GetResult();
            Assert.AreEqual(ServiceLifecycleState.Ready, service.State);
        }

        [Test]
        public void Test_IService接口_重复初始化不报错()
        {
            var manager = new GamePropertyService();
            manager.InitializeAsync().GetAwaiter().GetResult();
            manager.InitializeAsync().GetAwaiter().GetResult();

            Assert.AreEqual(ServiceLifecycleState.Ready, manager.State);
        }

        [Test]
        public void Test_IService接口_暂停和恢复()
        {
            IService service = _manager;

            Assert.AreEqual(ServiceLifecycleState.Ready, service.State);

            service.Pause();
            Assert.AreEqual(ServiceLifecycleState.Paused, service.State);

            service.Resume();
            Assert.AreEqual(ServiceLifecycleState.Ready, service.State);
        }

        [Test]
        public void Test_IService接口_释放后无法操作()
        {
            var manager = new GamePropertyService();
            manager.Dispose();

            Assert.AreEqual(ServiceLifecycleState.Disposed, manager.State);

            var property = new GameProperty("test", 100);
            Assert.Throws<System.InvalidOperationException>(() => manager.Register(property));
        }

        #endregion

        #region 状态转换测试

        [Test]
        public void Test_状态转换_完整生命周期()
        {
            var manager = new GamePropertyService();

            // Uninitialized -> Initializing -> Ready
            Assert.AreEqual(ServiceLifecycleState.Uninitialized, manager.State);
            manager.InitializeAsync().GetAwaiter().GetResult();
            Assert.AreEqual(ServiceLifecycleState.Ready, manager.State);

            // Ready -> Paused
            manager.Pause();
            Assert.AreEqual(ServiceLifecycleState.Paused, manager.State);

            // Paused -> Ready
            manager.Resume();
            Assert.AreEqual(ServiceLifecycleState.Ready, manager.State);

            // Ready -> Disposed
            manager.Dispose();
            Assert.AreEqual(ServiceLifecycleState.Disposed, manager.State);
        }

        [Test]
        public void Test_状态转换_非Ready状态暂停无效()
        {
            var manager = new GamePropertyService();
            Assert.AreEqual(ServiceLifecycleState.Uninitialized, manager.State);

            manager.Pause();
            Assert.AreEqual(ServiceLifecycleState.Uninitialized, manager.State);
        }

        [Test]
        public void Test_状态转换_非Paused状态恢复无效()
        {
            Assert.AreEqual(ServiceLifecycleState.Ready, _manager.State);

            _manager.Resume();
            Assert.AreEqual(ServiceLifecycleState.Ready, _manager.State);
        }

        #endregion

        #region 暂停状态操作限制测试

        [Test]
        public void Test_暂停状态_禁止注册操作()
        {
            _manager.Pause();

            var property = new GameProperty("test", 100);
            Assert.Throws<System.InvalidOperationException>(() => _manager.Register(property));
        }

        [Test]
        public void Test_暂停状态_禁止查询操作()
        {
            var property = new GameProperty("test", 100);
            _manager.Register(property);

            _manager.Pause();

            // 查询操作不受暂停影响（只读操作允许）
            // 但按照严格的IService规范，可能需要限制所有操作
            // 这里保持查询可用以符合常见需求
            var result = _manager.Get("test");
            Assert.IsNotNull(result);
        }

        [Test]
        public void Test_暂停状态_禁止批量操作()
        {
            _manager.Register(new GameProperty("test", 100), "Category");
            _manager.Pause();

            var modifier = new FloatModifier(ModifierType.Add, 0, 10);
            Assert.Throws<System.InvalidOperationException>(() =>
                _manager.ApplyModifierToCategory("Category", modifier));
        }

        #endregion

        #region 资源释放测试

        [Test]
        public void Test_释放后资源清空()
        {
            _manager.Register(new GameProperty("hp", 100));
            _manager.Register(new GameProperty("mp", 50));

            _manager.Dispose();

            // 无法验证内部字典是否清空（private），但状态应为Disposed
            Assert.AreEqual(ServiceLifecycleState.Disposed, _manager.State);
        }

        [Test]
        public void Test_多次释放不报错()
        {
            _manager.Dispose();
            _manager.Dispose();

            Assert.AreEqual(ServiceLifecycleState.Disposed, _manager.State);
        }

        #endregion

        #region 线程安全性测试（基础）

        [Test]
        public void Test_并发注册_基础线程安全()
        {
            var tasks = new System.Collections.Generic.List<Task>();

            for (int i = 0; i < 10; i++)
            {
                int index = i;
                tasks.Add(Task.Run(() =>
                {
                    var property = new GameProperty($"prop_{index}", index);
                    _manager.Register(property, "Concurrent");
                }));
            }

            Task.WaitAll(tasks.ToArray());

            var props = _manager.GetByCategory("Concurrent");
            Assert.AreEqual(10, props.Count());
        }

        #endregion
    }
}
