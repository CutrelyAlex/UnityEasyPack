using EasyPack;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System;

public static class CombineGamePropertyManager
{
    private static readonly ConcurrentDictionary<string, ICombineGameProperty> _properties = new ConcurrentDictionary<string, ICombineGameProperty>();

    /// <summary>
    /// ���������һ�� ICombineGameProperty
    /// </summary>
    public static void AddOrUpdate(ICombineGameProperty property)
    {
        if (property == null)
        {
            UnityEngine.Debug.LogWarning("������ӿյ����Ե�������");
            return;
        }

        var oldProperty = _properties.AddOrUpdate(property.ID, property, (key, oldValue) =>
        {
            // ����о�ֵ����������Դ
            oldValue?.Dispose();
            return property;
        });
    }

    /// <summary>
    /// ����ID���� ICombineGameProperty
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
    /// ɾ��ָ��ID�� ICombineGameProperty
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
    /// ��ȡ������Ч�� ICombineGameProperty
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
    /// �����������
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
    /// ��ȡ��������
    /// </summary>
    public static int Count => _properties.Count;

    /// <summary>
    /// ����Ƿ����ָ��ID������
    /// </summary>
    public static bool Contains(string id)
    {
        return !string.IsNullOrEmpty(id) && _properties.ContainsKey(id);
    }

    /// <summary>
    /// ������Ч������
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