using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// һ��BuffModule�����ڽ�ModifierӦ�õ�GameProperty�ϡ�
    /// �ڲ�ͬ�Ļص��¼�(�紴�����Ƴ���)�У����Զ�Ŀ���GameProperty��ӻ��Ƴ���������
    /// </summary>
    public class CastModifierToProperty : BuffModule
    {
        public CombineGamePropertyManager CombineGamePropertyManager { get; set; }
        /// <summary>
        /// ҪӦ�õ�������
        /// </summary>
        public IModifier Modifier { get; set; }

        /// <summary>
        /// ������Ե�ID�����ڴ�CombineGamePropertyManager�л�ȡ��Ӧ���������
        /// </summary>
        public string CombinePropertyID { get; set; }

        /// <summary>
        /// �������Ե�ID����������������в�����Ӧ��GameProperty
        /// ��Ϊ�գ����Ϊ����ʹ��������ԵĽ������
        /// </summary>
        public string PropertyID { get; set; }

        /// <summary>
        /// �洢��Ӧ�õ������������������Ƴ�
        /// </summary>
        private readonly List<IModifier> _appliedModifiers = new List<IModifier>();

        /// <summary>
        /// ����һ���µ�CastModifierToPropertyʵ������ע�����������ڻص�
        /// </summary>
        /// <param name="modifier">ҪӦ�õ�������</param>
        /// <param name="combinePropertyID">�������ID</param>
        /// <param name="propertyID">��������ID(��ѡ)</param>
        public CastModifierToProperty(IModifier modifier, string combinePropertyID,CombineGamePropertyManager combineGamePropertyManager, string propertyID = "")
        {
            Modifier = modifier;
            CombinePropertyID = combinePropertyID;
            PropertyID = propertyID;
            CombineGamePropertyManager = combineGamePropertyManager;

            // ע������������ڻص�
            RegisterCallback(BuffCallBackType.OnCreate, OnCreate);
            RegisterCallback(BuffCallBackType.OnAddStack, OnAddStack);
            RegisterCallback(BuffCallBackType.OnRemove, OnRemove);
            RegisterCallback(BuffCallBackType.OnReduceStack, OnReduceStack);
        }

        /// <summary>
        /// ����Buff����ʱ�Ļص�
        /// </summary>
        private void OnCreate(Buff buff, object[] parameters)
        {
            AddModifier();
        }

        /// <summary>
        /// ����Buff��������ʱ�Ļص�
        /// </summary>
        private void OnAddStack(Buff buff, object[] parameters)
        {
            AddModifier();
        }

        /// <summary>
        /// ����Buff�Ƴ�ʱ�Ļص�
        /// </summary>
        private void OnRemove(Buff buff, object[] parameters)
        {
            RemoveAllModifiers();
        }

        /// <summary>
        /// ����Buff��������ʱ�Ļص�
        /// </summary>
        private void OnReduceStack(Buff buff, object[] parameters)
        {
            RemoveSingleModifier();
        }

        /// <summary>
        /// �����������Ŀ������
        /// </summary>
        private void AddModifier()
        {
            var property = CombineGamePropertyManager.GetGamePropertyFromCombine(CombinePropertyID, PropertyID);
            if (property == null || Modifier == null)
                return;

            var modifierClone = Modifier.Clone();
            property.AddModifier(modifierClone);

            // ��¼��Ӧ�õ��������Ա�����Ƴ�
            _appliedModifiers.Add(modifierClone);
        }

        /// <summary>
        /// �Ƴ�����������
        /// </summary>
        private void RemoveSingleModifier()
        {
            var property = CombineGamePropertyManager.GetGamePropertyFromCombine(CombinePropertyID, PropertyID);
            if (property == null || _appliedModifiers.Count == 0)
                return;

            // �Ƴ������ӵ�������
            var lastModifier = _appliedModifiers[^1];
            property.RemoveModifier(lastModifier);
            _appliedModifiers.RemoveAt(_appliedModifiers.Count - 1);
        }

        /// <summary>
        /// �Ƴ�������Ӧ�õ�������
        /// </summary>
        private void RemoveAllModifiers()
        {
            var property = CombineGamePropertyManager.GetGamePropertyFromCombine(CombinePropertyID, PropertyID);
            if (property == null)
                return;

            // �Ƴ�������Ӧ�õ�������
            property.RemoveModifiers(_appliedModifiers);
            _appliedModifiers.Clear();
        }
    }
}