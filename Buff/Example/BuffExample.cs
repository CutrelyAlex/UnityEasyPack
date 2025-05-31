using UnityEngine;

// AIд�ĵ�Ԫ�����淽��ѽ��
namespace RPGPack
{
    public class BuffExample : MonoBehaviour
    {
        void Start()
        {
            Test_BuffManager_PropertyIntegration();
            Test_BuffManager_StackBuff_RemoveOneStackEachDuration();
        }

        void Test_BuffManager_PropertyIntegration()
        {
            Debug.Log("=== Test_BuffManager_PropertyIntegration ===");
            var combineManager = new CombineGamePropertyManager();
            var buffManager = new RPGPack.BuffManager(combineManager);

            // 1. �������ԣ�����ֵ100
            var prop = new GameProperty("HeroAtk", 100f);

            // 2. ���һ��+20������Buff
            var mod1 = new FloatModifier(ModifierType.Add, 0, 20f);
            var buff1 = new Buff("BuffAtkUp", mod1, 10f, canStack: false);
            buffManager.AddBuff(prop, buff1);
            float v1 = prop.GetValue();
            Debug.Assert(Mathf.Approximately(v1, 120f), $"Buff+20 failed, got {v1}");

            // 3. ���һ��+50%������Buff���˷���
            // (100+20)*1.5 = 180
            var mod2 = new FloatModifier(ModifierType.Mul, 0, 1.5f);
            var buff2 = new Buff("BuffAtkMul", mod2, 10f, canStack: false);
            buffManager.AddBuff(prop, buff2);
            float v2 = prop.GetValue();
            Debug.Assert(Mathf.Approximately(v2, 180f), $"Buff*1.5 failed, got {v2}");

            // 4. ��ӿɵ��ӵ�+10����Buff����������
            // (100+20+10*2)*1.5 = 210
            var mod3 = new FloatModifier(ModifierType.Add, 0, 10f);
            var buff3 = new Buff("BuffStack", mod3, 10f, canStack: true);
            buffManager.AddBuff(prop, buff3);
            buffManager.AddBuff(prop, buff3);
            float v3 = prop.GetValue();
            Debug.Assert(Mathf.Approximately(v3, 210f), $"StackBuff failed, got {v3}");

            // 5. �Ƴ�һ�����Buff
            // (100+20+10)*1.5 = 195
            buffManager.RemoveBuffStack(prop, "BuffStack", 1);
            float v4 = prop.GetValue();
            Debug.Assert(Mathf.Approximately(v4, 195f), $"Remove stack failed, got {v4}");

            // 6. �Ƴ�����Buff
            buffManager.RemoveBuff(prop, "BuffAtkUp");
            buffManager.RemoveBuff(prop, "BuffAtkMul");
            buffManager.RemoveBuff(prop, "BuffStack");
            float v5 = prop.GetValue();
            Debug.Assert(Mathf.Approximately(v5, 100f), $"Remove all buffs failed, got {v5}");

            // 7. ���һ��1�����ڵ�Buff
            var mod4 = new FloatModifier(ModifierType.Add, 0, 50f);
            var buff4 = new Buff("BuffTemp", mod4, 1f, canStack: false);
            buffManager.AddBuff(prop, buff4);
            float v6 = prop.GetValue();
            Debug.Assert(Mathf.Approximately(v6, 150f), $"Temp buff add failed, got {v6}");
            buffManager.Update(1.1f); // ����
            float v7 = prop.GetValue();
            Debug.Assert(Mathf.Approximately(v7, 100f), $"Temp buff expire failed, got {v7}");


            // 8. Source�ֶ��÷�ʾ����ͬID��ͬ��Դ��Buff�ɹ��桢�����Ƴ�
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

            // ֻ�Ƴ�sourceA��Դ��Buff
            buffManager.RemoveBuff(prop, "BuffSourceDemo", sourceA);
            float v9 = prop.GetValue();
            Debug.Assert(Mathf.Approximately(v9, 100f + 10f), $"Source Buff remove failed, got {v9}");

            // ���Ƴ�sourceB��Դ��Buff
            buffManager.RemoveBuff(prop, "BuffSourceDemo", sourceB);
            float v10 = prop.GetValue();
            Debug.Assert(Mathf.Approximately(v10, 100f), $"Source Buff final remove failed, got {v10}");


            Debug.Log("Test_BuffManager_PropertyIntegration passed");
        }

        void Test_BuffManager_StackBuff_RemoveOneStackEachDuration()
        {
            Debug.Log("=== Test_BuffManager_StackBuff_RemoveOneStackEachDuration ===");
            var combineManager = new CombineGamePropertyManager();
            var buffManager = new BuffManager(combineManager);

            var prop = new GameProperty("HeroAtk", 100f);

            // �����ɵ���Buff������1�룬RemoveOneStackEachDuration = true������3��
            var mod = new FloatModifier(ModifierType.Add, 0, 10f);
            var buff = new Buff("StackBuff", mod, 1f, canStack: true)
            {
                RemoveOneStackEachDuration = true
            };
            buffManager.AddBuff(prop, buff);
            buffManager.AddBuff(prop, buff);
            buffManager.AddBuff(prop, buff);

            // ��ʼӦΪ130
            float v1 = prop.GetValue();
            Debug.Assert(Mathf.Approximately(v1, 130f), $"Initial stack failed, got {v1}");

            // ��1����Ƴ�1�㣬ӦΪ120
            buffManager.Update(1.01f);
            float v2 = prop.GetValue();
            Debug.Assert(Mathf.Approximately(v2, 120f), $"After 1st expire, got {v2}");

            // ��2������Ƴ�1�㣬ӦΪ110
            buffManager.Update(1.01f);
            float v3 = prop.GetValue();
            Debug.Assert(Mathf.Approximately(v3, 110f), $"After 2nd expire, got {v3}");

            // ��3������һ���Ƴ���ӦΪ100
            buffManager.Update(1.01f);
            float v4 = prop.GetValue();
            Debug.Assert(Mathf.Approximately(v4, 100f), $"After 3rd expire, got {v4}");

            Debug.Log("Test_BuffManager_StackBuff_RemoveOneStackEachDuration passed");
        }
    }
}