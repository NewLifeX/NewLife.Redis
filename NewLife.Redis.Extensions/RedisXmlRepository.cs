using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace NewLife.Redis.Extensions;

/// <summary>在Redis中存储Xml</summary>
public class RedisXmlRepository : IXmlRepository
{
    private readonly NewLife.Caching.Redis _redis;

    private readonly String _key;

    /// <summary>实例化</summary>
    /// <param name="redis"></param>
    /// <param name="key"></param>
    public RedisXmlRepository(NewLife.Caching.Redis redis, String key)
    {
        _redis = redis;
        _key = key;
    }

    /// <summary>获取所有元素</summary>
    /// <returns></returns>
    public IReadOnlyCollection<XElement> GetAllElements() => GetAllElementsCore().ToList().AsReadOnly();

    /// <summary>遍历元素</summary>
    /// <returns></returns>
    private IEnumerable<XElement> GetAllElementsCore()
    {
        var list = _redis.GetList<String>(_key);
        foreach (var item in list)
        {
            yield return XElement.Parse(item);
        }
    }

    /// <summary>存储元素</summary>
    /// <param name="element"></param>
    /// <param name="friendlyName"></param>
    public void StoreElement(XElement element, String friendlyName)
    {
        var list = _redis.GetList<String>(_key);
        list.Add(element.ToString(SaveOptions.DisableFormatting));
    }
}
