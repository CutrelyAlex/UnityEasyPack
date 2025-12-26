using System;
using EasyPack.Category;
using EasyPack.Serialization;

namespace EasyPack.EmeCardSystem
{
    [Serializable]
    public class CardEngineDTO : ISerializable
    {
        public SerializableCategoryManagerState<Card, long> CategoryState;
    }
}