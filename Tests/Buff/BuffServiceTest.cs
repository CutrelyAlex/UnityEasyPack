using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using EasyPack.BuffSystem;
using EasyPack.ENekoFramework;

namespace EasyPack.BuffTests
{
    /// <summary>
    /// BuffManager的IBuffService集成测试
    /// 测试服务生命周期管理和状态转换
    /// </summary>
    [TestFixture]
    public class BuffServiceTest
    {
        private BuffService _manager;
        private IBuffService _service;
        private GameObject _dummyTarget;
        private GameObject _dummyCreator;

        [SetUp]
        public void Setup()
        {
            _manager = new BuffService();
            _service = _manager;
            _dummyTarget = new GameObject("DummyTarget");
            _dummyCreator = new GameObject("DummyCreator");
        }

        [TearDown]
        public void TearDown()
        {
            if (_dummyTarget != null)
                Object.DestroyImmediate(_dummyTarget);
            if (_dummyCreator != null)
                Object.DestroyImmediate(_dummyCreator);

            _service?.Dispose();
            _manager = null;
            _service = null;
        }

        #region IService接口测试

        [Test]
        public void Test_IService接口_初始化流程()
        {
            IService service = _service;

            Assert.AreEqual(ServiceLifecycleState.Uninitialized, service.State);

            service.InitializeAsync().GetAwaiter().GetResult();
            Assert.AreEqual(ServiceLifecycleState.Ready, service.State);
        }

        [Test]
        public void Test_IService接口_重复初始化不报错()
        {
            _service.InitializeAsync().GetAwaiter().GetResult();
            _service.InitializeAsync().GetAwaiter().GetResult();

            Assert.AreEqual(ServiceLifecycleState.Ready, _service.State);
        }

        [Test]
        public void Test_IService接口_暂停和恢复()
        {
            _service.InitializeAsync().GetAwaiter().GetResult();
            IService service = _service;

            Assert.AreEqual(ServiceLifecycleState.Ready, service.State);

            service.Pause();
            Assert.AreEqual(ServiceLifecycleState.Paused, service.State);

            service.Resume();
            Assert.AreEqual(ServiceLifecycleState.Ready, service.State);
        }

        [Test]
        public void Test_IService接口_释放后状态正确()
        {
            _service.InitializeAsync().GetAwaiter().GetResult();
            _service.Dispose();

            Assert.AreEqual(ServiceLifecycleState.Disposed, _service.State);
        }

        #endregion

        #region 状态转换测试

        [Test]
        public void Test_状态转换_完整生命周期()
        {
            // Uninitialized -> Initializing -> Ready
            Assert.AreEqual(ServiceLifecycleState.Uninitialized, _service.State);
            _service.InitializeAsync().GetAwaiter().GetResult();
            Assert.AreEqual(ServiceLifecycleState.Ready, _service.State);

            // Ready -> Paused
            _service.Pause();
            Assert.AreEqual(ServiceLifecycleState.Paused, _service.State);

            // Paused -> Ready
            _service.Resume();
            Assert.AreEqual(ServiceLifecycleState.Ready, _service.State);

            // Ready -> Disposed
            _service.Dispose();
            Assert.AreEqual(ServiceLifecycleState.Disposed, _service.State);
        }

        [Test]
        public void Test_状态转换_非Ready状态暂停无效()
        {
            Assert.AreEqual(ServiceLifecycleState.Uninitialized, _service.State);

            _service.Pause();
            Assert.AreEqual(ServiceLifecycleState.Uninitialized, _service.State);
        }

        [Test]
        public void Test_状态转换_非Paused状态恢复无效()
        {
            _service.InitializeAsync().GetAwaiter().GetResult();
            Assert.AreEqual(ServiceLifecycleState.Ready, _service.State);

            _service.Resume();
            Assert.AreEqual(ServiceLifecycleState.Ready, _service.State);
        }

        #endregion

        #region 暂停状态操作限制测试

        [Test]
        public void Test_暂停状态_Update不执行()
        {
            _service.InitializeAsync().GetAwaiter().GetResult();

            var buffData = new BuffData
            {
                ID = "test",
                Duration = 1f, // 1秒持续时间
                TriggerInterval = 0.1f
            };

            var buff = _service.CreateBuff(buffData, _dummyCreator, _dummyTarget);
            float initialDuration = buff.DurationTimer;

            _service.Pause();
            _service.Update(0.5f); // 暂停时更新0.5秒

            // 暂停状态下时间不应减少
            Assert.AreEqual(initialDuration, buff.DurationTimer,
                "暂停时Buff持续时间不应变化");
        }

        [Test]
        public void Test_暂停状态_查询操作允许()
        {
            _service.InitializeAsync().GetAwaiter().GetResult();

            var buffData = new BuffData { ID = "test", Duration = -1f };
            _service.CreateBuff(buffData, _dummyCreator, _dummyTarget);

            _service.Pause();

            // 查询操作在暂停状态下应该允许
            Assert.IsTrue(_service.ContainsBuff(_dummyTarget, "test"));
            Assert.IsNotNull(_service.GetBuff(_dummyTarget, "test"));
        }

        #endregion

        #region IBuffService API测试

        [Test]
        public void Test_API转发_CreateBuff()
        {
            _service.InitializeAsync().GetAwaiter().GetResult();

            var buffData = new BuffData { ID = "test", Duration = -1f };
            var buff = _service.CreateBuff(buffData, _dummyCreator, _dummyTarget);

            Assert.IsNotNull(buff);
            Assert.AreEqual("test", buff.BuffData.ID);
            Assert.IsTrue(_service.ContainsBuff(_dummyTarget, "test"));
        }

        [Test]
        public void Test_API转发_RemoveBuff()
        {
            _service.InitializeAsync().GetAwaiter().GetResult();

            var buffData = new BuffData { ID = "test", Duration = -1f };
            var buff = _service.CreateBuff(buffData, _dummyCreator, _dummyTarget);

            _service.RemoveBuff(buff);
            _service.Update(0); // 处理移除队列

            Assert.IsFalse(_service.ContainsBuff(_dummyTarget, "test"));
        }

        [Test]
        public void Test_API转发_GetBuffsByTag()
        {
            _service.InitializeAsync().GetAwaiter().GetResult();

            var buffData = new BuffData
            {
                ID = "test",
                Duration = -1f,
                Tags = new List<string> { "debuff" }
            };

            _service.CreateBuff(buffData, _dummyCreator, _dummyTarget);

            var buffs = _service.GetBuffsByTag(_dummyTarget, "debuff");
            Assert.AreEqual(1, buffs.Count);
        }

        #endregion

        #region 资源释放测试

        [Test]
        public void Test_释放后查询返回空()
        {
            _service.InitializeAsync().GetAwaiter().GetResult();

            var buffData = new BuffData { ID = "test", Duration = -1f };
            _service.CreateBuff(buffData, _dummyCreator, _dummyTarget);

            _service.Dispose();

            Assert.IsFalse(_service.ContainsBuff(_dummyTarget, "test"));
            Assert.IsNull(_service.GetBuff(_dummyTarget, "test"));
            Assert.AreEqual(0, _service.GetTargetBuffs(_dummyTarget).Count);
        }

        [Test]
        public void Test_多次释放不报错()
        {
            _service.InitializeAsync().GetAwaiter().GetResult();
            _service.Dispose();
            _service.Dispose();

            Assert.AreEqual(ServiceLifecycleState.Disposed, _service.State);
        }

        #endregion
    }
}
