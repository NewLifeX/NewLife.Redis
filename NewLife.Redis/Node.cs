using System;
using System.IO;
using NewLife.Collections;
using NewLife.Log;
using NewLife.Net;

namespace NewLife.Caching
{
    /// <summary>服务器节点。内部连接池</summary>
    public class Node
    {
        #region 属性
        /// <summary>拥有者</summary>
        public Redis Owner { get; set; }

        /// <summary>当前节点地址</summary>
        public String Address { get; set; }

        /// <summary>标识</summary>
        public String ID { get; set; }

        /// <summary>标志</summary>
        public String Flags { get; set; }

        /// <summary>链接状态</summary>
        public Int32 LinkState { get; set; }

        /// <summary>最小槽</summary>
        public Int32 MinSlot { get; set; }

        /// <summary>最大槽</summary>
        public Int32 MaxSlot { get; set; }
        #endregion

        #region 构造
        /// <summary>已重载。返回地址</summary>
        /// <returns></returns>
        public override String ToString() => Address;
        #endregion

        #region 方法
        /// <summary>分析结果行</summary>
        /// <param name="line"></param>
        public void Parse(String line)
        {
            // <id> <ip:port> <flags> <master> <ping-sent> <pong-recv> <config-epoch> <link-state> <slot> <slot> ... <slot>
            /*
             * 25cd3fd6d68b49a35e98050c3a7798dc907b905a 127.0.0.1:6002 master - 1548512034793 1548512031738 1 connected
             * a0f1a760f8681c2963490fce90722452701a89c8 127.0.0.1:6003 master - 0 1548512033751 0 connected
             * 84fd41c0ab900ea456419d68e7e28e7312f76b40 127.0.0.1:6004 master - 0 1548512032744 3 connected
             * 7cf3c4e1a1c3a6bb52778bbfcc457ca1d9460de8 127.0.0.1:6001 myself,master - 0 0 2 connected 1-4
             */

            if (line.IsNullOrEmpty()) return;

            var ss = line.Split(" ");
            if (ss.Length < 8) return;

            ID = ss[0];
            Address = ss[1];
            Flags = ss[2];

            LinkState = ss[7] == "connected" ? 1 : 0;

            if (ss.Length >= 9)
            {
                var ts = ss[8].SplitAsInt("-");
                if (ts.Length == 2)
                {
                    MinSlot = ts[0];
                    MaxSlot = ts[1];
                }
            }
        }
        #endregion

        #region 客户端池
        class MyPool : ObjectPool<RedisClient>
        {
            public Node Node { get; set; }

            protected override RedisClient OnCreate()
            {
                var node = Node;
                var rds = node.Owner;
                var addr = node.Address;
                if (addr.IsNullOrEmpty()) throw new ArgumentNullException(nameof(node.Address));

                var uri = new NetUri("tcp://" + addr);
                if (uri.Port == 0) uri.Port = 6379;

                var rc = new RedisClient
                {
                    Server = uri,
                    Password = rds.Password,
                };

                rc.Log = rds.Log;
                if (rds.Db > 0) rc.Select(rds.Db);

                return rc;
            }

            protected override Boolean OnGet(RedisClient value)
            {
                // 借出时清空残留
                value?.Reset();

                return base.OnGet(value);
            }
        }

        private MyPool _Pool;
        /// <summary>连接池</summary>
        public IPool<RedisClient> Pool
        {
            get
            {
                if (_Pool != null) return _Pool;
                lock (this)
                {
                    if (_Pool != null) return _Pool;

                    var pool = new MyPool
                    {
                        Name = Owner.Name + "Pool",
                        Node = this,
                        Min = 2,
                        Max = 1000,
                        IdleTime = 20,
                        AllIdleTime = 120,
                        Log = Owner.Log,
                    };

                    return _Pool = pool;
                }
            }
        }

        /// <summary>执行命令</summary>
        /// <typeparam name="TResult">返回类型</typeparam>
        /// <param name="func">回调函数</param>
        /// <param name="write">是否写入操作</param>
        /// <returns></returns>
        public virtual TResult Execute<TResult>(Func<RedisClient, TResult> func, Boolean write = false)
        {
            // 统计性能
            var sw = Owner.Counter?.StartCount();

            var i = 0;
            do
            {
                // 每次重试都需要重新从池里借出连接
                var client = Pool.Get();
                try
                {
                    client.Reset();
                    var rs = func(client);

                    Owner.Counter?.StopCount(sw);

                    return rs;
                }
                catch (InvalidDataException)
                {
                    if (i++ >= Owner.Retry) throw;
                }
                finally
                {
                    Pool.Put(client);
                }
            } while (true);
        }
        #endregion
    }
}