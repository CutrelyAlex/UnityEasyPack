using System;
using NUnit.Framework;
using EasyPack.Architecture;
using EasyPack.GamePropertySystem;
using EasyPack.Modifiers;
using UnityEngine;

namespace EasyPack.GamepropertyTests
{
    /// <summary>
    /// GameProperty 核心功能测试
    /// 涵盖属性添加、Modifier修改和复杂依赖
    /// </summary>
    [TestFixture]
    public class GamePropertyTest
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

        #region 基础属性测试

        [Test]
        public void Test_创建属性_初始值正确()
        {
            var property = new GameProperty("hp", 100);

            Assert.AreEqual("hp", property.ID);
            Assert.AreEqual(100, property.GetBaseValue());
            Assert.AreEqual(100, property.GetValue());
        }

        [Test]
        public void Test_设置基础值_值正确更新()
        {
            var property = new GameProperty("hp", 100);
            property.SetBaseValue(200);

            Assert.AreEqual(200, property.GetBaseValue());
            Assert.AreEqual(200, property.GetValue());
        }

        [Test]
        public void Test_值变化事件_正确触发()
        {
            var property = new GameProperty("hp", 100);
            float oldValue = 0, newValue = 0;

            property.OnValueChanged += (o, n) =>
            {
                oldValue = o;
                newValue = n;
            };

            property.SetBaseValue(200);

            Assert.AreEqual(100, oldValue);
            Assert.AreEqual(200, newValue);
        }

        [Test]
        public void Test_基础值变化事件_正确触发()
        {
            var property = new GameProperty("hp", 100);
            float oldBase = 0, newBase = 0;

            property.OnBaseValueChanged += (o, n) =>
            {
                oldBase = o;
                newBase = n;
            };

            property.SetBaseValue(150);

            Assert.AreEqual(100, oldBase);
            Assert.AreEqual(150, newBase);
        }

        #endregion

        #region Modifier修饰符测试

        [Test]
        public void Test_添加Add修饰符_值正确计算()
        {
            var property = new GameProperty("hp", 100);
            var modifier = new FloatModifier(ModifierType.Add, 1, 50f);

            property.AddModifier(modifier);

            Assert.AreEqual(150, property.GetValue());
        }

        [Test]
        public void Test_添加Mul修饰符_值正确计算()
        {
            var property = new GameProperty("hp", 100);
            var modifier = new FloatModifier(ModifierType.Mul, 1, 1.5f);

            property.AddModifier(modifier);

            Assert.AreEqual(150, property.GetValue());
        }

        [Test]
        public void Test_多个修饰符_按优先级计算()
        {
            var property = new GameProperty("hp", 100);
            // Add优先级1，Mul优先级2
            property.AddModifier(new FloatModifier(ModifierType.Add, 1, 50f));  // 100 + 50 = 150
            property.AddModifier(new FloatModifier(ModifierType.Mul, 2, 2f));   // 150 * 2 = 300

            Assert.AreEqual(300, property.GetValue());
        }

        [Test]
        public void Test_移除修饰符_值正确恢复()
        {
            var property = new GameProperty("hp", 100);
            var modifier = new FloatModifier(ModifierType.Add, 1, 50f);

            property.AddModifier(modifier);
            Assert.AreEqual(150, property.GetValue());

            property.RemoveModifier(modifier);
            Assert.AreEqual(100, property.GetValue());
        }

        [Test]
        public void Test_清除所有修饰符_值恢复基础值()
        {
            var property = new GameProperty("hp", 100);
            property.AddModifier(new FloatModifier(ModifierType.Add, 1, 50f));
            property.AddModifier(new FloatModifier(ModifierType.Mul, 2, 2f));

            property.ClearModifiers();

            Assert.AreEqual(100, property.GetValue());
        }

        [Test]
        public void Test_PriorityAdd修饰符_优先于Mul应用()
        {
            var property = new GameProperty("hp", 100);
            // Add -> PriorityAdd -> Mul
            property.AddModifier(new FloatModifier(ModifierType.Add, 1, 50f));          // 100 + 50 = 150
            property.AddModifier(new FloatModifier(ModifierType.PriorityAdd, 3, 100f)); // 150 + 100 = 250
            property.AddModifier(new FloatModifier(ModifierType.Mul, 2, 2f));           // 250 * 2 = 500

            Assert.AreEqual(500, property.GetValue());
        }

        [Test]
        public void Test_AfterAdd修饰符_在Mul后应用()
        {
            var property = new GameProperty("hp", 100);
            // Add -> Mul -> AfterAdd
            property.AddModifier(new FloatModifier(ModifierType.Add, 1, 50f));          // 100 + 50 = 150
            property.AddModifier(new FloatModifier(ModifierType.Mul, 2, 2f));           // 150 * 2 = 300
            property.AddModifier(new FloatModifier(ModifierType.AfterAdd, 3, 100f));    // 300 + 100 = 400

            Assert.AreEqual(400, property.GetValue());
        }

        #endregion

        #region 单依赖测试

        [Test]
        public void Test_单依赖_基本计算()
        {
            var strength = new GameProperty("strength", 10);
            var damage = new GameProperty("damage", 0);

            damage.AddDependency(strength, (dep, val) => val * 2);

            Assert.AreEqual(20, damage.GetValue()); // 10 * 2 = 20
        }

        [Test]
        public void Test_单依赖_依赖变化自动更新()
        {
            var strength = new GameProperty("strength", 10);
            var damage = new GameProperty("damage", 0);

            damage.AddDependency(strength, (dep, val) => val * 2);
            Assert.AreEqual(20, damage.GetValue());

            strength.SetBaseValue(20);
            Assert.AreEqual(40, damage.GetValue()); // 20 * 2 = 40
        }

        [Test]
        public void Test_多个单依赖_最后一个生效()
        {
            var baseDamage = new GameProperty("baseDamage", 50);
            var strength = new GameProperty("strength", 10);
            var finalDamage = new GameProperty("finalDamage", 0);

            // 注意：使用单依赖API时，每个依赖独立计算，最后设置的值会覆盖
            finalDamage.AddDependency(baseDamage, (dep, val) => val);
            finalDamage.AddDependency(strength, (dep, val) => val * 2);

            // strength的calculator是最后应用的，所以结果是 10 * 2 = 20
            Assert.AreEqual(20, finalDamage.GetValue());
        }

        [Test]
        public void Test_移除依赖_停止更新()
        {
            var strength = new GameProperty("strength", 10);
            var damage = new GameProperty("damage", 0);

            damage.AddDependency(strength, (dep, val) => val * 2);
            Assert.AreEqual(20, damage.GetValue());

            damage.RemoveDependency(strength);
            strength.SetBaseValue(100);

            // 移除依赖后，值保持不变
            Assert.AreEqual(20, damage.GetValue());
        }

        #endregion

        #region 多依赖复杂表达式测试

        [Test]
        public void Test_多依赖_加法表达式()
        {
            var baseAtk = new GameProperty("baseAtk", 100);
            var bonusAtk = new GameProperty("bonusAtk", 50);
            var totalAtk = new GameProperty("totalAtk", 0);

            totalAtk.AddDependencies(
                new[] { baseAtk, bonusAtk },
                () => baseAtk.GetValue() + bonusAtk.GetValue()
            );

            Assert.AreEqual(150, totalAtk.GetValue()); // 100 + 50 = 150
        }

        [Test]
        public void Test_多依赖_乘法表达式()
        {
            var baseAtk = new GameProperty("baseAtk", 100);
            var multiplier = new GameProperty("multiplier", 1.5f);
            var finalAtk = new GameProperty("finalAtk", 0);

            finalAtk.AddDependencies(
                new[] { baseAtk, multiplier },
                () => baseAtk.GetValue() * multiplier.GetValue()
            );

            Assert.AreEqual(150, finalAtk.GetValue()); // 100 * 1.5 = 150
        }

        [Test]
        public void Test_多依赖_指数表达式()
        {
            var baseAtk = new GameProperty("baseAtk", 100);
            var critRate = new GameProperty("critRate", 0.5f);
            var critMultiplier = new GameProperty("critMultiplier", 2f);
            var damage = new GameProperty("damage", 0);

            // damage = baseAtk * (1 + critRate) ^ critMultiplier
            damage.AddDependencies(
                new[] { baseAtk, critRate, critMultiplier },
                () => baseAtk.GetValue() * Mathf.Pow(1 + critRate.GetValue(), critMultiplier.GetValue())
            );

            // 100 * (1 + 0.5)^2 = 100 * 2.25 = 225
            Assert.AreEqual(225, damage.GetValue(), 0.01f);
        }

        [Test]
        public void Test_多依赖_指数衰减表达式()
        {
            var baseValue = new GameProperty("baseValue", 100);
            var decayRate = new GameProperty("decayRate", 0.1f);
            var time = new GameProperty("time", 5f);
            var decayedValue = new GameProperty("decayedValue", 0);

            // value = base * e^(-decay * time)
            decayedValue.AddDependencies(
                new[] { baseValue, decayRate, time },
                () => baseValue.GetValue() * Mathf.Exp(-decayRate.GetValue() * time.GetValue())
            );

            // 100 * e^(-0.1 * 5) = 100 * e^(-0.5) ≈ 60.65
            Assert.AreEqual(60.65f, decayedValue.GetValue(), 0.1f);
        }

        [Test]
        public void Test_多依赖_加权求和表达式()
        {
            var str = new GameProperty("str", 10);
            var agi = new GameProperty("agi", 15);
            var intel = new GameProperty("intel", 20);
            var totalPower = new GameProperty("totalPower", 0);

            // totalPower = str * 2 + agi * 1.5 + intel * 1
            totalPower.AddDependencies(
                new[] { str, agi, intel },
                () => str.GetValue() * 2.0f + agi.GetValue() * 1.5f + intel.GetValue() * 1.0f
            );

            // 10*2 + 15*1.5 + 20*1 = 20 + 22.5 + 20 = 62.5
            Assert.AreEqual(62.5f, totalPower.GetValue(), 0.01f);
        }

        [Test]
        public void Test_多依赖_任一依赖变化触发更新()
        {
            var a = new GameProperty("a", 10);
            var b = new GameProperty("b", 20);
            var c = new GameProperty("c", 30);
            var result = new GameProperty("result", 0);

            result.AddDependencies(
                new[] { a, b, c },
                () => a.GetValue() + b.GetValue() + c.GetValue()
            );

            Assert.AreEqual(60, result.GetValue()); // 10 + 20 + 30 = 60

            // 修改 a
            a.SetBaseValue(100);
            Assert.AreEqual(150, result.GetValue()); // 100 + 20 + 30 = 150

            // 修改 b
            b.SetBaseValue(200);
            Assert.AreEqual(330, result.GetValue()); // 100 + 200 + 30 = 330

            // 修改 c
            c.SetBaseValue(300);
            Assert.AreEqual(600, result.GetValue()); // 100 + 200 + 300 = 600
        }

        [Test]
        public void Test_多依赖_复杂公式()
        {
            var level = new GameProperty("level", 10);
            var baseHp = new GameProperty("baseHp", 100);
            var constitution = new GameProperty("constitution", 15);
            var maxHp = new GameProperty("maxHp", 0);

            // maxHp = baseHp + level * 10 + constitution * 5
            maxHp.AddDependencies(
                new[] { level, baseHp, constitution },
                () => baseHp.GetValue() + level.GetValue() * 10 + constitution.GetValue() * 5
            );

            // 100 + 10*10 + 15*5 = 100 + 100 + 75 = 275
            Assert.AreEqual(275, maxHp.GetValue());

            // 升级
            level.SetBaseValue(20);
            // 100 + 20*10 + 15*5 = 100 + 200 + 75 = 375
            Assert.AreEqual(375, maxHp.GetValue());
        }

        #endregion

        #region 循环依赖检测

        [Test]
        public void Test_自引用依赖_被阻止()
        {
            var property = new GameProperty("test", 100);

            // 自引用应该被阻止
            var result = property.DependencyManager.AddDependency(property, (dep, val) => val * 2);

            Assert.IsFalse(result);
        }

        [Test]
        public void Test_循环依赖_被检测并阻止()
        {
            var a = new GameProperty("a", 10);
            var b = new GameProperty("b", 20);
            var c = new GameProperty("c", 30);

            // a -> b -> c -> a (循环)
            a.AddDependency(b, (dep, val) => val);
            b.AddDependency(c, (dep, val) => val);

            // 尝试创建循环: c -> a
            var result = c.DependencyManager.AddDependency(a, (dep, val) => val);

            Assert.IsFalse(result);
        }

        #endregion

        #region 依赖与修饰符组合测试

        [Test]
        public void Test_依赖属性带修饰符_正确计算()
        {
            var strength = new GameProperty("strength", 10);
            strength.AddModifier(new FloatModifier(ModifierType.Add, 1, 5f));  // 10 + 5 = 15

            var damage = new GameProperty("damage", 0);
            damage.AddDependency(strength, (dep, val) => val * 2);

            Assert.AreEqual(30, damage.GetValue()); // 15 * 2 = 30
        }

        [Test]
        public void Test_派生属性带修饰符_正确计算()
        {
            var strength = new GameProperty("strength", 10);
            var damage = new GameProperty("damage", 0);

            damage.AddDependency(strength, (dep, val) => val * 2);
            damage.AddModifier(new FloatModifier(ModifierType.Add, 1, 10f));  // 基础值20 + 10 = 30

            Assert.AreEqual(30, damage.GetValue());
        }

        #endregion

        #region Manager集成测试

        [Test]
        public void Test_Manager注册依赖属性_正常工作()
        {
            var baseDamage = new GameProperty("baseDamage", 50);
            var strength = new GameProperty("strength", 10);
            var finalDamage = new GameProperty("finalDamage", 0);

            finalDamage.AddDependencies(
                new[] { baseDamage, strength },
                () => baseDamage.GetValue() + strength.GetValue() * 2
            );

            _manager.Register(baseDamage, "Character.Base");
            _manager.Register(strength, "Character.Base");
            _manager.Register(finalDamage, "Character.Derived");

            // 50 + 10*2 = 70
            Assert.AreEqual(70, _manager.Get("finalDamage").GetValue());

            // 修改strength
            _manager.Get("strength").SetBaseValue(20);
            // 50 + 20*2 = 90
            Assert.AreEqual(90, _manager.Get("finalDamage").GetValue());
        }

        [Test]
        public void Test_Manager批量应用修饰符到依赖属性()
        {
            var strength = new GameProperty("strength", 10);
            var damage = new GameProperty("damage", 0);

            damage.AddDependency(strength, (dep, val) => val * 2);

            _manager.Register(strength, "Character.Base");
            _manager.Register(damage, "Character.Derived");

            // 应用修饰符到strength
            var modifier = new FloatModifier(ModifierType.Add, 0, 5);
            var result = _manager.ApplyModifierToCategory("Character.Base", modifier);

            Assert.IsTrue(result.IsFullSuccess);

            // strength: 10 + 5 = 15, damage: 15 * 2 = 30
            Assert.AreEqual(15, _manager.Get("strength").GetValue());
            Assert.AreEqual(30, _manager.Get("damage").GetValue());
        }

        #endregion
    }
}
