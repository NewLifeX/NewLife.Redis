using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NewLife.Caching
{
    /// <summary>列表结构</summary>
    /// <typeparam name="T"></typeparam>
    public class RedisList<T> : RedisBase, IList<T>
    {
        #region 属性
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        /// <param name="redis"></param>
        /// <param name="key"></param>
        public RedisList(Redis redis, String key) : base(redis, key) { }
        #endregion

        #region 列表方法
        /// <summary>获取 或 设置 指定位置的值</summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T this[Int32 index]
        {
            get => Execute(r => r.Execute<T>("LINDEX", Key, index));
            set => Execute(r => r.Execute<String>("LSET", Key, index, value), true);
        }

        /// <summary>个数</summary>
        public Int32 Count => Execute(r => r.Execute<Int32>("LLEN", Key));

        Boolean ICollection<T>.IsReadOnly => false;

        /// <summary>添加元素在后面</summary>
        /// <param name="item"></param>
        public void Add(T item) => Execute(r => r.Execute<Int32>("RPUSH", Key, item), true);

        /// <summary>批量添加</summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public Int32 AddRange(IEnumerable<T> values) => RPUSH(values);

        /// <summary>清空列表-start>end 清空</summary>
        public void Clear() => LTrim(-1, 0);

        /// <summary>是否包含指定元素</summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Boolean Contains(T item)
        {
            var count = Count;
            if (count > 1000) throw new NotSupportedException($"[{Key}]的元素个数过多，不支持！");

            var list = GetAll();
            return list.Contains(item);
        }

        /// <summary>复制到目标数组</summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(T[] array, Int32 arrayIndex)
        {
            var count = array.Length - arrayIndex;

            var arr = LRange(0, count - 1);
            arr.CopyTo(array, arrayIndex);
        }

        /// <summary>查找指定元素位置</summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Int32 IndexOf(T item)
        {
            var count = Count;
            if (count > 1000) throw new NotSupportedException($"[{Key}]的元素个数过多，不支持！");

            var arr = GetAll();
            return Array.IndexOf(arr, item);
        }

        /// <summary>在指定位置插入</summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        public void Insert(Int32 index, T item)
        {
            var pivot = this[index];
            LInsertBefore(pivot, item);
        }

        /// <summary>删除指定元素</summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Boolean Remove(T item) => LRem(1, item) > 0;

        /// <summary>删除指定位置数据</summary>
        /// <param name="index"></param>
        public void RemoveAt(Int32 index) => Remove(this[index]);

        /// <summary>遍历</summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            // 前面一段一次性取回来
            var size = 100;
            var arr = LRange(0, size - 1);
            foreach (var item in arr)
            {
                yield return item;
            }
            if (arr.Length < size) yield break;

            // 后续逐个遍历
            var count = Count;
            for (var i = size; i < count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion

        #region 高级操作
        /// <summary>右边批量添加，返回队列元素总数</summary>
        /// <param name="values"></param>
        /// <returns>队列元素总数</returns>
        public Int32 RPUSH(IEnumerable<T> values)
        {
            var args = new List<Object>
            {
                Key
            };
            foreach (var item in values)
            {
                args.Add(item);
            }
            return Execute(rc => rc.Execute<Int32>("RPUSH", args.ToArray()), true);
        }

        /// <summary>左边批量添加，返回队列元素总数</summary>
        /// <param name="values"></param>
        /// <returns>队列元素总数</returns>
        public Int32 LPUSH(IEnumerable<T> values)
        {
            var args = new List<Object>
            {
                Key
            };
            foreach (var item in values)
            {
                args.Add(item);
            }
            return Execute(rc => rc.Execute<Int32>("LPUSH", args.ToArray()), true);
        }

        /// <summary>移除并返回最右边一个元素</summary>
        /// <returns></returns>
        public T RPOP() => Execute(rc => rc.Execute<T>("RPOP", Key), true);

        /// <summary>移除并返回最左边一个元素</summary>
        /// <returns></returns>
        public T LPOP() => Execute(rc => rc.Execute<T>("LPOP", Key), true);

        /// <summary>移除并返回最右边一个元素，并插入目标列表左边，原子操作</summary>
        /// <remarks>
        /// 用于高可靠性消费
        /// </remarks>
        /// <param name="destKey">目标列表</param>
        /// <returns></returns>
        public T RPOPLPUSH(String destKey) => Execute(rc => rc.Execute<T>("RPOPLPUSH", Key, destKey), true);

        /// <summary>在指定元素之前插入</summary>
        /// <param name="pivot"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Int32 LInsertBefore(T pivot, T value) => Execute(r => r.Execute<Int32>($"LINSERT", Key, "BEFORE", pivot, value), true);

        /// <summary>返回指定范围的列表</summary>
        /// <param name="pivot"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Int32 LInsertAfter(T pivot, T value) => Execute(r => r.Execute<Int32>($"LINSERT", Key, "AFTER", pivot, value), true);

        /// <summary>返回指定范围的列表</summary>
        /// <param name="start"></param>
        /// <param name="stop"></param>
        /// <returns></returns>
        public T[] LRange(Int32 start, Int32 stop) => Execute(r => r.Execute<T[]>("LRANGE", Key, start, stop));

        /// <summary>获取所有元素</summary>
        /// <returns></returns>
        public T[] GetAll() => LRange(0, -1);

        /// <summary>修剪一个已存在的列表</summary>
        /// <param name="start"></param>
        /// <param name="stop"></param>
        /// <returns></returns>
        public Boolean LTrim(Int32 start, Int32 stop) => Execute(r => r.Execute<Boolean>("LTRIM", Key, start, stop), true);

        /// <summary>从存于 key 的列表里移除前 count 次出现的值为 value 的元素</summary>
        /// <param name="count"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Int32 LRem(Int32 count, T value) => Execute(r => r.Execute<Int32>("LREM", Key, count, value), true);
        #endregion
    }
}