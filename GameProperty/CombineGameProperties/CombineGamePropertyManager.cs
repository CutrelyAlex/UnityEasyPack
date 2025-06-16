using RPGPack;
using System.Collections.Generic;

public static class CombineGamePropertyManager
{
    private static readonly Dictionary<string, ICombineGameProperty> _properties = new Dictionary<string, ICombineGameProperty>();

    /// <summary>
    /// 新增或更新一个 ICombineGameProperty
    /// </summary>
    public static void AddOrUpdate(ICombineGameProperty property)
    {
        _properties[property.ID] = property;
    }

    /// <summary>
    /// 根据ID查找 ICombineGameProperty
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
    /// 删除指定ID的 ICombineGameProperty
    /// </summary>
    public static bool Remove(string id)
    {
        return _properties.Remove(id);
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
        _properties.Clear();
    }
}