using System;
using System.Collections.Generic;
using EasyPack.GamePropertySystem;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    /// Card 的可序列化中间数据结构
    /// </summary>
    [Serializable]
    public class SerializableCard : ISerializable
    {
        // 来自 CardData 的静态字段
        public string ID;
        public string Name;
        public string Description;
        public CardCategory Category;
        public string[] DefaultTags;

        // 运行时实例字段
        public int Index;
        public SerializableGameProperty[] Properties;
        public string[] Tags;
        public string ChildrenJson;  
        public bool IsIntrinsic;
    }

    /// <summary>
    /// 子卡数组的包装器，用于 JSON 序列化
    /// </summary>
    [Serializable]
    internal class SerializableCardArray : ISerializable
    {
        public SerializableCard[] Cards;
    }
}

