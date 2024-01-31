using NewLife.Collections;
using NewLife.Net;

namespace NewLife.Caching.Clusters;

/// <summary>集群中的节点</summary>
public class RedisNode : IRedisNode
{
    #region 属性
    /// <summary>拥有者</summary>
    public Redis Owner { get; set; } = null!;

    /// <summary>节点地址</summary>
    public String EndPoint { get; set; } = null!;

    /// <summary>是否从节点</summary>
    public Boolean Slave { get; set; }

    /// <summary>连续错误次数。达到阈值后屏蔽该节点</summary>
    public Int32 Error { get; set; }

    /// <summary>下一次时间。节点出错时，将禁用一段时间</summary>
    public DateTime NextTime { get; set; }
    #endregion

    #region 构造
    /// <summary>已重载。友好显示节点地址</summary>
    /// <returns></returns>
    public override String ToString() => EndPoint ?? base.ToString();
    #endregion

    #region 客户端池
    class MyPool : ObjectPool<RedisClient>
    {
        public RedisNode Node { get; set; } = null!;

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
                Name = $"{uri.Address}-{uri.Port}",
                Log = rds.ClientLog
            };
            //if (rds.Db > 0 && (rds is not FullRedis rds2 || !rds2.Mode.EqualIgnoreCase("cluster", "sentinel"))) rc.Select(rds.Db);

            return rc;
        }

        protected override Boolean OnGet(RedisClient value)
        {
            // 借出时清空残留
            value.Reset();

            return base.OnGet(value);
        }
    }
    #endregion
}
