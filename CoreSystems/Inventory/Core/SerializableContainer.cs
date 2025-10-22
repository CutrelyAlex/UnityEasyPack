using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack
{
    [Serializable]
    public class SerializedContainer
    {
        public string ContainerKind;
        public string ID;
        public string Name;
        public string Type;
        public int Capacity;

        public bool IsGrid;
        public Vector2 Grid; // 若为网格容器可使用

        public List<SerializedSlot> Slots = new();
        public List<SerializedCondition> ContainerConditions = new();
    }
}