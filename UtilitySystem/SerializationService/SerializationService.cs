using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// 统一序列化服务实现
    /// </summary>
    public class SerializationService : ISerializationService
    {
        private readonly Dictionary<Type, ITypeSerializer> _serializers = new();
        private readonly object _lock = new();

        #region 注册管理

        public void RegisterSerializer<T>(ITypeSerializer<T> serializer)
        {
            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            RegisterSerializer((ITypeSerializer)serializer);
        }

        public void RegisterSerializer(ITypeSerializer serializer)
        {
            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            if (serializer.TargetType == null)
            {
                throw new ArgumentException("Serializer TargetType cannot be null", nameof(serializer));
            }

            lock (_lock)
            {
                _serializers[serializer.TargetType] = serializer;
            }
        }

        public bool HasSerializer(Type type)
        {
            if (type == null) return false;

            lock (_lock)
            {
                return _serializers.ContainsKey(type);
            }
        }

        public bool HasSerializer<T>()
        {
            return HasSerializer(typeof(T));
        }

        public SerializationStrategy GetSupportedStrategy(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            lock (_lock)
            {
                if (_serializers.TryGetValue(type, out var serializer))
                {
                    return serializer.SupportedStrategy;
                }
            }

            throw new SerializationException(
                $"No serializer found for type: {type.Name}",
                type,
                SerializationErrorCode.NoSerializerFound
            );
        }

        public SerializationStrategy GetSupportedStrategy<T>()
        {
            return GetSupportedStrategy(typeof(T));
        }

        private ITypeSerializer GetSerializer(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            lock (_lock)
            {
                // 1. 首先尝试精确匹配
                if (_serializers.TryGetValue(type, out var serializer))
                {
                    return serializer;
                }

                // 2. 如果精确匹配失败，尝试查找基类的序列化器
                Type currentType = type.BaseType;
                while (currentType != null)
                {
                    if (_serializers.TryGetValue(currentType, out serializer))
                    {
                        Debug.Log($"[SerializationService] Found serializer for base type {currentType.Name} when looking for {type.Name}");
                        return serializer;
                    }
                    currentType = currentType.BaseType;
                }

                // 3. 尝试查找接口的序列化器
                foreach (var interfaceType in type.GetInterfaces())
                {
                    if (_serializers.TryGetValue(interfaceType, out serializer))
                    {
                        Debug.Log($"[SerializationService] Found serializer for interface {interfaceType.Name} when looking for {type.Name}");
                        return serializer;
                    }
                }
            }

            throw new SerializationException(
                $"No serializer registered for type: {type.Name}. Please register a serializer first.",
                type,
                SerializationErrorCode.NoSerializerFound
            );
        }

        #endregion

        #region JSON 序列化

        public string SerializeToJson<T>(T obj)
        {
            if (obj == null)
            {
                Debug.LogWarning("[SerializationService] Attempting to serialize null object");
                return null;
            }

            return SerializeToJson(obj, typeof(T));
        }

        public string SerializeToJson(object obj, Type type)
        {
            if (obj == null)
            {
                Debug.LogWarning("[SerializationService] Attempting to serialize null object");
                return null;
            }

            if (type == null)
            {
                type = obj.GetType();
            }

            var serializer = GetSerializer(type);

            if (serializer.SupportedStrategy != SerializationStrategy.Json)
            {
                throw new SerializationException(
                    $"Serializer for type {type.Name} does not support JSON strategy",
                    type,
                    SerializationErrorCode.UnsupportedStrategy
                );
            }

            try
            {
                var json = serializer.SerializeToJson(obj);
                Debug.Log($"[SerializationService] Serialized {type.Name} to JSON ({json?.Length ?? 0} chars)");
                return json;
            }
            catch (SerializationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SerializationException(
                    $"Failed to serialize type {type.Name} to JSON: {ex.Message}",
                    type,
                    SerializationErrorCode.SerializationFailed,
                    ex
                );
            }
        }

        public T DeserializeFromJson<T>(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[SerializationService] Attempting to deserialize null or empty JSON");
                return default(T);
            }

            var result = DeserializeFromJson(json, typeof(T));
            return result != null ? (T)result : default(T);
        }

        public object DeserializeFromJson(string json, Type type)
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[SerializationService] Attempting to deserialize null or empty JSON");
                return null;
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            var serializer = GetSerializer(type);

            if (serializer.SupportedStrategy != SerializationStrategy.Json)
            {
                throw new SerializationException(
                    $"Serializer for type {type.Name} does not support JSON strategy",
                    type,
                    SerializationErrorCode.UnsupportedStrategy
                );
            }

            try
            {
                var result = serializer.DeserializeFromJson(json, type);
                Debug.Log($"[SerializationService] Deserialized {type.Name} from JSON");
                return result;
            }
            catch (SerializationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SerializationException(
                    $"Failed to deserialize type {type.Name} from JSON: {ex.Message}",
                    type,
                    SerializationErrorCode.DeserializationFailed,
                    ex
                );
            }
        }

        #endregion

        #region CustomData 序列化

        public List<CustomDataEntry> SerializeToCustomData<T>(T obj)
        {
            if (obj == null)
            {
                Debug.LogWarning("[SerializationService] Attempting to serialize null object");
                return new List<CustomDataEntry>();
            }

            return SerializeToCustomData(obj, typeof(T));
        }

        public List<CustomDataEntry> SerializeToCustomData(object obj, Type type)
        {
            if (obj == null)
            {
                Debug.LogWarning("[SerializationService] Attempting to serialize null object");
                return new List<CustomDataEntry>();
            }

            if (type == null)
            {
                type = obj.GetType();
            }

            var serializer = GetSerializer(type);

            if (serializer.SupportedStrategy != SerializationStrategy.CustomDataEntry)
            {
                throw new SerializationException(
                    $"Serializer for type {type.Name} does not support CustomDataEntry strategy",
                    type,
                    SerializationErrorCode.UnsupportedStrategy
                );
            }

            try
            {
                var entries = serializer.SerializeToCustomData(obj);
                Debug.Log($"[SerializationService] Serialized {type.Name} to CustomData ({entries?.Count ?? 0} entries)");
                return entries ?? new List<CustomDataEntry>();
            }
            catch (SerializationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SerializationException(
                    $"Failed to serialize type {type.Name} to CustomData: {ex.Message}",
                    type,
                    SerializationErrorCode.SerializationFailed,
                    ex
                );
            }
        }

        public T DeserializeFromCustomData<T>(List<CustomDataEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                Debug.LogWarning("[SerializationService] Attempting to deserialize null or empty CustomData entries");
                return default(T);
            }

            var result = DeserializeFromCustomData(entries, typeof(T));
            return result != null ? (T)result : default(T);
        }

        public object DeserializeFromCustomData(List<CustomDataEntry> entries, Type type)
        {
            if (entries == null || entries.Count == 0)
            {
                Debug.LogWarning("[SerializationService] Attempting to deserialize null or empty CustomData entries");
                return null;
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            var serializer = GetSerializer(type);

            if (serializer.SupportedStrategy != SerializationStrategy.CustomDataEntry)
            {
                throw new SerializationException(
                    $"Serializer for type {type.Name} does not support CustomDataEntry strategy",
                    type,
                    SerializationErrorCode.UnsupportedStrategy
                );
            }

            try
            {
                var result = serializer.DeserializeFromCustomData(entries, type);
                Debug.Log($"[SerializationService] Deserialized {type.Name} from CustomData");
                return result;
            }
            catch (SerializationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SerializationException(
                    $"Failed to deserialize type {type.Name} from CustomData: {ex.Message}",
                    type,
                    SerializationErrorCode.DeserializationFailed,
                    ex
                );
            }
        }

        #endregion

        #region Binary 序列化

        public byte[] SerializeToBinary<T>(T obj)
        {
            if (obj == null)
            {
                Debug.LogWarning("[SerializationService] Attempting to serialize null object");
                return null;
            }

            return SerializeToBinary(obj, typeof(T));
        }

        public byte[] SerializeToBinary(object obj, Type type)
        {
            if (obj == null)
            {
                Debug.LogWarning("[SerializationService] Attempting to serialize null object");
                return null;
            }

            if (type == null)
            {
                type = obj.GetType();
            }

            var serializer = GetSerializer(type);

            if (serializer.SupportedStrategy != SerializationStrategy.Binary)
            {
                throw new SerializationException(
                    $"Serializer for type {type.Name} does not support Binary strategy",
                    type,
                    SerializationErrorCode.UnsupportedStrategy
                );
            }

            try
            {
                var data = serializer.SerializeToBinary(obj);
                Debug.Log($"[SerializationService] Serialized {type.Name} to Binary ({data?.Length ?? 0} bytes)");
                return data;
            }
            catch (SerializationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SerializationException(
                    $"Failed to serialize type {type.Name} to Binary: {ex.Message}",
                    type,
                    SerializationErrorCode.SerializationFailed,
                    ex
                );
            }
        }

        public T DeserializeFromBinary<T>(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                Debug.LogWarning("[SerializationService] Attempting to deserialize null or empty binary data");
                return default(T);
            }

            var result = DeserializeFromBinary(data, typeof(T));
            return result != null ? (T)result : default(T);
        }

        public object DeserializeFromBinary(byte[] data, Type type)
        {
            if (data == null || data.Length == 0)
            {
                Debug.LogWarning("[SerializationService] Attempting to deserialize null or empty binary data");
                return null;
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            var serializer = GetSerializer(type);

            if (serializer.SupportedStrategy != SerializationStrategy.Binary)
            {
                throw new SerializationException(
                    $"Serializer for type {type.Name} does not support Binary strategy",
                    type,
                    SerializationErrorCode.UnsupportedStrategy
                );
            }

            try
            {
                var result = serializer.DeserializeFromBinary(data, type);
                Debug.Log($"[SerializationService] Deserialized {type.Name} from Binary");
                return result;
            }
            catch (SerializationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SerializationException(
                    $"Failed to deserialize type {type.Name} from Binary: {ex.Message}",
                    type,
                    SerializationErrorCode.DeserializationFailed,
                    ex
                );
            }
        }

        #endregion

        #region 调试和工具方法

        public Dictionary<Type, SerializationStrategy> GetAllRegisteredSerializers()
        {
            lock (_lock)
            {
                var result = new Dictionary<Type, SerializationStrategy>();
                foreach (var kvp in _serializers)
                {
                    result[kvp.Key] = kvp.Value.SupportedStrategy;
                }
                return result;
            }
        }

        public void ClearAllSerializers()
        {
            lock (_lock)
            {
                _serializers.Clear();
                Debug.Log("[SerializationService] Cleared all serializers");
            }
        }

        #endregion
    }
}
