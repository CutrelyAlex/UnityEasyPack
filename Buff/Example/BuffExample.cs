using UnityEngine;
using System;

namespace RPGPack
{
    public class BuffExample : MonoBehaviour
    {
        void Start()
        {
            Test_BuffManager();
            Test_BuffHandle();
            Example_BuffAndPropertyIntegration();
        }

        /// <summary>
        /// 基础的BuffManager功能
        /// </summary>

        void Test_BuffManager()
        {
            // 创建BuffManager实例
            var manager = new BuffManager();

            // 创建FloatModifier
            var modifier1 = new FloatModifier(ModifierType.Add, 1, 10f);
            var modifier2 = new FloatModifier(ModifierType.Add, 1, 20f);

            // 创建可叠加Buff
            var buff1 = new Buff("buff_stack", modifier1, duration: 5f, canStack: true);
            var buff2 = new Buff("buff_stack", modifier2, duration: 5f, canStack: true);

            // 添加第一个Buff
            manager.AddBuff(buff1);
            Debug.Assert(manager.GetBuff("buff_stack") != null, "Buff1 应该被添加");

            // 添加同ID可叠加Buff，验证叠加
            manager.AddBuff(buff2);
            var stackedBuff = manager.GetBuff("buff_stack");
            Debug.Assert(stackedBuff.StackCount == 2, "Buff 应该叠加为2层");
            Debug.Assert(stackedBuff.Elapsed == 0, "叠加时应刷新持续时间");

            // 测试叠加上限
            stackedBuff.MaxStackCount = 2;
            manager.AddBuff(new Buff("buff_stack", modifier1, duration: 5f, canStack: true));
            Debug.Assert(stackedBuff.StackCount == 2, "叠加层数不应超过最大上限");
 

            // 测试移除Buff
            manager.RemoveBuff("buff_stack");
            Debug.Assert(manager.GetBuff("buff_stack") == null, "Buff应被移除");

            // 测试叠加型Buff移除一层
            manager.AddBuff(buff1);
            manager.AddBuff(buff2);
            manager.RemoveBuffStack("buff_stack", 1);
            var afterRemove = manager.GetBuff("buff_stack");
            Debug.Assert(afterRemove != null && afterRemove.StackCount == 1, "移除一层后应剩1层");

            // 测试Buff过期
            manager.AddBuff(new Buff("buff_expire", modifier1, duration: 1f));
            manager.Update(1.1f);
            Debug.Assert(manager.GetBuff("buff_expire") == null, "过期Buff应被移除");

            Debug.Log("BuffManager 单元测试全部通过！");
        }

        /// <summary>
        /// 验证BuffHandle功能
        /// </summary>
        void Test_BuffHandle()
        {
            // 创建BuffManager和BuffHandle实例
            var manager = new BuffManager();
            var buffHandle = new BuffHandle(manager);

            // 创建GameProperty实例
            var prop = new GameProperty("test_hp", 100f);

            // 创建FloatModifier
            var modifier1 = new FloatModifier(ModifierType.Add, 1, 10f);
            var modifier2 = new FloatModifier(ModifierType.Add, 1, 20f);

            // 创建Buff
            var buff1 = new Buff("buff1", modifier1, duration: 5f, canStack: true);
            var buff2 = new Buff("buff2", modifier2, duration: 5f, canStack: true);

            // 应用第一个Buff
            buffHandle.ApplyToProperty(buff1, prop);
            Debug.Assert(Mathf.Approximately(prop.GetValue(), 110f), "应用buff1后属性应为110");

            // 应用第二个Buff
            buffHandle.ApplyToProperty(buff2, prop);
            Debug.Assert(Mathf.Approximately(prop.GetValue(), 130f), "应用buff2后属性应为130");

            // 叠加第一个Buff
            buffHandle.ApplyToProperty(buff1, prop);
            Debug.Assert(Mathf.Approximately(prop.GetValue(), 140f), $"应用buff1后属性应为140，实际为{prop.GetValue()}");

            // 移除一层
            manager.RemoveBuffStack("buff1", 1);
            Debug.Assert(Mathf.Approximately(prop.GetValue(), 130f), "移除一层后属性应为130");

            // 移除剩余Buff
            manager.RemoveBuff("buff1").RemoveBuff("buff2");
            Debug.Assert(Mathf.Approximately(prop.GetValue(), 100f), $"移除所有Buff后属性应为100，实际为{prop.GetValue()}");

            // 测试Buff过期
            var buff3 = new Buff("buff3", new FloatModifier(ModifierType.Add, 1, 50f), duration: 1f);
            buffHandle.ApplyToProperty(buff3, prop);
            Debug.Assert(Mathf.Approximately(prop.GetValue(), 150f), "应用buff3后属性应为150");
            manager.Update(1.1f);
            Debug.Assert(Mathf.Approximately(prop.GetValue(), 100f), "buff3过期后属性应为100");

            Debug.Log("BuffHandle 单元测试全部通过！");
        }

        /// <summary>
        /// 验证复杂的BUFF叠加过程
        /// </summary>
        void Test_ComplexBuffStacking()
        {
            // 1. 创建角色属性
            var hp = new GameProperty("hp", 100f);
            var atk = new GameProperty("atk", 20f);
            var def = new GameProperty("def", 10f);

            // 2. 创建Buff管理器和Handle
            var manager = new BuffManager();
            var buffHandle = new BuffHandle(manager);

            // 3. 定义多种Buff
            // 攻击力提升Buff（可叠加）
            var atkBuff = new Buff("atk_up", new FloatModifier(ModifierType.Add, 1, 5f), duration: 5f, canStack: true);
            // 防御力提升Buff（不可叠加）
            var defBuff = new Buff("def_up", new FloatModifier(ModifierType.Add, 1, 10f), duration: 10f, canStack: false);
            // HP上限提升Buff（可叠加，最大2层）
            var hpBuff = new Buff("hp_up", new FloatModifier(ModifierType.Add, 1, 50f), duration: 8f, canStack: true) { MaxStackCount = 2 };

            // 4. 应用Buff到属性
            buffHandle.ApplyToProperty(atkBuff, atk);
            buffHandle.ApplyToProperty(defBuff, def);
            buffHandle.ApplyToProperty(hpBuff, hp);

            // 5. 验证初始叠加效果
            Debug.Assert(Mathf.Approximately(atk.GetValue(), 25f), $"atk应为25，实际为{atk.GetValue()}");
            Debug.Assert(Mathf.Approximately(def.GetValue(), 20f), $"def应为20，实际为{def.GetValue()}");
            Debug.Assert(Mathf.Approximately(hp.GetValue(), 150f), $"hp应为150，实际为{hp.GetValue()}");

            // 6. 叠加攻击力Buff和HP Buff
            buffHandle.ApplyToProperty(atkBuff, atk);
            buffHandle.ApplyToProperty(hpBuff, hp);
            Debug.Assert(Mathf.Approximately(atk.GetValue(), 30f), $"atk叠加后应为30，实际为{atk.GetValue()}");
            Debug.Assert(Mathf.Approximately(hp.GetValue(), 200f), $"hp叠加后应为200，实际为{hp.GetValue()}");

            // 7. HP Buff叠加上限测试
            buffHandle.ApplyToProperty(hpBuff, hp);
            Debug.Assert(Mathf.Approximately(hp.GetValue(), 200f), $"hp叠加上限应为200，实际为{hp.GetValue()}");

            // 8. 模拟时间流逝，部分Buff过期
            manager.Update(6f); // atkBuff过期，defBuff未过期，hpBuff未过期
            Debug.Assert(Mathf.Approximately(atk.GetValue(), 20f), $"atk过期后应为20，实际为{atk.GetValue()}");
            Debug.Assert(Mathf.Approximately(def.GetValue(), 20f), $"def未过期应为20，实际为{def.GetValue()}");
            Debug.Assert(Mathf.Approximately(hp.GetValue(), 200f), $"hp未过期应为200，实际为{hp.GetValue()}");

            // 9. 再过3秒，所有Buff过期
            manager.Update(4.1f); // defBuff和hpBuff过期
            Debug.Assert(Mathf.Approximately(def.GetValue(), 10f), $"def过期后应为10，实际为{def.GetValue()}");
            Debug.Assert(Mathf.Approximately(hp.GetValue(), 100f), $"hp过期后应为100，实际为{hp.GetValue()}");

            // 10. 重新应用Buff并测试移除一层
            buffHandle.ApplyToProperty(atkBuff, atk);
            buffHandle.ApplyToProperty(atkBuff, atk);
            Debug.Assert(Mathf.Approximately(atk.GetValue(), 30f), $"atk再次叠加应为30，实际为{atk.GetValue()}");
            manager.RemoveBuffStack("atk_up", 1);
            Debug.Assert(Mathf.Approximately(atk.GetValue(), 25f), $"移除一层atk_up后应为25，实际为{atk.GetValue()}");
            manager.RemoveBuff("atk_up");
            Debug.Assert(Mathf.Approximately(atk.GetValue(), 20f), $"移除所有atk_up后应为20，实际为{atk.GetValue()}");


            Debug.Log("ComplexBuffStacking 单元测试通过！");
        }

        /// <summary>
        /// 示例：Buff系统与属性系统的集成使用
        /// 展示如何使用BuffManager和BuffHandle管理角色的多种属性
        /// </summary>
        void Example_BuffAndPropertyIntegration()
        {
            Debug.Log("=== Buff系统与属性系统集成示例 ===");

            // 创建角色基础属性
            var maxHp = new GameProperty("MaxHP", 100f);
            var attack = new GameProperty("Attack", 20f);
            var defense = new GameProperty("Defense", 10f);
            var critRate = new GameProperty("CritRate", 0.05f); // 5%基础暴击率

            // 创建组合属性：最终攻击力 = 基础攻击 * (1 + 暴击率)
            var finalAttack = new CombinePropertyCustom("FinalAttack");
            finalAttack.RegisterProperty(attack);
            finalAttack.RegisterProperty(critRate);
            finalAttack.Calculater = prop => {
                float baseAtk = prop.GetProperty("Attack").GetValue();
                float crit = prop.GetProperty("CritRate").GetValue();
                return baseAtk * (1 + crit); // 暴击伤害简化为攻击力 * (1 + 暴击率)
            };

            // 初始状态打印
            Debug.Log($"初始状态 - 最大生命: {maxHp.GetValue()}, 攻击力: {attack.GetValue()}, " +
                      $"防御力: {defense.GetValue()}, 暴击率: {critRate.GetValue() * 100}%, " +
                      $"最终攻击: {finalAttack.GetValue()}");

            // 创建Buff管理器和处理器
            var buffManager = new BuffManager();
            var buffHandle = new BuffHandle(buffManager);

            // 创建不同类型的Buff
            // 1. 力量药水：增加攻击力10点，持续30秒
            var atkBuff = new Buff(
                "StrengthPotion",
                new FloatModifier(ModifierType.Add, 0, 10f),
                30f,
                canStack: true
            );

            // 2. 防御护盾：增加防御力50%，持续20秒
            var defBuff = new Buff(
                "DefenseShield",
                new FloatModifier(ModifierType.Mul, 0, 1.5f),
                20f,
                canStack: false
            );

            // 3. 生命强化：增加最大生命30点，持续60秒
            var hpBuff = new Buff(
                "VitalityBoost",
                new FloatModifier(ModifierType.Add, 0, 30f),
                60f,
                canStack: true
            )
            { MaxStackCount = 3 }; // 设置最大叠加层数为3

            // 4. 会心祝福：增加暴击率8%，持续45秒
            var critBuff = new Buff(
                "CriticalBlessing",
                new FloatModifier(ModifierType.Add, 0, 0.08f),
                45f,
                canStack: false
            );

            // 应用Buff
            buffHandle.ApplyToProperty(atkBuff, attack);
            buffHandle.ApplyToProperty(defBuff, defense);
            buffHandle.ApplyToProperty(hpBuff, maxHp);
            buffHandle.ApplyToProperty(critBuff, critRate);

            // 打印Buff效果
            Debug.Log($"应用Buff后 - 最大生命: {maxHp.GetValue()}, 攻击力: {attack.GetValue()}, " +
                      $"防御力: {defense.GetValue()}, 暴击率: {critRate.GetValue() * 100}%, " +
                      $"最终攻击: {finalAttack.GetValue()}");

            // 叠加一层力量药水和生命强化
            buffHandle.ApplyToProperty(atkBuff, attack);
            buffHandle.ApplyToProperty(hpBuff, maxHp);

            // 打印叠加效果
            Debug.Log($"叠加Buff后 - 最大生命: {maxHp.GetValue()}, 攻击力: {attack.GetValue()}, " +
                      $"防御力: {defense.GetValue()}, 暴击率: {critRate.GetValue() * 100}%, " +
                      $"最终攻击: {finalAttack.GetValue()}");

            // 模拟时间流逝，力量药水过期
            buffManager.Update(35f);

            // 打印部分Buff过期后效果
            Debug.Log($"部分Buff过期后 - 最大生命: {maxHp.GetValue()}, 攻击力: {attack.GetValue()}, " +
                      $"防御力: {defense.GetValue()}, 暴击率: {critRate.GetValue() * 100}%, " +
                      $"最终攻击: {finalAttack.GetValue()}");
        }
    }
}