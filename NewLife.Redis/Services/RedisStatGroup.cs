using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewLife.Caching.Services;

/// <summary>Redis统计组</summary>
public class RedisStatGroup
{
    #region 属性
    /// <summary>实例</summary>
    public FullRedis Redis { get; set; }

    /// <summary>统计集合</summary>
    public IList<RedisStat> Stats { get; set; } = new List<RedisStat>();
    #endregion

    #region 方法
    //public RedisStat Add(String key)
    //{
    //    var st = new RedisStat(Redis, key);
    //    Stats.Add(st);

    //    return st;
    //}

    ///// <summary>增加指定标量的值</summary>
    ///// <param name="field"></param>
    ///// <param name="value"></param>
    //public void Increment(String field, Int32 value)
    //{
    //    foreach (var item in Stats)
    //    {
    //        item.Increment(field, value);
    //    }
    //}
    #endregion
}