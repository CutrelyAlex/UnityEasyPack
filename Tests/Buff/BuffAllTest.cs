using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using EasyPack.BuffSystem;
using EasyPack.GamePropertySystem;
using EasyPack.Modifiers;

namespace EasyPack.BuffTests
{
    /// <summary>
    /// Buff系统单元测试
    /// 
    /// 测试覆盖范围：
    /// =============
    /// 1. Buff创建和添加 - 验证基本功能
    /// 2. Buff持续时间 - 测试时间管理
    /// 3. Buff堆叠 - 验证堆叠机制
    /// 4. Buff移除 - 测试移除策略
    /// 5. Buff更新 - 验证更新和触发机制
    /// 6. 属性修饰器 - 测试与GameProperty的集成
    /// 7. 自定义模块 - 验证扩展性
    /// </summary>
    [TestFixture]
    public class BuffAllTest
    {
        private BuffService _buffManager;
        private GameObject _dummyTarget;
        private GameObject _dummyCreator;
        private GamePropertyService _gamePropertyManager;

        [SetUp]
        public void Setup()
        {
            _buffManager = new BuffService();
            _buffManager.InitializeAsync().GetAwaiter().GetResult();
            _dummyTarget = new GameObject("DummyTarget");
            _dummyCreator = new GameObject("DummyCreator");
            _gamePropertyManager = new GamePropertyService();
            _gamePropertyManager.InitializeAsync().GetAwaiter().GetResult();
        }

        [TearDown]
        public void TearDown()
        {
            if (_dummyTarget != null)
                Object.DestroyImmediate(_dummyTarget);
            if (_dummyCreator != null)
                Object.DestroyImmediate(_dummyCreator);

            _buffManager = null;
            _gamePropertyManager?.Dispose();
            _gamePropertyManager = null;
        }

        [Test]
        public void Test_AddBuff_Simple()
        {
            // 创建一个简单的BuffData
            var buffData = new BuffData
            {
                ID = "SimpleBuff",
                Name = "简单Buff",
                Duration = -1f,  // 无限时长
                MaxStacks = 1,
                TriggerOnCreate = false
            };

            // 添加Buff
            var buff = _buffManager.CreateBuff(buffData, _dummyCreator, _dummyTarget);

            // 验证Buff是否成功添加
            Assert.IsNotNull(buff, "Buff创建失败!");
            Assert.AreEqual("SimpleBuff", buff.BuffData.ID);
            Assert.AreEqual(1, buff.CurrentStacks);
            Assert.AreEqual(-1f, buff.DurationTimer);

            // 验证目标身上是否有此Buff
            bool hasBuff = _buffManager.ContainsBuff(_dummyTarget, "SimpleBuff");
            Assert.IsTrue(hasBuff, "目标应该拥有该Buff!");

            // 清理
            _buffManager.RemoveAllBuffs(_dummyTarget);
        }

        [Test]
        public void Test_AddBuff_WithDuration()
        {
            // 创建一个有限时长的BuffData
            var buffData = new BuffData
            {
                ID = "TimedBuff",
                Name = "定时Buff",
                Duration = 5.0f,  // 5秒持续时间
                MaxStacks = 1
            };

            // 添加Buff
            var buff = _buffManager.CreateBuff(buffData, _dummyCreator, _dummyTarget);

            // 验证初始持续时间
            Assert.That(buff.DurationTimer, Is.EqualTo(5.0f).Within(0.001f),
                $"初始持续时间应为5秒，实际为: {buff.DurationTimer}");

            // 模拟过去2秒
            float deltaTime = 2.0f;
            _buffManager.Update(deltaTime);

            // 验证剩余时间
            Assert.That(buff.DurationTimer, Is.EqualTo(3.0f).Within(0.001f),
                $"剩余时间应为3秒，实际为: {buff.DurationTimer}");

            // 清理
            _buffManager.RemoveAllBuffs(_dummyTarget);
        }

        [Test]
        public void Test_AddBuff_MaxStacks()
        {
            // 创建一个可堆叠的BuffData
            var buffData = new BuffData
            {
                ID = "StackableBuff",
                Name = "可堆叠Buff",
                Duration = -1f,
                MaxStacks = 3,
                BuffSuperpositionStacksStrategy = BuffSuperpositionStacksType.Add
            };

            // 第一次添加Buff
            var buff = _buffManager.CreateBuff(buffData, _dummyCreator, _dummyTarget);
            Assert.AreEqual(1, buff.CurrentStacks, $"初始堆叠数应为1，实际为: {buff.CurrentStacks}");

            // 第二次添加相同Buff
            _buffManager.CreateBuff(buffData, _dummyCreator, _dummyTarget);
            Assert.AreEqual(2, buff.CurrentStacks, $"第二次添加后堆叠数应为2，实际为: {buff.CurrentStacks}");

            // 第三次添加相同Buff
            _buffManager.CreateBuff(buffData, _dummyCreator, _dummyTarget);
            Assert.AreEqual(3, buff.CurrentStacks, $"第三次添加后堆叠数应为3，实际为: {buff.CurrentStacks}");

            // 第四次添加相同Buff（应该不会超过最大堆叠数）
            _buffManager.CreateBuff(buffData, _dummyCreator, _dummyTarget);
            Assert.AreEqual(3, buff.CurrentStacks, $"超过最大堆叠后的堆叠数应仍为3，实际为: {buff.CurrentStacks}");

            _buffManager.RemoveAllBuffs(_dummyTarget);
        }

        [Test]
        public void Test_BuffSuperpositionDuration()
        {
            // 1. 测试Add持续时间叠加策略
            var addDurationBuff = new BuffData
            {
                ID = "AddDurationBuff",
                Duration = 5.0f,
                BuffSuperpositionStrategy = BuffSuperpositionDurationType.Add
            };

            var buff1 = _buffManager.CreateBuff(addDurationBuff, _dummyCreator, _dummyTarget);
            Assert.That(buff1.DurationTimer, Is.EqualTo(5.0f).Within(0.001f), "初始持续时间应为5秒");

            _buffManager.CreateBuff(addDurationBuff, _dummyCreator, _dummyTarget);
            Assert.That(buff1.DurationTimer, Is.EqualTo(10.0f).Within(0.001f),
                $"Add策略下，持续时间应叠加为10秒，实际为: {buff1.DurationTimer}");

            _buffManager.RemoveAllBuffs(_dummyTarget);

            // 2. 测试Reset持续时间叠加策略
            var resetDurationBuff = new BuffData
            {
                ID = "ResetDurationBuff",
                Duration = 5.0f,
                BuffSuperpositionStrategy = BuffSuperpositionDurationType.Reset
            };

            var buff2 = _buffManager.CreateBuff(resetDurationBuff, _dummyCreator, _dummyTarget);

            // 模拟时间流逝2秒
            _buffManager.Update(2.0f);
            Assert.That(buff2.DurationTimer, Is.EqualTo(3.0f).Within(0.001f), "2秒后持续时间应为3秒");

            // 重新施加，应该重置为5秒
            _buffManager.CreateBuff(resetDurationBuff, _dummyCreator, _dummyTarget);
            Assert.That(buff2.DurationTimer, Is.EqualTo(5.0f).Within(0.001f),
                $"Reset策略下，持续时间应重置为5秒，实际为: {buff2.DurationTimer}");

            _buffManager.RemoveAllBuffs(_dummyTarget);

            // 3. 测试Keep持续时间策略
            var keepDurationBuff = new BuffData
            {
                ID = "KeepDurationBuff",
                Duration = 5.0f,
                BuffSuperpositionStrategy = BuffSuperpositionDurationType.Keep
            };

            var buff3 = _buffManager.CreateBuff(keepDurationBuff, _dummyCreator, _dummyTarget);

            // 模拟时间流逝2秒
            _buffManager.Update(2.0f);
            float currentDuration = buff3.DurationTimer;

            // 重新施加，持续时间应保持不变
            _buffManager.CreateBuff(keepDurationBuff, _dummyCreator, _dummyTarget);
            Assert.That(buff3.DurationTimer, Is.EqualTo(currentDuration).Within(0.001f),
                $"Keep策略下，持续时间应保持为{currentDuration}秒，实际为: {buff3.DurationTimer}");

            _buffManager.RemoveAllBuffs(_dummyTarget);
        }

        [Test]
        public void Test_BuffSuperpositionStacks()
        {
            // 1. 测试Add堆叠策略
            var addStacksBuff = new BuffData
            {
                ID = "AddStacksBuff",
                MaxStacks = 5,
                BuffSuperpositionStacksStrategy = BuffSuperpositionStacksType.Add
            };

            var buff1 = _buffManager.CreateBuff(addStacksBuff, _dummyCreator, _dummyTarget);
            Assert.AreEqual(1, buff1.CurrentStacks, "初始堆叠数应为1");

            _buffManager.CreateBuff(addStacksBuff, _dummyCreator, _dummyTarget);
            Assert.AreEqual(2, buff1.CurrentStacks,
                $"Add策略下，堆叠数应为2，实际为: {buff1.CurrentStacks}");

            _buffManager.RemoveAllBuffs(_dummyTarget);

            // 2. 测试Reset堆叠策略
            var resetStacksBuff = new BuffData
            {
                ID = "ResetStacksBuff",
                MaxStacks = 5,
                BuffSuperpositionStacksStrategy = BuffSuperpositionStacksType.Reset
            };

            var buff2 = _buffManager.CreateBuff(resetStacksBuff, _dummyCreator, _dummyTarget);
            _buffManager.CreateBuff(resetStacksBuff, _dummyCreator, _dummyTarget);
            Assert.AreEqual(1, buff2.CurrentStacks,
                $"Reset策略下，堆叠数应重置为1，实际为: {buff2.CurrentStacks}");

            _buffManager.RemoveAllBuffs(_dummyTarget);

            // 3. 测试Keep堆叠策略
            var keepStacksBuff = new BuffData
            {
                ID = "KeepStacksBuff",
                MaxStacks = 5,
                BuffSuperpositionStacksStrategy = BuffSuperpositionStacksType.Keep
            };

            var buff3 = _buffManager.CreateBuff(keepStacksBuff, _dummyCreator, _dummyTarget);
            Assert.AreEqual(1, buff3.CurrentStacks, "初始堆叠数应为1");

            _buffManager.CreateBuff(keepStacksBuff, _dummyCreator, _dummyTarget);
            Assert.AreEqual(1, buff3.CurrentStacks,
                $"Keep策略下，堆叠数应保持为1，实际为: {buff3.CurrentStacks}");

            _buffManager.RemoveAllBuffs(_dummyTarget);

            // 4. 测试ResetThenAdd堆叠策略
            var resetThenAddStacksBuff = new BuffData
            {
                ID = "ResetThenAddStacksBuff",
                MaxStacks = 5,
                BuffSuperpositionStacksStrategy = BuffSuperpositionStacksType.ResetThenAdd
            };

            var buff4 = _buffManager.CreateBuff(resetThenAddStacksBuff, _dummyCreator, _dummyTarget);
            _buffManager.CreateBuff(resetThenAddStacksBuff, _dummyCreator, _dummyTarget);
            Assert.AreEqual(2, buff4.CurrentStacks,
                $"ResetThenAdd策略下，堆叠数应为2，实际为: {buff4.CurrentStacks}");

            _buffManager.RemoveAllBuffs(_dummyTarget);
        }

        [Test]
        public void Test_RemoveBuff()
        {
            // 创建一个有3层堆叠的Buff
            var buffData = new BuffData
            {
                ID = "RemoveTestBuff",
                MaxStacks = 3,
                BuffSuperpositionStacksStrategy = BuffSuperpositionStacksType.Add,
                BuffRemoveStrategy = BuffRemoveType.OneStack
            };

            var buff = _buffManager.CreateBuff(buffData, _dummyCreator, _dummyTarget);
            _buffManager.CreateBuff(buffData, _dummyCreator, _dummyTarget);
            _buffManager.CreateBuff(buffData, _dummyCreator, _dummyTarget);

            Assert.AreEqual(3, buff.CurrentStacks, "初始堆叠数应为3");

            // 测试OneStack移除策略
            _buffManager.RemoveBuff(buff);
            Assert.AreEqual(2, buff.CurrentStacks,
                $"OneStack策略下，移除一次后堆叠数应为2，实际为: {buff.CurrentStacks}");

            buff.BuffData.BuffRemoveStrategy = BuffRemoveType.All;
            _buffManager.RemoveBuff(buff);

            // 验证Buff已完全移除
            bool hasBuff = _buffManager.ContainsBuff(_dummyTarget, "RemoveTestBuff");
            Assert.IsFalse(hasBuff, "All策略下，Buff应该被完全移除!");
        }

        [Test]
        public void Test_RemoveBuffByID()
        {
            // 创建两个不同ID的Buff
            var buffData1 = new BuffData { ID = "Buff1" };
            var buffData2 = new BuffData { ID = "Buff2" };

            _buffManager.CreateBuff(buffData1, _dummyCreator, _dummyTarget);
            _buffManager.CreateBuff(buffData2, _dummyCreator, _dummyTarget);

            // 验证两个Buff都存在
            bool hasBuff1 = _buffManager.ContainsBuff(_dummyTarget, "Buff1");
            bool hasBuff2 = _buffManager.ContainsBuff(_dummyTarget, "Buff2");
            Assert.IsTrue(hasBuff1 && hasBuff2, "两个Buff应该都存在");

            // 通过ID移除Buff1
            _buffManager.RemoveBuffByID(_dummyTarget, "Buff1");

            // 验证Buff1被移除，Buff2仍存在
            hasBuff1 = _buffManager.ContainsBuff(_dummyTarget, "Buff1");
            hasBuff2 = _buffManager.ContainsBuff(_dummyTarget, "Buff2");
            Assert.IsFalse(hasBuff1, "Buff1应该被移除");
            Assert.IsTrue(hasBuff2, "Buff2应该仍然存在");

            // 清理
            _buffManager.RemoveAllBuffs(_dummyTarget);
        }

        [Test]
        public void Test_RemoveBuffsByTag()
        {
            // 创建带有不同标签的Buff
            var buffData1 = new BuffData
            {
                ID = "BuffWithTagA",
                Tags = new List<string> { "TagA", "Common" }
            };

            var buffData2 = new BuffData
            {
                ID = "BuffWithTagB",
                Tags = new List<string> { "TagB", "Common" }
            };

            _buffManager.CreateBuff(buffData1, _dummyCreator, _dummyTarget);
            _buffManager.CreateBuff(buffData2, _dummyCreator, _dummyTarget);

            // 验证两个Buff都存在
            bool hasBuff1 = _buffManager.ContainsBuff(_dummyTarget, "BuffWithTagA");
            bool hasBuff2 = _buffManager.ContainsBuff(_dummyTarget, "BuffWithTagB");
            Assert.IsTrue(hasBuff1 && hasBuff2, "两个Buff应该都存在");

            // 通过TagA移除Buff
            _buffManager.RemoveBuffsByTag(_dummyTarget, "TagA");

            // 验证只有TagA的Buff被移除
            hasBuff1 = _buffManager.ContainsBuff(_dummyTarget, "BuffWithTagA");
            hasBuff2 = _buffManager.ContainsBuff(_dummyTarget, "BuffWithTagB");
            Assert.IsFalse(hasBuff1, "带有TagA的Buff应该被移除");
            Assert.IsTrue(hasBuff2, "带有TagB的Buff应该仍然存在");

            // 通过Common标签移除所有Buff
            _buffManager.RemoveBuffsByTag(_dummyTarget, "Common");

            // 验证所有Buff都被移除
            hasBuff1 = _buffManager.ContainsBuff(_dummyTarget, "BuffWithTagA");
            hasBuff2 = _buffManager.ContainsBuff(_dummyTarget, "BuffWithTagB");
            Assert.IsFalse(hasBuff1 && hasBuff2, "所有带有Common标签的Buff都应被移除");
        }

        [Test]
        public void Test_RemoveBuffsByLayer()
        {
            // 创建带有不同层级的Buff
            var buffData1 = new BuffData
            {
                ID = "BuffInLayerA",
                Layers = new List<string> { "LayerA" }
            };

            var buffData2 = new BuffData
            {
                ID = "BuffInLayerB",
                Layers = new List<string> { "LayerB" }
            };

            var buffData3 = new BuffData
            {
                ID = "BuffInBothLayers",
                Layers = new List<string> { "LayerA", "LayerB" }
            };

            _buffManager.CreateBuff(buffData1, _dummyCreator, _dummyTarget);
            _buffManager.CreateBuff(buffData2, _dummyCreator, _dummyTarget);
            _buffManager.CreateBuff(buffData3, _dummyCreator, _dummyTarget);

            // 验证三个Buff都存在
            bool hasBuff1 = _buffManager.ContainsBuff(_dummyTarget, "BuffInLayerA");
            bool hasBuff2 = _buffManager.ContainsBuff(_dummyTarget, "BuffInLayerB");
            bool hasBuff3 = _buffManager.ContainsBuff(_dummyTarget, "BuffInBothLayers");
            Assert.IsTrue(hasBuff1 && hasBuff2 && hasBuff3, "三个Buff应该都存在");

            // 通过LayerA移除Buff
            _buffManager.RemoveBuffsByLayer(_dummyTarget, "LayerA");

            // 验证LayerA的Buff被移除
            hasBuff1 = _buffManager.ContainsBuff(_dummyTarget, "BuffInLayerA");
            hasBuff2 = _buffManager.ContainsBuff(_dummyTarget, "BuffInLayerB");
            hasBuff3 = _buffManager.ContainsBuff(_dummyTarget, "BuffInBothLayers");
            Assert.IsFalse(hasBuff1, "LayerA中的Buff1应该被移除");
            Assert.IsTrue(hasBuff2, "LayerB中的Buff2应该仍然存在");
            Assert.IsFalse(hasBuff3, "同时在LayerA和LayerB中的Buff3也应该被移除");

            // 清理
            _buffManager.RemoveAllBuffs(_dummyTarget);
        }

        [Test]
        public void Test_HasBuff()
        {
            // 创建一个Buff
            var buffData = new BuffData { ID = "TestHasBuff" };
            _buffManager.CreateBuff(buffData, _dummyCreator, _dummyTarget);

            // 验证HasBuff方法
            bool hasBuff = _buffManager.ContainsBuff(_dummyTarget, "TestHasBuff");
            Assert.IsTrue(hasBuff, "HasBuff应该返回true");

            bool hasNonExistentBuff = _buffManager.ContainsBuff(_dummyTarget, "NonExistentBuff");
            Assert.IsFalse(hasNonExistentBuff, "对于不存在的Buff，HasBuff应该返回false");

            // 移除Buff后再次验证
            _buffManager.RemoveBuffByID(_dummyTarget, "TestHasBuff");
            hasBuff = _buffManager.ContainsBuff(_dummyTarget, "TestHasBuff");
            Assert.IsFalse(hasBuff, "移除后HasBuff应该返回false");
        }

        [Test]
        public void Test_GetBuff()
        {
            // 创建一个Buff
            var buffData = new BuffData
            {
                ID = "TestGetBuff",
                Name = "获取测试Buff"
            };
            _buffManager.CreateBuff(buffData, _dummyCreator, _dummyTarget);

            // 通过GetBuff获取Buff并验证
            Buff buff = _buffManager.GetBuff(_dummyTarget, "TestGetBuff");
            Assert.IsNotNull(buff, "应该能够获取到Buff");
            Assert.AreEqual("TestGetBuff", buff.BuffData.ID, "获取的Buff ID应该匹配");
            Assert.AreEqual("获取测试Buff", buff.BuffData.Name, "获取的Buff名称应该匹配");

            // 测试获取不存在的Buff
            Buff nonExistentBuff = _buffManager.GetBuff(_dummyTarget, "NonExistentBuff");
            Assert.IsNull(nonExistentBuff, "不存在的Buff应该返回null");

            // 清理
            _buffManager.RemoveAllBuffs(_dummyTarget);
        }

        [Test]
        public void Test_GetAllBuffs()
        {
            // 创建多个Buff
            var buffData1 = new BuffData { ID = "Buff1" };
            var buffData2 = new BuffData { ID = "Buff2" };
            var buffData3 = new BuffData { ID = "Buff3" };

            _buffManager.CreateBuff(buffData1, _dummyCreator, _dummyTarget);
            _buffManager.CreateBuff(buffData2, _dummyCreator, _dummyTarget);
            _buffManager.CreateBuff(buffData3, _dummyCreator, _dummyTarget);

            // 获取所有Buff并验证
            List<Buff> allBuffs = _buffManager.GetTargetBuffs(_dummyTarget);
            Assert.AreEqual(3, allBuffs.Count, $"应该有3个Buff，实际有: {allBuffs.Count}");

            // 验证Buff ID
            bool hasBuff1 = allBuffs.Exists(b => b.BuffData.ID == "Buff1");
            bool hasBuff2 = allBuffs.Exists(b => b.BuffData.ID == "Buff2");
            bool hasBuff3 = allBuffs.Exists(b => b.BuffData.ID == "Buff3");
            Assert.IsTrue(hasBuff1 && hasBuff2 && hasBuff3, "应该包含所有添加的Buff");

            // 移除一个Buff后再次验证
            _buffManager.RemoveBuffByID(_dummyTarget, "Buff1");
            allBuffs = _buffManager.GetTargetBuffs(_dummyTarget);
            Assert.AreEqual(2, allBuffs.Count, $"移除后应该有2个Buff，实际有: {allBuffs.Count}");

            // 清理
            _buffManager.RemoveAllBuffs(_dummyTarget);

            // 验证空列表
            allBuffs = _buffManager.GetTargetBuffs(_dummyTarget);
            Assert.AreEqual(0, allBuffs.Count, "移除所有后应该返回空列表");
        }

        [Test]
        public void Test_BuffUpdate()
        {
            // 创建一个带触发间隔的Buff
            var buffData = new BuffData
            {
                ID = "TickingBuff",
                Duration = -1f, // 无限持续时间
                TriggerInterval = 2.0f, // 每2秒触发一次
            };

            // 添加触发事件计数器
            int triggerCount = 0;
            var buff = _buffManager.CreateBuff(buffData, _dummyCreator, _dummyTarget);
            buff.OnTrigger += (b) => triggerCount++;

            // 验证初始状态
            Assert.AreEqual(0, triggerCount, "初始触发计数应为0");
            Assert.That(buff.TriggerTimer, Is.EqualTo(2.0f).Within(0.001f), "初始触发计时器应为2.0秒");

            // 更新1秒
            _buffManager.Update(1.0f);
            Assert.AreEqual(0, triggerCount, "1秒后不应触发");
            Assert.That(buff.TriggerTimer, Is.EqualTo(1.0f).Within(0.001f), "1秒后触发计时器应为1.0秒");

            // 再更新1秒，应该触发一次
            _buffManager.Update(1.0f);
            Assert.AreEqual(1, triggerCount, "2秒后应触发一次");
            Assert.That(buff.TriggerTimer, Is.EqualTo(2.0f).Within(0.001f), "触发后计时器应重置为2.0秒");

            // 更新3秒，应该再次触发
            _buffManager.Update(3.0f);
            Assert.AreEqual(2, triggerCount, "再经过3秒后应再次触发");

            // 清理
            _buffManager.RemoveAllBuffs(_dummyTarget);
        }

        [Test]
        public void Test_BuffAutoExpire()
        {
            // 创建一个3秒持续时间的Buff
            var buffData = new BuffData
            {
                ID = "ExpiringBuff",
                Duration = 3.0f
            };

            // 添加Buff
            _buffManager.CreateBuff(buffData, _dummyCreator, _dummyTarget);

            // 验证Buff存在
            bool hasBuff = _buffManager.ContainsBuff(_dummyTarget, "ExpiringBuff");
            Assert.IsTrue(hasBuff, "Buff应该已添加");

            // 更新2秒
            _buffManager.Update(2.0f);
            hasBuff = _buffManager.ContainsBuff(_dummyTarget, "ExpiringBuff");
            Assert.IsTrue(hasBuff, "2秒后Buff应该仍然存在");

            // 再更新2秒，应该过期
            _buffManager.Update(2.0f);
            hasBuff = _buffManager.ContainsBuff(_dummyTarget, "ExpiringBuff");
            Assert.IsFalse(hasBuff, "4秒后Buff应该已过期被自动移除");
        }

        [Test]
        public void Test_BuffRemovalStopsUpdates()
        {
            // 定时Buff移除后不应继续更新
            var timedBuffData = new BuffData
            {
                ID = "TimedRemovalBuff",
                Duration = 5.0f,
                TriggerInterval = 0.5f
            };

            int timedUpdateCount = 0;
            var timedBuff = _buffManager.CreateBuff(timedBuffData, _dummyCreator, _dummyTarget);
            timedBuff.OnUpdate += _ => timedUpdateCount++;

            _buffManager.Update(0.5f);
            Assert.Greater(timedUpdateCount, 0, "更新后应至少触发一次定时Buff更新");

            _buffManager.RemoveBuff(timedBuff);
            int timedCountBefore = timedUpdateCount;

            _buffManager.Update(1.0f);
            Assert.AreEqual(timedCountBefore, timedUpdateCount,
                $"移除定时Buff后不应再触发更新，移除前:{timedCountBefore}，移除后:{timedUpdateCount}");

            // 永久Buff移除后不应继续更新
            var permanentBuffData = new BuffData
            {
                ID = "PermanentRemovalBuff",
                Duration = -1f,
                TriggerInterval = 0.5f
            };

            int permanentUpdateCount = 0;
            var permanentBuff = _buffManager.CreateBuff(permanentBuffData, _dummyCreator, _dummyTarget);
            permanentBuff.OnUpdate += _ => permanentUpdateCount++;

            _buffManager.Update(0.5f);
            Assert.Greater(permanentUpdateCount, 0, "更新后应至少触发一次永久Buff更新");

            _buffManager.RemoveBuff(permanentBuff);
            int permanentCountBefore = permanentUpdateCount;

            _buffManager.Update(1.0f);
            Assert.AreEqual(permanentCountBefore, permanentUpdateCount,
                $"移除永久Buff后不应再触发更新，移除前:{permanentCountBefore}，移除后:{permanentUpdateCount}");

            _buffManager.RemoveAllBuffs(_dummyTarget);
        }

        [Test]
        public void Test_CastModifierToProperty_Add()
        {
            // 1. 创建和注册一个GameProperty
            const string propertyId = "TestAddProperty";
            const float baseValue = 100f;
            var property = new GameProperty(propertyId, baseValue);
            _gamePropertyManager.Register(property);

            // 2. 创建一个Add修饰器
            const float addValue = 50f;
            var modifier = new FloatModifier(ModifierType.Add, 0, addValue);

            // 3. 创建CastModifierToProperty模块
            var castModule = new CastModifierToProperty(modifier, propertyId, _gamePropertyManager);

            // 4. 创建BuffData并添加模块
            var buffData = new BuffData
            {
                ID = "AddModifierBuff",
                Duration = -1f,
                BuffModules = new List<BuffModule> { castModule },
                TriggerOnCreate = true
            };

            // 5. 添加Buff前验证属性初始值
            float initialValue = property.GetValue();
            Assert.That(initialValue, Is.EqualTo(baseValue).Within(0.001f),
                $"属性初始值应为{baseValue}，实际为: {initialValue}");

            // 6. 添加Buff并验证修饰器效果
            var buff = _buffManager.CreateBuff(buffData, _dummyCreator, _dummyTarget);
            float modifiedValue = property.GetValue();
            Assert.That(modifiedValue, Is.EqualTo(baseValue + addValue).Within(0.001f),
                $"添加Add修饰器后值应为{baseValue + addValue}，实际为: {modifiedValue}");

            // 7. 移除Buff并验证修饰器被移除
            _buffManager.RemoveBuff(buff);
            float valueAfterRemoval = property.GetValue();
            Assert.That(valueAfterRemoval, Is.EqualTo(baseValue).Within(0.001f),
                $"移除Buff后值应恢复为{baseValue}，实际为: {valueAfterRemoval}");

            // 清理
            _gamePropertyManager.Unregister(propertyId);
        }

        [Test]
        public void Test_CastModifierToProperty_Mul()
        {
            // 1. 创建和注册一个GameProperty
            const string propertyId = "TestMulProperty";
            const float baseValue = 100f;
            var property = new GameProperty(propertyId, baseValue);
            _gamePropertyManager.Register(property);

            // 2. 创建一个Mul修饰器
            const float mulFactor = 1.5f;
            var modifier = new FloatModifier(ModifierType.Mul, 0, mulFactor);

            // 3. 创建CastModifierToProperty模块
            var castModule = new CastModifierToProperty(modifier, propertyId, _gamePropertyManager);

            // 4. 创建BuffData并添加模块
            var buffData = new BuffData
            {
                ID = "MulModifierBuff",
                Duration = -1f,
                MaxStacks = 2, // 允许2层堆叠
                BuffModules = new List<BuffModule> { castModule }
            };

            // 5. 添加Buff前验证属性初始值
            float initialValue = property.GetValue();
            Assert.That(initialValue, Is.EqualTo(baseValue).Within(0.001f),
                $"属性初始值应为{baseValue}，实际为: {initialValue}");

            // 6. 添加Buff并验证修饰器效果
            var buff = _buffManager.CreateBuff(buffData, _dummyCreator, _dummyTarget);
            float modifiedValue = property.GetValue();
            Assert.That(modifiedValue, Is.EqualTo(baseValue * mulFactor).Within(0.001f),
                $"添加Mul修饰器后值应为{baseValue * mulFactor}，实际为: {modifiedValue}");

            // 7. 添加第二层堆叠并验证效果
            _buffManager.CreateBuff(buffData, _dummyCreator, _dummyTarget);
            float valueWithTwoStacks = property.GetValue();
            Assert.That(valueWithTwoStacks, Is.EqualTo(baseValue * mulFactor * mulFactor).Within(0.001f),
                $"两层Mul修饰器后值应为{baseValue * mulFactor * mulFactor}，实际为: {valueWithTwoStacks}");

            // 8. 移除一层堆叠并验证效果
            buff.BuffData.BuffRemoveStrategy = BuffRemoveType.OneStack;
            _buffManager.RemoveBuff(buff);
            float valueAfterOneStackRemoval = property.GetValue();
            Assert.That(valueAfterOneStackRemoval, Is.EqualTo(baseValue * mulFactor).Within(0.001f),
                $"移除一层后值应为{baseValue * mulFactor}，实际为: {valueAfterOneStackRemoval}");

            // 9. 完全移除Buff并验证属性恢复
            buff.BuffData.BuffRemoveStrategy = BuffRemoveType.All;
            _buffManager.RemoveBuff(buff);
            float valueAfterRemoval = property.GetValue();
            Assert.That(valueAfterRemoval, Is.EqualTo(baseValue).Within(0.001f),
                $"完全移除后值应恢复为{baseValue}，实际为: {valueAfterRemoval}");

            // 清理
            _gamePropertyManager.Unregister(propertyId);
        }

        [Test]
        public void Test_BuffModule_CustomCallback()
        {
            // 创建一个自定义BuffModule
            var customModule = new CustomTestBuffModule();

            // 创建BuffData并添加模块
            var buffData = new BuffData
            {
                ID = "CustomCallbackBuff",
                BuffModules = new List<BuffModule> { customModule }
            };

            // 添加Buff
            var buff = _buffManager.CreateBuff(buffData, _dummyCreator, _dummyTarget);

            // 验证OnCreate回调被执行
            Assert.AreEqual(1, customModule.CreateCallCount,
                $"OnCreate回调应被执行1次，实际为: {customModule.CreateCallCount}次");

            // 执行自定义回调
            customModule.ExecuteCustomAction(buff, "TestCustomAction", 42);
            Assert.AreEqual(42, customModule.CustomActionValue,
                $"自定义回调应设置值为42，实际为: {customModule.CustomActionValue}");

            // 移除Buff并验证OnRemove回调被执行
            _buffManager.RemoveBuff(buff);
            Assert.AreEqual(1, customModule.RemoveCallCount,
                $"OnRemove回调应被执行1次，实际为: {customModule.RemoveCallCount}次");
        }

        // 用于测试自定义回调的BuffModule
        private class CustomTestBuffModule : BuffModule
        {
            public int CreateCallCount { get; private set; }
            public int RemoveCallCount { get; private set; }
            public int CustomActionValue { get; private set; }

            public CustomTestBuffModule()
            {
                // 注册标准回调
                RegisterCallback(BuffCallBackType.OnCreate, OnCreate);
                RegisterCallback(BuffCallBackType.OnRemove, OnRemove);

                // 注册自定义回调
                RegisterCallback("TestCustomAction", OnTestCustomAction);
            }

            private void OnCreate(Buff buff, object[] parameters)
            {
                CreateCallCount++;
            }

            private void OnRemove(Buff buff, object[] parameters)
            {
                RemoveCallCount++;
            }

            private void OnTestCustomAction(Buff buff, object[] parameters)
            {
                if (parameters != null && parameters.Length > 0 && parameters[0] is int value)
                {
                    CustomActionValue = value;
                }
            }

            public void ExecuteCustomAction(Buff buff, string actionName, int value)
            {
                Execute(buff, BuffCallBackType.Custom, actionName, new object[] { value });
            }
        }
    }
}