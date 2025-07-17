using EasyPack;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System;

public static class CombineGamePropertyManager
{
    private static readonly ConcurrentDictionary<string, ICombineGameProperty> _properties = new ConcurrentDictionary<string, ICombineGameProperty>();

    #region ��ɾ����
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
            oldValue?.Dispose();
            return property;
        });
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
    #endregion

    #region ����
    /// <summary>
    /// ����ID���� ICombineGameProperty
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

    #endregion

    #region ���洦��

    /// <summary>
    /// ��ȡ�򴴽�GamePropertyʵ��
    /// </summary>
    /// <param name="id">GameProperty��ID</param>
    /// <param name="initValue">��ʼֵ�����ڴ�����ʵ��ʱʹ�ã�</param>
    /// <returns>GamePropertyʵ��</returns>
    public static GameProperty GetOrCreateGameProperty(string id, float initValue = 0f)
    {
        return GamePropertyManager.GetOrCreate(id, initValue);
    }

    /// <summary>
    /// ��ȡ�ѻ����GamePropertyʵ��
    /// </summary>
    /// <param name="id">GameProperty��ID</param>
    /// <returns>GamePropertyʵ�����������򷵻�null</returns>
    public static GameProperty GetCachedGameProperty(string id)
    {
        return GamePropertyManager.Get(id);
    }

    /// <summary>
    /// ����GameProperty�����ü���
    /// ��ICombineGameProperty������Ҫĳ��GamePropertyʱ����
    /// </summary>
    /// <param name="id">GameProperty��ID</param>
    /// <returns>���ٺ�����ü���</returns>
    public static int ReleaseGameProperty(string id)
    {
        return GamePropertyManager.RemoveReference(id);
    }

    /// <summary>
    /// ��ȡ���л����GamePropertyͳ����Ϣ
    /// </summary>
    /// <returns>����ͳ����Ϣ</returns>
    public static GamePropertyCacheStats GetGamePropertyCacheStats()
    {
        return GamePropertyManager.GetCacheStats();
    }

    /// <summary>
    /// ����δ���õ�GamePropertyʵ��
    /// </summary>
    /// <returns>�����ʵ������</returns>
    public static int CleanupUnreferencedGameProperties()
    {
        return GamePropertyManager.CleanupUnreferencedProperties();
    }
    #endregion
}