using System;
using System.Collections.Generic;

namespace EasyPack
{
    /// <summary>
    /// Card 的可序列化中间数据结构
    /// </summary>
    [Serializable]
    public class SerializableCard
    {
        // 来自 CardData 的静态字段
        public string ID;
        public string Name;
        public string Description;
        public CardCategory Category;
        public string[] DefaultTags;

        // 运行时实例字段
        public int Index;
        public List<SerializableGameProperty> Properties;
        public string[] Tags;
        public List<SerializableCard> Children;
        public bool IsIntrinsic;
    }
}
