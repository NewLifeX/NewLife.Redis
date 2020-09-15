using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NewLife.Collections;
using NewLife.Log;
using NewLife.Net;

namespace NewLife.Caching.Models
{
    /// <summary>服务器节点。内部连接池</summary>
    public class Node
    {
        #region 属性
        /// <summary>拥有者</summary>
        public Redis Owner { get; set; }

        /// <summary>当前节点地址</summary>
        public String EndPoint { get; set; }

        /// <summary>标识</summary>
        public String ID { get; set; }

        /// <summary>标志</summary>
        public String Flags { get; set; }

        /// <summary>主机。当前节点对应的主机</summary>
        public String Master { get; set; }

        /// <summary>链接状态</summary>
        public Int32 LinkState { get; set; }

        /// <summary>是否从节点</summary>
        public Boolean Slave { get; set; }

        /// <summary>当前节点的从节点集合</summary>
        public IList<Node> Slaves { get; set; }

        /// <summary>本节点数据槽</summary>
        public IList<Slot> Slots { get; private set; } = new List<Slot>();

        /// <summary>正在转入</summary>
        public IDictionary<Int32, String> Importings { get; private set; }

        /// <summary>正在转出</summary>
        public IDictionary<Int32, String> Migratings { get; private set; }
        #endregion

        #region 构造
        /// <summary>已重载。返回地址</summary>
        /// <returns></returns>
        public override String ToString() => EndPoint;
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
             * 7cf3c4e1a1c3a6bb52778bbfcc457ca1d9460de8 127.0.0.1:6001 myself,master - 0 0 2 connected 1-4 103-105 107 109
             */

            if (line.IsNullOrEmpty()) return;

            var ss = line.Split(" ");
            if (ss.Length < 8) return;

            ID = ss[0];
            EndPoint = ss[1];
            Flags = ss[2];
            Master = ss[3];

            // Redis的集群信息中出现 172.16.10.32:6379@16379
            var p = EndPoint.IndexOf("@");
            if (p > 0) EndPoint = EndPoint.Substring(0, p);

            var fs = ss[2].Split(",");
            Slave = fs.Contains("slave");

            LinkState = fs.Contains("fail?") ? 0 : 1;

            if (ss.Length >= 9)
            {
                for (var i = 8; i < ss.Length; i++)
                {
                    var str = ss[i];
                    if (str[0] == '[' && str[str.Length - 1] == ']')
                    {
                        ParseImportingAndMigrating(str);
                    }
                    else
                    {
                        var ts = str.SplitAsInt("-");
                        var end = ts.Length == 2 ? 1 : 0;

                        if (ts.Length > 0) Slots.Add(new Slot
                        {
                            From = ts[0],
                            To = ts[end],
                        });
                    }
                }
            }
        }

        private void ParseImportingAndMigrating(String str)
        {
            str = str.Trim('[', ']');

            var p = str.IndexOf("-<-");
            if (p > 0)
            {
                var dic = Importings ?? new Dictionary<Int32, String>();
                var slot = str.Substring(0, p).ToInt();
                var nodeid = str.Substring(p + 3);
                dic[slot] = nodeid;

                Importings = dic;
            }
            else if (str.Contains("->-"))
            {
                p = str.IndexOf("->-");

                var dic = Migratings ?? new Dictionary<Int32, String>();
                var slot = str.Substring(0, p).ToInt();
                var nodeid = str.Substring(p + 3);
                dic[slot] = nodeid;

                Migratings = dic;
            }
        }

        /// <summary>是否包含数据槽</summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public Boolean Contain(Int32 slot)
        {
            foreach (var item in Slots)
            {
                if (slot >= item.From && slot <= item.To) return true;
            }
            return false;
        }

        /// <summary>返回所有槽</summary>
        /// <returns></returns>
        public Int32[] GetSlots()
        {
            var list = new List<Int32>();
            foreach (var item in Slots)
            {
                for (var i = item.From; i <= item.To; i++)
                {
                    list.Add(i);
                }
            }

            return list.Distinct().OrderBy(e => e).ToArray();
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
                var addr = node.EndPoint;
                if (addr.IsNullOrEmpty()) throw new ArgumentNullException(nameof(node.EndPoint));

                var uri = new NetUri("tcp://" + addr);
                if (uri.Port == 0) uri.Port = 6379;

                var rc = new RedisClient(rds, uri)
                {
                    Log = rds.Log
                };
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