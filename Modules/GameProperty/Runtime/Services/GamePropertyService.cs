using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EasyPack.Architecture;
using EasyPack.ENekoFramework;
using EasyPack.Modifiers;
using EasyPack.Serialization;
using UnityEngine;

namespace EasyPack.GamePropertySystem
{
    /// <summary>
    ///     游戏属性管理器
    ///     提供属性注册、查询、分类管理和批量操作功能
    ///     继承 BaseService，支持标准生命周期管理
    /// </summary>
    public class GamePropertyService : BaseService, IGamePropertyService
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
        private readonly object _categoryLock = new();
        private readonly object _tagLock = new();

        #endregion

        #region 生命周期管理

        /// <summary>
        ///     服务初始化
        ///     初始化所有数据结构并注册序列化器
        /// </summary>
        protected override async Task OnInitializeAsync()
        {
            await base.OnInitializeAsync();

            // 初始化字典
            _properties = new();
            _metadata = new();
            _propertyToCategory = new();
            _categories = new();
            _tagIndex = new();

            // 注册 GameProperty 系统的序列化器
            await RegisterSerializers();

            Debug.Log("[GamePropertyService] 游戏属性服务初始化完成");
        }

        /// <summary>
        ///     注册 GameProperty 系统的序列化器
        /// </summary>
        private async Task RegisterSerializers()
        {
            try
            {
                // 获取序列化服务
                var serializationService = await EasyPackArchitecture.Instance.ResolveAsync<ISerializationService>();

                if (serializationService == null)
                {
                    Debug.LogWarning("[GamePropertyService] SerializationService 未初始化，跳过序列化器注册");
                    return;
                }

                // 注册所有修饰符相关的序列化器
                serializationService.RegisterSerializer(new ModifierSerializer());
                serializationService.RegisterSerializer(new FloatModifierSerializer());
                serializationService.RegisterSerializer(new RangeModifierSerializer());
                serializationService.RegisterSerializer(new ModifierListSerializer());

                serializationService.RegisterSerializer(new GamePropertyJsonSerializer());
                serializationService.RegisterSerializer(new PropertyManagerSerializer());

                Debug.Log("[GamePropertyService] 序列化器注册完成");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GamePropertyService] 序列化器注册失败: {ex.Message}");
            }
        }

        /// <summary>
        ///     服务释放
        /// </summary>
        protected override async Task OnDisposeAsync()
        {
            _properties?.Clear();
            _metadata?.Clear();
            _categories?.Clear();
            _tagIndex?.Clear();
            _propertyToCategory?.Clear();

            await base.OnDisposeAsync();

            Debug.Log("[GamePropertyService] 游戏属性服务已释放");
        }

        #endregion

        #region 注册API

        /// <summary>
        ///     注册单个属性到指定分类
        /// </summary>
        public void Register(GameProperty property, string category = "Default", PropertyMetadata metadata = null)
        {
            ThrowIfNotReady();

            if (property == null)
                throw new ArgumentNullException(nameof(property));

            if (string.IsNullOrEmpty(property.ID))
                throw new ArgumentException("属性ID不能为空");

            if (_properties.ContainsKey(property.ID))
                throw new ArgumentException($"属性ID '{property.ID}' 已存在，不能重复注册");

            RegisterInternal(property, category, metadata);
        }

        /// <summary>
        ///     内部注册逻辑
        /// </summary>
        private void RegisterInternal(GameProperty property, string category, PropertyMetadata metadata)
        {
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
                    foreach (string tag in metadata.Tags)
                    {
                        lock (_tagLock)
                        {
                            if (!_tagIndex.ContainsKey(tag))
                                _tagIndex[tag] = new();

                            _tagIndex[tag].Add(property.ID);
                        }
                    }
                }
            }

            // 更新分类索引（线程安全）
            lock (_categoryLock)
            {
                if (category == null) return;
                if (!_categories.ContainsKey(category))
                    _categories[category] = new();

                _categories[category].Add(property.ID);
            }
        }

        /// <summary>
        ///     批量注册属性到指定分类
        /// </summary>
        public void RegisterRange(IEnumerable<GameProperty> properties, string category = "Default")
        {
            ThrowIfNotReady();

            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            foreach (GameProperty property in properties)
            {
                Register(property, category);
            }
        }

        #endregion

        #region 查询API

        /// <summary>
        ///     通过ID获取属性
        /// </summary>
        public GameProperty Get(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            _properties.TryGetValue(id, out GameProperty property);
            return property;
        }

        /// <summary>
        ///     获取指定分类的所有属性
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

            // 支持通配符："Category.*" 匹配所有子分类（线程安全）
            var results = new List<GameProperty>();
            string prefix = category.EndsWith(".*")
                ? category[..^2] + "."
                : category + ".";

            lock (_categoryLock)
            {
                foreach (var kvp in _categories)
                {
                    if (kvp.Key == category || kvp.Key.StartsWith(prefix))
                        results.AddRange(kvp.Value.ToList().Select(id => _properties[id]).Where(p => p != null));
                }
            }

            return results;
        }

        /// <summary>
        ///     获取包含指定标签的所有属性
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
        ///     组合查询：获取同时满足分类和标签条件的属性（交集）
        /// </summary>
        public IEnumerable<GameProperty> GetByCategoryAndTag(string category, string tag)
        {
            var categoryProps = GetByCategory(category).Select(p => p.ID).ToHashSet();
            var tagProps = GetByTag(tag).Select(p => p.ID).ToHashSet();

            var intersection = categoryProps.Intersect(tagProps);
            return intersection.Select(id => _properties[id]).Where(p => p != null);
        }

        /// <summary>
        ///     获取属性的元数据
        /// </summary>
        public PropertyMetadata GetMetadata(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            _metadata.TryGetValue(id, out PropertyMetadata metadata);
            return metadata;
        }

        /// <summary>
        ///     获取所有已注册的属性ID
        /// </summary>
        public IEnumerable<string> GetAllPropertyIds() => _properties.Keys;

        /// <summary>
        ///     获取所有分类名
        /// </summary>
        public IEnumerable<string> GetAllCategories() => _categories.Keys;

        #endregion

        #region 移除API

        /// <summary>
        ///     移除指定属性
        /// </summary>
        public bool Unregister(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;

            // 从主表移除
            if (!_properties.TryRemove(id, out GameProperty property))
                return false;

            // 从元数据移除
            _metadata.TryRemove(id, out _);

            // 从分类索引移除（线程安全）
            if (_propertyToCategory.TryRemove(id, out string category))
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
        ///     移除整个分类及其所有属性
        /// </summary>
        public void UnregisterCategory(string category)
        {
            if (string.IsNullOrEmpty(category))
                return;

            if (_categories.TryRemove(category, out var ids))
            {
                foreach (string id in ids.ToList())
                {
                    Unregister(id);
                }
            }
        }

        #endregion

        #region 批量操作API

        /// <summary>
        ///     设置分类中所有属性的激活状态
        /// </summary>
        public OperationResult<List<string>> SetCategoryActive(string category, bool active)
        {
            ThrowIfNotReady();

            var successIds = new List<string>();
            var failures = new List<FailureRecord>();

            var properties = GetByCategory(category).ToList();

            if (properties.Count == 0)
            {
                failures.Add(new(category, "分类不存在或为空", FailureType.CategoryNotFound));
                return OperationResult<List<string>>.PartialSuccess(successIds, 0, failures);
            }

            foreach (GameProperty property in properties)
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
                    failures.Add(new(property.ID, ex.Message, FailureType.UnknownError));
                }
            }

            return failures.Count == 0
                ? OperationResult<List<string>>.Success(successIds, successIds.Count)
                : OperationResult<List<string>>.PartialSuccess(successIds, successIds.Count, failures);
        }

        /// <summary>
        ///     为分类中所有属性应用修饰符
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
                failures.Add(new(category, "分类不存在或为空", FailureType.CategoryNotFound));
                return OperationResult<List<string>>.PartialSuccess(successIds, 0, failures);
            }

            foreach (GameProperty property in properties)
            {
                try
                {
                    property.AddModifier(modifier);
                    successIds.Add(property.ID);
                }
                catch (Exception ex)
                {
                    failures.Add(new(property.ID, ex.Message, FailureType.InvalidModifier));
                }
            }

            return failures.Count == 0
                ? OperationResult<List<string>>.Success(successIds, successIds.Count)
                : OperationResult<List<string>>.PartialSuccess(successIds, successIds.Count, failures);
        }

        #endregion

        #region 辅助方法

        /// <summary>
        ///     检查服务是否就绪，否则抛出异常
        /// </summary>
        private void ThrowIfNotReady()
        {
            if (State != ServiceLifecycleState.Ready)
                throw new InvalidOperationException($"服务未就绪，当前状态: {State}");
        }

        #endregion
    }
}