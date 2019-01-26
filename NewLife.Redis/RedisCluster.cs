using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Log;

namespace NewLife.Caching
{
    /// <summary>Redis集群</summary>
    public class RedisCluster : RedisBase
    {
        #region 属性
        /// <summary>集群节点</summary>
        public Node[] Nodes { get; private set; }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        /// <param name="redis"></param>
        public RedisCluster(Redis redis) : base(redis, null) => GetNodes();
        #endregion

        #region 方法
        private void GetNodes()
        {
            var rs = Execute(r => r.Execute<String>("Cluster", "Nodes"));
            if (rs.IsNullOrEmpty()) return;

            var list = new List<Node>();
            foreach (var item in rs.Split("\r", "\n"))
            {
                if (!item.IsNullOrEmpty())
                {
                    var node = new Node
                    {
                        Owner = Redis
                    };

                    node.Parse(item);
                    list.Add(node);

                    XTrace.WriteLine("[{0}]节点：{1}", Redis.Name, node);
                }
            }
            Nodes = list.ToArray();
        }

        /// <summary>根据Key选择节点</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual Node SelectNode(String key)
        {
            if (key.IsNullOrEmpty()) return null;

            var slot = key.GetBytes().Crc16() / 16384;
            var ns = Nodes.Where(e => e.LinkState == 1).ToList();
            // 找主节点
            foreach (var node in ns)
            {
                if (!node.Slave && node.Contain(slot)) return node;
            }
            // 找从节点
            foreach (var node in ns)
            {
                if (node.Contain(slot)) return node;
            }

            return null;
        }

        /// <summary>重新负载均衡</summary>
        public virtual Boolean Rebalance()
        {
            // 全部有效节点
            var ns = Nodes.Where(e => e.LinkState == 1 && !e.Slave).ToList();
            if (ns.Count == 0) return false;

            // 地址排序，然后分配
            ns = ns.OrderBy(e => e.Address).ToList();

            // 平均分
            var size = 16384 / ns.Count;
            var y = 16384 % ns.Count;
            var start = 0;
            var k = 0;
            foreach (var item in ns)
            {
                item.Slots.Clear();

                var to = start + size;
                // 前面y个可以多分一个
                if (k++ < y) to++;
                item.Slots.Add(new Slot
                {
                    From = start,
                    To = to - 1,
                });

                start = to;
            }

            return true;
        }
        #endregion
    }
}