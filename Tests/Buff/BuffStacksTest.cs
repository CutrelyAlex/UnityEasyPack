using NUnit.Framework;
using UnityEngine;
using EasyPack.BuffSystem;

namespace EasyPack.BuffTests
{
    /// <summary>
    /// Buff堆叠系统专项测试
    /// 
    /// 重点测试DecreaseBuffStacks的事件触发逻辑
    /// 验证堆叠数减少时OnReduceStack事件的正确触发
    /// </summary>
    [TestFixture]
    public class BuffStacksTest
    {
        private BuffService _buffManager;
        private GameObject _dummyTarget;
        private GameObject _dummyCreator;

        [SetUp]
        public void Setup()
        {
            _buffManager = new BuffService();
            _buffManager.InitializeAsync().GetAwaiter().GetResult();
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

            _buffManager = null;
        }

        #region IncreaseBuffStacks 测试

        [Test]
        public void Test_IncreaseBuffStacks_正常增加()
        {
            var buffData = new BuffData
            {
                ID = "TestBuff",
                MaxStacks = 5,
                Duration = -1f
            };

            var buff = _buffManager.CreateBuff(buffData, _dummyCreator, _dummyTarget);
            Assert.AreEqual(1, buff.CurrentStacks, "初始堆叠数应为1");

            // 通过重复添加增加堆叠
            buffData.BuffSuperpositionStacksStrategy = BuffSuperpositionStacksType.Add;
            _buffManager.CreateBuff(buffData, _dummyCreator, _dummyTarget);
            Assert.AreEqual(2, buff.CurrentStacks, "增加后堆叠数应为2");
        }

        [Test]
        public void Test_IncreaseBuffStacks_达到最大堆叠上限()
        {
            var buffData = new BuffData
            {
                ID = "TestBuff",
                MaxStacks = 3,
                BuffSuperpositionStacksStrategy = BuffSuperpositionStacksType.Add,
                Duration = -1f
            };

            var buff = _buffManager.CreateBuff(buffData, _dummyCreator, _dummyTarget);
            _buffManager.CreateBuff(buffData, _dummyCreator, _dummyTarget);
            _buffManager.CreateBuff(buffData, _dummyCreator, _dummyTarget);

            Assert.AreEqual(3, buff.CurrentStacks, "应达到最大堆叠数3");

            // 尝试超过最大堆叠
            _buffManager.CreateBuff(buffData, _dummyCreator, _dummyTarget);
            Assert.AreEqual(3, buff.CurrentStacks, "不应超过最大堆叠数");
        }

        [Test]
        public void Test_IncreaseBuffStacks_OnAddStack事件触发()
        {
            var buffData = new BuffData
            {
                ID = "TestBuff",
                MaxStacks = 5,
                BuffSuperpositionStacksStrategy = BuffSuperpositionStacksType.Add,
                Duration = -1f
            };

            int addStackEventCount = 0;
            var buff = _buffManager.CreateBuff(buffData, _dummyCreator, _dummyTarget);
            buff.OnAddStack += (b) => addStackEventCount++;

            // 增加堆叠
            _buffManager.CreateBuff(buffData, _dummyCreator, _dummyTarget);
            _buffManager.CreateBuff(buffData, _dummyCreator, _dummyTarget);

            Assert.AreEqual(2, addStackEventCount, "OnAddStack事件应触发2次");
        }

        #endregion

        #region DecreaseBuffStacks 测试

        [Test]
        public void Test_DecreaseBuffStacks_从多层堆叠减少()
        {
            var buffData = new BuffData
            {
                ID = "TestBuff",
                MaxStacks = 5,
                BuffSuperpositionStacksStrategy = BuffSuperpositionStacksType.Add,
                Duration = -1f
            };

            // 创建3层堆叠
            var buff = _buffManager.CreateBuff(buffData, _dummyCreator, _dummyTarget);
            _buffManager.CreateBuff(buffData, _dummyCreator, _dummyTarget);
            _buffManager.CreateBuff(buffData, _dummyCreator, _dummyTarget);
            Assert.AreEqual(3, buff.CurrentStacks);

            int reduceStackEventCount = 0;
            buff.OnReduceStack += (b) => reduceStackEventCount++;

            // 直接调用 public 方法 DecreaseBuffStacks
            // 减少1层 (从3到2)
            _buffManager.DecreaseBuffStacks(buff, 1);
            Assert.AreEqual(2, buff.CurrentStacks, "堆叠数应从3减少到2");
            Assert.AreEqual(1, reduceStackEventCount, "OnReduceStack事件应触发1次");

            // 再减少1层 (从2到1)
            _buffManager.DecreaseBuffStacks(buff, 1);
            Assert.AreEqual(1, buff.CurrentStacks, "堆叠数应从2减少到1");
            Assert.AreEqual(2, reduceStackEventCount, "OnReduceStack事件应触发2次");
        }

        [Test]
        public void Test_DecreaseBuffStacks_从1层减少到0_触发事件()
        {
            var buffData = new BuffData
            {
                ID = "TestBuff",
                MaxStacks = 5,
                Duration = -1f
            };

            var buff = _buffManager.CreateBuff(buffData, _dummyCreator, _dummyTarget);
            Assert.AreEqual(1, buff.CurrentStacks, "初始堆叠数应为1");

            int reduceStackEventCount = 0;
            int removeEventCount = 0;
            buff.OnReduceStack += (b) => reduceStackEventCount++;
            buff.OnRemove += (b) => removeEventCount++;

            // 直接调用 public 方法
            // 减少1层 (从1到0) - 这应该触发OnReduceStack
            _buffManager.DecreaseBuffStacks(buff, 1);

            // 修复后: OnReduceStack事件应该触发
            Assert.AreEqual(1, reduceStackEventCount,
                "从1减少到0时,OnReduceStack事件应触发");

            // 处理移除队列
            _buffManager.Update(0);

            // Buff应该被移除
            Assert.AreEqual(1, removeEventCount, "OnRemove事件应触发1次");
            Assert.IsFalse(_buffManager.ContainsBuff(_dummyTarget, "TestBuff"),
                "Buff应该已被移除");
        }

        [Test]
        public void Test_DecreaseBuffStacks_一次减少多层_触发事件()
        {
            var buffData = new BuffData
            {
                ID = "TestBuff",
                MaxStacks = 10,
                BuffSuperpositionStacksStrategy = BuffSuperpositionStacksType.Add,
                Duration = -1f
            };

            // 创建5层堆叠
            var buff = _buffManager.CreateBuff(buffData, _dummyCreator, _dummyTarget);
            for (int i = 0; i < 4; i++)
                _buffManager.CreateBuff(buffData, _dummyCreator, _dummyTarget);
            Assert.AreEqual(5, buff.CurrentStacks);

            int reduceStackEventCount = 0;
            buff.OnReduceStack += (b) => reduceStackEventCount++;

            // 直接调用 public 方法
            // 一次减少3层 (从5到2)
            _buffManager.DecreaseBuffStacks(buff, 3);
            Assert.AreEqual(2, buff.CurrentStacks, "堆叠数应从5减少到2");
            Assert.AreEqual(1, reduceStackEventCount, "OnReduceStack事件应触发1次");
        }

        [Test]
        public void Test_DecreaseBuffStacks_减少过多仍触发事件()
        {
            var buffData = new BuffData
            {
                ID = "TestBuff",
                MaxStacks = 5,
                BuffSuperpositionStacksStrategy = BuffSuperpositionStacksType.Add,
                Duration = -1f
            };

            // 创建3层堆叠
            var buff = _buffManager.CreateBuff(buffData, _dummyCreator, _dummyTarget);
            _buffManager.CreateBuff(buffData, _dummyCreator, _dummyTarget);
            _buffManager.CreateBuff(buffData, _dummyCreator, _dummyTarget);
            Assert.AreEqual(3, buff.CurrentStacks);

            int reduceStackEventCount = 0;
            int removeEventCount = 0;
            buff.OnReduceStack += (b) => reduceStackEventCount++;
            buff.OnRemove += (b) => removeEventCount++;

            // 直接调用 public 方法
            // 减少5层 (超过当前堆叠)
            _buffManager.DecreaseBuffStacks(buff, 5);

            // 处理移除队列
            _buffManager.Update(0);

            Assert.AreEqual(1, reduceStackEventCount,
                "即使减少过多，OnReduceStack事件也应触发以通知逻辑层");
            Assert.AreEqual(1, removeEventCount, "OnRemove事件应触发1次");
            Assert.IsFalse(_buffManager.ContainsBuff(_dummyTarget, "TestBuff"));
        }

        #endregion

        #region 边界条件测试

        [Test]
        public void Test_DecreaseBuffStacks_空Buff不崩溃()
        {
            // 直接调用 public 方法，传入null不应崩溃
            Assert.DoesNotThrow(() => _buffManager.DecreaseBuffStacks(null, 1));
        }

        [Test]
        public void Test_DecreaseBuffStacks_负数堆叠参数被拦截()
        {
            var buffData = new BuffData
            {
                ID = "TestBuff",
                MaxStacks = 5,
                Duration = -1f
            };

            var buff = _buffManager.CreateBuff(buffData, _dummyCreator, _dummyTarget);
            int originalStacks = buff.CurrentStacks;

            // 直接调用 public 方法，传入负数参数
            _buffManager.DecreaseBuffStacks(buff, -1);

            // 参数验证应拦截操作，堆叠数保持不变
            Assert.AreEqual(originalStacks, buff.CurrentStacks,
                "负数参数应被参数验证拦截，堆叠数保持不变");
            Assert.IsTrue(_buffManager.ContainsBuff(_dummyTarget, "TestBuff"),
                "Buff不应被移除");
        }

        #endregion
    }
}
