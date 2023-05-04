using NewLife.Caching.Queues;

namespace NewLife.Caching;

/// <summary>支持键前缀的Redis</summary>
public class PrefixedRedis : FullRedis
{
    #region 属性
    /// <summary>键前缀</summary>
    public String Prefix { get; set; }
    #endregion

    #region 构造
    /// <summary>
    /// 实例化支持键前缀的Redis
    /// </summary>
    /// <param name="prefix"></param>
    /// <param name="server"></param>
    /// <param name="password"></param>
    /// <param name="db"></param>
    public PrefixedRedis(String server, String password, Int32 db, String prefix) :base(server, password, db) => Prefix = prefix ?? string.Empty;
    /// <summary>
    /// 实例化支持键前缀的Redis
    /// </summary>
    /// <param name="options"></param>
    public PrefixedRedis(RedisOptions options):base(options) => Prefix = options.Prefix ?? string.Empty;
    #endregion

    /// <summary>重载执行，支持集群</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="func"></param>
    /// <param name="write">是否写入操作</param>
    /// <returns></returns>
    public override T Execute<T>(String key, Func<RedisClient, T> func, Boolean write = false) => base.Execute(Prefix + key, func, write);

    /// <summary>重载执行，支持集群</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="func"></param>
    /// <param name="write">是否写入操作</param>
    /// <returns></returns>
    public override Task<T> ExecuteAsync<T>(String key, Func<RedisClient, Task<T>> func, Boolean write = false) => base.ExecuteAsync(Prefix + key, func, write);

    #region 集合操作
    /// <inheritdoc/>
    public override IDictionary<String, T> GetDictionary<T>(String key) => base.GetDictionary<T>(Prefix + key);
    /// <inheritdoc/>
    public override IList<T> GetList<T>(String key) => base.GetList<T>(Prefix + key);
    /// <inheritdoc/>
    public override IProducerConsumer<T> GetQueue<T>(String topic) => base.GetQueue<T>(Prefix + topic);
    /// <summary>获取可靠队列，消息需要确认</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="topic">消息队列主题</param>
    /// <returns></returns>
    public new RedisReliableQueue<T> GetReliableQueue<T>(String topic) => base.GetReliableQueue<T>(Prefix + topic);
    /// <summary>获取延迟队列</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="topic">消息队列主题</param>
    /// <returns></returns>
    public new RedisDelayQueue<T> GetDelayQueue<T>(String topic) => base.GetDelayQueue<T>(Prefix + topic);
    /// <inheritdoc/>
    public override ICollection<T> GetSet<T>(String key) => base.GetSet<T>(Prefix + key);
    /// <inheritdoc/>
    public override IProducerConsumer<T> GetStack<T>(String key) => base.GetStack<T>(Prefix + key);
    /// <summary>获取消息流</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="topic">消息队列主题</param>
    /// <returns></returns>
    public new RedisStream<T> GetStream<T>(String topic) => base.GetStream<T>(Prefix + topic);
    /// <summary>获取有序集合</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    public new RedisSortedSet<T> GetSortedSet<T>(String key) => base.GetSortedSet<T>(Prefix + key);
    #endregion
}
