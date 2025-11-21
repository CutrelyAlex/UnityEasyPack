using EasyPack.Architecture;
using EasyPack.ENekoFramework;
using EasyPack.Serialization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace EasyPack.Category
{
    /// <summary>
    /// 分类服务
    /// 作为 EasyPack 服务，持有并管理多个 CategoryManager 实例
    /// 支持自动的序列化器注册和生命周期管理
    /// </summary>
    public class CategoryService : BaseService, ICategoryService
    {
        private readonly Dictionary<Type, ICategoryManager> _managers = new();
        private ISerializationService _serializationService;
        private readonly Dictionary<Type, object> _serializers = new();

        #region 生命周期管理

        /// <summary>
        /// 获取或初始化 SerializationService
        /// </summary>
        protected override async Task OnInitializeAsync()
        {
            await base.OnInitializeAsync();

            // 获取序列化服务实例
            _serializationService = await EasyPackArchitecture.Instance.ResolveAsync<ISerializationService>();

            if (_serializationService != null)
            {
                Debug.Log("[CategoryService] 已连接 SerializationService");
            }
            else
            {
                Debug.LogWarning("[CategoryService] SerializationService 未初始化");
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
            _serializers.Clear();

            await base.OnDisposeAsync();

            Debug.Log("[CategoryService] 分类服务已释放");
        }

        #endregion

        #region Manager 管理

        /// <summary>
        /// 创建或获取指定实体类型的 CategoryManager
        /// 自动注册 CategoryManager&lt;T&gt; 的序列化器到 SerializationService
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="idExtractor">实体 ID 提取函数</param>
        /// <param name="cacheStrategy">缓存策略</param>
        /// <returns>CategoryManager 实例</returns>
        public CategoryManager<T> GetOrCreateManager<T>(
            Func<T, string> idExtractor,
            CacheStrategy cacheStrategy = CacheStrategy.LRUFrequencyHybrid)
        {
            var entityType = typeof(T);

            // 如果已存在，直接返回
            if (_managers.TryGetValue(entityType, out var existingManager))
            {
                return existingManager as CategoryManager<T>;
            }

            // 创建新的 Manager
            var manager = new CategoryManager<T>(idExtractor, cacheStrategy);
            _managers[entityType] = manager;

            // 自动注册该实体类型的 CategoryManager 序列化器
            RegisterManagerSerializer(idExtractor);

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

        /// <summary>
        /// 删除所有 CategoryManager 实例
        /// </summary>
        public bool RemoveAllManagers()
        {
            try
            {
                foreach (var manager in _managers.Values)
                {
                    if (manager is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }

                _managers.Clear();
                return true;
            }
            catch(SystemException e)
            {
                Debug.LogError($"移除所有 CategoryManager 时候发射了错误: {e}");
                return false;
            }
        }
        #endregion

        #region 序列化支持

        /// <summary>
        /// 注册 CategoryManager&lt;T&gt; 的序列化器
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="idExtractor">实体 ID 提取函数</param>
        private void RegisterManagerSerializer<T>(Func<T, string> idExtractor)
        {
            if (_serializationService == null)
            {
                return;
            }

            var entityType = typeof(T);
            var categoryManagerType = typeof(CategoryManager<T>);

            // 避免重复注册
            if (_serializers.ContainsKey(categoryManagerType))
            {
                return;
            }

            try
            {
                var serializer = new CategoryManagerJsonSerializer<T>(idExtractor);
                _serializationService.RegisterSerializer(serializer);
                _serializers[categoryManagerType] = serializer;
                
                Debug.Log($"[CategoryService] 已注册 CategoryManager<{entityType.Name}> 序列化器到 SerializationService");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CategoryService] 注册 CategoryManager<{entityType.Name}> 序列化器失败: {ex.Message}");
            }
        }

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
                RegisterManagerSerializer(idExtractor);

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
