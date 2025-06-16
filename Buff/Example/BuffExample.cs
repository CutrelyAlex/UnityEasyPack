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

            Debug.Log("开始Buff系统单元测试");
            TestCastModifierToPropertyAdd();

            Debug.Log("Buff系统单元测试完成");
        }

        private void TestCastModifierToPropertyAdd()
        {
            // 1. 创建和注册一个 CombinePropertySingle
            const string propertyId = "TestProperty";
            const float baseValue = 100f;
            var combineProperty = new CombinePropertySingle(propertyId, baseValue);
            CombineGamePropertyManager.AddOrUpdate(combineProperty);

            // 获取属性的初始值
            GameProperty property = combineProperty.GetProperty();
            float initialValue = property.GetValue();
            Debug.Log($"初始属性值: {initialValue}");

            // 2. 创建一个增加属性值的修饰符
            const float modifierValue = 50f;
            var modifier = new FloatModifier(ModifierType.Add, 0, modifierValue);

            // 3. 创建 CastModifierToProperty 模块
            var castModule = new CastModifierToProperty(modifier, propertyId);

            // 4. 创建一个 BuffData 并添加模块
            var buffData = new BuffData
            {
                ID = "TestBuff",
                Name = "测试Buff",
                Duration = 5f,
                BuffModules = new List<BuffModule> { castModule }
            };

            // 5. 创建 Buff
            var buff = _buffManager.AddBuff(buffData, _dummyCreator, _dummyTarget);

            // 6. 验证修饰符已被应用到属性
            float modifiedValue = property.GetValue();
            Debug.Log($"修改后属性值: {modifiedValue}");

            // 断言：确认属性值已正确增加
            Debug.Assert(modifiedValue == initialValue + modifierValue,
                $"修饰符未正确添加到属性! 期望值: {initialValue + modifierValue}, 实际值: {modifiedValue}");

            // 7. 测试移除 Buff 时修饰符是否被移除
            _buffManager.RemoveBuff(buff);

            // 8. 验证修饰符已被移除
            float valueAfterRemoval = property.GetValue();
            Debug.Log($"移除Buff后属性值: {valueAfterRemoval}");

            // 断言：确认属性值已恢复到初始值
            Debug.Assert(valueAfterRemoval == initialValue,
                $"移除Buff后修饰符未被正确移除! 期望值: {initialValue}, 实际值: {valueAfterRemoval}");

            // 9. 测试多层叠加效果
            // 创建具有两层堆叠的Buff
            buffData.MaxStacks = 2;
            buff = _buffManager.AddBuff(buffData, _dummyCreator, _dummyTarget);
            // 添加第二层
            _buffManager.AddBuff(buffData, _dummyCreator, _dummyTarget);

            // 验证修饰符已被叠加两次
            float valueWithTwoStacks = property.GetValue();
            Debug.Log($"两层堆叠后属性值: {valueWithTwoStacks}");

            // 断言：确认属性值已正确叠加两次
            Debug.Assert(valueWithTwoStacks == initialValue + modifierValue * 2,
                $"两层修饰符未正确叠加! 期望值: {initialValue + modifierValue * 2}, 实际值: {valueWithTwoStacks}");

            // 清理
            _buffManager.RemoveAllBuffs(_dummyTarget);
            CombineGamePropertyManager.Remove(propertyId);
        }
    }
}