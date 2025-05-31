using UnityEngine;

/// 使用规范（推荐）：
/// 1. 所有可被Buff、Debuff、装备、成长等动态影响的属性，优先使用 CombineGameProperty（如 CombinePropertyClassic/Single/Custom），避免直接操作 GameProperty。
/// 2. 静态属性（如最大生命、护甲、攻击力等）变化，务必通过修饰器（Modifier）实现，禁止直接修改 GameProperty 的基础值（SetBaseValue），以便属性变动可追踪、可撤销、可序列化。
/// 3. 动态属性（如当前生命、当前魔法等）可用单独 GameProperty 或者 SingleProperty 表示（建议使用后者，因为可以直接管理），允许直接 SetBaseValue，不建议通过修饰器叠加。
/// 4. 所有游戏内的 Buff/Debuff 效果，必须通过 BuffManager 进行添加、移除和生命周期管理，Buff 的属性变动通过其携带的 Modifier 实现，禁止手动对属性添加/移除 Modifier 来实现Buff效果。
/// 5. 静态属性的所有增减、乘除、限制（Clamp）等变动，均应通过 ModifierType 体系实现，建议避免自定义“特殊处理”绕过修饰器体系，除非修饰器体系不足以完成逻辑。
/// 6. 组合属性（CombineGameProperty）统一由 CombineGamePropertyManager 管理，便于批量查询、序列化、保存与还原。
/// 7. 需要保存/还原的属性，优先使用 GamePropertySerializer 进行序列化与反序列化，避免直接存储 GameProperty 实例。
/// 8. BuffManager 只管理 GameProperty 的 Buff，不直接管理 CombineGameProperty；如需对组合属性生效，应操作其 ResultHolder 或相关子属性。
/// 9. 禁止跨系统直接操作属性的 Modifiers 列表，所有修饰器的增删应通过 AddModifier/RemoveModifier/相关 BuffManager 方法完成。
/// 10. 若需监听属性变动，建议使用 GameProperty.OnValueChanged 事件，避免轮询。

namespace RPGPack
{
    public class GamePropertyExample : MonoBehaviour
    {
        void Start()
        {
            Test_RPG_AttackPower_Complex();
            Test_MultiCombinePropertyCustom_ShareGameProperty();
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
            prop.AddModifier(new Vector2Modifier(ModifierType.Clamp, 0, new Vector2(0, 150)));
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
            var manager = new CombineGamePropertyManager();
            manager.AddOrUpdate(classic);
            manager.AddOrUpdate(single);

            foreach (var p in manager.GetAll())
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
    }
}