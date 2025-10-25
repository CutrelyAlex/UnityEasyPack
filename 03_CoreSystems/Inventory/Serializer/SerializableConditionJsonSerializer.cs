using System;
using UnityEngine;

namespace EasyPack.InventorySystem
{
    /// <summary>
    /// 条件类型，必须实现 ISerializableCondition 并有无参构造函数
    /// </summary>
    public class SerializableConditionJsonSerializer<T> : JsonSerializerBase<T>
        where T : ISerializableCondition, new()
    {
        public override string SerializeToJson(T obj)
        {
            if (obj == null) return null;

            try
            {
                var dto = obj.ToDto();
                if (dto != null)
                {
                    return JsonUtility.ToJson(dto);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SerializableConditionJsonSerializer<{typeof(T).Name}>] 序列化失败: {e.Message}");
            }

            return null;
        }

        public override T DeserializeFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return default;

            try
            {
                var dto = JsonUtility.FromJson<SerializedCondition>(json);
                if (dto == null) return default;

                var condition = new T();
                return (T)condition.FromDto(dto);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SerializableConditionJsonSerializer<{typeof(T).Name}>] 反序列化失败: {e.Message}");
                return default;
            }
        }
    }
}

