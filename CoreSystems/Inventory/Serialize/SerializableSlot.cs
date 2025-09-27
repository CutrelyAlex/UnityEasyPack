using System;
using System.Collections.Generic;
using UnityEngine;
namespace EasyPack
{
    [Serializable]
    public class SerializableSlot
    {
        public int Index;
        public string ItemJson;
        public int ItemCount; 
        public SerializableCondition SlotCondition;
    }
}
