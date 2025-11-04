using System;
using System.Collections.Generic;

namespace EasyPack.InventorySystem
{
    // 条件数据化表示
    [Serializable]
    public class SerializedCondition : ISerializable
    {
        public string Kind;
        public List<CustomDataEntry> Params = new();
    }

    // 条件序列化器接口（将 IItemCondition <-> SerializedCondition）
    public interface IConditionSerializer
    {
        string Kind { get; }
        bool CanHandle(IItemCondition condition);
        SerializedCondition Serialize(IItemCondition condition);
        IItemCondition Deserialize(SerializedCondition dto);
    }
}
