using EasyPack.ENekoFramework;
using EasyPack.Serialization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace EasyPack.CategoryService
{
    /// <summary>
    /// 分类服务
    /// 作为 EasyPack 服务，持有并管理多个 CategoryManager 实例
    /// 负责序列化器注册和生命周期管理
    /// </summary>
    public class CategoryService : BaseService, ICategoryService
    {
        private readonly Dictionary<Type, object> _managers;
        private ISerializationService _serializationService;

        public CategoryService()
        {
            _managers = new Dictionary<Type, object>();
        }

        #region 生命周期管理

        /// <summary>
        /// 服务初始化
        /// 注册所有序列化器
        /// </summary>
        protected override async Task OnInitializeAsync()
        {
            await base.OnInitializeAsync();

            await RegisterSerializers();

            Debug.Log("[CategoryService] 分类服务初始化完成");
        }

        /// <summary>
        /// 注册序列化器
        /// 实体类型必须已注册到 SerializationService
        /// </summary>
        private async Task RegisterSerializers()
        {
            try
            {
                // 获取序列化服务
                _serializationService = await Architecture.EasyPackArchitecture.Instance.ResolveAsync<ISerializationService>();

                // 注意：CategoryManager 是泛型类型，具体的序列化器需要在创建 Manager 时注册
                // 这里可以预注册一些常用类型，但主要由 CreateManager 方法处理

                Debug.Log("[CategoryService] 序列化器注册完成");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CategoryService] 序列化器注册失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 服务释放
        /// </summary>
        protected override async Task OnDisposeAsync()
        {
            // 释放所有 Manager
            foreach (var manager in _managers.Values)
            {
                if (manager is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            _managers.Clear();

            await base.OnDisposeAsync();

            Debug.Log("[CategoryService] 分类服务已释放");
        }

        #endregion

        #region Manager 管理

        /// <summary>
        /// 创建或获取指定实体类型的 CategoryManager
        /// </summary>
        /// <typeparam name="T">实体类型（必须已在 SerializationService 中注册序列化器）</typeparam>
        /// <param name="idExtractor">实体 ID 提取函数</param>
        /// <param name="comparisonMode">字符串比较模式</param>
        /// <param name="cacheStrategy">缓存策略</param>
        /// <returns>CategoryManager 实例</returns>
        public CategoryManager<T> GetOrCreateManager<T>(
            Func<T, string> idExtractor,
            StringComparison comparisonMode = StringComparison.OrdinalIgnoreCase,
            CacheStrategy cacheStrategy = CacheStrategy.Balanced)
        {
            var entityType = typeof(T);

            // 如果已存在，直接返回
            if (_managers.TryGetValue(entityType, out var existingManager))
            {
                return existingManager as CategoryManager<T>;
            }

            // 创建新的 Manager
            var manager = new CategoryManager<T>(idExtractor, comparisonMode, cacheStrategy);
            _managers[entityType] = manager;

            // 注册该实体类型的序列化器
            if (_serializationService != null)
            {
                try
                {
                    var serializer = new CategoryManagerJsonSerializer<T>(idExtractor);
                    _serializationService.RegisterSerializer(serializer);
                    Debug.Log($"[CategoryService] 已注册 CategoryManager<{typeof(T).Name}> 序列化器");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[CategoryService] 注册 CategoryManager<{typeof(T).Name}> 序列化器失败: {ex.Message}");
                }
            }

            return manager;
        }

        /// <summary>
        /// 获取指定实体类型的 CategoryManager
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <returns>CategoryManager 实例，如果不存在则返回 null</returns>
        public CategoryManager<T> GetManager<T>()
        {
            var entityType = typeof(T);
            if (_managers.TryGetValue(entityType, out var manager))
            {
                return manager as CategoryManager<T>;
            }
            return null;
        }

        /// <summary>
        /// 移除指定实体类型的 CategoryManager
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <returns>是否成功移除</returns>
        public bool RemoveManager<T>()
        {
            var entityType = typeof(T);
            if (_managers.TryGetValue(entityType, out var manager))
            {
                if (manager is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                _managers.Remove(entityType);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取所有已注册的实体类型
        /// </summary>
        /// <returns>实体类型列表</returns>
        public IReadOnlyList<Type> GetRegisteredEntityTypes()
        {
            return new List<Type>(_managers.Keys);
        }

        #endregion

        #region 序列化支持

        /// <summary>
        /// 序列化指定类型的 Manager
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <returns>JSON 字符串，如果 Manager 不存在则返回 null</returns>
        public string SerializeManager<T>()
        {
            var manager = GetManager<T>();
            if (manager == null)
            {
                Debug.LogWarning($"[CategoryService] CategoryManager<{typeof(T).Name}> 不存在");
                return null;
            }

            return manager.SerializeToJson();
        }

        /// <summary>
        /// 从 JSON 加载 Manager 数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="json">JSON 字符串</param>
        /// <param name="idExtractor">实体 ID 提取函数</param>
        /// <returns>操作结果</returns>
        public OperationResult LoadManager<T>(string json, Func<T, string> idExtractor)
        {
            try
            {
                // 创建新的 Manager 并加载数据
                var manager = CategoryManager<T>.CreateFromJson(json, idExtractor);
                
                // 替换现有 Manager
                var entityType = typeof(T);
                if (_managers.TryGetValue(entityType, out var oldManager) && oldManager is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                
                _managers[entityType] = manager;

                // 注册序列化器
                if (_serializationService != null)
                {
                    try
                    {
                        var serializer = new CategoryManagerJsonSerializer<T>(idExtractor);
                        _serializationService.RegisterSerializer(serializer);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[CategoryService] 注册序列化器失败: {ex.Message}");
                    }
                }

                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                return OperationResult.Failure(ErrorCode.InvalidCategory, 
                    $"加载 Manager 失败: {ex.Message}");
            }
        }

        #endregion
    }
}
