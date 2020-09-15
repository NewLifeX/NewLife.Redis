using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NewLife.Caching.Models;
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
            if (Redis == null) return;

            var rs = Execute(r => r.Execute<String>("Cluster", "Nodes"));
            if (rs.IsNullOrEmpty()) return;

            ParseNodes(rs);
        }

        /// <summary>分析节点</summary>
        /// <param name="nodes"></param>
        public void ParseNodes(String nodes)
        {
            var list = new List<Node>();
            foreach (var item in nodes.Split("\r", "\n"))
            {
                if (!item.IsNullOrEmpty())
                {
                    var node = new Node
                    {
                        Owner = Redis
                    };

                    XTrace.WriteLine("{0}", item);

                    node.Parse(item);
                    list.Add(node);

                    //XTrace.WriteLine("[{0}]节点：{1}", Redis.Name, node);
                }
            }
            //list = list.OrderBy(e => e.EndPoint).ToList();
            list = SortNodes(list);

            foreach (var node in list)
            {
                var name = Redis?.Name + "";
                if (!name.IsNullOrEmpty()) name = $"[{name}]";
                XTrace.WriteLine("{0}节点：{1} {2} {3}", name, node, node.Flags, node.Slots.Join(" "));

                if (node.Slaves != null)
                {
                    name += "节点：";
                    name = new String(' ', name.GetBytes(Encoding.BigEndianUnicode).Length);
                    foreach (var item in node.Slaves)
                    {
                        XTrace.WriteLine("{0}{1} {2}", name, item, item.Flags);
                    }
                }
            }
            Nodes = list.ToArray();
        }

        private List<Node> SortNodes(List<Node> list)
        {
            // 主节点按照数据槽排序
            var masters = list.Where(e => e.Master == "-").OrderBy(e => e.Slots.Min(x => x.From)).ToList();
            var slaves = list.Where(e => e.Master != "-").ToList();

            // 从节点插入主节点
            foreach (var node in masters)
            {
                var ns = slaves.Where(e => e.Master == node.ID).OrderBy(e => e.EndPoint).ToList();
                if (ns.Count > 0) node.Slaves = ns;
            }

            return masters;
        }

        /// <summary>根据Key选择节点</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual Node SelectNode(String key)
        {
            if (key.IsNullOrEmpty()) return null;

            var slot = key.GetBytes().Crc16() % 16384;
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

        /// <summary>把Key映射到指定地址的节点</summary>
        /// <param name="endpoint"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual Node Map(String endpoint, String key)
        {
            var node = Nodes.FirstOrDefault(e => e.EndPoint == endpoint);
            if (node == null) return null;

            if (!key.IsNullOrEmpty())
            {
                var slot = key.GetBytes().Crc16() % 16384;
                AddSlots(node, slot);
            }

            return node;
        }

        /// <summary>向集群添加新节点</summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        public virtual void Meet(String ip, Int32 port)
        {
            Execute(r => r.Execute("CLUSTER", "MEET", ip, port));
        }

        /// <summary>向节点增加槽</summary>
        /// <param name="node"></param>
        /// <param name="slots"></param>
        /// <returns></returns>
        public virtual void AddSlots(Node node, params Int32[] slots)
        {
            var pool = node.Pool;
            var client = pool.Get();
            try
            {
                var args = new List<Object>(slots.Length + 1) { "ADDSLOTS" };
                args.AddRange(slots.Cast<Object>());

                client.Execute("CLUSTER", args.ToArray());
            }
            catch (Exception ex)
            {
                Redis.Log.Error(ex.Message);
            }
            finally
            {
                pool.Put(client);
            }
        }

        /// <summary>从节点删除槽</summary>
        /// <param name="node"></param>
        /// <param name="slots"></param>
        /// <returns></returns>
        public virtual void DeleteSlots(Node node, params Int32[] slots)
        {
            var pool = node.Pool;
            var client = pool.Get();
            try
            {
                var args = new List<Object>(slots.Length + 1) { "DELSLOTS" };
                args.AddRange(slots.Cast<Object>());

                client.Execute("CLUSTER", args.ToArray());

                //foreach (var item in slots)
                //{
                //    try
                //    {
                //        client.Execute("CLUSTER", "DELSLOTS", item);
                //    }
                //    catch (Exception ex)
                //    {
                //        Redis.Log.Error(ex.Message);
                //    }
                //}
            }
            catch (Exception ex)
            {
                Redis.Log.Error(ex.Message);
            }
            finally
            {
                pool.Put(client);
            }
        }

        /// <summary>重新负载均衡</summary>
        /// <remarks>
        /// 节点迁移太负责，直接干掉原来的分配，重新全局分配
        /// </remarks>
        public virtual Boolean Rebalance()
        {
            GetNodes();

            var ns = Nodes?.ToList();
            if (ns == null || ns.Count == 0) return false;

            // 全部有效节点
            ns = ns.Where(e => e.LinkState == 1 && !e.Slave).ToList();
            if (ns.Count == 0) return false;

            //!!! 节点迁移太复杂，直接干掉原来的分配，重新全局分配
            foreach (var item in ns)
            {
                var sts = item.GetSlots();
                if (sts == null || sts.Length == 0) continue;

                DeleteSlots(item, sts);
            }

            // 地址排序，然后分配
            ns = ns.OrderBy(e => e.EndPoint).ToList();

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

                // 执行命令
                AddSlots(item, Enumerable.Range(start, to - start).ToArray());

                start = to;
            }

            return true;
        }
        #endregion
    }
}