using System;
using EasyPack.GamePropertySystem;
using EasyPack.Serialization;
using UnityEngine;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     Card 的可序列化中间数据结构
    ///     
    ///     字段说明：
    ///     - ID: 模板标识
    ///     - Properties, ChildrenUIDs, IntrinsicChildrenUIDs: 运行时实例状态
    ///     - Position, HasPosition: 空间位置信息
    ///     - UID, Index: 实例标记
    /// </summary>
    [Serializable]
    public class SerializableCard : ISerializable
    {
        // 模板标识
        public string ID;

        // 运行时实例字段
        public int Index;
        public long UID = -1;
        public SerializableGameProperty[] Properties;
        public long[] ChildrenUIDs;  // 子卡牌的 UID 引用
        public long[] IntrinsicChildrenUIDs;  // 固有子卡牌的 UID 列表

        // 位置信息
        public bool HasPosition;
        public Vector3Int Position;
    }

    /// <summary>
    ///     子卡数组的包装器，用于 JSON 序列化
    /// </summary>
    [Serializable]
    internal class SerializableCardArray : ISerializable
    {
        public SerializableCard[] Cards;
    }
}