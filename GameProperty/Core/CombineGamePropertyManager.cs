using EasyPack;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System;

public static class CombineGamePropertyManager
{
    private static readonly ConcurrentDictionary<string, ICombineGameProperty> _properties = new ConcurrentDictionary<string, ICombineGameProperty>();

    #region 增删属性
    /// <summary>
    /// 新增或更新一个 ICombineGameProperty
    /// </summary>
    public static void AddOrUpdate(ICombineGameProperty property)
    {
        if (property == null)
        {
            UnityEngine.Debug.LogWarning("尝试添加空的属性到管理器");
            return;
        }

        var oldProperty = _properties.AddOrUpdate(property.ID, property, (key, oldValue) =>
        {
            oldValue?.Dispose();
            return property;
        });
    }


    /// <summary>
    /// 删除指定ID的 ICombineGameProperty
    /// </summary>
    public static bool Remove(string id)
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
    /// 清空所有属性
    /// </summary>
    public static void Clear()
    {
        foreach (var property in _properties.Values)
        {
            property?.Dispose();
        }

        _properties.Clear();
    }

    /// <summary>
    /// 清理无效的属性
    /// </summary>
    public static int CleanupInvalidProperties()
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

    #region 查找
    /// <summary>
    /// 根据ID查找 ICombineGameProperty
    /// </summary>
    public static ICombineGameProperty Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        _properties.TryGetValue(id, out var property);
        return property?.IsValid() == true ? property : null;
    }

    public static GameProperty GetGamePropertyFromCombine(string combinePropertyID, string id = "")
    {
        if (string.IsNullOrEmpty(combinePropertyID)) return null;

        var property = Get(combinePropertyID);
        return property?.GetProperty(id);
    }


    /// <summary>
    /// 获取所有有效的 ICombineGameProperty
    /// </summary>
    public static IEnumerable<ICombineGameProperty> GetAll()
    {
        foreach (var property in _properties.Values)
        {
            if (property?.IsValid() == true)
                yield return property;
        }
    }

    /// <summary>
    /// 获取属性数量
    /// </summary>
    public static int Count => _properties.Count;

    /// <summary>
    /// 检查是否包含指定ID的属性
    /// </summary>
    public static bool Contains(string id)
    {
        return !string.IsNullOrEmpty(id) && _properties.ContainsKey(id);
    }

    #endregion

    #region 缓存处理

    /// <summary>
    /// 获取或创建GameProperty实例
    /// </summary>
    /// <param name="id">GameProperty的ID</param>
    /// <param name="initValue">初始值（仅在创建新实例时使用）</param>
    /// <returns>GameProperty实例</returns>
    public static GameProperty GetOrCreateGameProperty(string id, float initValue = 0f)
    {
        return GamePropertyManager.GetOrCreate(id, initValue);
    }

    /// <summary>
    /// 获取已缓存的GameProperty实例
    /// </summary>
    /// <param name="id">GameProperty的ID</param>
    /// <returns>GameProperty实例，不存在则返回null</returns>
    public static GameProperty GetCachedGameProperty(string id)
    {
        return GamePropertyManager.Get(id);
    }

    /// <summary>
    /// 减少GameProperty的引用计数
    /// 当ICombineGameProperty不再需要某个GameProperty时调用
    /// </summary>
    /// <param name="id">GameProperty的ID</param>
    /// <returns>减少后的引用计数</returns>
    public static int ReleaseGameProperty(string id)
    {
        return GamePropertyManager.RemoveReference(id);
    }

    /// <summary>
    /// 获取所有缓存的GameProperty统计信息
    /// </summary>
    /// <returns>缓存统计信息</returns>
    public static GamePropertyCacheStats GetGamePropertyCacheStats()
    {
        return GamePropertyManager.GetCacheStats();
    }

    /// <summary>
    /// 清理未引用的GameProperty实例
    /// </summary>
    /// <returns>清理的实例数量</returns>
    public static int CleanupUnreferencedGameProperties()
    {
        return GamePropertyManager.CleanupUnreferencedProperties();
    }
    #endregion
}