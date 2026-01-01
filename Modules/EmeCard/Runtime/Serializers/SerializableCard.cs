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
    ///     - ID, Name, Description: 来自 CardData，用作容错和审计（运行时由 CardData 提供）
    ///     - DefaultCategory: 来自 CardData，用作分类容错（运行时分类由 CategoryManager 提供）
    ///     - DefaultTags: 来自 CardData 编译时定义（注意：存档中的 Tags 数组是运行时标签，来自 CategoryManager，不应用 DefaultTags）
    ///     - Properties, ChildrenJson: 运行时实例状态
    ///     - Position, HasPosition: 空间位置信息
    ///     - UID, Index, IsIntrinsic: 实例和关系标记
    /// </summary>
    [Serializable]
    public class SerializableCard : ISerializable
    {
        // 来自 CardData 的静态字段
        public string ID;
        public string Name;
        public string Description;
        public string DefaultCategory;
        public string[] DefaultTags;

        // 运行时实例字段
        public int Index;
        public long UID = -1;
        public SerializableGameProperty[] Properties;
        // Tags 已删除：由 CategoryManager.Tags 保存和恢复
        public string ChildrenJson;
        public bool IsIntrinsic;
        // Category 已删除：由 CategoryManager.SerializedEntity.Category 保存

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