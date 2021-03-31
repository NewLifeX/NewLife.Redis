#if !NET40
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewLife.Caching
{
    public class RedisStatGroup
    {
        #region 属性
        public FullRedis Redis { get; set; }

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
}
#endif