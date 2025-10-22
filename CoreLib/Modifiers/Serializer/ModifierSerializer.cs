using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// IModifier 的 JSON 序列化器
    /// </summary>
    public class ModifierSerializer : JsonSerializerBase<IModifier>
    {
        public override string SerializeToJson(IModifier obj)
        {
            if (obj == null) return null;

            var serializable = new SerializableModifier
            {
                Type = obj.Type,
                Priority = obj.Priority
            };

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

            return JsonUtility.ToJson(serializable);
        }

        public override IModifier DeserializeFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;

            var serializable = JsonUtility.FromJson<SerializableModifier>(json);
            if (serializable == null) return null;

            if (serializable.IsRangeModifier)
            {
                return new RangeModifier(
                    serializable.Type,
                    serializable.Priority,
                    serializable.RangeValue
                );
            }
            else
            {
                return new FloatModifier(
                    serializable.Type,
                    serializable.Priority,
                    serializable.FloatValue
                );
            }
        }
    }

    /// <summary>
    /// FloatModifier 的 JSON 序列化器
    /// </summary>
    public class FloatModifierSerializer : JsonSerializerBase<FloatModifier>
    {
        public override string SerializeToJson(FloatModifier obj)
        {
            if (obj == null) return null;

            var serializable = new SerializableModifier
            {
                Type = obj.Type,
                Priority = obj.Priority,
                IsRangeModifier = false,
                FloatValue = obj.Value
            };

            return JsonUtility.ToJson(serializable);
        }

        public override FloatModifier DeserializeFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;

            var serializable = JsonUtility.FromJson<SerializableModifier>(json);
            if (serializable == null) return null;

            return new FloatModifier(
                serializable.Type,
                serializable.Priority,
                serializable.FloatValue
            );
        }
    }

    /// <summary>
    /// RangeModifier 的 JSON 序列化器
    /// </summary>
    public class RangeModifierSerializer : JsonSerializerBase<RangeModifier>
    {
        public override string SerializeToJson(RangeModifier obj)
        {
            if (obj == null) return null;

            var serializable = new SerializableModifier
            {
                Type = obj.Type,
                Priority = obj.Priority,
                IsRangeModifier = true,
                RangeValue = obj.Value
            };

            return JsonUtility.ToJson(serializable);
        }

        public override RangeModifier DeserializeFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;

            var serializable = JsonUtility.FromJson<SerializableModifier>(json);
            if (serializable == null) return null;

            return new RangeModifier(
                serializable.Type,
                serializable.Priority,
                serializable.RangeValue
            );
        }
    }

    /// <summary>
    /// 修饰器列表的 JSON 序列化器
    /// </summary>
    public class ModifierListSerializer : JsonSerializerBase<List<IModifier>>
    {
        public override string SerializeToJson(List<IModifier> obj)
        {
            if (obj == null || obj.Count == 0) return null;

            var wrapper = new ModifierListWrapper
            {
                Modifiers = new List<SerializableModifier>()
            };

            foreach (var modifier in obj)
            {
                if (modifier == null) continue;

                var serializable = new SerializableModifier
                {
                    Type = modifier.Type,
                    Priority = modifier.Priority
                };

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

            return JsonUtility.ToJson(wrapper);
        }

        public override List<IModifier> DeserializeFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return new List<IModifier>();

            var wrapper = JsonUtility.FromJson<ModifierListWrapper>(json);
            if (wrapper?.Modifiers == null) return new List<IModifier>();

            var result = new List<IModifier>();

            foreach (var serializable in wrapper.Modifiers)
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

        [Serializable]
        private class ModifierListWrapper
        {
            public List<SerializableModifier> Modifiers;
        }
    }
}