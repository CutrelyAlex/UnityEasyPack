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

    // 条件序列化器注册表
    public static class ConditionSerializerRegistry
    {
        private static readonly Dictionary<string, IConditionSerializer> _byKind = new Dictionary<string, IConditionSerializer>();

        public static void Register(IConditionSerializer serializer)
        {
            if (serializer == null || string.IsNullOrEmpty(serializer.Kind)) return;
            _byKind[serializer.Kind] = serializer;
        }

        public static IConditionSerializer Get(string kind)
        {
            if (string.IsNullOrEmpty(kind)) return null;
            return _byKind.TryGetValue(kind, out var ser) ? ser : null;
        }

        public static IConditionSerializer FindFor(IItemCondition condition)
        {
            foreach (var kv in _byKind)
            {
                if (kv.Value.CanHandle(condition)) return kv.Value;
            }
            return null;
        }
    }
}