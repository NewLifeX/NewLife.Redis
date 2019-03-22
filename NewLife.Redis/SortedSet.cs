using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Data;
using NewLife.Reflection;

namespace NewLife.Caching
{
    /// <summary>排序集合</summary>
    public class SortedSet<T> : RedisBase
    {
        #region 实例化
        /// <summary>实例化超级基数</summary>
        /// <param name="redis"></param>
        /// <param name="key"></param>
        public SortedSet(Redis redis, String key) : base(redis, key) { }
        #endregion

        #region 方法
        /// <summary>个数</summary>
        public Int32 Count => Execute(rc => rc.Execute<Int32>("ZCARD", Key));

        /// <summary>添加</summary>
        /// <param name="item"></param>
        /// <param name="score"></param>
        public Int32 Add(T item, Double score)
        {
            var dic = new Dictionary<T, Double>
            {
                [item] = score
            };

            return Add(null, dic);
        }

        /// <summary>删除</summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Int32 Remove(T item) => Execute(r => r.Execute<Int32>("ZREM", Key, item), true);

        /// <summary>返回有序集key中，成员member的score值</summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Double GetScore(T item) => Execute(r => r.Execute<Int32>("ZSCORE", Key, item), false);
        #endregion

        #region 高级操作
        /// <summary>批量添加</summary>
        /// <remarks>
        /// 将所有指定成员添加到键为key有序集合（sorted set）里面。 添加时可以指定多个分数/成员（score/member）对。
        /// 如果指定添加的成员已经是有序集合里面的成员，则会更新改成员的分数（scrore）并更新到正确的排序位置。
        /// 
        /// 
        /// ZADD 命令在key后面分数/成员（score/member）对前面支持一些参数，他们是：
        /// XX: 仅仅更新存在的成员，不添加新成员。
        /// NX: 不更新存在的成员。只添加新成员。
        /// CH: 修改返回值为发生变化的成员总数，原始是返回新添加成员的总数(CH 是 changed 的意思)。
        /// 更改的元素是新添加的成员，已经存在的成员更新分数。 所以在命令中指定的成员有相同的分数将不被计算在内。
        /// 注：在通常情况下，ZADD返回值只计算新添加成员的数量。
        /// INCR: 当ZADD指定这个选项时，成员的操作就等同ZINCRBY命令，对成员的分数进行递增操作。
        /// </remarks>
        /// <param name="options">支持参数</param>
        /// <param name="members"></param>
        /// <returns></returns>
        public Int32 Add(String options, IDictionary<T, Double> members)
        {
            var args = new List<Object>
            {
                Key
            };

            // 支持参数
            if (!options.IsNullOrEmpty() && options.EqualIgnoreCase("XX", "NX", "CH", "INCR")) args.Add(options);

            foreach (var item in members)
            {
                args.Add(item.Value);
                args.Add(item.Key);
            }
            return Execute(rc => rc.Execute<Int32>("ZADD", args.ToArray()), true);
        }

        /// <summary>
        /// 返回有序集key中，score值在min和max之间(默认包括score值等于min或max)的成员
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public Int32 FindCount(Double min, Double max) => Execute(rc => rc.Execute<Int32>("ZCOUNT", Key, min, max));

        /// <summary>为有序集key的成员member的score值加上增量increment</summary>
        /// <param name="member"></param>
        /// <param name="score"></param>
        /// <returns></returns>
        public Int32 Increment(T member, Double score) => Execute(rc => rc.Execute<Int32>("ZINCRBY", Key, score, member));

        /// <summary>删除并返回有序集合key中的最多count个具有最高得分的成员</summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public IDictionary<T, Double> PopMax(Int32 count = 1)
        {
            var list = Execute(rc => rc.Execute<String[]>("ZPOPMAX", Key), false);
            if (list == null || list.Length == 0) return null;

            var dic = new Dictionary<T, Double>();
            for (var i = 0; i < list.Length; i += 2)
            {
                var score = list[i].ToDouble();
                var member = list[i + 1].ChangeType<T>();
                dic[member] = score;
            }

            return dic;
        }

        /// <summary>删除并返回有序集合key中的最多count个具有最低得分的成员</summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public IDictionary<T, Double> PopMin(Int32 count = 1)
        {
            var list = Execute(rc => rc.Execute<String[]>("ZPOPMIN", Key), false);
            if (list == null || list.Length == 0) return null;

            var dic = new Dictionary<T, Double>();
            for (var i = 0; i < list.Length; i += 2)
            {
                var score = list[i].ToDouble();
                var member = list[i + 1].ChangeType<T>();
                dic[member] = score;
            }

            return dic;
        }

        /// <summary>返回指定范围的列表</summary>
        /// <param name="start"></param>
        /// <param name="stop"></param>
        /// <returns></returns>
        public T[] Range(Int32 start, Int32 stop) => Execute(r => r.Execute<T[]>("ZRANGE", Key, start, stop));

        /// <summary>返回有序集key中成员member的排名。其中有序集成员按score值递增(从小到大)顺序排列</summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public Int32 Rank(T member) => Execute(r => r.Execute<Int32>("ZRANK", Key, member));

        /// <summary>模糊搜索，支持?和*</summary>
        /// <param name="pattern"></param>
        /// <param name="count"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public virtual String[] Search(String pattern, Int32 count, ref Int32 position)
        {
            var p = position;
            var rs = Execute(r => r.Execute<Object[]>("ZSCAN", p, "MATCH", pattern + "", "COUNT", count));

            if (rs != null)
            {
                position = (rs[0] as Packet).ToStr().ToInt();

                var ps = rs[1] as Object[];
                var ss = ps.Select(e => (e as Packet).ToStr()).ToArray();
                return ss;
            }

            return null;
        }
        #endregion
    }
}