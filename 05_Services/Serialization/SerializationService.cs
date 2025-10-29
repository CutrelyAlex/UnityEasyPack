using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasyPack.ENekoFramework;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// 双泛型序列化器适配器
    /// 将 ITypeSerializer&lt;TOriginal, TSerializable&gt; 适配为 ITypeSerializer&lt;TOriginal&gt;
    /// </summary>
    internal class TypeSerializerAdapter<TOriginal, TSerializable> : ITypeSerializer<TOriginal>
        where TSerializable : ISerializable
    {
        private readonly ITypeSerializer<TOriginal, TSerializable> _serializer;

        public TypeSerializerAdapter(ITypeSerializer<TOriginal, TSerializable> serializer)
        {
            _serializer = serializer;
        }

        public Type TargetType => typeof(TOriginal);
        public SerializationStrategy SupportedStrategy => SerializationStrategy.Json;

        public string SerializeToJson(TOriginal obj)
        {
            return _serializer.SerializeToJson(obj);
        }

        public TOriginal DeserializeFromJson(string json)
        {
            return _serializer.DeserializeFromJson(json);
        }

        public string SerializeToJson(object obj)
        {
            return SerializeToJson((TOriginal)obj);
        }

        public object DeserializeFromJson(string json, Type targetType)
        {
            return DeserializeFromJson(json);
        }

        public List<CustomDataEntry> SerializeToCustomData(TOriginal obj)
        {
            throw new NotSupportedException($"类型 {typeof(TOriginal).Name} 的双泛型序列化器不支持 CustomDataEntry 序列化");
        }

        public TOriginal DeserializeFromCustomData(List<CustomDataEntry> entries)
        {
            throw new NotSupportedException($"类型 {typeof(TOriginal).Name} 的双泛型序列化器不支持 CustomDataEntry 反序列化");
        }

        public List<CustomDataEntry> SerializeToCustomData(object obj)
        {
            return SerializeToCustomData((TOriginal)obj);
        }

        public object DeserializeFromCustomData(List<CustomDataEntry> entries, Type targetType)
        {
            return DeserializeFromCustomData(entries);
        }

        public byte[] SerializeToBinary(TOriginal obj)
        {
            throw new NotSupportedException($"类型 {typeof(TOriginal).Name} 的双泛型序列化器不支持二进制序列化");
        }

        public TOriginal DeserializeFromBinary(byte[] data)
        {
            throw new NotSupportedException($"类型 {typeof(TOriginal).Name} 的双泛型序列化器不支持二进制反序列化");
        }

        public byte[] SerializeToBinary(object obj)
        {
            return SerializeToBinary((TOriginal)obj);
        }

        public object DeserializeFromBinary(byte[] data, Type targetType)
        {
            return DeserializeFromBinary(data);
        }
    }

    /// <summary>
    /// 统一序列化服务实现
    /// </summary>
    public class SerializationService : BaseService, ISerializationService
    {
        private readonly Dictionary<Type, ITypeSerializer> _serializers = new();
        private readonly object _lock = new();

        #region 生命周期管理

        /// <summary>
        /// 服务初始化时的钩子方法
        /// 在此处注册所有系统的序列化器
        /// </summary>
        protected override async Task OnInitializeAsync()
        {
            await base.OnInitializeAsync();
            
            // TODO: 可以在此处调用未迁移系统的 RegisterSerializers() 方法

            Debug.Log("[SerializationService] 序列化服务初始化完成");
        }

        /// <summary>
        /// 服务释放时的钩子方法
        /// 清理所有已注册的序列化器
        /// </summary>
        protected override async Task OnDisposeAsync()
        {     
            lock (_lock)
            {
                _serializers.Clear();
            }
            
            await base.OnDisposeAsync();

            Debug.Log("[SerializationService] 序列化服务已释放");
        }

        #endregion

        #region 注册管理

        /// <summary>
        /// 注册双泛型类型序列化器
        /// </summary>
        public void RegisterSerializer<TOriginal, TSerializable>(ITypeSerializer<TOriginal, TSerializable> serializer) 
            where TSerializable : ISerializable
        {
            lock (_lock)
            {
                _serializers[typeof(TOriginal)] = new TypeSerializerAdapter<TOriginal, TSerializable>(serializer);
            }
        }

        /// <summary>
        /// 注册单泛型类型序列化器（向后兼容）
        /// </summary>
        public void RegisterSerializer<T>(ITypeSerializer<T> serializer)
        {
            RegisterSerializer((ITypeSerializer)serializer);
        }

        /// <summary>
        /// 注册非泛型类型序列化器
        /// </summary>
        public void RegisterSerializer(ITypeSerializer serializer)
        {
            lock (_lock)
            {
                _serializers[serializer.TargetType] = serializer;
            }
        }

        public bool HasSerializer(Type type)
        {
            lock (_lock)
            {
                return _serializers.ContainsKey(type);
            }
        }

        public bool HasSerializer<T>()
        {
            return HasSerializer(typeof(T));
        }

        /// <summary>
        /// 获取所有已注册的序列化器
        /// </summary>
        public IReadOnlyDictionary<Type, ITypeSerializer> GetRegisteredSerializers()
        {
            lock (_lock)
            {
                return new Dictionary<Type, ITypeSerializer>(_serializers);
            }
        }

        public SerializationStrategy GetSupportedStrategy(Type type)
        {
            lock (_lock)
            {
                if (_serializers.TryGetValue(type, out var serializer))
                {
                    return serializer.SupportedStrategy;
                }
            }

            throw new SerializationException(
                $"未找到类型的序列化器: {type.Name}",
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
                        return serializer;
                    }
                    currentType = currentType.BaseType;
                }

                // 3. 尝试查找接口的序列化器
                foreach (var interfaceType in type.GetInterfaces())
                {
                    if (_serializers.TryGetValue(interfaceType, out serializer))
                    {
                        return serializer;
                    }
                }
            }

            throw new SerializationException(
                $"未注册类型的序列化器: {type.Name}. 请先注册序列化器。",
                type,
                SerializationErrorCode.NoSerializerFound
            );
        }

        #endregion

        #region JSON 序列化

        public string SerializeToJson<T>(T obj)
        {
            return SerializeToJson(obj, typeof(T));
        }

        public string SerializeToJson(object obj, Type type)
        {
            if (type == null)
            {
                type = obj.GetType();
            }

            var serializer = GetSerializer(type);

            if (serializer.SupportedStrategy != SerializationStrategy.Json)
            {
                throw new SerializationException(
                    $"类型 {type.Name} 的序列化器不支持 JSON 策略",
                    type,
                    SerializationErrorCode.UnsupportedStrategy
                );
            }

            try
            {
                var json = serializer.SerializeToJson(obj);
                return json;
            }
            catch (SerializationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SerializationException(
                    $"序列化类型 {type.Name} 到 JSON 失败: {ex.Message}",
                    type,
                    SerializationErrorCode.SerializationFailed,
                    ex
                );
            }
        }

        public T DeserializeFromJson<T>(string json)
        {
            var result = DeserializeFromJson(json, typeof(T));
            return result != null ? (T)result : default(T);
        }

        public object DeserializeFromJson(string json, Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            var serializer = GetSerializer(type);

            if (serializer.SupportedStrategy != SerializationStrategy.Json)
            {
                throw new SerializationException(
                    $"类型 {type.Name} 的序列化器不支持 JSON 策略",
                    type,
                    SerializationErrorCode.UnsupportedStrategy
                );
            }

            try
            {
                var result = serializer.DeserializeFromJson(json, type);
                return result;
            }
            catch (SerializationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SerializationException(
                    $"从 JSON 反序列化类型 {type.Name} 失败: {ex.Message}",
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
            return SerializeToCustomData(obj, typeof(T));
        }

        public List<CustomDataEntry> SerializeToCustomData(object obj, Type type)
        {
            if (type == null)
            {
                type = obj.GetType();
            }

            var serializer = GetSerializer(type);

            if (serializer.SupportedStrategy != SerializationStrategy.CustomDataEntry)
            {
                throw new SerializationException(
                    $"类型 {type.Name} 的序列化器不支持 CustomDataEntry 策略",
                    type,
                    SerializationErrorCode.UnsupportedStrategy
                );
            }

            try
            {
                var entries = serializer.SerializeToCustomData(obj);
                Debug.Log($"[SerializationService] 已将 {type.Name} 序列化为 CustomData（{entries?.Count ?? 0} 条目）");
                return entries ?? new List<CustomDataEntry>();
            }
            catch (SerializationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SerializationException(
                    $"序列化类型 {type.Name} 到 CustomData 失败: {ex.Message}",
                    type,
                    SerializationErrorCode.SerializationFailed,
                    ex
                );
            }
        }

        public T DeserializeFromCustomData<T>(List<CustomDataEntry> entries)
        {
            var result = DeserializeFromCustomData(entries, typeof(T));
            return result != null ? (T)result : default(T);
        }

        public object DeserializeFromCustomData(List<CustomDataEntry> entries, Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            var serializer = GetSerializer(type);

            if (serializer.SupportedStrategy != SerializationStrategy.CustomDataEntry)
            {
                throw new SerializationException(
                    $"类型 {type.Name} 的序列化器不支持 CustomDataEntry 策略",
                    type,
                    SerializationErrorCode.UnsupportedStrategy
                );
            }

            try
            {
                var result = serializer.DeserializeFromCustomData(entries, type);
                Debug.Log($"[SerializationService] 已从 CustomData 反序列化 {type.Name}");
                return result;
            }
            catch (SerializationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SerializationException(
                    $"从 CustomData 反序列化类型 {type.Name} 失败: {ex.Message}",
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
            return SerializeToBinary(obj, typeof(T));
        }

        public byte[] SerializeToBinary(object obj, Type type)
        {
            if (type == null)
            {
                type = obj.GetType();
            }

            var serializer = GetSerializer(type);

            if (serializer.SupportedStrategy != SerializationStrategy.Binary)
            {
                throw new SerializationException(
                    $"类型 {type.Name} 的序列化器不支持 Binary 策略",
                    type,
                    SerializationErrorCode.UnsupportedStrategy
                );
            }

            try
            {
                var data = serializer.SerializeToBinary(obj);
                Debug.Log($"[SerializationService] 已将 {type.Name} 序列化为二进制（{data?.Length ?? 0} 字节）");
                return data;
            }
            catch (SerializationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SerializationException(
                    $"序列化类型 {type.Name} 到 Binary 失败: {ex.Message}",
                    type,
                    SerializationErrorCode.SerializationFailed,
                    ex
                );
            }
        }

        public T DeserializeFromBinary<T>(byte[] data)
        {
            var result = DeserializeFromBinary(data, typeof(T));
            return result != null ? (T)result : default(T);
        }

        public object DeserializeFromBinary(byte[] data, Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            var serializer = GetSerializer(type);

            if (serializer.SupportedStrategy != SerializationStrategy.Binary)
            {
                throw new SerializationException(
                    $"类型 {type.Name} 的序列化器不支持 Binary 策略",
                    type,
                    SerializationErrorCode.UnsupportedStrategy
                );
            }

            try
            {
                var result = serializer.DeserializeFromBinary(data, type);
                Debug.Log($"[SerializationService] 已从二进制数据反序列化 {type.Name}");
                return result;
            }
            catch (SerializationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SerializationException(
                    $"从 Binary 反序列化类型 {type.Name} 失败: {ex.Message}",
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
            }
        }

        #endregion
    }
}
