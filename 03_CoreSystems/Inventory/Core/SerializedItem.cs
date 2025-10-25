using System.Collections.Generic;

namespace EasyPack
{
    [System.Serializable]
    public class SerializedItem
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