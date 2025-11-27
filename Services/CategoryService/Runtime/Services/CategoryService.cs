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
    ///     分类服务
    ///     作为 EasyPack 服务，持有并管理多个 CategoryManager 实例
    ///     支持双泛型 CategoryManager&lt;T, TKey&gt;
    /// </summary>
    public class CategoryService : BaseService, ICategoryService
    {
        // 使用复合键 (EntityType, KeyType) 来索引 Manager
        private readonly Dictionary<(Type EntityType, Type KeyType), ICategoryManager> _managers = new();
        private ISerializationService _serializationService;
        private readonly Dictionary<(Type EntityType, Type KeyType), object> _serializers = new();

        #region 生命周期管理

        /// <summary>
        ///     获取或初始化 SerializationService
        /// </summary>
        protected override async Task OnInitializeAsync()
        {
            await base.OnInitializeAsync();

            // 获取序列化服务实例
            _serializationService = await EasyPackArchitecture.Instance.ResolveAsync<ISerializationService>();

            if (_serializationService != null)
                Debug.Log("[CategoryService] 已连接 SerializationService");
            else
                Debug.LogWarning("[CategoryService] SerializationService 未初始化");
        }

        /// <summary>
        ///     服务释放
        /// </summary>
        protected override async Task OnDisposeAsync()
        {
            // 释放所有 Manager
            foreach (ICategoryManager manager in _managers.Values)
            {
                if (manager is IDisposable disposable)
                    disposable.Dispose();
            }

            _managers.Clear();
            _serializers.Clear();

            await base.OnDisposeAsync();

            Debug.Log("[CategoryService] 分类服务已释放");
        }

        #endregion

        #region Manager 管理

        /// <summary>
        ///     创建或获取指定实体类型和键类型的 CategoryManager
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <param name="keyExtractor">实体键提取函数</param>
        /// <returns>CategoryManager 实例</returns>
        public ICategoryManager<T, TKey> GetOrCreateManager<T, TKey>(Func<T, TKey> keyExtractor)
            where TKey : IEquatable<TKey>
        {
            var key = (typeof(T), typeof(TKey));

            // 如果已存在，直接返回
            if (_managers.TryGetValue(key, out ICategoryManager existingManager))
                return existingManager as ICategoryManager<T, TKey>;

            // 创建新的 Manager
            var manager = new CategoryManager<T, TKey>(keyExtractor);
            _managers[key] = manager;

            // 自动注册该实体类型的 CategoryManager 序列化器
            RegisterManagerSerializer<T, TKey>(keyExtractor);

            Debug.Log($"[CategoryService] 创建 CategoryManager<{typeof(T).Name}, {typeof(TKey).Name}>");

            return manager;
        }

        /// <summary>
        ///     获取指定实体类型和键类型的 CategoryManager
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <returns>CategoryManager 实例，如果不存在则返回 null</returns>
        public ICategoryManager<T, TKey> GetManager<T, TKey>()
            where TKey : IEquatable<TKey>
        {
            var key = (typeof(T), typeof(TKey));
            if (_managers.TryGetValue(key, out ICategoryManager manager))
                return manager as ICategoryManager<T, TKey>;
            return null;
        }

        /// <summary>
        ///     移除指定实体类型和键类型的 CategoryManager
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <returns>是否成功移除</returns>
        public bool RemoveManager<T, TKey>()
            where TKey : IEquatable<TKey>
        {
            var key = (typeof(T), typeof(TKey));
            if (!_managers.TryGetValue(key, out ICategoryManager manager)) return false;

            if (manager is IDisposable disposable) disposable.Dispose();
            _managers.Remove(key);
            _serializers.Remove(key);
            return true;
        }

        /// <summary>
        ///     获取所有已注册的实体类型
        /// </summary>
        /// <returns>实体类型和键类型的列表</returns>
        public IReadOnlyList<(Type EntityType, Type KeyType)> GetRegisteredManagerTypes() =>
            new List<(Type, Type)>(_managers.Keys);

        /// <summary>
        ///     删除所有 CategoryManager 实例
        /// </summary>
        public bool RemoveAllManagers()
        {
            try
            {
                foreach (ICategoryManager manager in _managers.Values)
                {
                    if (manager is IDisposable disposable)
                        disposable.Dispose();
                }

                _managers.Clear();
                _serializers.Clear();
                return true;
            }
            catch (SystemException e)
            {
                Debug.LogError($"[CategoryService] 移除所有 CategoryManager 时发生错误: {e}");
                return false;
            }
        }

        #endregion

        #region 序列化支持

        /// <summary>
        ///     注册 CategoryManager&lt;T, TKey&gt; 的序列化器
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <param name="keyExtractor">实体键提取函数</param>
        private void RegisterManagerSerializer<T, TKey>(Func<T, TKey> keyExtractor)
            where TKey : IEquatable<TKey>
        {
            if (_serializationService == null) return;

            var typeKey = (typeof(T), typeof(TKey));

            // 避免重复注册
            if (_serializers.ContainsKey(typeKey)) return;

            try
            {
                var serializer = new CategoryManagerJsonSerializer<T, TKey>(keyExtractor);
                _serializationService.RegisterSerializer<
                    CategoryManager<T, TKey>,
                    SerializableCategoryManagerState<T, TKey>>(serializer);
                _serializers[typeKey] = serializer;

                Debug.Log($"[CategoryService] 已注册 CategoryManager<{typeof(T).Name}, {typeof(TKey).Name}> 序列化器");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CategoryService] 注册序列化器失败: {ex.Message}");
            }
        }

        /// <summary>
        ///     序列化指定类型的 Manager
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <returns>JSON 字符串，如果 Manager 不存在则返回 null</returns>
        public string SerializeManager<T, TKey>()
            where TKey : IEquatable<TKey>
        {
            var manager = GetManager<T, TKey>();
            if (manager == null)
            {
                Debug.LogWarning($"[CategoryService] CategoryManager<{typeof(T).Name}, {typeof(TKey).Name}> 不存在");
                return null;
            }

            if (manager is CategoryManager<T, TKey> concreteManager)
                return concreteManager.SerializeToJson();

            Debug.LogWarning($"[CategoryService] Manager 类型不支持序列化");
            return null;
        }

        /// <summary>
        ///     从 JSON 加载 Manager 数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <param name="json">JSON 字符串</param>
        /// <param name="keyExtractor">实体键提取函数</param>
        /// <returns>操作结果</returns>
        public OperationResult LoadManager<T, TKey>(string json, Func<T, TKey> keyExtractor)
            where TKey : IEquatable<TKey>
        {
            try
            {
                // 创建新的 Manager 并加载数据
                var manager = CategoryManager<T, TKey>.CreateFromJson(json, keyExtractor);

                // 替换现有 Manager
                var key = (typeof(T), typeof(TKey));
                if (_managers.TryGetValue(key, out ICategoryManager oldManager) &&
                    oldManager is IDisposable disposable)
                    disposable.Dispose();

                _managers[key] = manager;

                // 注册序列化器
                RegisterManagerSerializer<T, TKey>(keyExtractor);

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