using EasyPack;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System;

public static class CombineGamePropertyManager
{
    private static readonly ConcurrentDictionary<string, ICombineGameProperty> _properties = new ConcurrentDictionary<string, ICombineGameProperty>();

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
            // 如果有旧值，先清理资源
            oldValue?.Dispose();
            return property;
        });
    }

    /// <summary>
    /// 根据ID查找 ICombineGameProperty
    /// </summary>
    public static ICombineGameProperty Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        _properties.TryGetValue(id, out var property);
        return property?.IsValid() == true ? property : null;
    }

    public static GameProperty GetGameProperty(string combinePropertyID, string id = "")
    {
        if (string.IsNullOrEmpty(combinePropertyID)) return null;

        var property = Get(combinePropertyID);
        return property?.GetProperty(id);
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
}