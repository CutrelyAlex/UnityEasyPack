using System;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// 修饰器的可序列化表示形式
    /// </summary>
    [Serializable]
    public class SerializableModifier
    {
        public ModifierType Type;
        public int Priority;
        public float FloatValue;
        public Vector2 RangeValue;
        public bool IsRangeModifier;
    }
}