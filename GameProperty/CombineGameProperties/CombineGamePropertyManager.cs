using RPGPack;
using System.Collections.Generic;

public static class CombineGamePropertyManager
{
    private static readonly Dictionary<string, ICombineGameProperty> _properties = new Dictionary<string, ICombineGameProperty>();

    /// <summary>
    /// ���������һ�� ICombineGameProperty
    /// </summary>
    public static void AddOrUpdate(ICombineGameProperty property)
    {
        _properties[property.ID] = property;
    }

    /// <summary>
    /// ����ID���� ICombineGameProperty
    /// </summary>
    public static ICombineGameProperty Get(string id)
    {
        _properties.TryGetValue(id, out var property);
        return property;
    }

    public static GameProperty GetGameProperty(string combinePropertyID, string id = "")
    {
        _properties.TryGetValue(combinePropertyID, out var property);
        if (property == null)
        {
            return null;
        }
        return property.GetProperty(id);
    }

    /// <summary>
    /// ɾ��ָ��ID�� ICombineGameProperty
    /// </summary>
    public static bool Remove(string id)
    {
        return _properties.Remove(id);
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
        _properties.Clear();
    }
}