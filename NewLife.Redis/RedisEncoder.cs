using System.Runtime.CompilerServices;
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
#if NET8_0_OR_GREATER
        // net8.0 目标编译期直连 SystemJson；NativeAOT 下 STJ 泛型集合等反射路径缺少原生代码，
        // 按动态代码能力守卫回退默认主机，避免序列化在裁剪环境静默失败
        return RuntimeFeature.IsDynamicCodeSupported ? new SystemJson() : JsonHelper.Default;
#else
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
#endif
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