using EasyPack;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// 游戏属性管理器，负责集中管理所有组合属性（ICombineGameProperty）的生命周期
    /// 提供属性的添加、查询、更新、删除等功能，支持线程安全的并发访问
    /// </summary>
    public class GamePropertyManager
    {

        private readonly ConcurrentDictionary<string, ICombineGameProperty> _properties = new ConcurrentDictionary<string, ICombineGameProperty>();

        #region 增删改查

        /// <summary>
        /// 添加或更新一个 ICombineGameProperty，如果已存在则替换并释放旧实例
        /// </summary>
        /// <param name="property">要添加或更新的组合属性</param>
        public void AddOrUpdate(ICombineGameProperty property)
        {
            if (property == null)
            {
                UnityEngine.Debug.LogWarning("不能添加空的属性到管理器");
                return;
            }

            var oldProperty = _properties.AddOrUpdate(property.ID, property, (key, oldValue) =>
            {
                // 释放旧值占用的资源
                oldValue?.Dispose();
                return property;
            });
        }

        /// <summary>
        /// 添加或更新一个 GameProperty（自动包装为 CombinePropertySingle）
        /// </summary>
        /// <param name="property">要添加的 GameProperty</param>
        /// <returns>自动包装的 CombinePropertySingle 实例</returns>
        public CombinePropertySingle Wrap(GameProperty property)
        {
            if (property == null)
            {
                UnityEngine.Debug.LogWarning("不能添加空的属性到管理器");
                return null;
            }

            // 检查是否已存在相同 ID 的包装器
            if (_properties.TryGetValue(property.ID, out var existing) && existing is CombinePropertySingle existingSingle)
            {
                // 如果 ResultHolder 的 ID 和基础值与原属性匹配，认为是同一个属性的包装
                if (existingSingle.ResultHolder.ID == property.ID)
                {
                    // 更新 ResultHolder 的基础值（如果有变化）
                    if (!Mathf.Approximately(existingSingle.ResultHolder.GetBaseValue(), property.GetBaseValue()))
                    {
                        existingSingle.ResultHolder.SetBaseValue(property.GetBaseValue());
                    }

                    // 返回已存在的包装器实例
                    return existingSingle;
                }
            }

            // 创建新的 CombinePropertySingle 包装器
            var wrapper = new CombinePropertySingle(property.ID, property.GetBaseValue());

            // 复制所有修饰符到 ResultHolder
            if (property.Modifiers != null && property.Modifiers.Count > 0)
            {
                foreach (var modifier in property.Modifiers)
                {
                    var clonedModifier = modifier.Clone();
                    wrapper.ResultHolder.AddModifier(clonedModifier);
                }
            }

            AddOrUpdate(wrapper);
            return wrapper;
        }

        /// <summary>
        /// 批量包装并添加 GameProperty
        /// </summary>
        /// <param name="properties">要添加的 GameProperty 集合</param>
        /// <returns>包装后的 CombinePropertySingle 集合</returns>
        public IEnumerable<CombinePropertySingle> WrapRange(IEnumerable<GameProperty> properties)
        {
            if (properties == null) yield break;

            foreach (var property in properties)
            {
                var wrapper = Wrap(property);
                if (wrapper != null)
                    yield return wrapper;
            }
        }

        /// <summary>
        /// 根据ID获取 ICombineGameProperty
        /// </summary>
        /// <param name="id">属性的唯一标识符</param>
        /// <returns>对应的组合属性实例，如果不存在或无效返回 null</returns>
        public ICombineGameProperty Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            _properties.TryGetValue(id, out var property);
            return property?.IsValid() == true ? property : null;
        }

        /// <summary>
        /// 根据ID获取 CombinePropertySingle
        /// </summary>
        /// <param name="id">属性ID</param>
        /// <returns>CombinePropertySingle 实例，如果不存在或类型不匹配返回 null</returns>
        public CombinePropertySingle GetSingle(string id)
        {
            return Get(id) as CombinePropertySingle;
        }

        /// <summary>
        /// 根据ID获取 CombinePropertyCustom
        /// </summary>
        /// <param name="id">属性ID</param>
        /// <returns>CombinePropertyCustom 实例，如果不存在或类型不匹配返回 null</returns>
        public CombinePropertyCustom GetCustom(string id)
        {
            return Get(id) as CombinePropertyCustom;
        }

        /// <summary>
        /// 根据ID直接获取 GameProperty（自动从组合属性中提取）
        /// </summary>
        /// <param name="id">组合属性ID</param>
        /// <param name="subId">子属性ID（对于 CombinePropertyCustom）</param>
        /// <returns>GameProperty 实例</returns>
        public GameProperty GetGameProperty(string id, string subId = "")
        {
            return GetGamePropertyFromCombine(id, subId);
        }

        /// <summary>
        /// 从组合属性中获取内部的 GameProperty
        /// </summary>
        /// <param name="combinePropertyID">组合属性的ID</param>
        /// <param name="id">子属性ID（对于 CombinePropertyCustom），为空则返回 ResultHolder</param>
        /// <returns>内部的 GameProperty 实例</returns>
        public GameProperty GetGamePropertyFromCombine(string combinePropertyID, string id = "")
        {
            if (string.IsNullOrEmpty(combinePropertyID)) return null;

            var property = Get(combinePropertyID);
            return property?.GetProperty(id);
        }

        /// <summary>
        /// 删除指定ID的 ICombineGameProperty 并释放其资源
        /// </summary>
        /// <param name="id">要删除的属性ID</param>
        /// <returns>删除成功返回 true，否则返回 false</returns>
        public bool Remove(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;

            var removed = _properties.TryRemove(id, out var property);

            if (removed)
            {
                property?.Dispose();
            }

            return removed;
        }

        /// <summary>
        /// 获取所有有效的 ICombineGameProperty
        /// </summary>
        public IEnumerable<ICombineGameProperty> GetAll()
        {
            foreach (var property in _properties.Values)
            {
                if (property?.IsValid() == true)
                    yield return property;
            }
        }

        /// <summary>
        /// 获取所有 CombinePropertySingle
        /// </summary>
        public IEnumerable<CombinePropertySingle> GetAllSingles()
        {
            foreach (var property in GetAll())
            {
                if (property is CombinePropertySingle single)
                    yield return single;
            }
        }

        /// <summary>
        /// 获取所有 CombinePropertyCustom
        /// </summary>
        public IEnumerable<CombinePropertyCustom> GetAllCustoms()
        {
            foreach (var property in GetAll())
            {
                if (property is CombinePropertyCustom custom)
                    yield return custom;
            }
        }

        /// <summary>
        /// 清空所有属性
        /// </summary>
        public void Clear()
        {
            foreach (var property in _properties.Values)
            {
                property?.Dispose();
            }

            _properties.Clear();
        }

        /// <summary>
        /// 清理无效属性
        /// </summary>
        public int CleanupInvalidProperties()
        {
            var invalidKeys = new List<string>();

            foreach (var kvp in _properties)
            {
                if (kvp.Value?.IsValid() != true)
                {
                    invalidKeys.Add(kvp.Key);
                }
            }

            foreach (var key in invalidKeys)
            {
                Remove(key);
            }

            return invalidKeys.Count;
        }
        #endregion

        #region 查询

        /// <summary>
        /// 获取属性数量
        /// </summary>
        public int Count => _properties.Count;

        /// <summary>
        /// 检查是否包含指定ID的属性
        /// </summary>
        public bool Contains(string id)
        {
            return !string.IsNullOrEmpty(id) && _properties.ContainsKey(id);
        }

        /// <summary>
        /// 检查指定ID的属性是否为 CombinePropertySingle
        /// </summary>
        public bool IsSingle(string id)
        {
            return Get(id) is CombinePropertySingle;
        }

        /// <summary>
        /// 检查指定ID的属性是否为 CombinePropertyCustom
        /// </summary>
        public bool IsCustom(string id)
        {
            return Get(id) is CombinePropertyCustom;
        }

        #endregion

        #region 批量操作

        /// <summary>
        /// 批量添加或更新组合属性
        /// </summary>
        /// <param name="properties">要添加的组合属性集合</param>
        public void AddOrUpdateRange(IEnumerable<ICombineGameProperty> properties)
        {
            if (properties == null) return;

            foreach (var property in properties)
            {
                AddOrUpdate(property);
            }
        }

        /// <summary>
        /// 批量移除属性
        /// </summary>
        /// <param name="ids">要移除的属性ID集合</param>
        /// <returns>成功移除的数量</returns>
        public int RemoveRange(IEnumerable<string> ids)
        {
            if (ids == null) return 0;

            int count = 0;
            foreach (var id in ids)
            {
                if (Remove(id))
                    count++;
            }

            return count;
        }

        #endregion
    }
}