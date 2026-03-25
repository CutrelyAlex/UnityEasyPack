using System;
using EasyPack.Serialization;

namespace EasyPack.EmeCardSystem
{
    [Serializable]
    public class CardEngineDTO : ISerializable
    {
        public SerializableCard[] Cards;
        public SerializableCardMetadata[] Metadata;
    }

    [Serializable]
    public class SerializableCardMetadata : ISerializable
    {
        public long UID;
        public string MetadataJson;
    }
}