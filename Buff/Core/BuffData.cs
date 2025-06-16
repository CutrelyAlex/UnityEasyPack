using System.Collections.Generic;
using UnityEngine;

namespace RPGPack
{
    public class BuffData
    {
        public string ID;
        public string Name;
        public string Description;
        public Sprite Sprite;
        public Dictionary<string, object> CustomData;

        public int MaxStacks = 1;
        public float Duration = -1f; // -1 ��������Ч��
        public float TriggerInterval = 1f; // Buff ÿ�� ���� �ļ��ʱ��

        public BuffSuperpositionDurationType BuffSuperpositionStrategy = BuffSuperpositionDurationType.Add;
        public BuffSuperpositionStacksType BuffSuperpositionStacksStrategy = BuffSuperpositionStacksType.Add;

        public BuffRemoveType BuffRemoveStrategy = BuffRemoveType.All;

        public bool TriggerOnCreate = false;
        public List<BuffModule> BuffModules = new List<BuffModule>();

        public List<string> Tags = new List<string>();
        public List<string> Layers = new List<string>();

        public bool HasTag(string tag) => Tags.Contains(tag);
        public bool InLayer(string layer) => Layers.Contains(layer);
    }

    // Buff����ʱ����Ӳ���
    public enum BuffSuperpositionDurationType
    {
        Add, // ���ӳ���ʱ��
        ResetThenAdd, // ���ó���ʱ����ٵ���
        Reset, // ���ó���ʱ��
        Keep // ����ԭ�г���ʱ�䲻��
    }

    // Buff���Ӷѵ�������
    public enum BuffSuperpositionStacksType
    {
        Add, // ���Ӷѵ���
        ResetThenAdd, // ���öѵ������ٵ���
        Reset, // ���öѵ���
        Keep // ����ԭ�жѵ�������
    }

    public enum BuffRemoveType
    { 
        All,
        OneStack,
        Manual,
    }
}
