using NewLife.Data;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Caching;

/// <summary>Redis编码器</summary>
public class RedisJsonEncoder : DefaultPacketEncoder
{
    #region 属性
    private static IJsonHost _host;
    #endregion

    static RedisJsonEncoder() => _host = GetJsonHost();

    /// <summary>实例化Redis编码器</summary>
    public RedisJsonEncoder() => JsonHost = _host;

    internal static IJsonHost GetJsonHost()
    {
        // 尝试使用System.Text.Json，不支持时使用FastJson
        var host = JsonHelper.Default;
        if (host == null || host.GetType().Name == "FastJson")
        {
            // 当前组件输出net45和netstandard2.0，而SystemJson要求net5以上，因此通过反射加载
            try
            {
                var type = $"{typeof(FastJson).Namespace}.SystemJson".GetTypeEx();
                if (type != null)
                {
                    host = type.CreateInstance() as IJsonHost;
                }
            }
            catch { }
        }

        return host ?? JsonHelper.Default;
    }

    /// <summary>字符串解码为对象。复杂类型采用Json反序列化</summary>
    /// <param name="value"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    protected override Object? OnDecode(String value, Type type)
    {
        if (type == typeof(Boolean) && value == "OK") return true;

        return base.OnDecode(value, type);
    }
}