using System;
using System.Collections.Generic;
using EasyPack.Serialization;
using UnityEngine;

namespace EasyPack.Modifiers
{
    /// <summary>
    ///     IModifier 的 JSON 序列化器
    /// </summary>
    public class ModifierSerializer : ITypeSerializer<IModifier, SerializableModifier>
    {
        /// <summary>
        ///     将 IModifier 转换为可序列化的 DTO 对象
        /// </summary>
        /// <param name="obj">要转换的 IModifier 对象</param>
        /// <returns>SerializableModifier 对象，如果输入为 null 则返回 null</returns>
        public SerializableModifier ToSerializable(IModifier obj)
        {
            if (obj == null) return null;

            var serializable = new SerializableModifier { Type = obj.Type, Priority = obj.Priority };

            if (obj is FloatModifier floatModifier)
            {
                serializable.IsRangeModifier = false;
                serializable.FloatValue = floatModifier.Value;
            }
            else if (obj is RangeModifier rangeModifier)
            {
                serializable.IsRangeModifier = true;
                serializable.RangeValue = rangeModifier.Value;
            }
            else
            {
                throw new NotSupportedException($"Unsupported modifier type: {obj.GetType().Name}");
            }

            return serializable;
        }

        public IModifier FromSerializable(SerializableModifier serializable)
        {
            if (serializable == null) return null;

            if (serializable.IsRangeModifier)
            {
                return new RangeModifier(
                    serializable.Type,
                    serializable.Priority,
                    serializable.RangeValue
                );
            }

            return new FloatModifier(
                serializable.Type,
                serializable.Priority,
                serializable.FloatValue
            );
        }

        public string ToJson(SerializableModifier dto) => dto == null ? null : JsonUtility.ToJson(dto);

        public SerializableModifier FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            return JsonUtility.FromJson<SerializableModifier>(json);
        }

        public string SerializeToJson(IModifier obj)
        {
            SerializableModifier serializable = ToSerializable(obj);
            return ToJson(serializable);
        }

        public IModifier DeserializeFromJson(string json)
        {
            SerializableModifier serializable = FromJson(json);
            return FromSerializable(serializable);
        }
    }

    /// <summary>
    ///     FloatModifier 的 JSON 序列化器
    /// </summary>
    public class FloatModifierSerializer : ITypeSerializer<FloatModifier, SerializableModifier>
    {
        public SerializableModifier ToSerializable(FloatModifier obj)
        {
            if (obj == null) return null;
            return new SerializableModifier
            {
                Type = obj.Type, Priority = obj.Priority, IsRangeModifier = false, FloatValue = obj.Value,
            };
        }

        public FloatModifier FromSerializable(SerializableModifier serializable)
        {
            if (serializable == null) return null;
            return new(
                serializable.Type,
                serializable.Priority,
                serializable.FloatValue
            );
        }

        public string ToJson(SerializableModifier dto) => dto == null ? null : JsonUtility.ToJson(dto);

        public SerializableModifier FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            return JsonUtility.FromJson<SerializableModifier>(json);
        }

        public string SerializeToJson(FloatModifier obj)
        {
            SerializableModifier dto = ToSerializable(obj);
            return ToJson(dto);
        }

        public FloatModifier DeserializeFromJson(string json)
        {
            SerializableModifier dto = FromJson(json);
            return FromSerializable(dto);
        }
    }

    /// <summary>
    ///     RangeModifier 的 JSON 序列化器
    /// </summary>
    public class RangeModifierSerializer : ITypeSerializer<RangeModifier, SerializableModifier>
    {
        public SerializableModifier ToSerializable(RangeModifier obj)
        {
            if (obj == null) return null;
            return new SerializableModifier
            {
                Type = obj.Type, Priority = obj.Priority, IsRangeModifier = true, RangeValue = obj.Value,
            };
        }

        public RangeModifier FromSerializable(SerializableModifier serializable)
        {
            if (serializable == null) return null;
            return new(
                serializable.Type,
                serializable.Priority,
                serializable.RangeValue
            );
        }

        public string ToJson(SerializableModifier dto) => dto == null ? null : JsonUtility.ToJson(dto);

        public SerializableModifier FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            return JsonUtility.FromJson<SerializableModifier>(json);
        }

        public string SerializeToJson(RangeModifier obj)
        {
            SerializableModifier dto = ToSerializable(obj);
            return ToJson(dto);
        }

        public RangeModifier DeserializeFromJson(string json)
        {
            SerializableModifier dto = FromJson(json);
            return FromSerializable(dto);
        }
    }

    /// <summary>
    ///     修饰器列表的 JSON 序列化器
    /// </summary>
    public class ModifierListSerializer : ITypeSerializer<List<IModifier>, ModifierListWrapper>
    {
        public ModifierListWrapper ToSerializable(List<IModifier> obj)
        {
            if (obj == null || obj.Count == 0) return null;

            var wrapper = new ModifierListWrapper { Modifiers = new() };

            foreach (IModifier modifier in obj)
            {
                if (modifier == null) continue;

                var serializable = new SerializableModifier { Type = modifier.Type, Priority = modifier.Priority };

                if (modifier is FloatModifier floatModifier)
                {
                    serializable.IsRangeModifier = false;
                    serializable.FloatValue = floatModifier.Value;
                }
                else if (modifier is RangeModifier rangeModifier)
                {
                    serializable.IsRangeModifier = true;
                    serializable.RangeValue = rangeModifier.Value;
                }

                wrapper.Modifiers.Add(serializable);
            }

            return wrapper;
        }

        public List<IModifier> FromSerializable(ModifierListWrapper wrapper)
        {
            if (wrapper?.Modifiers == null) return new();

            var result = new List<IModifier>();

            foreach (SerializableModifier serializable in wrapper.Modifiers)
            {
                IModifier modifier;

                if (serializable.IsRangeModifier)
                {
                    modifier = new RangeModifier(
                        serializable.Type,
                        serializable.Priority,
                        serializable.RangeValue
                    );
                }
                else
                {
                    modifier = new FloatModifier(
                        serializable.Type,
                        serializable.Priority,
                        serializable.FloatValue
                    );
                }

                result.Add(modifier);
            }

            return result;
        }

        public string ToJson(ModifierListWrapper dto) => dto == null ? null : JsonUtility.ToJson(dto);

        public ModifierListWrapper FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            return JsonUtility.FromJson<ModifierListWrapper>(json);
        }

        public string SerializeToJson(List<IModifier> obj)
        {
            ModifierListWrapper wrapper = ToSerializable(obj);
            return ToJson(wrapper);
        }

        public List<IModifier> DeserializeFromJson(string json)
        {
            ModifierListWrapper wrapper = FromJson(json);
            return FromSerializable(wrapper);
        }
    }

    /// <summary>
    ///     修饰器列表的包装器 DTO
    /// </summary>
    [Serializable]
    public class ModifierListWrapper : ISerializable
    {
        public List<SerializableModifier> Modifiers;
    }
}