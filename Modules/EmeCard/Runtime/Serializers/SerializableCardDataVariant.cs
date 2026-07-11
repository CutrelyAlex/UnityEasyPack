using System;
using EasyPack.CustomData;
using EasyPack.Serialization;

namespace EasyPack.EmeCardSystem
{
    [Serializable]
    public class SerializableCardDataVariant : ISerializable
    {
        public string ID;
        public string BaseID;

        public SerializableDefaultChildDiff[] AddedChildren;
        public SerializableDefaultChildDiff[] RemovedChildren;
        public SerializableDefaultChildModifyDiff[] ModifiedChildren;
        public SerializableDefaultChildDiff[] OrderedChildren;

        public CustomDataEntry[] ModifiedMetaData;
    }

    [Serializable]
    public class SerializableDefaultChildDiff : ISerializable
    {
        public string ChildID;
        public bool Intrinsic;
        public int Count = 1;
    }

    [Serializable]
    public class SerializableDefaultChildModifyDiff : ISerializable
    {
        public string FromChildID;
        public bool FromIntrinsic;

        public string ToChildID;
        public bool ToIntrinsic;

        public int Count = 1;
    }
}
