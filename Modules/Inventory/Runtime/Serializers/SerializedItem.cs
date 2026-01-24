using System;
using System.Collections.Generic;
using EasyPack.CustomData;
using EasyPack.Serialization;

namespace EasyPack.InventorySystem
{
    [Serializable]
    public class SerializedItem : ISerializable
    {
        public string ID;
        public string Name;
        public string Type;
        public string Description;
        public float Weight;
        public bool IsStackable;
        public int MaxStackCount;
        public bool isContainerItem;
        public long ItemUID;
        
        /// <summary>
        ///     物品数量（从Item.Count序列化）
        /// </summary>
        public int Count = 1;
        
        /// <summary>
        ///     物品分类
        /// </summary>
        public string Category;
        
        /// <summary>
        ///     物品标签
        /// </summary>
        public string[] Tags;
        
        /// <summary>
        ///     运行时元数据
        /// </summary>
        public List<CustomDataEntry> RuntimeMetadata;

        /// <summary>
        ///     自定义数据列表
        /// </summary>
        public List<CustomDataEntry> CustomData;

        public List<string> ContainerIds;
    }
}