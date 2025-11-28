using System;
using EasyPack.Serialization;

namespace EasyPack.InventorySystem
{
    [Serializable]
    public class SerializedSlot : ISerializable
    {
        public int Index;
        public string ItemJson;
        public int ItemCount;
        public SerializedCondition SlotCondition;
    }
}