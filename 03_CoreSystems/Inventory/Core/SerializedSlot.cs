using System;
namespace EasyPack
{
    [Serializable]
    public class SerializedSlot
    {
        public int Index;
        public string ItemJson;
        public int ItemCount;
        public SerializedCondition SlotCondition;
    }
}
