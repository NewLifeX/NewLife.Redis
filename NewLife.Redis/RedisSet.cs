﻿using System.Collections;
using NewLife.Caching.Models;
using NewLife.Data;

namespace NewLife.Caching;

/// <summary>Set结构</summary>
/// <typeparam name="T"></typeparam>
public class RedisSet<T> : RedisBase, ICollection<T>
{
    #region 属性
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    /// <param name="redis"></param>
    /// <param name="key"></param>
    public RedisSet(Redis redis, String key) : base(redis, key) { }
    #endregion

    #region 列表方法
    /// <summary>个数</summary>
    public Int32 Count => Execute((r, k) => r.Execute<Int32>("SCARD", Key));

    Boolean ICollection<T>.IsReadOnly => false;

    /// <summary>添加元素在后面</summary>
    /// <param name="item"></param>
    public void Add(T item) => SAdd(item);

    /// <summary>清空列表</summary>
    public void Clear() => throw new NotSupportedException();

    /// <summary>是否包含指定元素</summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public Boolean Contains(T item) => Execute((r, k) => r.Execute<Int32>("SISMEMBER", Key, item)) > 0;

    /// <summary>复制到目标数组</summary>
    /// <param name="array"></param>
    /// <param name="arrayIndex"></param>
    public void CopyTo(T[] array, Int32 arrayIndex)
    {
        //var count = array.Length - arrayIndex;

        var arr = GetAll();
        arr.CopyTo(array, arrayIndex);
    }

    /// <summary>删除指定元素</summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public Boolean Remove(T item) => SDel(item) > 0;

    /// <summary>遍历</summary>
    /// <returns></returns>
    public IEnumerator<T> GetEnumerator() => GetAll().ToList().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    #endregion

    #region 高级操作
    /// <summary>批量添加</summary>
    /// <param name="members"></param>
    /// <returns></returns>
    public Int32 SAdd(params T[] members) => Execute((r, k) => r.ExecuteByKey<T, Int32>("SADD", Key, members), true);

    /// <summary>批量删除</summary>
    /// <param name="members"></param>
    /// <returns></returns>
    public Int32 SDel(params T[] members) => Execute((r, k) => r.ExecuteByKey<T, Int32>("SREM", Key, members), true);

    /// <summary>获取所有元素</summary>
    /// <returns></returns>
    public T[] GetAll() => Execute((r, k) => r.Execute<T[]>("SMEMBERS", Key)) ?? [];

    /// <summary>将member从source集合移动到destination集合中</summary>
    /// <param name="dest"></param>
    /// <param name="member"></param>
    /// <returns></returns>
    public T[] Move(String dest, T member) => Execute((r, k) => r.Execute<T[]>("SMOVE", Key, dest, member), true) ?? [];

    /// <summary>随机获取多个</summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public T[] RandomGet(Int32 count) => Execute((r, k) => r.Execute<T[]>("SRANDMEMBER", Key, count)) ?? [];

    /// <summary>随机获取并弹出</summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public T[] Pop(Int32 count) => Execute((r, k) => r.Execute<T[]>("SPOP", Key, count), true) ?? [];

    /// <summary>模糊搜索，支持?和*</summary>
    /// <param name="model">搜索模型</param>
    /// <returns></returns>
    public virtual IEnumerable<String> Search(SearchModel model)
    {
        var count = model.Count;
        while (count > 0)
        {
            var p = model.Position;
            var rs = Execute((r, k) => r.Execute<Object[]>("SSCAN", Key, p, "MATCH", model.Pattern + "", "COUNT", count));
            if (rs == null || rs.Length != 2) break;

            model.Position = (rs[0] as IPacket)!.ToStr().ToInt();

            var ps = rs[1] as Object[];
            foreach (IPacket item in ps!)
            {
                if (count-- > 0) yield return item.ToStr();
            }

            if (model.Position == 0) break;
        }
    }

    /// <summary>模糊搜索，支持?和*</summary>
    /// <param name="pattern">匹配表达式</param>
    /// <param name="count">返回个数</param>
    /// <returns></returns>
    public virtual IEnumerable<String> Search(String pattern, Int32 count) => Search(new SearchModel { Pattern = pattern, Count = count });
    #endregion

    #region 集合运算
    /// <summary>返回第一个集合与其他集合之间的差异</summary>
    /// <remarks>也可以认为说第一个集合中独有的元素</remarks>
    /// <param name="keys"></param>
    /// <returns></returns>
    public T[] Diff(params String[] keys) => Execute((r, k) => r.ExecuteByKey<String, T[]>("SDIFF", Key, keys)) ?? [];

    #endregion
}