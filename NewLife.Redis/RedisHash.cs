using System;
using System.Collections;
using System.Collections.Generic;

namespace NewLife.Caching
{
    /// <summary>字典缓存</summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    class RedisHash<TKey, TValue> : IDictionary<TKey, TValue>
    {
        public FullRedis Redis { get; }

        public String Key { get; }

        public RedisHash(FullRedis redis, String key) { Redis = redis; Key = key; }

        public Int32 Count
        {
            get
            {
                using (var client = Redis.GetClient())
                {
                    return (Int32)client.GetHashCount(Key);
                }
            }
        }

        public Boolean IsReadOnly => false;

        public ICollection<TKey> Keys
        {
            get
            {
                using (var client = Redis.GetClient())
                {
                    var hash = client.As<TValue>().GetHash<TKey>(Key);
                    return hash.Keys;
                }
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                using (var client = Redis.GetClient())
                {
                    var hash = client.As<TValue>().GetHash<TKey>(Key);
                    return hash.Values;
                }
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                using (var client = Redis.GetClient())
                {
                    var hash = client.As<TValue>().GetHash<TKey>(Key);
                    return hash[key];
                }
            }
            set
            {
                using (var client = Redis.GetClient())
                {
                    var hash = client.As<TValue>().GetHash<TKey>(Key);
                    hash[key] = value;
                }
            }
        }

        public Boolean ContainsKey(TKey key)
        {
            using (var client = Redis.GetClient())
            {
                var hash = client.As<TValue>().GetHash<TKey>(Key);
                return hash.ContainsKey(key);
            }
        }

        public void Add(TKey key, TValue value)
        {
            using (var client = Redis.GetClient())
            {
                var hash = client.As<TValue>().GetHash<TKey>(Key);
                hash.Add(key, value);
            }
        }

        public Boolean Remove(TKey key)
        {
            using (var client = Redis.GetClient())
            {
                var hash = client.As<TValue>().GetHash<TKey>(Key);
                return hash.Remove(key);
            }
        }

        public Boolean TryGetValue(TKey key, out TValue value)
        {
            using (var client = Redis.GetClient())
            {
                var hash = client.As<TValue>().GetHash<TKey>(Key);
                return hash.TryGetValue(key, out value);
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            using (var client = Redis.GetClient())
            {
                var hash = client.As<TValue>().GetHash<TKey>(Key);
                hash.Add(item);
            }
        }

        public void Clear()
        {
            using (var client = Redis.GetClient())
            {
                var hash = client.As<TValue>().GetHash<TKey>(Key);
                hash.Clear();
            }
        }

        public Boolean Contains(KeyValuePair<TKey, TValue> item)
        {
            using (var client = Redis.GetClient())
            {
                var hash = client.As<TValue>().GetHash<TKey>(Key);
                return hash.Contains(item);
            }
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, Int32 arrayIndex)
        {
            using (var client = Redis.GetClient())
            {
                var hash = client.As<TValue>().GetHash<TKey>(Key);
                hash.CopyTo(array, arrayIndex);
            }
        }

        public Boolean Remove(KeyValuePair<TKey, TValue> item)
        {
            using (var client = Redis.GetClient())
            {
                var hash = client.As<TValue>().GetHash<TKey>(Key);
                return hash.Remove(item);
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            using (var client = Redis.GetClient())
            {
                var hash = client.As<TValue>().GetHash<TKey>(Key);
                return hash.GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}