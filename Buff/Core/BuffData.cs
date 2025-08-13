using System.Collections.Generic;
using UnityEngine;

namespace EasyPack
{
    public class BuffData
    {
        public string ID;
        public string Name;
        public string Description;
        public Sprite Sprite;
        public List<CustomDataEntry> CustomData;

        public int MaxStacks = 1;
        public float Duration = -1f; // -1 代表永久效果
        public float TriggerInterval = 1f; // Buff 每次 触发 的间隔时间

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

    // Buff持续时间叠加策略
    public enum BuffSuperpositionDurationType
    {
        Add, // 叠加持续时间
        ResetThenAdd, // 重置持续时间后再叠加
        Reset, // 重置持续时间
        Keep // 保持原有持续时间不变
    }

    // Buff叠加堆叠数策略
    public enum BuffSuperpositionStacksType
    {
        Add, // 叠加堆叠数
        ResetThenAdd, // 重置堆叠数后再叠加
        Reset, // 重置堆叠数
        Keep // 保持原有堆叠数不变
    }

    public enum BuffRemoveType
    {
        All,
        OneStack,
        Manual,
    }
}
