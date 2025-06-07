using UnityEngine;

// AI写的单元测试真方便呀（
namespace RPGPack
{
    public class BuffExample : MonoBehaviour
    {
        void Start()
        {
            Test_BuffManager_PropertyIntegration();
            Test_BuffManager_StackBuff_RemoveOneStackEachDuration();
            Test_BuffManager_ActionEvents();
            Test_TriggerableBuff();
            Test_Buff_Priority_Type_Group();
        }


        // 测试Buff管理器与GameProperty的集成
        void Test_BuffManager_PropertyIntegration()
        {
            Debug.Log("=== Test_BuffManager_PropertyIntegration ===");
            var combineManager = new CombineGamePropertyManager();
            var buffManager = new RPGPack.BuffManager(combineManager);

            // 1. 创建属性，基础值100
            var prop = new GameProperty("HeroAtk", 100f);

            // 2. 添加一个+20攻击的Buff
            var mod1 = new FloatModifier(ModifierType.Add, 0, 20f);
            var buff1 = new Buff("BuffAtkUp", mod1, 10f, canStack: false);
            buffManager.AddBuff(prop, buff1);
            float v1 = prop.GetValue();
            Debug.Assert(Mathf.Approximately(v1, 120f), $"Buff+20 failed, got {v1}");

            // 3. 添加一个+50%攻击的Buff（乘法）
            // (100+20)*1.5 = 180
            var mod2 = new FloatModifier(ModifierType.Mul, 0, 1.5f);
            var buff2 = new Buff("BuffAtkMul", mod2, 10f, canStack: false);
            buffManager.AddBuff(prop, buff2);
            float v2 = prop.GetValue();
            Debug.Assert(Mathf.Approximately(v2, 180f), $"Buff*1.5 failed, got {v2}");

            // 4. 添加可叠加的+10攻击Buff，叠加两层
            // (100+20+10*2)*1.5 = 210
            var mod3 = new FloatModifier(ModifierType.Add, 0, 10f);
            var buff3 = new Buff("BuffStack", mod3, 10f, canStack: true);
            buffManager.AddBuff(prop, buff3);
            buffManager.AddBuff(prop, buff3);
            float v3 = prop.GetValue();
            Debug.Assert(Mathf.Approximately(v3, 210f), $"StackBuff failed, got {v3}");

            // 5. 移除一层叠加Buff
            // (100+20+10)*1.5 = 195
            buffManager.RemoveBuffStack(prop, "BuffStack", 1);
            float v4 = prop.GetValue();
            Debug.Assert(Mathf.Approximately(v4, 195f), $"Remove stack failed, got {v4}");

            // 6. 移除所有Buff
            buffManager.RemoveBuff(prop, "BuffAtkUp");
            buffManager.RemoveBuff(prop, "BuffAtkMul");
            buffManager.RemoveBuff(prop, "BuffStack");
            float v5 = prop.GetValue();
            Debug.Assert(Mathf.Approximately(v5, 100f), $"Remove all buffs failed, got {v5}");

            // 7. 添加一个1秒后过期的Buff
            var mod4 = new FloatModifier(ModifierType.Add, 0, 50f);
            var buff4 = new Buff("BuffTemp", mod4, 1f, canStack: false);
            buffManager.AddBuff(prop, buff4);
            float v6 = prop.GetValue();
            Debug.Assert(Mathf.Approximately(v6, 150f), $"Temp buff add failed, got {v6}");
            buffManager.Update(1.1f); // 过期
            float v7 = prop.GetValue();
            Debug.Assert(Mathf.Approximately(v7, 100f), $"Temp buff expire failed, got {v7}");


            // 8. Source字段用法示例：同ID不同来源的Buff可共存、独立移除
            var modA = new FloatModifier(ModifierType.Add, 0, 5f);
            var modB = new FloatModifier(ModifierType.Add, 0, 10f);
            var sourceA = "SkillA";
            var sourceB = "SkillB";
            var buffA = new Buff("BuffSourceDemo", modA, 10f, canStack: false) { Source = sourceA };
            var buffB = new Buff("BuffSourceDemo", modB, 10f, canStack: false) { Source = sourceB };

            buffManager.AddBuff(prop, buffA);
            buffManager.AddBuff(prop, buffB);
            float v8 = prop.GetValue();
            Debug.Assert(Mathf.Approximately(v8, 100f + 5f + 10f), $"Source Buff add failed, got {v8}");

            // 只移除sourceA来源的Buff
            buffManager.RemoveBuff(prop, "BuffSourceDemo", sourceA);
            float v9 = prop.GetValue();
            Debug.Assert(Mathf.Approximately(v9, 100f + 10f), $"Source Buff remove failed, got {v9}");

            // 再移除sourceB来源的Buff
            buffManager.RemoveBuff(prop, "BuffSourceDemo", sourceB);
            float v10 = prop.GetValue();
            Debug.Assert(Mathf.Approximately(v10, 100f), $"Source Buff final remove failed, got {v10}");


            Debug.Log("Test_BuffManager_PropertyIntegration passed");
        }

        // 测试可叠加Buff的RemoveOneStackEachDuration功能
        void Test_BuffManager_StackBuff_RemoveOneStackEachDuration()
        {
            Debug.Log("=== Test_BuffManager_StackBuff_RemoveOneStackEachDuration ===");
            var combineManager = new CombineGamePropertyManager();
            var buffManager = new BuffManager(combineManager);

            var prop = new GameProperty("HeroAtk", 100f);

            var mod = new FloatModifier(ModifierType.Add, 0, 10f);
            var stackBuff = new Buff("StackBuff", mod, 1f, canStack: true)
            {
                RemoveOneStackEachDuration = true
            };

            // 复用同一个Buff对象，BuffManager内部会Clone
            buffManager
                .AddBuff(prop, stackBuff)
                .AddBuff(prop, stackBuff)
                .AddBuff(prop, stackBuff);

            float v1 = prop.GetValue();
            Debug.Assert(Mathf.Approximately(v1, 130f), $"Initial stack failed, got {v1}");

            buffManager.Update(1.01f);
            float v2 = prop.GetValue();
            Debug.Assert(Mathf.Approximately(v2, 120f), $"After 1st expire, got {v2}");

            buffManager.Update(1.01f);
            float v3 = prop.GetValue();
            Debug.Assert(Mathf.Approximately(v3, 110f), $"After 2nd expire, got {v3}");

            buffManager.Update(1.01f);
            float v4 = prop.GetValue();
            Debug.Assert(Mathf.Approximately(v4, 100f), $"After 3rd expire, got {v4}");

            Debug.Log("Test_BuffManager_StackBuff_RemoveOneStackEachDuration passed");
        }

        // 测试Action事件触发
        void Test_BuffManager_ActionEvents()
        {
            Debug.Log("=== Test_BuffManager_ActionEvents ===");
            var combineManager = new CombineGamePropertyManager();
            var buffManager = new BuffManager(combineManager);
            var prop = new GameProperty("HeroAtk", 100f);

            bool added = false, removed = false, expired = false, stackRemoved = false;

            buffManager.BuffAdded += (p, b) => { added = true; Debug.Log("BuffAdded"); };
            buffManager.BuffRemoved += (p, b) => { removed = true; Debug.Log("BuffRemoved"); };
            buffManager.BuffExpired += (p, b) => { expired = true; Debug.Log("BuffExpired"); };
            buffManager.BuffStackRemoved += (p, b) => { stackRemoved = true; Debug.Log("BuffStackRemoved"); };

            // 添加Buff
            var mod = new FloatModifier(ModifierType.Add, 0, 10f);
            var buff = new Buff("TestBuff", mod, 1f, canStack: true) { RemoveOneStackEachDuration = true };
            buffManager.AddBuff(prop, buff);
            Debug.Assert(added, "BuffAdded event not triggered");

            // 叠加Buff
            added = false;
            buffManager.AddBuff(prop, buff);
            Debug.Assert(added, "BuffAdded event not triggered on stack");

            // 移除一层
            buffManager.RemoveBuffStack(prop, "TestBuff", 1);
            Debug.Assert(stackRemoved, "BuffStackRemoved event not triggered");

            // 过期移除
            buffManager.Update(2f);
            Debug.Assert(expired, "BuffExpired event not triggered");
            Debug.Assert(removed, "BuffRemoved event not triggered");

            Debug.Log("Test_BuffManager_ActionEvents passed");
        }

        // TriggerableBuff 使用示例
        void Test_TriggerableBuff()
        {
            Debug.Log("=== Test_TriggerableBuff ===");
            var combineManager = new CombineGamePropertyManager();
            var buffManager = new BuffManager(combineManager);

            var prop = new GameProperty("HeroAtk", 100f);

            // 创建一个+5攻击的普通Buff
            var mod = new FloatModifier(ModifierType.Add, 0, 5f);
            var normalBuff = new Buff("NormalBuff", mod, 10f, canStack: false);
            buffManager.AddBuff(prop, normalBuff);

            // 创建TriggerableBuff，条件为属性值大于110时触发
            bool triggered = false;
            var triggerBuff = new TriggerableBuff(
                "TriggerBuff",
                new FloatModifier(ModifierType.Add, 0, 20f),
                triggerCondition: () => prop.GetValue() > 110f,
                onTriggered: () => {
                    triggered = true;
                    Debug.Log("TriggerableBuff触发！当前属性值: " + prop.GetValue());
                    // 触发后移除自身
                    buffManager.RemoveBuff(prop, "TriggerBuff");
                },
                duration: 10f,
                canStack: false
            );
            buffManager.AddBuff(prop, triggerBuff);

            // 初始未触发
            Debug.Assert(!triggered, "TriggerableBuff 不应立即触发");

            // 增加属性值使其超过110
            var addBuff = new Buff("AddBuff", new FloatModifier(ModifierType.Add, 0, 10f), 10f, false);
            buffManager.AddBuff(prop, addBuff);

            // 更新BuffManager，触发条件成立
            buffManager.Update(0.1f);

            Debug.Assert(triggered, "TriggerableBuff 未被触发");
            Debug.Log("Test_TriggerableBuff passed");
        }

        void Test_Buff_Priority_Type_Group()
        {
            Debug.Log("=== Test_Buff_Priority_Type_Group ===");
            var combineManager = new CombineGamePropertyManager();
            var buffManager = new BuffManager(combineManager);
            var prop = new GameProperty("HeroAtk", 100f);

            // Stackable: 同Group可叠加
            var modA = new FloatModifier(ModifierType.Add, 0, 10f);
            var buffA = new Buff("BuffA", modA, 10f, true, null, "GroupS", 0, BuffStackType.Stackable);
            buffManager.AddBuff(prop, buffA);
            buffManager.AddBuff(prop, buffA); // 叠加一层
            float v1 = prop.GetValue();
            Debug.Assert(Mathf.Approximately(v1, 120f), $"Stackable Group叠加异常，期望120，实际{v1}");

            // Override: 低优先级不能覆盖，高优先级能覆盖
            var modB = new FloatModifier(ModifierType.Add, 0, 20f);
            var buffB = new Buff("BuffB", modB, 10f, false, null, "GroupO", 1, BuffStackType.Override);
            var modC = new FloatModifier(ModifierType.Add, 0, 30f);
            var buffC = new Buff("BuffC", modC, 10f, false, null, "GroupO", 0, BuffStackType.Override);
            buffManager.AddBuff(prop, buffB); // 先加B
            buffManager.AddBuff(prop, buffC); // C优先级低，不能覆盖B
            float v2 = prop.GetValue();
            Debug.Assert(Mathf.Approximately(v2, 120f + 20f), $"Override低优先级未被覆盖异常，期望140，实际{v2}");
            var buffD = new Buff("BuffD", new FloatModifier(ModifierType.Add, 0, 40f), 10f, false, null, "GroupO", 2, BuffStackType.Override);
            buffManager.AddBuff(prop, buffD); // D优先级高，能覆盖B
            float v3 = prop.GetValue();
            Debug.Assert(Mathf.Approximately(v3, 120f + 40f), $"Override高优先级未覆盖异常，期望160，实际{v3}");

            // IgnoreIfExists: 已存在时忽略
            var modE = new FloatModifier(ModifierType.Add, 0, 50f);
            var buffE = new Buff("BuffE", modE, 10f, false, null, "GroupI", 0, BuffStackType.IgnoreIfExists);
            buffManager.AddBuff(prop, buffE);
            float v4 = prop.GetValue();
            buffManager.AddBuff(prop, buffE); // 应忽略
            float v5 = prop.GetValue();
            Debug.Assert(Mathf.Approximately(v4, v5), $"IgnoreIfExists未生效，期望{v4}，实际{v5}");

            Debug.Log("Test_Buff_Priority_Type_Group passed");
        }
    }
}