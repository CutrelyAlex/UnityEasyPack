using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using EasyPack.ENekoFramework;
using EasyPack.GamePropertySystem;

namespace EasyPack
{
    /// <summary>
    /// 游戏属性管理器
    /// 提供属性注册、查询、分类管理和批量操作功能
    /// 实现IService接口，支持生命周期管理
    /// </summary>
    public class GamePropertyService : IGamePropertyService
    {
        #region 字段

        // 核心数据
        private ConcurrentDictionary<string, GameProperty> _properties;
        private ConcurrentDictionary<string, PropertyMetadata> _metadata;
        private ConcurrentDictionary<string, string> _propertyToCategory;

        // 运行时索引
        private ConcurrentDictionary<string, HashSet<string>> _categories;
        private ConcurrentDictionary<string, HashSet<string>> _tagIndex;

        // 线程安全锁（保护 HashSet 操作）
        private readonly object _categoryLock = new object();
        private readonly object _tagLock = new object();

        // 服务生命周期状态
        private ServiceLifecycleState _state = ServiceLifecycleState.Uninitialized;

        #endregion

        #region IService 生命周期

        /// <summary>
        /// 服务的当前生命周期状态
        /// </summary>
        public ServiceLifecycleState State => _state;

        /// <summary>
        /// 异步初始化服务
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_state != ServiceLifecycleState.Uninitialized)
                return;

            _state = ServiceLifecycleState.Initializing;

            // 初始化字典
            _properties = new ConcurrentDictionary<string, GameProperty>();
            _metadata = new ConcurrentDictionary<string, PropertyMetadata>();
            _propertyToCategory = new ConcurrentDictionary<string, string>();
            _categories = new ConcurrentDictionary<string, HashSet<string>>();
            _tagIndex = new ConcurrentDictionary<string, HashSet<string>>();

            // 注册GameProperty系统的序列化器
            await RegisterSerializers();

            _state = ServiceLifecycleState.Ready;
            await Task.CompletedTask;
        }

        /// <summary>
        /// 注册GameProperty系统的序列化器
        /// </summary>
        private async Task RegisterSerializers()
        {
            try
            {
                // 获取序列化服务
                var serializationService = await EasyPackArchitecture.Instance.ResolveAsync<ISerializationService>();

                // 注册所有修饰符相关的序列化器
                serializationService.RegisterSerializer(new ModifierSerializer());
                serializationService.RegisterSerializer(new FloatModifierSerializer());
                serializationService.RegisterSerializer(new RangeModifierSerializer());
                serializationService.RegisterSerializer(new ModifierListSerializer());

                // 注册GameProperty JSON序列化器
                serializationService.RegisterSerializer(new GamePropertyJsonSerializer());
                serializationService.RegisterSerializer(new PropertyManagerSerializer());
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GamePropertyManager] 序列化器注册失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 暂停服务
        /// </summary>
        public void Pause()
        {
            if (_state == ServiceLifecycleState.Ready)
                _state = ServiceLifecycleState.Paused;
        }

        /// <summary>
        /// 恢复服务
        /// </summary>
        public void Resume()
        {
            if (_state == ServiceLifecycleState.Paused)
                _state = ServiceLifecycleState.Ready;
        }

        /// <summary>
        /// 释放服务资源
        /// </summary>
        public void Dispose()
        {
            _properties?.Clear();
            _metadata?.Clear();
            _categories?.Clear();
            _tagIndex?.Clear();
            _propertyToCategory?.Clear();
            _state = ServiceLifecycleState.Disposed;
        }

        #endregion

        #region 注册API

        /// <summary>
        /// 注册单个属性到指定分类
        /// </summary>
        public void Register(GameProperty property, string category = "Default", PropertyMetadata metadata = null)
        {
            ThrowIfNotReady();

            if (property == null)
                throw new ArgumentNullException(nameof(property));

            if (string.IsNullOrEmpty(property.ID))
                throw new ArgumentException("属性ID不能为空");

            if (_properties.ContainsKey(property.ID))
                throw new ArgumentException($"属性 '{property.ID}' 已注册");

            // 添加到主表
            _properties[property.ID] = property;
            _propertyToCategory[property.ID] = category ?? "Default";

            // 添加元数据
            if (metadata != null)
            {
                // 去重标签
                if (metadata.Tags != null)
                    metadata.Tags = metadata.Tags.Distinct().ToArray();

                _metadata[property.ID] = metadata;

                // 更新标签索引（线程安全）
                if (metadata.Tags != null)
                {
                    foreach (var tag in metadata.Tags)
                    {
                        lock (_tagLock)
                        {
                            if (!_tagIndex.ContainsKey(tag))
                                _tagIndex[tag] = new HashSet<string>();

                            _tagIndex[tag].Add(property.ID);
                        }
                    }
                }
            }

            // 更新分类索引（线程安全）
            lock (_categoryLock)
            {
                if (!_categories.ContainsKey(category))
                    _categories[category] = new HashSet<string>();

                _categories[category].Add(property.ID);
            }
        }

        /// <summary>
        /// 批量注册属性到指定分类
        /// </summary>
        public void RegisterRange(IEnumerable<GameProperty> properties, string category = "Default")
        {
            ThrowIfNotReady();

            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            foreach (var property in properties)
            {
                Register(property, category, null);
            }
        }

        #endregion

        #region 查询API

        /// <summary>
        /// 通过ID获取属性
        /// </summary>
        public GameProperty Get(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            _properties.TryGetValue(id, out var property);
            return property;
        }

        /// <summary>
        /// 获取指定分类的所有属性
        /// </summary>
        public IEnumerable<GameProperty> GetByCategory(string category, bool includeChildren = false)
        {
            if (string.IsNullOrEmpty(category))
                return Enumerable.Empty<GameProperty>();

            if (!includeChildren)
            {
                // 精确匹配（线程安全）
                if (_categories.TryGetValue(category, out var ids))
                {
                    lock (_categoryLock)
                    {
                        return ids.ToList().Select(id => _properties[id]).Where(p => p != null).ToList();
                    }
                }
                return Enumerable.Empty<GameProperty>();
            }
            else
            {
                // 支持通配符："Category.*" 匹配所有子分类（线程安全）
                var results = new List<GameProperty>();
                var prefix = category.EndsWith(".*") ? category.Substring(0, category.Length - 2) + "." : category + ".";

                lock (_categoryLock)
                {
                    foreach (var kvp in _categories)
                    {
                        if (kvp.Key == category || kvp.Key.StartsWith(prefix))
                        {
                            results.AddRange(kvp.Value.ToList().Select(id => _properties[id]).Where(p => p != null));
                        }
                    }
                }

                return results;
            }
        }

        /// <summary>
        /// 获取包含指定标签的所有属性
        /// </summary>
        public IEnumerable<GameProperty> GetByTag(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return Enumerable.Empty<GameProperty>();

            if (_tagIndex.TryGetValue(tag, out var ids))
            {
                lock (_tagLock)
                {
                    return ids.ToList().Select(id => _properties[id]).Where(p => p != null).ToList();
                }
            }

            return Enumerable.Empty<GameProperty>();
        }

        /// <summary>
        /// 组合查询：获取同时满足分类和标签条件的属性（交集）
        /// </summary>
        public IEnumerable<GameProperty> GetByCategoryAndTag(string category, string tag)
        {
            var categoryProps = GetByCategory(category).Select(p => p.ID).ToHashSet();
            var tagProps = GetByTag(tag).Select(p => p.ID).ToHashSet();

            var intersection = categoryProps.Intersect(tagProps);
            return intersection.Select(id => _properties[id]).Where(p => p != null);
        }

        /// <summary>
        /// 获取属性的元数据
        /// </summary>
        public PropertyMetadata GetMetadata(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            _metadata.TryGetValue(id, out var metadata);
            return metadata;
        }

        /// <summary>
        /// 获取所有已注册的属性ID
        /// </summary>
        public IEnumerable<string> GetAllPropertyIds()
        {
            return _properties.Keys;
        }

        /// <summary>
        /// 获取所有分类名
        /// </summary>
        public IEnumerable<string> GetAllCategories()
        {
            return _categories.Keys;
        }

        #endregion

        #region 移除API

        /// <summary>
        /// 移除指定属性
        /// </summary>
        public bool Unregister(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;

            // 从主表移除
            if (!_properties.TryRemove(id, out var property))
                return false;

            // 从元数据移除
            _metadata.TryRemove(id, out _);

            // 从分类索引移除（线程安全）
            if (_propertyToCategory.TryRemove(id, out var category))
            {
                lock (_categoryLock)
                {
                    if (_categories.TryGetValue(category, out var categorySet))
                    {
                        categorySet.Remove(id);
                        if (categorySet.Count == 0)
                            _categories.TryRemove(category, out _);
                    }
                }
            }

            // 从标签索引移除（线程安全）
            lock (_tagLock)
            {
                foreach (var tagSet in _tagIndex.Values)
                {
                    tagSet.Remove(id);
                }
            }

            return true;
        }

        /// <summary>
        /// 移除整个分类及其所有属性
        /// </summary>
        public void UnregisterCategory(string category)
        {
            if (string.IsNullOrEmpty(category))
                return;

            if (_categories.TryRemove(category, out var ids))
            {
                foreach (var id in ids.ToList())
                {
                    Unregister(id);
                }
            }
        }

        #endregion

        #region 批量操作API

        /// <summary>
        /// 设置分类中所有属性的激活状态
        /// </summary>
        public OperationResult<List<string>> SetCategoryActive(string category, bool active)
        {
            ThrowIfNotReady();

            var successIds = new List<string>();
            var failures = new List<FailureRecord>();

            var properties = GetByCategory(category).ToList();

            if (properties.Count == 0)
            {
                failures.Add(new FailureRecord(category, "分类不存在或为空", FailureType.CategoryNotFound));
                return OperationResult<List<string>>.PartialSuccess(successIds, 0, failures);
            }

            foreach (var property in properties)
            {
                try
                {
                    // GameProperty没有直接的Active属性，这里通过Enable/Disable修饰符来实现
                    // 实际实现可能需要根据项目具体需求调整
                    property.SetBaseValue(active ? property.GetBaseValue() : 0);
                    successIds.Add(property.ID);
                }
                catch (Exception ex)
                {
                    failures.Add(new FailureRecord(property.ID, ex.Message, FailureType.UnknownError));
                }
            }

            return failures.Count == 0
                ? OperationResult<List<string>>.Success(successIds, successIds.Count)
                : OperationResult<List<string>>.PartialSuccess(successIds, successIds.Count, failures);
        }

        /// <summary>
        /// 为分类中所有属性应用修饰符
        /// </summary>
        public OperationResult<List<string>> ApplyModifierToCategory(string category, IModifier modifier)
        {
            ThrowIfNotReady();

            if (modifier == null)
                throw new ArgumentNullException(nameof(modifier));

            var successIds = new List<string>();
            var failures = new List<FailureRecord>();

            var properties = GetByCategory(category).ToList();

            if (properties.Count == 0)
            {
                failures.Add(new FailureRecord(category, "分类不存在或为空", FailureType.CategoryNotFound));
                return OperationResult<List<string>>.PartialSuccess(successIds, 0, failures);
            }

            foreach (var property in properties)
            {
                try
                {
                    var clonedModifier = modifier.Clone();
                    property.AddModifier(clonedModifier);
                    successIds.Add(property.ID);
                }
                catch (Exception ex)
                {
                    failures.Add(new FailureRecord(property.ID, ex.Message, FailureType.InvalidModifier));
                }
            }

            return failures.Count == 0
                ? OperationResult<List<string>>.Success(successIds, successIds.Count)
                : OperationResult<List<string>>.PartialSuccess(successIds, successIds.Count, failures);
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 检查服务是否就绪，否则抛出异常
        /// </summary>
        private void ThrowIfNotReady()
        {
            if (_state != ServiceLifecycleState.Ready)
                throw new InvalidOperationException($"服务未就绪，当前状态: {_state}");
        }

        #endregion
    }
}
