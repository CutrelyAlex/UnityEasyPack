using System;
using System.Collections.Generic;

namespace EasyPack
{
    // 条件数据化表示
    [Serializable]
    public class SerializableCondition
    {
        public string Kind; // 条件类型标识（自定义短名称）
        public List<CustomDataEntry> Params = new List<CustomDataEntry>();
    }

    // 条件序列化器接口（将 IItemCondition <-> SerilizableCondition）
    public interface IConditionSerializer
    {
        string Kind { get; }
        bool CanHandle(IItemCondition condition);
        SerializableCondition Serialize(IItemCondition condition);
        IItemCondition Deserialize(SerializableCondition dto);
    }
}