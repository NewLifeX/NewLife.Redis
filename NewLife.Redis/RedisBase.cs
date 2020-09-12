using System;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Caching
{
    /// <summary>基础结构</summary>
    public abstract class RedisBase
    {
        #region 属性
        /// <summary>客户端对象</summary>
        public Redis Redis { get; }

        /// <summary>键</summary>
        public String Key { get; }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        /// <param name="redis"></param>
        /// <param name="key"></param>
        public RedisBase(Redis redis, String key) { Redis = redis; Key = key; }
        #endregion

        #region 方法
        /// <summary>执行命令</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <param name="write">是否写入操作</param>
        /// <returns></returns>
        public virtual T Execute<T>(Func<RedisClient, T> func, Boolean write = false) => Redis.Execute(Key, func, write);

        /// <summary>异步执行命令</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <param name="write">是否写入操作</param>
        /// <returns></returns>
        public virtual async Task<T> ExecuteAsync<T>(Func<RedisClient, Task<T>> func, Boolean write = false) => await Redis.ExecuteAsync(Key, func, write);
        #endregion

        #region 辅助
        /// <summary>数值转字节数组</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected virtual Packet ToBytes(Object value)
        {
            if (value == null) return new Byte[0];

            if (value is Packet pk) return pk;
            if (value is Byte[] buf) return buf;
            if (value is IAccessor acc) return acc.ToPacket();

            var type = value.GetType();
            switch (type.GetTypeCode())
            {
                case TypeCode.Object: return value.ToJson().GetBytes();
                case TypeCode.String: return (value as String).GetBytes();
                case TypeCode.DateTime: return ((DateTime)value).ToFullString().GetBytes();
                default: return value.ToString().GetBytes();
            }
        }

        /// <summary>字节数组转对象</summary>
        /// <param name="pk"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        protected virtual Object FromBytes(Packet pk, Type type)
        {
            if (type == typeof(Packet)) return pk;
            if (type == typeof(Byte[])) return pk.ToArray();
            if (type.As<IAccessor>()) return type.AccessorRead(pk);

            var str = pk.ToStr().Trim('\"');
            if (type.GetTypeCode() == TypeCode.String) return str;
            if (type.GetTypeCode() != TypeCode.Object) return str.ChangeType(type);

            return str.ToJsonEntity(type);
        }

        /// <summary>字节数组转对象</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pk"></param>
        /// <returns></returns>
        protected T FromBytes<T>(Packet pk) => (T)FromBytes(pk, typeof(T));
        #endregion
    }
}