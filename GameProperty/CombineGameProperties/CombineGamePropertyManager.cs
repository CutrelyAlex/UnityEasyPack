using EasyPack;
using System.Collections.Generic;
using System.Collections.Concurrent;

public static class CombineGamePropertyManager
{
    // �̰߳�ȫ�ֵ�
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

        _properties.AddOrUpdate(property.ID, property, (key, oldValue) => property);
    }

    /// <summary>
    /// ����ID���� ICombineGameProperty
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
    /// ɾ��ָ��ID�� ICombineGameProperty
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
    /// ��ȡ���� ICombineGameProperty
    /// </summary>
    public static IEnumerable<ICombineGameProperty> GetAll()
    {
        return _properties.Values;
    }

    /// <summary>
    /// �����������
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
}