using System;
using EasyPack.Serialization;

namespace EasyPack.EmeCardSystem
{
    [Serializable]
    public class CardEngineDTO : ISerializable
    {
        public CardCategoryManagerState CategoryState;
    }
}