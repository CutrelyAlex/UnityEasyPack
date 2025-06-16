using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPGPack
{
    public class BuffExample : MonoBehaviour
    {
        private BuffManager _buffManager;
        private GameObject _dummyTarget;
        private GameObject _dummyCreator;
        void Start()
        {
            _buffManager = new BuffManager();
            _dummyTarget = new GameObject("DummyTarget");
            _dummyCreator = new GameObject("DummyCreator");

            Debug.Log("��ʼBuffϵͳ��Ԫ����");
            TestCastModifierToPropertyAdd();

            Debug.Log("Buffϵͳ��Ԫ�������");
        }

        private void TestCastModifierToPropertyAdd()
        {
            // 1. ������ע��һ�� CombinePropertySingle
            const string propertyId = "TestProperty";
            const float baseValue = 100f;
            var combineProperty = new CombinePropertySingle(propertyId, baseValue);
            CombineGamePropertyManager.AddOrUpdate(combineProperty);

            // ��ȡ���Եĳ�ʼֵ
            GameProperty property = combineProperty.GetProperty();
            float initialValue = property.GetValue();
            Debug.Log($"��ʼ����ֵ: {initialValue}");

            // 2. ����һ����������ֵ�����η�
            const float modifierValue = 50f;
            var modifier = new FloatModifier(ModifierType.Add, 0, modifierValue);

            // 3. ���� CastModifierToProperty ģ��
            var castModule = new CastModifierToProperty(modifier, propertyId);

            // 4. ����һ�� BuffData �����ģ��
            var buffData = new BuffData
            {
                ID = "TestBuff",
                Name = "����Buff",
                Duration = 5f,
                BuffModules = new List<BuffModule> { castModule }
            };

            // 5. ���� Buff
            var buff = _buffManager.AddBuff(buffData, _dummyCreator, _dummyTarget);

            // 6. ��֤���η��ѱ�Ӧ�õ�����
            float modifiedValue = property.GetValue();
            Debug.Log($"�޸ĺ�����ֵ: {modifiedValue}");

            // ���ԣ�ȷ������ֵ����ȷ����
            Debug.Assert(modifiedValue == initialValue + modifierValue,
                $"���η�δ��ȷ��ӵ�����! ����ֵ: {initialValue + modifierValue}, ʵ��ֵ: {modifiedValue}");

            // 7. �����Ƴ� Buff ʱ���η��Ƿ��Ƴ�
            _buffManager.RemoveBuff(buff);

            // 8. ��֤���η��ѱ��Ƴ�
            float valueAfterRemoval = property.GetValue();
            Debug.Log($"�Ƴ�Buff������ֵ: {valueAfterRemoval}");

            // ���ԣ�ȷ������ֵ�ѻָ�����ʼֵ
            Debug.Assert(valueAfterRemoval == initialValue,
                $"�Ƴ�Buff�����η�δ����ȷ�Ƴ�! ����ֵ: {initialValue}, ʵ��ֵ: {valueAfterRemoval}");

            // 9. ���Զ�����Ч��
            // ������������ѵ���Buff
            buffData.MaxStacks = 2;
            buff = _buffManager.AddBuff(buffData, _dummyCreator, _dummyTarget);
            // ��ӵڶ���
            _buffManager.AddBuff(buffData, _dummyCreator, _dummyTarget);

            // ��֤���η��ѱ���������
            float valueWithTwoStacks = property.GetValue();
            Debug.Log($"����ѵ�������ֵ: {valueWithTwoStacks}");

            // ���ԣ�ȷ������ֵ����ȷ��������
            Debug.Assert(valueWithTwoStacks == initialValue + modifierValue * 2,
                $"�������η�δ��ȷ����! ����ֵ: {initialValue + modifierValue * 2}, ʵ��ֵ: {valueWithTwoStacks}");

            // ����
            _buffManager.RemoveAllBuffs(_dummyTarget);
            CombineGamePropertyManager.Remove(propertyId);
        }
    }
}