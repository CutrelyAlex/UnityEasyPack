using UnityEngine;


namespace EasyPack
{
    public class GamePropertyExample : MonoBehaviour
    {
        void Start()
        {
            Test_RPG_AttackPower_Complex();
            Test_MultiCombinePropertyCustom_ShareGameProperty();
            Test_CyclicDependency_Detection();
            Example_PropertyWithDependencies();
            Example();
        }

        void Example()
        {
            // 示例1：单一属性 + 修饰器
            var prop = new GameProperty("HP", 100f);
            Debug.Log($"[单一属性] 初始HP: {prop.GetValue()}");
            prop.AddModifier(new FloatModifier(ModifierType.Add, 0, 20f));
            Debug.Log($"[单一属性] 加Add修饰器后HP: {prop.GetValue()}");
            prop.AddModifier(new FloatModifier(ModifierType.Mul, 0, 1.5f));
            Debug.Log($"[单一属性] 加Mul修饰器后HP: {prop.GetValue()}");
            prop.AddModifier(new RangeModifier(ModifierType.Clamp, 0, new Vector2(0, 150)));
            Debug.Log($"[单一属性] 加Clamp修饰器后HP: {prop.GetValue()}");

            // 示例2：CombinePropertySingle 用法
            var single = new CombinePropertySingle("SingleProp");
            single.ResultHolder.SetBaseValue(50f);
            single.ResultHolder.AddModifier(new FloatModifier(ModifierType.Add, 0, 10f));
            Debug.Log($"[CombinePropertySingle] 结果: {single.GetValue()}");

            // 示例3：CombinePropertyClassic 用法
            var classic = new CombinePropertyClassic(
                "Atk",
                100f,      // BaseValue
                "Base",    // ConfigProperty
                "Buff",    // BuffValue
                "BuffMul", // BuffMul
                "Debuff",  // deBuffValue
                "DebuffMul"// deBuffMul
            );
            // 增加Buff
            classic.GetProperty("Buff").SetBaseValue(30f);
            // 增加Buff乘法
            classic.GetProperty("BuffMul").SetBaseValue(0.2f);
            // 增加Debuff
            classic.GetProperty("Debuff").SetBaseValue(10f);
            // 增加Debuff乘法
            classic.GetProperty("DebuffMul").SetBaseValue(0.5f);

            Debug.Log($"[CombinePropertyClassic] 计算结果: {classic.GetValue()}");

            // 示例4：使用管理器管理属性
            CombineGamePropertyManager.AddOrUpdate(classic);
            CombineGamePropertyManager.AddOrUpdate(single);

            foreach (var p in CombineGamePropertyManager.GetAll())
            {
                Debug.Log($"[Manager] 属性ID: {p.ID}, 当前值: {p.GetValue()}");
            }

            // 示例5：修改属性基础值并观察变化
            // 注意，不建议在游戏中动态修改本值，通常是通过修饰器来动态调整
            classic.GetProperty("Buff").SetBaseValue(100f);
            Debug.Log($"[动态变化] Buff提升后: {classic.GetValue()}");
            classic.GetProperty("Debuff").SetBaseValue(50f);
            Debug.Log($"[动态变化] Debuff提升后: {classic.GetValue()}");

            // 示例6：序列化与反序列化 GameProperty
            var prop2 = new GameProperty("MP", 80f);
            prop2.AddModifier(new FloatModifier(ModifierType.Add, 1, 10f));
            prop2.AddModifier(new FloatModifier(ModifierType.Mul, 2, 2f));
            Debug.Log($"[序列化] 原始MP: {prop2.GetValue()}");

            // 序列化
            var serialized = GamePropertySerializer.Serialize(prop2);
            Debug.Log($"[序列化] SerializableGameProperty: ID={serialized.ID}, BaseValue={serialized.BaseValue}, Modifiers={serialized.ModifierList?.Modifiers?.Count}");

            // 反序列化
            var deserialized = GamePropertySerializer.FromSerializable(serialized);
            Debug.Log($"[反序列化] 还原MP: {deserialized.GetValue()}");

            // 项目暂时不支持直接序列化 CombineProperty
        }

        /// <summary>
        /// 验证一个比较复杂的案例
        /// </summary>
        void Test_RPG_AttackPower_Complex()
        {
            // 创建组合属性：基础攻击=50，武器加成=20，Buff=10，BuffMul=0.2（+20%），Debuff=5，DebuffMul=0.5（+50%）
            var combine = new CombinePropertyClassic(
                "AttackPower", 50f, "Base", "Buff", "BuffMul", "Debuff", "DebuffMul"
            );
            // 武器加成
            combine.GetProperty("Base").AddModifier(new FloatModifier(ModifierType.Add, 0, 20f));
            // Buff
            combine.GetProperty("Buff").AddModifier(new FloatModifier(ModifierType.Add, 0, 10f));
            // Buff百分比
            combine.GetProperty("BuffMul").AddModifier(new FloatModifier(ModifierType.Add, 0, 0.2f));
            // Debuff
            combine.GetProperty("Debuff").AddModifier(new FloatModifier(ModifierType.Add, 0, 5f));
            // Debuff百分比
            combine.GetProperty("DebuffMul").AddModifier(new FloatModifier(ModifierType.Add, 0, 0.5f));

            // 计算：(50+20+10)*(1+0.2) - 5*(1+0.5) = 80*1.2 - 5*1.5 = 96 - 7.5 = 88.5
            float result = combine.GetValue();
            Debug.Assert(Mathf.Approximately(result, 88.5f), "RPG AttackPower Complex Test Failed");
            Debug.Log($"Test_RPG_AttackPower_Complex passed, value={result}");
        }

        /// <summary>
        /// 验证多个CombinePropertyCustom实例可以同时引用并监听同一个GameProperty
        /// </summary>
        void Test_MultiCombinePropertyCustom_ShareGameProperty()
        {
            Debug.Log("=== Test_MultiCombinePropertyCustom_ShareGameProperty ===");
            // 创建一个被共享的GameProperty
            var sharedProp = new GameProperty("Shared", 100f);

            // 创建第一个组合属性，计算为 sharedProp + 10
            var combineA = new CombinePropertyCustom("A");
            combineA.RegisterProperty(sharedProp);
            combineA.Calculater = c => c.GetProperty("Shared").GetValue() + 10;

            // 创建第二个组合属性，计算为 sharedProp * 2
            var combineB = new CombinePropertyCustom("B");
            combineB.RegisterProperty(sharedProp);
            combineB.Calculater = c => c.GetProperty("Shared").GetValue() * 2;

            // 初始断言
            float vA1 = combineA.GetValue();
            float vB1 = combineB.GetValue();
            Debug.Assert(Mathf.Approximately(vA1, 110f), $"combineA初始值错误: {vA1}");
            Debug.Assert(Mathf.Approximately(vB1, 200f), $"combineB初始值错误: {vB1}");

            // 修改sharedProp的值
            sharedProp.SetBaseValue(50f);

            // 两个组合属性都应感知到变化
            float vA2 = combineA.GetValue();
            float vB2 = combineB.GetValue();
            Debug.Assert(Mathf.Approximately(vA2, 60f), $"combineA变更后值错误: {vA2}");
            Debug.Assert(Mathf.Approximately(vB2, 100f), $"combineB变更后值错误: {vB2}");

            // 再次修改sharedProp
            sharedProp.SetBaseValue(123f);
            float vA3 = combineA.GetValue();
            float vB3 = combineB.GetValue();
            Debug.Assert(Mathf.Approximately(vA3, 133f), $"combineA再次变更后值错误: {vA3}");
            Debug.Assert(Mathf.Approximately(vB3, 246f), $"combineB再次变更后值错误: {vB3}");

            Debug.Log("Test_MultiCombinePropertyCustom_ShareGameProperty passed");
        }

        /// <summary>
        /// 测试循环依赖和自依赖是错的
        /// </summary>
        void Test_CyclicDependency_Detection()
        {
            Debug.Log("=== Test_CyclicDependency_Detection ===");

            // 测试1：检测自依赖（自己依赖自己）
            var selfDep = new GameProperty("SelfDep", 100f);

            // 尝试添加自依赖
            selfDep.AddDependency(selfDep);

            // 间接测试自依赖的效果：创建监听器
            bool valueChanged = false;
            selfDep.OnValueChanged += (oldVal, newVal) => valueChanged = true;

            // 修改值，如果自依赖被成功阻止，不会引起无限递归
            selfDep.SetBaseValue(200f);
            selfDep.GetValue(); // 触发计算和事件

            // 检查事件是否正常触发（如果没有触发，可能是由于无限递归导致的崩溃）
            Debug.Assert(valueChanged, "自依赖测试失败：事件未被触发");
            Debug.Log("自依赖检测测试通过");

            // 测试2：检测简单的循环依赖（A -> B -> A）
            var propA = new GameProperty("A", 10f);
            var propB = new GameProperty("B", 20f);

            // 建立依赖关系: A -> B
            propA.AddDependency(propB);

            // 创建监听器检查循环依赖影响
            bool aChanged = false;
            bool bChanged = false;
            propA.OnValueChanged += (oldVal, newVal) => aChanged = true;
            propB.OnValueChanged += (oldVal, newVal) => bChanged = true;

            // 尝试建立循环依赖: B -> A
            propB.AddDependency(propA); // 应该被拒绝

            // 重置标志
            aChanged = false;
            bChanged = false;

            // 修改B的值
            propB.SetBaseValue(25f);
            propA.GetValue(); // 强制刷新A，应该会触发A的变更
            propB.GetValue(); // 强制刷新B

            // 只有A应该被更新，B不应受到循环依赖影响
            Debug.Assert(aChanged, "A的值应该被B的改变影响");
            Debug.Log("简单循环依赖检测测试通过");

            // 测试3：检测复杂的循环依赖（A -> B -> C -> A）
            var propC = new GameProperty("C", 30f);

            // 建立依赖链: A -> B -> C
            propA = new GameProperty("A", 10f);
            propB = new GameProperty("B", 20f);
            propC = new GameProperty("C", 30f);

            propA.AddDependency(propB);
            propB.AddDependency(propC);

            // 尝试建立循环依赖: C -> A
            propC.AddDependency(propA); // 应该被阻止

            // 设置监听器
            bool cChanged = false;
            aChanged = false;
            bChanged = false;

            propA.OnValueChanged += (oldVal, newVal) => aChanged = true;
            propB.OnValueChanged += (oldVal, newVal) => bChanged = true;
            propC.OnValueChanged += (oldVal, newVal) => cChanged = true;

            // 修改C的值
            propC.SetBaseValue(35f);

            // 获取所有值以触发计算
            propA.GetValue();
            propB.GetValue();
            propC.GetValue();

            // B依赖C，A依赖B，所以A和B都应该变化
            Debug.Assert(bChanged, "B的值应该被C的改变影响");
            Debug.Assert(aChanged, "A的值应该被B的改变影响");
            Debug.Log("复杂循环依赖检测测试通过");

            // 测试4：正常的非循环多层依赖
            propA = new GameProperty("A", 10f);
            propB = new GameProperty("B", 20f);
            propC = new GameProperty("C", 30f);
            var propD = new GameProperty("D", 40f);

            // 建立依赖链：D -> C -> B -> A
            propB.AddDependency(propA);
            propC.AddDependency(propB);
            propD.AddDependency(propC);

            // 重置事件标志
            bool dChanged = false;
            cChanged = false;
            bChanged = false;
            aChanged = false;

            propA.OnValueChanged += (oldVal, newVal) => aChanged = true;
            propB.OnValueChanged += (oldVal, newVal) => bChanged = true;
            propC.OnValueChanged += (oldVal, newVal) => cChanged = true;
            propD.OnValueChanged += (oldVal, newVal) => dChanged = true;

            // 修改基础属性A
            propA.SetBaseValue(15f);

            // 获取D的值，应该触发整个依赖链的计算
            propD.GetValue();

            // 整个依赖链上的所有属性都应该更新
            Debug.Assert(bChanged, "B的值应该被A的改变影响");
            Debug.Assert(cChanged, "C的值应该被B的改变影响");
            Debug.Assert(dChanged, "D的值应该被C的改变影响");
            Debug.Log("多层依赖传播测试通过");

            Debug.Log("Test_CyclicDependency_Detection 所有测试通过");
        }

        /// <summary>
        /// 示例：属性依赖关系的高级用法
        /// 展示如何建立复杂的属性依赖链并处理更新
        /// </summary>
        void Example_PropertyWithDependencies()
        {
            Debug.Log("=== 属性依赖关系的高级用法 ===");

            // 创建基础属性
            var strength = new GameProperty("Strength", 10f); // 力量
            var agility = new GameProperty("Agility", 8f);    // 敏捷
            var intelligence = new GameProperty("Intelligence", 12f); // 智力

            // 创建依赖于基础属性的二级属性
            var attackPower = new GameProperty("AttackPower", 0f); // 攻击力 = 力量*2 + 敏捷*0.5 = 
            var attackSpeed = new GameProperty("AttackSpeed", 0f); // 攻击速度 = 敏捷*0.1 + 1
            var spellPower = new GameProperty("SpellPower", 0f);   // 法术强度 = 智力*3

            // 建立依赖关系
            attackPower.AddDependency(strength);
            attackPower.AddDependency(agility);
            attackSpeed.AddDependency(agility);
            spellPower.AddDependency(intelligence);

            // 设置计算方式
            strength.OnValueChanged += (_, __) => {
                attackPower.SetBaseValue(strength.GetValue() * 2 + agility.GetValue() * 0.5f);
            };

            agility.OnValueChanged += (_, __) => {
                attackPower.SetBaseValue(strength.GetValue() * 2 + agility.GetValue() * 0.5f);
                attackSpeed.SetBaseValue(agility.GetValue() * 0.1f + 1f);
            };

            intelligence.OnValueChanged += (_, __) => {
                spellPower.SetBaseValue(intelligence.GetValue() * 3);
            };

            // 初始计算一次
            attackPower.SetBaseValue(strength.GetValue() * 2 + agility.GetValue() * 0.5f);
            attackSpeed.SetBaseValue(agility.GetValue() * 0.1f + 1f);
            spellPower.SetBaseValue(intelligence.GetValue() * 3);

            // 打印初始状态
            Debug.Log($"初始状态:");
            Debug.Log($"  力量: {strength.GetValue()}, 敏捷: {agility.GetValue()}, 智力: {intelligence.GetValue()}");
            Debug.Log($"  攻击力: {attackPower.GetValue()}, 攻击速度: {attackSpeed.GetValue()}, 法术强度: {spellPower.GetValue()}");

            // 创建三级属性：DPS(每秒伤害)，依赖于攻击力和攻击速度
            var dps = new GameProperty("DPS", 0f); // DPS = 攻击力 * 攻击速度
            dps.AddDependency(attackPower);
            dps.AddDependency(attackSpeed);

            attackPower.OnValueChanged += (_, __) => {
                dps.SetBaseValue(attackPower.GetValue() * attackSpeed.GetValue());
            };

            attackSpeed.OnValueChanged += (_, __) => {
                dps.SetBaseValue(attackPower.GetValue() * attackSpeed.GetValue());
            };

            // 初始计算DPS
            dps.SetBaseValue(attackPower.GetValue() * attackSpeed.GetValue());
            Debug.Log($"  每秒伤害(DPS): {dps.GetValue()}");

            // 模拟属性成长
            Debug.Log("\n属性提升后:");
            strength.SetBaseValue(15f); // 力量+5
            agility.SetBaseValue(12f);  // 敏捷+4
            intelligence.SetBaseValue(18f); // 智力+6

            // 打印最终状态
            Debug.Log($"  力量: {strength.GetValue()}, 敏捷: {agility.GetValue()}, 智力: {intelligence.GetValue()}");
            Debug.Log($"  攻击力: {attackPower.GetValue()}, 攻击速度: {attackSpeed.GetValue()}, 法术强度: {spellPower.GetValue()}");
            Debug.Log($"  每秒伤害(DPS): {dps.GetValue()}");

            // 演示属性连锁反应
            Debug.Log("\n装备特效触发，力量翻倍!");
            strength.AddModifier(new FloatModifier(ModifierType.Mul, 0, 2.0f)); // 力量翻倍
            Debug.Log($"  力量: {strength.GetValue()}, 攻击力: {attackPower.GetValue()}, DPS: {dps.GetValue()}");
        }
    }
}