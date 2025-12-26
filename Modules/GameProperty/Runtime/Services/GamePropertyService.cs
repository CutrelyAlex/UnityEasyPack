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
        private const string DefaultCategory = "Default";

        #region 字段

        // 核心数据
        private ConcurrentDictionary<string, GameProperty> _properties;
        private ConcurrentDictionary<string, PropertyDisplayInfo> _propertyDisplayInfo;

        // UID -> Property 查找缓存
        private ConcurrentDictionary<long, GameProperty> _uidToProperties;

        // 分类/标签/元数据系统（使用 UID 作为 key）
        internal ICategoryManager<GameProperty, long> _categoryManager;

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
            _propertyDisplayInfo = new();

            _uidToProperties = new();

            // 直接初始化 CategoryManager
            _categoryManager = new CategoryManager<GameProperty, long>(p => p.UID);

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

                // 注册 CategoryManager 序列化器
                serializationService.RegisterSerializer(
                    new CategoryManagerJsonSerializer<GameProperty, long>(p => p.UID));

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
            _propertyDisplayInfo?.Clear();
            _uidToProperties?.Clear();

            // 释放 CategoryManager
            if (_categoryManager is IDisposable disposable)
            {
                disposable.Dispose();
            }

            _categoryManager = null;

            await base.OnDisposeAsync();

            Debug.Log("[GamePropertyService] 游戏属性服务已释放");
        }

        #endregion

        #region 注册API

        /// <summary>
        ///     注册单个属性到指定分类
        /// </summary>
        public void Register(GameProperty property, string category = DefaultCategory,
                             PropertyDisplayInfo displayInfo = null, string[] tags = null,
                             CustomDataCollection customData = null)
        {
            ThrowIfNotReady();

            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            if (string.IsNullOrEmpty(property.ID))
            {
                throw new ArgumentException("属性ID不能为空");
            }

            if (_properties.ContainsKey(property.ID))
            {
                throw new ArgumentException($"属性ID '{property.ID}' 已存在，不能重复注册");
            }

            RegisterInternal(property, category, displayInfo, tags, customData);
        }

        /// <summary>
        ///     内部注册逻辑
        /// </summary>
        private void RegisterInternal(GameProperty property, string category, PropertyDisplayInfo displayInfo,
                                      string[] tags, CustomDataCollection customData)
        {
            if (_categoryManager == null)
            {
                throw new InvalidOperationException("CategoryManager 未初始化");
            }

            string normalizedCategory = CategoryNameNormalizer.Normalize(category);
            if (string.IsNullOrWhiteSpace(normalizedCategory))
            {
                normalizedCategory = "Default";
            }

            if (!CategoryNameNormalizer.IsValid(normalizedCategory, out string errorMessage))
            {
                throw new ArgumentException(errorMessage ?? "分类名称无效", nameof(category));
            }

            // 1) 处理 UID
            // 必须先处理，因为 CategoryManager 使用 UID 作为 key
            EnsureUidAssigned(property);

            // 1.1) 如果 CategoryManager 已预先加载，
            //      则优先使用 Manager 内的分类/标签/元数据，并避免 DuplicateId。
            string effectiveCategory = normalizedCategory;
            string[] effectiveTags = tags;
            CustomDataCollection effectiveCustomData = customData;

            var existingInManager = _categoryManager.GetById(property.UID);
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
                    // 直接更新 CategoryManager 中的实体引用，保留原有的分类、标签和元数据
                    OperationResult updateResult = _categoryManager.UpdateEntityReference(property.UID, property);
                    if (updateResult.IsSuccess)
                    {
                        // 更新成功，建立本地索引并记录显示信息
                        _properties[property.ID] = property;
                        _uidToProperties[property.UID] = property;

                        if (displayInfo != null)
                        {
                            _propertyDisplayInfo[property.ID] = displayInfo;
                        }

                        return;
                    }

                    // 如果更新失败（理论上不应该），则回退到旧的“删除并重新注册”逻辑
                    Debug.LogWarning(
                        $"[GamePropertyService] UpdateEntityReference 失败: {updateResult.ErrorMessage}，将尝试重新注册。");

                    string existingCategory = _categoryManager.GetReadableCategoryPath(property.UID);
                    if (!string.IsNullOrWhiteSpace(existingCategory))
                    {
                        effectiveCategory = existingCategory;
                    }

                    var existingTags = _categoryManager.GetEntityTags(property.UID);
                    if (existingTags is { Count: > 0 })
                    {
                        effectiveTags = existingTags.ToArray();
                    }

                    CustomDataCollection existingMetadata = _categoryManager.GetMetadata(property.UID);
                    if (existingMetadata != null)
                    {
                        effectiveCustomData = existingMetadata;
                    }

                    _categoryManager.DeleteEntity(property.UID);
                }
            }

            // 2) 建立本地索引
            _properties[property.ID] = property;
            _uidToProperties[property.UID] = property;

            // 3) 记录 PropertyDisplayInfo
            if (displayInfo != null)
            {
                _propertyDisplayInfo[property.ID] = displayInfo;
            }

            // 4) 注册到分类系统
            OperationResult registerResult = _categoryManager.RegisterEntity(property.UID, property, effectiveCategory);
            if (!registerResult.IsSuccess)
            {
                throw new InvalidOperationException($"CategoryManager 注册失败: {registerResult.ErrorMessage}");
            }

            // 5) 同步标签
            if (effectiveTags is { Length: > 0 })
            {
                // 去重
                string[] distinctTags = effectiveTags.Distinct().ToArray();
                OperationResult tagResult = _categoryManager.AddTags(property.UID, distinctTags);
                if (!tagResult.IsSuccess)
                {
                    throw new InvalidOperationException($"标签注册失败: {tagResult.ErrorMessage}");
                }
            }

            // 6) 同步元数据（CustomData）
            CustomDataCollection finalCustomData = effectiveCustomData ?? new();
            OperationResult metadataResult = _categoryManager.UpdateMetadata(property.UID, finalCustomData);
            if (!metadataResult.IsSuccess)
            {
                throw new InvalidOperationException($"元数据写入失败: {metadataResult.ErrorMessage}");
            }
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

                Debug.LogWarning(
                    $"[GamePropertyService] UID 冲突（本地缓存），已重新分配: OldUID={property.UID}, NewUID={newUid}, Id={property.ID}");
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

                    Debug.LogWarning(
                        $"[GamePropertyService] UID 冲突（CategoryManager），已重新分配: OldUID={property.UID}, NewUID={newUid}, Id={property.ID}, ExistingId={managerEntity.ID}");
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
            {
                throw new ArgumentNullException(nameof(properties));
            }

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
            {
                return null;
            }

            _properties.TryGetValue(id, out GameProperty property);
            return property;
        }

        /// <summary>
        ///     通过 UID 获取属性。
        /// </summary>
        public GameProperty GetByUid(long uid)
        {
            if (uid < 0) return null;
            return _uidToProperties != null && _uidToProperties.TryGetValue(uid, out GameProperty p) ? p : null;
        }

        /// <summary>
        ///     获取指定分类的所有属性
        /// </summary>
        public IEnumerable<GameProperty> GetByCategory(string category, bool includeChildren = false)
        {
            if (_categoryManager == null || string.IsNullOrEmpty(category))
            {
                return Enumerable.Empty<GameProperty>();
            }

            return _categoryManager.GetByCategory(category, includeChildren);
        }

        /// <summary>
        ///     获取包含指定标签的所有属性
        /// </summary>
        public IEnumerable<GameProperty> GetByTag(string tag)
        {
            if (_categoryManager == null || string.IsNullOrEmpty(tag))
            {
                return Enumerable.Empty<GameProperty>();
            }

            return _categoryManager.GetByTag(tag);
        }

        public bool HasTag(string id, string tag)
        {
            GameProperty property = Get(id);
            return property != null && _categoryManager.HasTag(property, tag);
        }

        public IEnumerable<string> GetTags(string id)
        {
            GameProperty property = Get(id);
            return property != null ? _categoryManager.GetTags(property) : Enumerable.Empty<string>();
        }

        /// <summary>
        ///     组合查询：获取同时满足分类和标签条件的属性（交集）
        /// </summary>
        public IEnumerable<GameProperty> GetByCategoryAndTag(string category, string tag)
        {
            if (_categoryManager == null) return Enumerable.Empty<GameProperty>();
            return _categoryManager.GetByCategoryAndTag(category, tag, false);
        }

        /// <summary>
        ///     获取属性的显示元数据
        /// </summary>
        public PropertyDisplayInfo GetPropertyDisplayInfo(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            _propertyDisplayInfo.TryGetValue(id, out PropertyDisplayInfo propertyDisplayInfo);
            return propertyDisplayInfo;
        }

        /// <summary>
        ///     获取属性的自定义扩展数据
        /// </summary>
        public CustomDataCollection GetCustomData(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            _properties.TryGetValue(id, out GameProperty property);
            if (property == null) return null;

            return _categoryManager?.GetMetadata(property.UID);
        }

        internal bool TryGetCategoryOfProperty(string propertyId, out string category)
        {
            category = null;
            if (_properties.TryGetValue(propertyId, out GameProperty property))
            {
                category = _categoryManager?.GetReadableCategoryPath(property.UID);
                return !string.IsNullOrEmpty(category);
            }

            return false;
        }

        /// <summary>
        ///     获取所有已注册的属性ID
        /// </summary>
        public IEnumerable<string> GetAllPropertyIds() => _properties.Keys;

        /// <summary>
        ///     获取所有分类名
        /// </summary>
        public IEnumerable<string> GetAllCategories() =>
            _categoryManager?.GetCategoriesNodes() ?? Array.Empty<string>();

        #endregion

        #region 移除API

        /// <summary>
        ///     移除指定属性
        /// </summary>
        public bool Unregister(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            // 从主表移除
            if (!_properties.TryRemove(id, out GameProperty property))
            {
                return false;
            }

            if (property != null && property.UID >= 0)
            {
                _uidToProperties.TryRemove(property.UID, out _);
                _categoryManager?.DeleteEntity(property.UID);
            }

            // 从元数据移除
            _propertyDisplayInfo.TryRemove(id, out _);

            return true;
        }

        /// <summary>
        ///     通过 UID 移除指定属性
        /// </summary>
        public bool UnregisterByUid(long uid)
        {
            if (uid <= 0)
            {
                return false;
            }

            if (_uidToProperties != null && _uidToProperties.TryGetValue(uid, out GameProperty property) &&
                property != null)
            {
                return Unregister(property.ID);
            }

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
            return result.IsSuccess;
        }

        /// <summary>
        ///     移除整个分类及其所有属性
        /// </summary>
        public void UnregisterCategory(string category)
        {
            if (string.IsNullOrEmpty(category))
            {
                return;
            }

            if (_categoryManager == null) return;

            // 仅移除该分类下的一级实体
            var properties = _categoryManager.GetByCategory(category, false).ToList();
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
            {
                throw new ArgumentNullException(nameof(modifier));
            }

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
            {
                throw new InvalidOperationException($"服务未就绪，当前状态: {State}");
            }
        }

        #endregion
    }
}