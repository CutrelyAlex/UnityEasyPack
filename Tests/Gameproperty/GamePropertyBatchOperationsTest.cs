using System;
using System.Linq;
using NUnit.Framework;
using EasyPack.Architecture;
using EasyPack.GamePropertySystem;
using EasyPack.Modifiers;

namespace EasyPack.GamepropertyTests
{
    /// <summary>
    /// 测试GamePropertyManager的批量操作功能和容错机制
    /// </summary>
    [TestFixture]
    public class TestBatchOperations
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

        #region 批量设置激活状态测试

        [Test]
        public void Test_批量设置激活状态_全部成功()
        {
            _manager.Register(new GameProperty("hp", 100), "Character.Vital");
            _manager.Register(new GameProperty("mp", 50), "Character.Vital");

            var result = _manager.SetCategoryActive("Character.Vital", false);

            Assert.IsTrue(result.IsFullSuccess);
            Assert.AreEqual(2, result.SuccessCount);
            Assert.AreEqual(0, result.Failures.Count);
            Assert.IsTrue(result.SuccessData.Contains("hp"));
            Assert.IsTrue(result.SuccessData.Contains("mp"));
        }

        [Test]
        public void Test_批量设置激活状态_分类不存在()
        {
            var result = _manager.SetCategoryActive("NonExistent", true);

            Assert.IsFalse(result.IsFullSuccess);
            Assert.AreEqual(0, result.SuccessCount);
            Assert.AreEqual(1, result.Failures.Count);
            Assert.AreEqual(FailureType.CategoryNotFound, result.Failures[0].Type);
        }

        #endregion

        #region 批量应用修饰符测试

        [Test]
        public void Test_批量应用修饰符_全部成功()
        {
            _manager.Register(new GameProperty("hp", 100), "Character.Vital");
            _manager.Register(new GameProperty("mp", 50), "Character.Vital");

            var modifier = new FloatModifier(ModifierType.Add, 0, 10);
            var result = _manager.ApplyModifierToCategory("Character.Vital", modifier);

            Assert.IsTrue(result.IsFullSuccess);
            Assert.AreEqual(2, result.SuccessCount);
            Assert.AreEqual(0, result.Failures.Count);

            // 验证修饰符已应用
            var hp = _manager.Get("hp");
            var mp = _manager.Get("mp");
            Assert.AreEqual(110, hp.GetValue());
            Assert.AreEqual(60, mp.GetValue());
        }
        
        [Test]
        public void Test_批量应用修饰符_分类不存在()
        {
            var modifier = new FloatModifier(ModifierType.Add, 0, 10);
            var result = _manager.ApplyModifierToCategory("NonExistent", modifier);

            Assert.IsFalse(result.IsFullSuccess);
            Assert.AreEqual(0, result.SuccessCount);
            Assert.AreEqual(1, result.Failures.Count);
            Assert.AreEqual(FailureType.CategoryNotFound, result.Failures[0].Type);
        }

        [Test]
        public void Test_批量应用修饰符_空修饰符抛出异常()
        {
            _manager.Register(new GameProperty("hp", 100), "Character");

            Assert.Throws<System.ArgumentNullException>(() =>
                _manager.ApplyModifierToCategory("Character", null));
        }

        #endregion

        #region 批量注册测试

        [Test]
        public void Test_批量注册_全部成功()
        {
            var properties = new[]
            {
                new GameProperty("hp", 100),
                new GameProperty("mp", 50),
                new GameProperty("stamina", 80)
            };

            _manager.RegisterRange(properties, "Character.Resources");

            Assert.AreEqual(3, _manager.GetByCategory("Character.Resources").Count());
        }

        [Test]
        public void Test_批量注册_空集合不报错()
        {
            _manager.RegisterRange(Array.Empty<GameProperty>(), "Category");
            Assert.AreEqual(0, _manager.GetByCategory("Category").Count());
        }

        #endregion

        #region 容错机制测试

        [Test]
        public void Test_部分成功策略_继续处理后续项()
        {
            // 注册3个属性
            _manager.Register(new GameProperty("prop1", 10), "TestCategory");
            _manager.Register(new GameProperty("prop2", 20), "TestCategory");
            _manager.Register(new GameProperty("prop3", 30), "TestCategory");

            // 应用修饰符（全部应该成功）
            var modifier = new FloatModifier(ModifierType.Add, 0, 5);
            var result = _manager.ApplyModifierToCategory("TestCategory", modifier);

            // 验证全部成功
            Assert.IsTrue(result.IsFullSuccess);
            Assert.AreEqual(3, result.SuccessCount);

            // 验证所有属性都被修改
            Assert.AreEqual(15, _manager.Get("prop1").GetValue());
            Assert.AreEqual(25, _manager.Get("prop2").GetValue());
            Assert.AreEqual(35, _manager.Get("prop3").GetValue());
        }

        [Test]
        public void Test_操作结果包含详细失败信息()
        {
            var result = _manager.SetCategoryActive("NonExistent", true);

            Assert.IsFalse(result.IsFullSuccess);
            Assert.AreEqual(1, result.Failures.Count);

            var failure = result.Failures[0];
            Assert.IsNotNull(failure.ItemId);
            Assert.IsNotNull(failure.ErrorMessage);
            Assert.AreEqual(FailureType.CategoryNotFound, failure.Type);
        }

        #endregion

        #region 性能测试（基础）

        [Test]
        public void Test_批量操作_100个属性性能()
        {
            // 注册100个属性
            for (int i = 0; i < 100; i++)
            {
                _manager.Register(new GameProperty($"prop_{i}", i), "LargeCategory");
            }

            var startTime = System.DateTime.Now;
            var modifier = new FloatModifier(ModifierType.Mul, 0, 1.1f);
            var result = _manager.ApplyModifierToCategory("LargeCategory", modifier);
            var elapsed = (System.DateTime.Now - startTime).TotalMilliseconds;

            Assert.IsTrue(result.IsFullSuccess);
            Assert.AreEqual(100, result.SuccessCount);
            Assert.Less(elapsed, 100, "批量操作100个属性应在100ms内完成");
        }

        #endregion
    }
}
