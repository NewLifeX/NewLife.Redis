using System;
using System.Collections.Generic;

namespace NewLife.Caching
{
    /// <summary>超级基数估算</summary>
    /// <remarks>
    /// HyperLogLog可以使用固定且很少的内存（每个HyperLogLog结构需要12K字节再加上key本身的几个字节）来存储集合的唯一元素。
    /// 返回的可见集合基数并不是精确值， 而是一个带有 0.81% 标准错误（standard error）的近似值。
    /// 例如为了记录一天会执行多少次各不相同的搜索查询， 一个程序可以在每次执行搜索查询时调用一次PFADD， 并通过调用PFCOUNT命令来获取这个记录的近似结果。
    /// 注意: 这个命令的一个副作用是可能会导致HyperLogLog内部被更改，出于缓存的目的,它会用8字节的来记录最近一次计算得到基数,所以PFCOUNT命令在技术上是个写命令。
    /// </remarks>
    public class HyperLogLog : RedisBase
    {
        #region 实例化
        /// <summary>实例化超级基数</summary>
        /// <param name="redis"></param>
        /// <param name="key"></param>
        public HyperLogLog(Redis redis, String key) : base(redis, key) { }
        #endregion

        /// <summary>添加</summary>
        /// <remarks>
        /// 这个命令的一个副作用是它可能会更改这个HyperLogLog的内部来反映在每添加一个唯一的对象时估计的基数(集合的基数)。
        /// 如果一个HyperLogLog的估计的近似基数在执行命令过程中发了变化， PFADD 返回1，否则返回0，
        /// 如果指定的key不存在，这个命令会自动创建一个空的HyperLogLog结构（指定长度和编码的字符串）。
        /// 如果在调用该命令时仅提供变量名而不指定元素也是可以的，如果这个变量名存在，则不会有任何操作，如果不存在，则会创建一个数据结构（返回1）
        /// </remarks>
        /// <param name="items"></param>
        /// <returns></returns>
        public Int32 Add(params String[] items)
        {
            var args = new List<Object>
            {
                Key
            };
            foreach (var item in items)
            {
                args.Add(item);
            }
            return Execute(rc => rc.Execute<Int32>("PFADD", args.ToArray()), true);
        }

        /// <summary>近似基数</summary>
        /// <remarks>
        /// 返回存储在HyperLogLog结构体的该变量的近似基数，如果该变量不存在,则返回0。
        /// 当参数为多个key时，返回这些HyperLogLog并集的近似基数，这个值是将所给定的所有key的HyperLoglog结构合并到一个临时的HyperLogLog结构中计算而得到的。
        /// </remarks>
        public Int32 Count => Execute(rc => rc.Execute<Int32>("PFCOUNT", Key));

        /// <summary>合并</summary>
        /// <remarks>
        /// 将多个 HyperLogLog 合并（merge）为一个 HyperLogLog ， 合并后的 HyperLogLog 的基数接近于所有输入 HyperLogLog 的可见集合（observed set）的并集。
        /// 合并得出的 HyperLogLog 会被储存在目标变量（第一个参数）里面， 如果该键并不存在， 那么命令在执行之前， 会先为该键创建一个空的。
        /// </remarks>
        /// <param name="keys"></param>
        /// <returns></returns>
        public Boolean Merge(params String[] keys)
        {
            var args = new List<Object>
            {
                Key
            };
            foreach (var item in keys)
            {
                args.Add(item);
            }
            return Execute(rc => rc.Execute<Boolean>("PFMERGE", args.ToArray()), true);
        }
    }
}