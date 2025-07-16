using EasyPack;
using System.Collections.Generic;
using System.Collections.Concurrent;

public static class CombineGamePropertyManager
{
    // 线程安全字典
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

        _properties.AddOrUpdate(property.ID, property, (key, oldValue) => property);
    }

    /// <summary>
    /// 根据ID查找 ICombineGameProperty
    /// </summary>
    public static ICombineGameProperty Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        _properties.TryGetValue(id, out var property);
        return property;
    }

    public static GameProperty GetGameProperty(string combinePropertyID, string id = "")
    {
        if (string.IsNullOrEmpty(combinePropertyID)) return null;

        _properties.TryGetValue(combinePropertyID, out var property);
        return property?.GetProperty(id);
    }

    /// <summary>
    /// 删除指定ID的 ICombineGameProperty
    /// </summary>
    public static bool Remove(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;

        var removed = _properties.TryRemove(id, out var property);

        if (removed && property is System.IDisposable disposable)
        {
            disposable.Dispose();
        }

        return removed;
    }

    /// <summary>
    /// 获取所有 ICombineGameProperty
    /// </summary>
    public static IEnumerable<ICombineGameProperty> GetAll()
    {
        return _properties.Values;
    }

    /// <summary>
    /// 清空所有属性
    /// </summary>
    public static void Clear()
    {
        foreach (var property in _properties.Values)
        {
            if (property is System.IDisposable disposable)
            {
                disposable.Dispose();
            }
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
}