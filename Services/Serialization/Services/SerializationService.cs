using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasyPack.EmeCardSystem;
using EasyPack.ENekoFramework;
using UnityEngine;

namespace EasyPack.Serialization
{
    /// <summary>
    ///     双泛型序列化器的内部包装器
    /// </summary>
    internal class SerializerWrapper<TOriginal, TSerializable> : ISerializerWrapper
        where TSerializable : ISerializable
    {
        private readonly ITypeSerializer<TOriginal, TSerializable> _serializer;

        public SerializerWrapper(ITypeSerializer<TOriginal, TSerializable> serializer) => _serializer = serializer;

        public Type TargetType => typeof(TOriginal);

        public string SerializeToJson(object obj) => _serializer.SerializeToJson((TOriginal)obj);

        public object DeserializeFromJson(string json) => _serializer.DeserializeFromJson(json);
    }

    /// <summary>
    ///     Unity JsonUtility 的通用包装器
    ///     用于自动处理带有 [Serializable] 特性的类型
    /// </summary>
    internal class UnityJsonSerializerWrapper : ISerializerWrapper
    {
        public UnityJsonSerializerWrapper(Type targetType) => TargetType = targetType;

        public Type TargetType { get; }

        public string SerializeToJson(object obj) => obj == null ? null : JsonUtility.ToJson(obj);

        public object DeserializeFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            return JsonUtility.FromJson(json, TargetType);
        }
    }

    /// <summary>
    ///     统一序列化服务实现（仅支持双泛型接口）
    /// </summary>
    public class SerializationService : BaseService, ISerializationService
    {
        private readonly Dictionary<Type, ISerializerWrapper> _serializers = new();
        private readonly object _lock = new();

        #region 生命周期管理

        /// <summary>
        ///     服务初始化时的钩子方法
        ///     在此处注册所有未实现IService的系统的序列化器
        /// </summary>
        protected override async Task OnInitializeAsync()
        {
            await base.OnInitializeAsync();

            CardSerializationInitializer.RegisterSerializers(this);

            Debug.Log("[SerializationService] 序列化服务初始化完成");
        }

        /// <summary>
        ///     服务释放时的钩子方法
        ///     清理所有已注册的序列化器
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
        ///     注册双泛型类型序列化器
        /// </summary>
        public void RegisterSerializer<TOriginal, TSerializable>(ITypeSerializer<TOriginal, TSerializable> serializer)
            where TSerializable : ISerializable
        {
            lock (_lock)
            {
                _serializers[typeof(TOriginal)] = new SerializerWrapper<TOriginal, TSerializable>(serializer);
            }
        }

        public bool HasSerializer(Type type)
        {
            lock (_lock)
            {
                return _serializers.ContainsKey(type);
            }
        }

        public bool HasSerializer<T>() => HasSerializer(typeof(T));

        /// <summary>
        ///     获取所有已注册的序列化器类型（用于调试）
        /// </summary>
        public IReadOnlyCollection<Type> GetRegisteredTypes()
        {
            lock (_lock)
            {
                return new List<Type>(_serializers.Keys);
            }
        }

        private ISerializerWrapper GetSerializer(Type type)
        {
            lock (_lock)
            {
                // 1. 首先尝试精确匹配
                if (_serializers.TryGetValue(type, out ISerializerWrapper serializer)) return serializer;

                // 2. 如果精确匹配失败，尝试查找基类的序列化器
                Type currentType = type.BaseType;
                while (currentType != null)
                {
                    if (_serializers.TryGetValue(currentType, out serializer)) return serializer;

                    currentType = currentType.BaseType;
                }

                // 3. 尝试查找接口的序列化器
                foreach (Type interfaceType in type.GetInterfaces())
                {
                    if (_serializers.TryGetValue(interfaceType, out serializer))
                    {
                        return serializer;
                    }
                }

                // 4. 如果没有注册的序列化器，检查是否有 [Serializable] 特性
                //    自动使用 Unity JsonUtility 进行序列化
                if (type.GetCustomAttributes(typeof(SerializableAttribute), true).Length > 0)
                {
                    var unitySerializer = new UnityJsonSerializerWrapper(type);
                    _serializers[type] = unitySerializer;
                    Debug.Log($"[SerializationService] 自动创建 Unity JsonUtility 序列化器: {type.Name}");
                    return unitySerializer;
                }
            }

            throw new SerializationException(
                $"未注册类型的序列化器: {type.Name}. 请先使用 RegisterSerializer<TOriginal, TSerializable> 注册序列化器，或为类型添加 [Serializable] 特性。",
                type,
                SerializationErrorCode.NoSerializerFound
            );
        }

        #endregion

        #region JSON 序列化

        public string SerializeToJson<T>(T obj) => SerializeToJson(obj, typeof(T));

        public string SerializeToJson(object obj, Type type)
        {
            type ??= obj.GetType();

            ISerializerWrapper serializer = GetSerializer(type);

            try
            {
                string json = serializer.SerializeToJson(obj);
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
            object result = DeserializeFromJson(json, typeof(T));
            return result != null ? (T)result : default;
        }

        public object DeserializeFromJson(string json, Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            ISerializerWrapper serializer = GetSerializer(type);

            try
            {
                object result = serializer.DeserializeFromJson(json);
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

        #region 调试和工具方法

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