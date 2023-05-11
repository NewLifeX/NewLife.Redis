using System.Linq;

using NewLife.Caching.Models;
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
    public PrefixedRedis() { }
    /// <summary>
    /// 实例化支持键前缀的Redis, Remove，GetAll\SetAll Rename  Search 支持前缀的有问题
    /// </summary>
    /// <param name="prefix"></param>
    /// <param name="server"></param>
    /// <param name="password"></param>
    /// <param name="db"></param>
    public PrefixedRedis(String server, String password, Int32 db, String prefix) : base(server, password, db) => Prefix = prefix ?? string.Empty;
    /// <summary>
    /// 实例化支持键前缀的Redis
    /// </summary>
    /// <param name="options"></param>
    public PrefixedRedis(RedisOptions options) : base(options) => Prefix = options.Prefix ?? string.Empty;
    #endregion

    /// <summary>重载执行，支持集群</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="func"></param>
    /// <param name="write">是否写入操作</param>
    /// <returns></returns>
    public override T Execute<T>(String key, Func<RedisClient, string, T> func, Boolean write = false) => base.Execute(key is null || key.StartsWith(Prefix) ? key : Prefix  + key, func, write);

    /// <summary>重载执行，支持集群</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="func"></param>
    /// <param name="write">是否写入操作</param>
    /// <returns></returns>
    public override Task<T> ExecuteAsync<T>(String key, Func<RedisClient, string, Task<T>> func, Boolean write = false) => base.ExecuteAsync(key is null || key.StartsWith(Prefix) ? key : Prefix + key, func, write);

    #region 子库
    /// <inheritdoc/>
    public override Redis CreateSub(Int32 db) { 
        var rd = base.CreateSub(db) as PrefixedRedis;
        rd.Prefix = Prefix;
        return rd;
    }
    #endregion

    #region 基础操作
    /// <inheritdoc/>
    public override Int32 Remove(params String[] keys) => base.Remove(keys.Select(o => o.StartsWith(Prefix) ? o : Prefix + o).ToArray());
    #endregion

    #region 高级操作
    /// <inheritdoc/>
    public override Boolean Rename(String key, String newKey, Boolean overwrite = true) => base.Rename(key, newKey.StartsWith(Prefix) ? newKey : Prefix + newKey, overwrite);
    /// <inheritdoc/>
    public override IEnumerable<String> Search(SearchModel model) { model.Pattern = model.Pattern.StartsWith(Prefix) ? model.Pattern : Prefix + model.Pattern; return base.Search(model); }
    #endregion

    #region 集合操作
    /// <inheritdoc/>
    public override void SetAll<T>(IDictionary<String, T> values, Int32 expire = -1) 
        => base.SetAll<T>(values.ToDictionary(k => k.Key.StartsWith(Prefix) ? k.Key : Prefix + k.Key, v => v.Value), expire);  
        
    /// <inheritdoc/>
    public override IDictionary<String, T> GetAll<T>(IEnumerable<String> keys) 
        => base.GetAll<T>(keys.Select(k => k.StartsWith(Prefix) ? k :  Prefix + k));
    /// <inheritdoc/>
    public override IDictionary<String, T> GetDictionary<T>(String key) => base.GetDictionary<T>(Prefix + key);
    /// <inheritdoc/>
    /// <remarks>RPOPLPUSH\BRPOPLPUSH不支持destKey参数加前缀</remarks>
    public override IList<T> GetList<T>(String key) => base.GetList<T>(key.StartsWith(Prefix) ? key : Prefix + key);
    /// <inheritdoc/>
    public override IProducerConsumer<T> GetQueue<T>(String topic) => base.GetQueue<T>(topic.StartsWith(Prefix) ? topic : Prefix + topic);
    /// <inheritdoc/>
    public override RedisReliableQueue<T> GetReliableQueue<T>(String topic) => base.GetReliableQueue<T>(topic.StartsWith(Prefix) ? topic : Prefix + topic);
    /// <inheritdoc/>
    public override RedisDelayQueue<T> GetDelayQueue<T>(String topic) => base.GetDelayQueue<T>(topic.StartsWith(Prefix) ? topic : Prefix + topic);
    /// <inheritdoc/>
    public override ICollection<T> GetSet<T>(String key) => base.GetSet<T>(key.StartsWith(Prefix) ? key : Prefix + key);
    /// <inheritdoc/>
    public override IProducerConsumer<T> GetStack<T>(String key) => base.GetStack<T>(key.StartsWith(Prefix) ? key : Prefix + key);
    /// <inheritdoc/>
    public override RedisStream<T> GetStream<T>(String topic) => base.GetStream<T>(topic.StartsWith(Prefix) ? topic : Prefix + topic);
    /// <inheritdoc/>
    public override RedisSortedSet<T> GetSortedSet<T>(String key) => base.GetSortedSet<T>(key.StartsWith(Prefix) ? key : Prefix + key);
    #endregion
}
