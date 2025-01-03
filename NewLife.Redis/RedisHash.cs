﻿using System.Collections;
using System.Diagnostics.CodeAnalysis;
using NewLife.Caching.Models;
using NewLife.Data;
using NewLife.Reflection;

namespace NewLife.Caching;

/// <summary>哈希结构</summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
public class RedisHash<TKey, TValue> : RedisBase, IDictionary<TKey, TValue>
{
    #region 构造
    /// <summary>实例化</summary>
    /// <param name="redis"></param>
    /// <param name="key"></param>
    public RedisHash(Redis redis, String key) : base(redis, key) { }
    #endregion

    #region 字典接口
    /// <summary>个数</summary>
    public Int32 Count => Execute((r, k) => r.Execute<Int32>("HLEN", Key));

    Boolean ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

    /// <summary>获取所有键</summary>
    public ICollection<TKey> Keys => Execute((r, k) => r.Execute<TKey[]>("HKEYS", Key)) ?? new TKey[0];

    /// <summary>获取所有值</summary>
    public ICollection<TValue> Values => Execute((r, k) => r.Execute<TValue[]>("HVALS", Key)) ?? new TValue[0];

    /// <summary>获取 或 设置 指定键的值</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public TValue? this[TKey key]
    {
        get => Execute((r, k) => r.Execute<TValue>("HGET", Key, key));
        set => Execute((r, k) => r.Execute<Int32>("HSET", Key, key, value), true);
    }

    /// <summary>是否包含指定键</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public Boolean ContainsKey(TKey key) => Execute((r, k) => r.Execute<Int32>("HEXISTS", Key, key)) > 0;

    /// <summary>添加</summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void Add(TKey key, TValue value) => Execute((r, k) => r.Execute<Int32>("HSET", Key, key, value), true);

    /// <summary>删除</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public Boolean Remove(TKey key) => HDel(key) > 0;

    /// <summary>尝试获取</summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public Boolean TryGetValue(TKey key, out TValue value)
    {
        value = default!;

        var pk = Execute((r, k) => r.Execute<IPacket>("HGET", Key, key!));
        if (pk == null || pk.Length == 0) return false;

        value = Redis.Encoder.Decode<TValue>(pk)!;
        //value = (TValue?)Redis.Encoder.Decode(pk, typeof(TValue))!;

        return true;
    }

    /// <summary>清空</summary>
    public void Clear() => Redis.Remove(Key);

    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    Boolean ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);

    void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, Int32 arrayIndex) => throw new NotSupportedException();

    Boolean ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);

    /// <summary>迭代</summary>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        foreach (var item in Search("*", 1000000))
        {
            yield return item;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    #endregion

    #region 高级操作
    /// <summary>批量删除</summary>
    /// <param name="fields"></param>
    /// <returns></returns>
    public Int32 HDel(params TKey[] fields)
    {
        var args = new List<Object?>
        {
            Key
        };
        foreach (var item in fields)
        {
            args.Add(item);
        }

        return Execute((r, k) => r.Execute<Int32>("HDEL", args.ToArray()), true);
    }

    /// <summary>只在 key 指定的哈希集中不存在指定的字段时，设置字段的值</summary>
    /// <param name="fields"></param>
    /// <returns></returns>
    public TValue[]? HMGet(params TKey[] fields)
    {
        var args = new List<Object?>
        {
            Key
        };
        foreach (var item in fields)
        {
            args.Add(item);
        }

        return Execute((r, k) => r.Execute<TValue[]>("HMGET", args.ToArray()));
    }

    /// <summary>批量插入</summary>
    /// <param name="keyValues"></param>
    /// <returns></returns>
    public Boolean HMSet(IEnumerable<KeyValuePair<TKey, TValue>> keyValues)
    {
        var args = new List<Object?>
        {
            Key
        };
        foreach (var item in keyValues)
        {
            args.Add(item.Key);
            args.Add(item.Value);
        }

        return Execute((r, k) => r.Execute<String>("HMSET", args.ToArray()) == "OK", true);
    }

    /// <summary>获取所有名值对</summary>
    /// <returns></returns>
    public IDictionary<TKey, TValue> GetAll()
    {
        var dic = new Dictionary<TKey, TValue>();
        var rs = Execute((r, k) => r.Execute<IPacket[]>("HGETALL", Key));
        if (rs == null || rs.Length == 0) return dic;

        for (var i = 0; i < rs.Length; i++)
        {
            var pk = rs[i];
            var pk2 = rs[++i];
            var key = Redis.Encoder.Decode<TKey>(pk);
            var value = Redis.Encoder.Decode<TValue>(pk2);
            dic[key] = value;
        }

        return dic;
    }

    /// <summary>增加指定字段值</summary>
    /// <param name="field"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public Int64 HIncrBy(TKey field, Int64 value) => Execute((r, k) => r.Execute<Int64>("HINCRBY", Key, field, value), true);

    /// <summary>增加指定字段值</summary>
    /// <param name="field"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public Double HIncrBy(TKey field, Double value) => Execute((r, k) => r.Execute<Double>("HINCRBY", Key, field, value), true);

    /// <summary>只在 key 指定的哈希集中不存在指定的字段时，设置字段的值</summary>
    /// <param name="field"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public Int32 HSetNX(TKey field, TValue value) => Execute((r, k) => r.Execute<Int32>("HSETNX", Key, field, value), true);

    /// <summary>返回hash指定field的value的字符串长度</summary>
    /// <param name="field"></param>
    /// <returns></returns>
    public Int32 HStrLen(TKey field) => Execute((r, k) => r.Execute<Int32>("HSTRLEN", Key, field));

    /// <summary>模糊搜索，支持?和*</summary>
    /// <param name="model">搜索模型</param>
    /// <returns></returns>
    public virtual IEnumerable<KeyValuePair<TKey, TValue>> Search(SearchModel model)
    {
        var count = model.Count;
        while (count > 0)
        {
            var p = model.Position;
            var rs = Execute((r, k) => r.Execute<Object[]>("HSCAN", Key, p, "MATCH", model.Pattern + "", "COUNT", count));
            if (rs == null || rs.Length != 2) break;

            model.Position = (rs[0] as IPacket)!.ToStr().ToInt();

            if (rs[1] is not Object[] ps) break;

            for (var i = 0; i < ps.Length - 1; i += 2)
            {
                if (count-- > 0)
                {
                    var key = (ps[i] as IPacket)!.ToStr().ChangeType<TKey>();
                    var val = (ps[i + 1] as IPacket)!.ToStr().ChangeType<TValue>();
                    if (key != null) yield return new KeyValuePair<TKey, TValue>(key, val!);
                }
            }

            if (model.Position == 0) break;
        }
    }

    /// <summary>模糊搜索，支持?和*</summary>
    /// <param name="pattern">匹配表达式</param>
    /// <param name="count">返回个数</param>
    /// <returns></returns>
    public virtual IEnumerable<KeyValuePair<TKey, TValue>> Search(String pattern, Int32 count) => Search(new SearchModel { Pattern = pattern, Count = count });
    #endregion
}
