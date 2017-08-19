using System;
using System.Collections;
using System.Collections.Generic;

namespace NewLife.Redis
{
    /// <summary>列表缓存</summary>
    /// <typeparam name="T"></typeparam>
    class RedisList<T> : IList<T>
    {
        public Redis Redis { get; }

        public String Key { get; }

        public RedisList(Redis redis, String key) { Redis = redis; Key = key; }

        public T this[Int32 index]
        {
            get
            {
                using (var client = Redis.GetClient())
                {
                    var list = client.As<T>().Lists[Key];
                    return list[index];
                }
            }
            set
            {
                using (var client = Redis.GetClient())
                {
                    var list = client.As<T>().Lists[Key];
                    list[index] = value;
                }
            }
        }

        public Int32 Count
        {
            get
            {
                using (var client = Redis.GetClient())
                {
                    return (Int32)client.GetListCount(Key);
                }
            }
        }

        public Boolean IsReadOnly => false;

        public void Add(T item)
        {
            using (var client = Redis.GetClient())
            {
                var list = client.As<T>().Lists[Key];
                list.Add(item);
            }
        }

        public void Clear()
        {
            using (var client = Redis.GetClient())
            {
                var list = client.As<T>().Lists[Key];
                list.RemoveAll();
            }
        }

        public Boolean Contains(T item)
        {
            using (var client = Redis.GetClient())
            {
                var list = client.As<T>().Lists[Key];
                return list.Contains(item);
            }
        }

        public void CopyTo(T[] array, Int32 arrayIndex)
        {
            using (var client = Redis.GetClient())
            {
                var list = client.As<T>().Lists[Key];
                list.CopyTo(array, arrayIndex);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            using (var client = Redis.GetClient())
            {
                var list = client.As<T>().Lists[Key];
                return list.GetEnumerator();
            }
        }

        public Int32 IndexOf(T item)
        {
            using (var client = Redis.GetClient())
            {
                var list = client.As<T>().Lists[Key];
                return list.IndexOf(item);
            }
        }

        public void Insert(Int32 index, T item)
        {
            using (var client = Redis.GetClient())
            {
                var list = client.As<T>().Lists[Key];
                list.Insert(index, item);
            }
        }

        public Boolean Remove(T item)
        {
            using (var client = Redis.GetClient())
            {
                var list = client.As<T>().Lists[Key];
                return list.Remove(item);
            }
        }

        public void RemoveAt(Int32 index)
        {
            using (var client = Redis.GetClient())
            {
                var list = client.As<T>().Lists[Key];
                list.RemoveAt(index);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}