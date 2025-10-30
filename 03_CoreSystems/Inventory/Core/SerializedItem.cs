using System.Collections.Generic;

namespace EasyPack.InventorySystem
{
    [System.Serializable]
    public class SerializedItem : ISerializable
    {
        public string ID;
        public string Name;
        public string Type;
        public string Description;
        public float Weight;
        public bool IsStackable;
        public int MaxStackCount;
        public bool isContanierItem;
        public List<CustomDataEntry> Attributes;
        public List<string> ContainerIds;
    }
}
