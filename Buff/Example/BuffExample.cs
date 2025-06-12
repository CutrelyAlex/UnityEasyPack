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
        /// ������BuffManager����
        /// </summary>

        void Test_BuffManager()
        {
            // ����BuffManagerʵ��
            var manager = new BuffManager();

            // ����FloatModifier
            var modifier1 = new FloatModifier(ModifierType.Add, 1, 10f);
            var modifier2 = new FloatModifier(ModifierType.Add, 1, 20f);

            // �����ɵ���Buff
            var buff1 = new Buff("buff_stack", modifier1, duration: 5f, canStack: true);
            var buff2 = new Buff("buff_stack", modifier2, duration: 5f, canStack: true);

            // ��ӵ�һ��Buff
            manager.AddBuff(buff1);
            Debug.Assert(manager.GetBuff("buff_stack") != null, "Buff1 Ӧ�ñ����");

            // ���ͬID�ɵ���Buff����֤����
            manager.AddBuff(buff2);
            var stackedBuff = manager.GetBuff("buff_stack");
            Debug.Assert(stackedBuff.StackCount == 2, "Buff Ӧ�õ���Ϊ2��");
            Debug.Assert(stackedBuff.Elapsed == 0, "����ʱӦˢ�³���ʱ��");

            // ���Ե�������
            stackedBuff.MaxStackCount = 2;
            manager.AddBuff(new Buff("buff_stack", modifier1, duration: 5f, canStack: true));
            Debug.Assert(stackedBuff.StackCount == 2, "���Ӳ�����Ӧ�����������");
 

            // �����Ƴ�Buff
            manager.RemoveBuff("buff_stack");
            Debug.Assert(manager.GetBuff("buff_stack") == null, "BuffӦ���Ƴ�");

            // ���Ե�����Buff�Ƴ�һ��
            manager.AddBuff(buff1);
            manager.AddBuff(buff2);
            manager.RemoveBuffStack("buff_stack", 1);
            var afterRemove = manager.GetBuff("buff_stack");
            Debug.Assert(afterRemove != null && afterRemove.StackCount == 1, "�Ƴ�һ���Ӧʣ1��");

            // ����Buff����
            manager.AddBuff(new Buff("buff_expire", modifier1, duration: 1f));
            manager.Update(1.1f);
            Debug.Assert(manager.GetBuff("buff_expire") == null, "����BuffӦ���Ƴ�");

            Debug.Log("BuffManager ��Ԫ����ȫ��ͨ����");
        }

        /// <summary>
        /// ��֤BuffHandle����
        /// </summary>
        void Test_BuffHandle()
        {
            // ����BuffManager��BuffHandleʵ��
            var manager = new BuffManager();
            var buffHandle = new BuffHandle(manager);

            // ����GamePropertyʵ��
            var prop = new GameProperty("test_hp", 100f);

            // ����FloatModifier
            var modifier1 = new FloatModifier(ModifierType.Add, 1, 10f);
            var modifier2 = new FloatModifier(ModifierType.Add, 1, 20f);

            // ����Buff
            var buff1 = new Buff("buff1", modifier1, duration: 5f, canStack: true);
            var buff2 = new Buff("buff2", modifier2, duration: 5f, canStack: true);

            // Ӧ�õ�һ��Buff
            buffHandle.ApplyToProperty(buff1, prop);
            Debug.Assert(Mathf.Approximately(prop.GetValue(), 110f), "Ӧ��buff1������ӦΪ110");

            // Ӧ�õڶ���Buff
            buffHandle.ApplyToProperty(buff2, prop);
            Debug.Assert(Mathf.Approximately(prop.GetValue(), 130f), "Ӧ��buff2������ӦΪ130");

            // ���ӵ�һ��Buff
            buffHandle.ApplyToProperty(buff1, prop);
            Debug.Assert(Mathf.Approximately(prop.GetValue(), 140f), $"Ӧ��buff1������ӦΪ140��ʵ��Ϊ{prop.GetValue()}");

            // �Ƴ�һ��
            manager.RemoveBuffStack("buff1", 1);
            Debug.Assert(Mathf.Approximately(prop.GetValue(), 130f), "�Ƴ�һ�������ӦΪ130");

            // �Ƴ�ʣ��Buff
            manager.RemoveBuff("buff1").RemoveBuff("buff2");
            Debug.Assert(Mathf.Approximately(prop.GetValue(), 100f), $"�Ƴ�����Buff������ӦΪ100��ʵ��Ϊ{prop.GetValue()}");

            // ����Buff����
            var buff3 = new Buff("buff3", new FloatModifier(ModifierType.Add, 1, 50f), duration: 1f);
            buffHandle.ApplyToProperty(buff3, prop);
            Debug.Assert(Mathf.Approximately(prop.GetValue(), 150f), "Ӧ��buff3������ӦΪ150");
            manager.Update(1.1f);
            Debug.Assert(Mathf.Approximately(prop.GetValue(), 100f), "buff3���ں�����ӦΪ100");

            Debug.Log("BuffHandle ��Ԫ����ȫ��ͨ����");
        }

        /// <summary>
        /// ��֤���ӵ�BUFF���ӹ���
        /// </summary>
        void Test_ComplexBuffStacking()
        {
            // 1. ������ɫ����
            var hp = new GameProperty("hp", 100f);
            var atk = new GameProperty("atk", 20f);
            var def = new GameProperty("def", 10f);

            // 2. ����Buff��������Handle
            var manager = new BuffManager();
            var buffHandle = new BuffHandle(manager);

            // 3. �������Buff
            // ����������Buff���ɵ��ӣ�
            var atkBuff = new Buff("atk_up", new FloatModifier(ModifierType.Add, 1, 5f), duration: 5f, canStack: true);
            // ����������Buff�����ɵ��ӣ�
            var defBuff = new Buff("def_up", new FloatModifier(ModifierType.Add, 1, 10f), duration: 10f, canStack: false);
            // HP��������Buff���ɵ��ӣ����2�㣩
            var hpBuff = new Buff("hp_up", new FloatModifier(ModifierType.Add, 1, 50f), duration: 8f, canStack: true) { MaxStackCount = 2 };

            // 4. Ӧ��Buff������
            buffHandle.ApplyToProperty(atkBuff, atk);
            buffHandle.ApplyToProperty(defBuff, def);
            buffHandle.ApplyToProperty(hpBuff, hp);

            // 5. ��֤��ʼ����Ч��
            Debug.Assert(Mathf.Approximately(atk.GetValue(), 25f), $"atkӦΪ25��ʵ��Ϊ{atk.GetValue()}");
            Debug.Assert(Mathf.Approximately(def.GetValue(), 20f), $"defӦΪ20��ʵ��Ϊ{def.GetValue()}");
            Debug.Assert(Mathf.Approximately(hp.GetValue(), 150f), $"hpӦΪ150��ʵ��Ϊ{hp.GetValue()}");

            // 6. ���ӹ�����Buff��HP Buff
            buffHandle.ApplyToProperty(atkBuff, atk);
            buffHandle.ApplyToProperty(hpBuff, hp);
            Debug.Assert(Mathf.Approximately(atk.GetValue(), 30f), $"atk���Ӻ�ӦΪ30��ʵ��Ϊ{atk.GetValue()}");
            Debug.Assert(Mathf.Approximately(hp.GetValue(), 200f), $"hp���Ӻ�ӦΪ200��ʵ��Ϊ{hp.GetValue()}");

            // 7. HP Buff�������޲���
            buffHandle.ApplyToProperty(hpBuff, hp);
            Debug.Assert(Mathf.Approximately(hp.GetValue(), 200f), $"hp��������ӦΪ200��ʵ��Ϊ{hp.GetValue()}");

            // 8. ģ��ʱ�����ţ�����Buff����
            manager.Update(6f); // atkBuff���ڣ�defBuffδ���ڣ�hpBuffδ����
            Debug.Assert(Mathf.Approximately(atk.GetValue(), 20f), $"atk���ں�ӦΪ20��ʵ��Ϊ{atk.GetValue()}");
            Debug.Assert(Mathf.Approximately(def.GetValue(), 20f), $"defδ����ӦΪ20��ʵ��Ϊ{def.GetValue()}");
            Debug.Assert(Mathf.Approximately(hp.GetValue(), 200f), $"hpδ����ӦΪ200��ʵ��Ϊ{hp.GetValue()}");

            // 9. �ٹ�3�룬����Buff����
            manager.Update(4.1f); // defBuff��hpBuff����
            Debug.Assert(Mathf.Approximately(def.GetValue(), 10f), $"def���ں�ӦΪ10��ʵ��Ϊ{def.GetValue()}");
            Debug.Assert(Mathf.Approximately(hp.GetValue(), 100f), $"hp���ں�ӦΪ100��ʵ��Ϊ{hp.GetValue()}");

            // 10. ����Ӧ��Buff�������Ƴ�һ��
            buffHandle.ApplyToProperty(atkBuff, atk);
            buffHandle.ApplyToProperty(atkBuff, atk);
            Debug.Assert(Mathf.Approximately(atk.GetValue(), 30f), $"atk�ٴε���ӦΪ30��ʵ��Ϊ{atk.GetValue()}");
            manager.RemoveBuffStack("atk_up", 1);
            Debug.Assert(Mathf.Approximately(atk.GetValue(), 25f), $"�Ƴ�һ��atk_up��ӦΪ25��ʵ��Ϊ{atk.GetValue()}");
            manager.RemoveBuff("atk_up");
            Debug.Assert(Mathf.Approximately(atk.GetValue(), 20f), $"�Ƴ�����atk_up��ӦΪ20��ʵ��Ϊ{atk.GetValue()}");


            Debug.Log("ComplexBuffStacking ��Ԫ����ͨ����");
        }

        /// <summary>
        /// ʾ����Buffϵͳ������ϵͳ�ļ���ʹ��
        /// չʾ���ʹ��BuffManager��BuffHandle�����ɫ�Ķ�������
        /// </summary>
        void Example_BuffAndPropertyIntegration()
        {
            Debug.Log("=== Buffϵͳ������ϵͳ����ʾ�� ===");

            // ������ɫ��������
            var maxHp = new GameProperty("MaxHP", 100f);
            var attack = new GameProperty("Attack", 20f);
            var defense = new GameProperty("Defense", 10f);
            var critRate = new GameProperty("CritRate", 0.05f); // 5%����������

            // ����������ԣ����չ����� = �������� * (1 + ������)
            var finalAttack = new CombinePropertyCustom("FinalAttack");
            finalAttack.RegisterProperty(attack);
            finalAttack.RegisterProperty(critRate);
            finalAttack.Calculater = prop => {
                float baseAtk = prop.GetProperty("Attack").GetValue();
                float crit = prop.GetProperty("CritRate").GetValue();
                return baseAtk * (1 + crit); // �����˺���Ϊ������ * (1 + ������)
            };

            // ��ʼ״̬��ӡ
            Debug.Log($"��ʼ״̬ - �������: {maxHp.GetValue()}, ������: {attack.GetValue()}, " +
                      $"������: {defense.GetValue()}, ������: {critRate.GetValue() * 100}%, " +
                      $"���չ���: {finalAttack.GetValue()}");

            // ����Buff�������ʹ�����
            var buffManager = new BuffManager();
            var buffHandle = new BuffHandle(buffManager);

            // ������ͬ���͵�Buff
            // 1. ����ҩˮ�����ӹ�����10�㣬����30��
            var atkBuff = new Buff(
                "StrengthPotion",
                new FloatModifier(ModifierType.Add, 0, 10f),
                30f,
                canStack: true
            );

            // 2. �������ܣ����ӷ�����50%������20��
            var defBuff = new Buff(
                "DefenseShield",
                new FloatModifier(ModifierType.Mul, 0, 1.5f),
                20f,
                canStack: false
            );

            // 3. ����ǿ���������������30�㣬����60��
            var hpBuff = new Buff(
                "VitalityBoost",
                new FloatModifier(ModifierType.Add, 0, 30f),
                60f,
                canStack: true
            )
            { MaxStackCount = 3 }; // ���������Ӳ���Ϊ3

            // 4. ����ף�������ӱ�����8%������45��
            var critBuff = new Buff(
                "CriticalBlessing",
                new FloatModifier(ModifierType.Add, 0, 0.08f),
                45f,
                canStack: false
            );

            // Ӧ��Buff
            buffHandle.ApplyToProperty(atkBuff, attack);
            buffHandle.ApplyToProperty(defBuff, defense);
            buffHandle.ApplyToProperty(hpBuff, maxHp);
            buffHandle.ApplyToProperty(critBuff, critRate);

            // ��ӡBuffЧ��
            Debug.Log($"Ӧ��Buff�� - �������: {maxHp.GetValue()}, ������: {attack.GetValue()}, " +
                      $"������: {defense.GetValue()}, ������: {critRate.GetValue() * 100}%, " +
                      $"���չ���: {finalAttack.GetValue()}");

            // ����һ������ҩˮ������ǿ��
            buffHandle.ApplyToProperty(atkBuff, attack);
            buffHandle.ApplyToProperty(hpBuff, maxHp);

            // ��ӡ����Ч��
            Debug.Log($"����Buff�� - �������: {maxHp.GetValue()}, ������: {attack.GetValue()}, " +
                      $"������: {defense.GetValue()}, ������: {critRate.GetValue() * 100}%, " +
                      $"���չ���: {finalAttack.GetValue()}");

            // ģ��ʱ�����ţ�����ҩˮ����
            buffManager.Update(35f);

            // ��ӡ����Buff���ں�Ч��
            Debug.Log($"����Buff���ں� - �������: {maxHp.GetValue()}, ������: {attack.GetValue()}, " +
                      $"������: {defense.GetValue()}, ������: {critRate.GetValue() * 100}%, " +
                      $"���չ���: {finalAttack.GetValue()}");
        }
    }
}