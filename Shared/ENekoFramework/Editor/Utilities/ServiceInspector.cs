using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace EasyPack.ENekoFramework.Editor
{
    /// <summary>
    ///     编辑器时服务检查器
    ///     提供服务状态、依赖关系和元数据的访问接口
    /// </summary>
    public static class ServiceInspector
    {
        /// <summary>
        ///     获取所有已注册的架构实例
        /// </summary>
        public static List<object> GetAllArchitectureInstances()
        {
            var instances = new List<object>();

            try
            {
                var architectureTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .Where(t => t.IsClass && !t.IsAbstract && IsArchitectureType(t))
                    .ToList();

                foreach (Type archType in architectureTypes)
                {
                    // 通过反射访问私有静态字段 _instance，而不是访问 Instance 属性
                    // 这样可以避免触发懒加载单例的初始化逻辑
                    FieldInfo instanceField = archType.BaseType?.GetField("_instance",
                        BindingFlags.NonPublic | BindingFlags.Static);

                    if (instanceField != null)
                    {
                        object instance = instanceField.GetValue(null);
                        if (instance != null) instances.Add(instance);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"ServiceInspector: 无法获取架构实例 - {ex.Message}");
            }

            return instances;
        }

        /// <summary>
        ///     获取所有架构名称
        /// </summary>
        public static List<string> GetAllArchitectureNames()
        {
            var architectures = GetAllArchitectureInstances();
            return architectures.Select(a => a.GetType().Name).Distinct().ToList();
        }

        /// <summary>
        ///     获取指定架构的EventBus
        /// </summary>
        public static EventBus GetEventBusFromArchitecture(object architecture)
        {
            try
            {
                PropertyInfo eventBusProp = architecture.GetType().GetProperty("EventBus",
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

                if (eventBusProp != null) return eventBusProp.GetValue(architecture) as EventBus;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"ServiceInspector: 无法获取EventBus - {ex.Message}");
            }

            return null;
        }

        /// <summary>
        ///     从架构实例获取服务容器
        /// </summary>
        public static ServiceContainer GetContainerFromArchitecture(object architecture)
        {
            try
            {
                PropertyInfo containerProp = architecture.GetType().GetProperty("Container",
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

                if (containerProp != null) return containerProp.GetValue(architecture) as ServiceContainer;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"ServiceInspector: 无法获取容器 - {ex.Message}");
            }

            return null;
        }

        /// <summary>
        ///     获取所有服务描述符
        /// </summary>
        public static List<ServiceDescriptor> GetAllServices()
        {
            var allServices = new List<ServiceDescriptor>();

            var architectures = GetAllArchitectureInstances();
            foreach (object arch in architectures)
            {
                ServiceContainer container = GetContainerFromArchitecture(arch);
                if (container != null) allServices.AddRange(container.GetAllServices());
            }

            return allServices;
        }

        /// <summary>
        ///     获取服务的依赖项
        /// </summary>
        public static List<Type> GetServiceDependencies(Type serviceType)
        {
            var dependencies = new List<Type>();

            try
            {
                var constructors = serviceType.GetConstructors();
                foreach (ConstructorInfo ctor in constructors)
                {
                    var parameters = ctor.GetParameters();
                    foreach (ParameterInfo param in parameters)
                    {
                        if (typeof(IService).IsAssignableFrom(param.ParameterType))
                        {
                            dependencies.Add(param.ParameterType);
                        }
                    }
                }

                var fields = serviceType.GetFields(
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                foreach (FieldInfo field in fields)
                {
                    if (typeof(IService).IsAssignableFrom(field.FieldType))
                    {
                        dependencies.Add(field.FieldType);
                    }
                }

                var properties = serviceType.GetProperties(
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                foreach (PropertyInfo prop in properties)
                {
                    if (typeof(IService).IsAssignableFrom(prop.PropertyType))
                    {
                        dependencies.Add(prop.PropertyType);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"ServiceInspector: 无法分析依赖 - {ex.Message}");
            }

            return dependencies.Distinct().ToList();
        }

        /// <summary>
        ///     检查服务是否有循环依赖
        /// </summary>
        public static bool HasCircularDependency(Type serviceType, HashSet<Type> visited = null)
        {
            if (visited == null) visited = new();

            if (!visited.Add(serviceType)) return true;

            var dependencies = GetServiceDependencies(serviceType);
            foreach (Type dependency in dependencies)
            {
                if (HasCircularDependency(dependency, new(visited)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     获取服务的元数据信息
        /// </summary>
        public static ServiceMetadata GetServiceMetadata(ServiceDescriptor descriptor) =>
            new()
            {
                ServiceType = descriptor.ServiceType,
                ImplementationType = descriptor.ImplementationType,
                State = descriptor.State,
                RegisteredAt = descriptor.RegisteredAt,
                LastAccessedAt = descriptor.LastAccessedAt ?? default(DateTime),
                Dependencies = GetServiceDependencies(descriptor.ImplementationType),
                IsCircularDependency = HasCircularDependency(descriptor.ImplementationType),
            };

        /// <summary>
        ///     检查类型是否为架构类型
        /// </summary>
        private static bool IsArchitectureType(Type type)
        {
            if (type.BaseType == null)
            {
                return false;
            }

            if (!type.BaseType.IsGenericType)
            {
                return false;
            }

            string baseTypeName = type.BaseType.GetGenericTypeDefinition().Name;
            return baseTypeName.Contains("ENekoArchitecture");
        }

        /// <summary>
        ///     服务元数据
        /// </summary>
        public class ServiceMetadata
        {
            public Type ServiceType { get; set; }
            public Type ImplementationType { get; set; }
            public ServiceLifecycleState State { get; set; }
            public DateTime RegisteredAt { get; set; }
            public DateTime LastAccessedAt { get; set; }
            public List<Type> Dependencies { get; set; }
            public bool IsCircularDependency { get; set; }
        }
    }
}