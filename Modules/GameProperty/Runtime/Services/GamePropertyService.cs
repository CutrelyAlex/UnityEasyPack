using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasyPack.Architecture;
using EasyPack.Category;
using EasyPack.CustomData;
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
        private const int FirstUID = 1000;
        #region 字段

        // 核心数据
        private ConcurrentDictionary<string, GameProperty> _properties;
        private ConcurrentDictionary<string, PropertyData> _propertyData;
        private ConcurrentDictionary<string, string> _propertyToCategory;

        // UID -> Property 查找缓存
        private ConcurrentDictionary<long, GameProperty> _uidToProperties;

        // 分类/标签/元数据系统（使用 UID 作为 key）
        private ICategoryManager<GameProperty, long> _categoryManager;

        // 分类服务（持有并管理 CategoryManager 实例）
        private ICategoryService _categoryService;

        // UID 分配器（单实例内全局唯一）
        private long _nextUid;

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
            _propertyData = new();
            _propertyToCategory = new();

            _uidToProperties = new();

            // CategoryManager 由 CategoryService 统一持有与管理
            _categoryService = await EasyPackArchitecture.Instance.ResolveAsync<ICategoryService>()
                               ?? throw new InvalidOperationException("[GamePropertyService] ICategoryService 未初始化，无法获取 CategoryManager");
            _categoryManager = _categoryService.GetOrCreateManager<GameProperty, long>(p => p.UID) ?? throw new InvalidOperationException("[GamePropertyService] 获取 CategoryManager 失败");

            // UID 从 1001 起算
            _nextUid = FirstUID;

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
            _propertyData?.Clear();
            _propertyToCategory?.Clear();
            _uidToProperties?.Clear();

            // CategoryManager 生命周期由 CategoryService 管理
            _categoryManager = null;
            _categoryService = null;

            await base.OnDisposeAsync();

            Debug.Log("[GamePropertyService] 游戏属性服务已释放");
        }

        #endregion

        #region 注册API

        /// <summary>
        ///     注册单个属性到指定分类
        /// </summary>
        public void Register(GameProperty property, string category = "Default", PropertyData metadata = null)
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
        private void RegisterInternal(GameProperty property, string category, PropertyData metadata)
        {
            if (_categoryManager == null)
                throw new InvalidOperationException("CategoryManager 未初始化");

            string normalizedCategory = CategoryNameNormalizer.Normalize(category);
            if (string.IsNullOrWhiteSpace(normalizedCategory))
                normalizedCategory = "Default";

            if (!CategoryNameNormalizer.IsValid(normalizedCategory, out string errorMessage))
                throw new ArgumentException(errorMessage ?? "分类名称无效", nameof(category));

            // 1) 处理 UID
            // 必须先处理，因为 CategoryManager 使用 UID 作为 key
            EnsureUidAssigned(property);

            // 1.1) 如果 CategoryManager 已预先加载，
            //      则优先使用 Manager 内的分类/标签/元数据，并避免 DuplicateId。
            string effectiveCategory = normalizedCategory;
            string[] effectiveTags = metadata?.Tags;
            CustomDataCollection effectiveCustomData = metadata?.CustomData;

            Category.OperationResult<GameProperty> existingInManager = _categoryManager.GetById(property.UID);
            if (existingInManager != null && existingInManager.IsSuccess)
            {
                GameProperty managerEntity = existingInManager.Value;

                // 只有 ID 相同，才认为是同一实体（例如 Manager 已预先加载）
                // 否则视为 UID 冲突，重分配 UID，避免错误复用 Manager 的 category/tags/metadata。
                if (managerEntity != null && managerEntity.ID != property.ID)
                {
                    EnsureUidAssigned(property);
                }
                else
                {
                string existingCategory = _categoryManager.GetReadableCategoryPath(property.UID);
                if (!string.IsNullOrWhiteSpace(existingCategory))
                    effectiveCategory = existingCategory;

                IReadOnlyList<string> existingTags = _categoryManager.GetEntityTags(property.UID);
                if (existingTags is { Count: > 0 })
                    effectiveTags = existingTags.ToArray();

                CustomDataCollection existingMetadata = _categoryManager.GetMetadata(property.UID);
                if (existingMetadata != null)
                    effectiveCustomData = existingMetadata;

                // 由于 CategoryManager 不支持直接替换 entity，这里保留旧数据后重建 entity 引用。
                OperationResult deleteResult = _categoryManager.DeleteEntity(property.UID);
                if (!deleteResult.IsSuccess)
                    Debug.LogWarning($"[GamePropertyService] 预清理已存在的 CategoryManager 实体失败: UID={property.UID}, Error={deleteResult.ErrorMessage}");
                }
            }

            // 2) 建立本地索引
            _properties[property.ID] = property;
            _propertyToCategory[property.ID] = effectiveCategory;
            _uidToProperties[property.UID] = property;

            // 3) 记录 PropertyData
            if (metadata == null && (effectiveTags != null || effectiveCustomData != null))
                metadata = new();

            if (metadata != null)
            {
                // 若 Manager 已有数据，则以 Manager 为准
                if (effectiveTags != null)
                    metadata.Tags = effectiveTags;

                if (effectiveCustomData != null)
                    metadata.CustomData = effectiveCustomData;

                if (metadata.CustomData == null)
                    metadata.CustomData = new();

                // 去重标签
                if (metadata.Tags != null)
                    metadata.Tags = metadata.Tags.Distinct().ToArray();

                _propertyData[property.ID] = metadata;
            }

            // 4) 注册到分类系统
            OperationResult registerResult = _categoryManager.RegisterEntity(property.UID, property, effectiveCategory);
            if (!registerResult.IsSuccess)
                throw new InvalidOperationException($"CategoryManager 注册失败: {registerResult.ErrorMessage}");

            // 5) 同步标签（默认使用 PropertyData.Tags）
            if (effectiveTags is { Length: > 0 })
            {
                OperationResult tagResult = _categoryManager.AddTags(property.UID, effectiveTags);
                if (!tagResult.IsSuccess)
                    throw new InvalidOperationException($"标签注册失败: {tagResult.ErrorMessage}");
            }

            // 6) 同步元数据（仅承载 CustomData）
            CustomDataCollection customData = effectiveCustomData ?? metadata?.CustomData ?? new();
            OperationResult metadataResult = _categoryManager.UpdateMetadata(property.UID, customData);
            if (!metadataResult.IsSuccess)
                throw new InvalidOperationException($"元数据写入失败: {metadataResult.ErrorMessage}");
        }

        private long AllocateUid() => Interlocked.Increment(ref _nextUid);

        private bool UidExistsInManager(long uid)
        {
            if (_categoryManager == null || uid <= 0) return false;
            var result = _categoryManager.GetById(uid);
            return result != null && result.IsSuccess;
        }

        private void EnsureUidCounterAtLeast(long uid)
        {
            if (uid <= 0) return;

            while (true)
            {
                long current = Interlocked.Read(ref _nextUid);
                if (current >= uid) return;
                if (Interlocked.CompareExchange(ref _nextUid, uid, current) == current) return;
            }
        }

        private void EnsureUidAssigned(GameProperty property)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));

            if (property.UID <= 0)
            {
                long newUid;
                do
                {
                    newUid = AllocateUid();
                } while (_uidToProperties.ContainsKey(newUid) || UidExistsInManager(newUid));

                property.UID = newUid;
                return;
            }

            // 已携带 UID（例如反序列化场景），确保计数器不回退
            EnsureUidCounterAtLeast(property.UID);

            // 本地缓存冲突
            if (_uidToProperties.TryGetValue(property.UID, out GameProperty existing) && existing != property)
            {
                long newUid;
                do
                {
                    newUid = AllocateUid();
                } while (_uidToProperties.ContainsKey(newUid) || UidExistsInManager(newUid));

                Debug.LogWarning($"[GamePropertyService] UID 冲突（本地缓存），已重新分配: OldUID={property.UID}, NewUID={newUid}, Id={property.ID}");
                property.UID = newUid;
                return;
            }

            // 与 CategoryManager 中已存在实体冲突：
            // - 若 ID 相同，认为是“Manager 已预加载”的同一实体，保留 UID
            // - 若 ID 不同，认为是真冲突，重分配 UID
            var inManager = _categoryManager?.GetById(property.UID);
            if (inManager != null && inManager.IsSuccess)
            {
                GameProperty managerEntity = inManager.Value;
                if (managerEntity != null && managerEntity != property && managerEntity.ID != property.ID)
                {
                    long newUid;
                    do
                    {
                        newUid = AllocateUid();
                    } while (_uidToProperties.ContainsKey(newUid) || UidExistsInManager(newUid));

                    Debug.LogWarning($"[GamePropertyService] UID 冲突（CategoryManager），已重新分配: OldUID={property.UID}, NewUID={newUid}, Id={property.ID}, ExistingId={managerEntity.ID}");
                    property.UID = newUid;
                }
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
        ///     通过 UID 获取属性。
        /// </summary>
        public GameProperty GetByUid(long uid)
        {
            if (uid < 0) return null;
            return _uidToProperties != null && _uidToProperties.TryGetValue(uid, out var p) ? p : null;
        }

        /// <summary>
        ///     获取指定分类的所有属性
        /// </summary>
        public IEnumerable<GameProperty> GetByCategory(string category, bool includeChildren = false)
        {
            if (_categoryManager == null || string.IsNullOrEmpty(category))
                return Enumerable.Empty<GameProperty>();

            return _categoryManager.GetByCategory(category, includeChildren);
        }

        /// <summary>
        ///     获取包含指定标签的所有属性
        /// </summary>
        public IEnumerable<GameProperty> GetByTag(string tag)
        {
            if (_categoryManager == null || string.IsNullOrEmpty(tag))
                return Enumerable.Empty<GameProperty>();

            return _categoryManager.GetByTag(tag);
        }

        /// <summary>
        ///     组合查询：获取同时满足分类和标签条件的属性（交集）
        /// </summary>
        public IEnumerable<GameProperty> GetByCategoryAndTag(string category, string tag)
        {
            if (_categoryManager == null) return Enumerable.Empty<GameProperty>();
            return _categoryManager.GetByCategoryAndTag(category, tag, includeChildren: false);
        }

        /// <summary>
        ///     获取属性的元数据
        /// </summary>
        public PropertyData GetMetadata(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            _propertyData.TryGetValue(id, out PropertyData metadata);
            return metadata;
        }

        internal bool TryGetCategoryOfProperty(string propertyId, out string category)
        {
            category = null;
            return _propertyToCategory != null && _propertyToCategory.TryGetValue(propertyId, out category);
        }

        /// <summary>
        ///     获取所有已注册的属性ID
        /// </summary>
        public IEnumerable<string> GetAllPropertyIds() => _properties.Keys;

        /// <summary>
        ///     获取所有分类名
        /// </summary>
        public IEnumerable<string> GetAllCategories() => _categoryManager?.GetCategoriesNodes() ?? Array.Empty<string>();

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

            if (property != null && property.UID >= 0)
            {
                _uidToProperties.TryRemove(property.UID, out _);
                _categoryManager?.DeleteEntity(property.UID);
            }

            // 从元数据移除
            _propertyData.TryRemove(id, out _);

            _propertyToCategory.TryRemove(id, out _);

            return true;
        }

        /// <summary>
        ///     通过 UID 移除指定属性
        /// </summary>
        public bool UnregisterByUid(long uid)
        {
            if (uid <= 0)
                return false;

            if (_uidToProperties != null && _uidToProperties.TryGetValue(uid, out GameProperty property) && property != null)
                return Unregister(property.ID);

            // 本地不存在时，仍尝试从 CategoryManager 移除（避免留下脏数据）
            OperationResult result = _categoryManager?.DeleteEntity(uid);
            return result != null && result.IsSuccess;
        }

        /// <summary>
        ///     通过 UID 将属性移动到新的分类
        /// </summary>
        public bool MoveToCategoryByUid(long uid, string newCategory)
        {
            if (uid <= 0) return false;
            if (string.IsNullOrWhiteSpace(newCategory)) return false;
            if (_categoryManager == null) return false;

            // Move API 目前仅在具体实现 CategoryManager 上提供
            if (_categoryManager is not CategoryManager<GameProperty, long> concrete)
            {
                Debug.LogWarning("[GamePropertyService] 当前 CategoryManager 不支持 MoveEntityToCategory 操作");
                return false;
            }

            OperationResult result = concrete.MoveEntityToCategorySafe(uid, newCategory);
            if (!result.IsSuccess) return false;

            // 更新本地缓存
            if (_uidToProperties != null && _uidToProperties.TryGetValue(uid, out GameProperty property) && property != null)
            {
                string normalizedCategory = CategoryNameNormalizer.Normalize(newCategory);
                if (string.IsNullOrWhiteSpace(normalizedCategory))
                    normalizedCategory = "Default";
                _propertyToCategory[property.ID] = normalizedCategory;
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

            if (_categoryManager == null) return;

            // 仅移除该分类下的一级实体
            var properties = _categoryManager.GetByCategory(category, includeChildren: false).ToList();
            foreach (GameProperty p in properties)
            {
                if (p != null) Unregister(p.ID);
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