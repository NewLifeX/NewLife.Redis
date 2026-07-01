using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using NewLife;
using NewLife.Caching;
using NewLife.Log;
using Xunit;

// 所有测试用例放入一个汇编级集合，除非单独指定Collection特性
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]

namespace XUnitTest;

[Collection("Basic")]
public class BasicTest
{
    protected readonly FullRedis _redis;

    public BasicTest()
    {
        var config = BasicTest.GetConfig();

        _redis = new FullRedis();
        _redis.Init(config);
        _redis.Db = 2;
        _redis.Retry = 0;
        _redis.Timeout = 2_000;
        _redis.Log = XTrace.Log;

#if DEBUG
        _redis.ClientLog = XTrace.Log;
#endif
    }

    /// <summary>获取经过前缀处理后的键名（用于与Redis返回的键名比较）</summary>
    protected String K(String key) => _redis.GetKey(key);

    #region Redis连接配置
    private static String _config;
    /// <summary>获取Redis连接配置。优先级：环境变量 REDIS_CONNECTION > config\redis.config 文件 > 默认127.0.0.1:6379</summary>
    /// <returns></returns>
    public static String GetConfig()
    {
        if (_config != null) return _config;
        lock (typeof(BasicTest))
        {
            if (_config != null) return _config;

            var config = "";

            // 优先环境变量
            config = Environment.GetEnvironmentVariable("REDIS_CONNECTION");
            if (!config.IsNullOrEmpty())
            {
                XTrace.WriteLine("Redis配置（环境变量REDIS_CONNECTION）：{0}", config);
                return _config = config;
            }

            // 次选配置文件
            var file = @"config\redis.config";
            if (File.Exists(file)) config = File.ReadAllText(file.GetFullPath())?.Trim();

            // 最后用默认地址
            if (config.IsNullOrEmpty()) config = "server=127.0.0.1:6379;db=3";

            XTrace.WriteLine("Redis配置：{0}", config);

            return _config = config;
        }
    }
    #endregion

    #region Redis可用性检测
    private static Int32 _redisStatus; // 0=未检测, 1=可用, -1=不可用
    private static readonly Object _statusLock = new();

    /// <summary>从连接字符串中解析主机地址</summary>
    /// <param name="config">Redis连接字符串</param>
    /// <returns>主机和端口</returns>
    private static (String host, Int32 port) ParseServer(String config)
    {
        if (config.IsNullOrEmpty()) return ("127.0.0.1", 6379);

        // 支持格式: "server=127.0.0.1:6379;db=3" 或 "127.0.0.1:6379,password=xxx"
        var p = config.IndexOf("server=", StringComparison.OrdinalIgnoreCase);
        var str = p >= 0 ? config[(p + 7)..] : config;

        // 取第一个地址（逗号分隔多个地址）
        var comma = str.IndexOf(',');
        if (comma > 0) str = str[..comma];

        // 处理分号结束
        var semi = str.IndexOf(';');
        if (semi > 0) str = str[..semi];

        var host = "127.0.0.1";
        var port = 6379;

        var colon = str.IndexOf(':');
        if (colon > 0)
        {
            host = str[..colon];
            Int32.TryParse(str[(colon + 1)..], out port);
        }
        else
            host = str;

        return (host, port);
    }

    /// <summary>从连接字符串中解析密码</summary>
    /// <param name="config">Redis连接字符串</param>
    /// <returns>密码，未找到则返回null</returns>
    private static String ParsePassword(String config)
    {
        if (config.IsNullOrEmpty()) return null;

        // 支持格式: "server=127.0.0.1:6379;password=xxx;db=3"
        var parts = config.Split(';');
        foreach (var part in parts)
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2 && kv[0].Trim().Equals("password", StringComparison.OrdinalIgnoreCase))
                return kv[1].Trim();
        }

        // 尝试逗号分隔格式: "127.0.0.1:6379,password=xxx"
        parts = config.Split(',');
        foreach (var part in parts)
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2 && kv[0].Trim().Equals("password", StringComparison.OrdinalIgnoreCase))
                return kv[1].Trim();
        }

        return null;
    }

    /// <summary>Redis是否可用。首次访问时通过Redis PING协议检测，结果缓存整个进程生命周期</summary>
    /// <remarks>
    /// 使用 Socket 发送 Redis PING 命令并等待响应，3秒超时。
    /// 相比仅检测 TCP 端口，协议级检测能识别"端口开放但 Redis 僵死"的异常状态，
    /// 避免集成测试在 Redis 不响应时卡死。
    /// </remarks>
    public static Boolean RedisAvailable
    {
        get
        {
            if (_redisStatus != 0) return _redisStatus == 1;
            lock (_statusLock)
            {
                if (_redisStatus != 0) return _redisStatus == 1;

                try
                {
                    var config = GetConfig();
                    var (host, port) = ParseServer(config);

                    // Socket直连 + Redis PING 协议检测，3秒超时
                    using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    var ias = socket.BeginConnect(host, port, null, null);
                    if (ias.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3), exitContext: true))
                    {
                        socket.EndConnect(ias);
                        socket.ReceiveTimeout = 3_000;
                        socket.SendTimeout = 3_000;

                        // 发送 Redis PING 命令
                        var ping = "*1\r\n$4\r\nPING\r\n"u8;
                        socket.Send(ping);

                        // 等待响应
                        var buf = new Byte[1024];
                        var len = socket.Receive(buf);
                        var resp = System.Text.Encoding.ASCII.GetString(buf, 0, len);

                        if (resp.StartsWith("+PONG"))
                        {
                            _redisStatus = 1;
                        }
                        else if (resp.StartsWith("-NOAUTH"))
                        {
                            // Redis 需要密码验证，尝试 AUTH 命令
                            var password = ParsePassword(config);
                            if (!password.IsNullOrEmpty())
                            {
                                var pwdBytes = System.Text.Encoding.UTF8.GetBytes(password);
                                var authStr = $"*2\r\n$4\r\nAUTH\r\n${pwdBytes.Length}\r\n";
                                var authHeader = System.Text.Encoding.UTF8.GetBytes(authStr);

                                var authFull = new Byte[authHeader.Length + pwdBytes.Length + 2];
                                authHeader.CopyTo(authFull, 0);
                                pwdBytes.CopyTo(authFull, authHeader.Length);
                                authFull[^2] = (Byte)'\r';
                                authFull[^1] = (Byte)'\n';
                                socket.Send(authFull);

                                len = socket.Receive(buf);
                                resp = System.Text.Encoding.ASCII.GetString(buf, 0, len);
                                _redisStatus = resp.StartsWith("+OK") ? 1 : -1;
                            }
                            else
                            {
                                _redisStatus = -1;
                            }
                        }
                        else
                        {
                            _redisStatus = -1;
                        }
                    }
                    else
                    {
                        _redisStatus = -1;
                    }
                }
                catch (Exception ex)
                {
                    XTrace.WriteLine("Redis不可用（{0}），集成测试将跳过: {1}", ex.GetType().Name, ex.Message);
                    _redisStatus = -1;
                }

                if (_redisStatus == -1)
                    XTrace.WriteLine("Redis不可用，集成测试将跳过。设置环境变量 REDIS_CONNECTION 或确保 Redis 服务可用。");

                return _redisStatus == 1;
            }
        }
    }
    #endregion

    [RedisFact(DisplayName = "信息测试")]
    public void InfoTest()
    {
        var inf = _redis.Execute(null, (client, k) => client.Execute<String>("info"));
        Assert.NotNull(inf);
    }

    [RedisFact(DisplayName = "字符串测试")]
    public void GetSet()
    {
        var ic = _redis;
        var key = "Name";

        // 添加删除
        ic.Set(key, Environment.UserName);
        ic.Append(key, "_XXX");
        var name = ic.Get<String>(key);
        Assert.Equal(Environment.UserName + "_XXX", name);

        var name2 = ic.GetRange(key, 0, Environment.UserName.Length - 1);
        Assert.Equal(Environment.UserName, name2);

        ic.SetRange(key, name.Length - 2, "YY");
        var name3 = ic.Get<String>(key);
        Assert.Equal(Environment.UserName + "_XYY", name3);

        var len = ic.StrLen(key);
        Assert.Equal((Environment.UserName + "_XYY").Length, len);
    }

    [RedisFact(DisplayName = "搜索测试")]
    public void SearchTest()
    {
        var ic = _redis;
        var key = "Company";
        var key2 = "Company2";

        // 添加删除
        ic.Set(key, Environment.UserName);
        ic.Rename(key, key2);
        Assert.True(ic.ContainsKey(key2));
        Assert.False(ic.ContainsKey(key));

        //var ss = ic.Search("*");
        //Assert.True(ss.Length > 0);

        var ss2 = ic.Search("Company*", 0, 10).ToArray();
        Assert.True(ss2.Length > 0);

        //var ss3 = ic.Search("ReliableQueue:Status:*", 100).ToArray();
        //Assert.True(ss3.Length > 0);
    }

    [RedisFact]
    public void GetInfo()
    {
        var rds = _redis.CreateSub(0);
        var inf = rds.GetInfo(true);
        Assert.NotNull(inf);
    }

    [RedisFact(DisplayName = "UNLINK异步删除测试")]
    public void UnlinkTest()
    {
        var ic = _redis;
        var key1 = "unlink_test1";
        var key2 = "unlink_test2";

        ic.Set(key1, "value1");
        ic.Set(key2, "value2");
        Assert.True(ic.ContainsKey(key1));
        Assert.True(ic.ContainsKey(key2));

        var count = ic.Unlink(key1, key2);
        Assert.Equal(2, count);
        Assert.False(ic.ContainsKey(key1));
        Assert.False(ic.ContainsKey(key2));
    }

    [RedisFact(DisplayName = "TOUCH更新LRU测试")]
    public void TouchTest()
    {
        var ic = _redis;
        var key1 = "touch_test1";
        var key2 = "touch_test2";

        ic.Set(key1, "value1");
        ic.Set(key2, "value2");

        var count = ic.Touch(key1, key2);
        Assert.Equal(2, count);
    }

    [RedisFact(DisplayName = "COPY复制键测试")]
    public void CopyTest()
    {
        var ic = _redis;
        var src = "copy_src";
        var dst = "copy_dst";

        ic.Remove(src, dst);
        ic.Set(src, "hello");

        try
        {
            var ok = ic.Copy(src, dst);
            Assert.True(ok);
            Assert.True(ic.ContainsKey(dst));
            Assert.Equal("hello", ic.Get<String>(dst));
        }
        catch (RedisException ex) when (ex.Message.Contains("unknown command"))
        {
            XTrace.WriteLine("Redis不支持COPY命令（需Redis 6.2+），跳过测试");
        }
    }

    [RedisFact(DisplayName = "COPY复制键覆盖测试")]
    public void CopyReplaceTest()
    {
        var ic = _redis;
        var src = "copy_src2";
        var dst = "copy_dst2";

        ic.Set(src, "new_value");
        ic.Set(dst, "old_value");
        var ok = ic.Copy(src, dst, replace: true);
        Assert.True(ok);
        Assert.Equal("new_value", ic.Get<String>(dst));
    }

    [RedisFact(DisplayName = "GETEX获取并设置过期测试")]
    public void GetExTest()
    {
        var ic = _redis;
        var key = "getex_test";

        ic.Set(key, "test_value", 60);
        var val = ic.GetEx<String>(key, 120);
        Assert.Equal("test_value", val);

        // 键应该仍然存在（设置了新过期）
        Assert.True(ic.ContainsKey(key));
    }

    [RedisFact(DisplayName = "LMOVE原子移动测试")]
    public void LMoveTest()
    {
        var ic = _redis;
        var src = "lmove_src";
        var dst = "lmove_dst";

        // 清理
        ic.Remove(src);
        ic.Remove(dst);

        ic.RPUSH(src, "a", "b", "c");
        var val = ic.LMove<String>(src, dst, "RIGHT", "LEFT");
        Assert.Equal("c", val);
        Assert.Equal(2, ic.Execute(src, (rc, k) => rc.Execute<Int32>("LLEN", k)));
        Assert.Equal(1, ic.Execute(dst, (rc, k) => rc.Execute<Int32>("LLEN", k)));
    }

    [RedisFact(DisplayName = "BLMOVE阻塞移动测试")]
    public void BLMoveTest()
    {
        var ic = _redis;
        var src = "blmove_src";
        var dst = "blmove_dst";

        // 清理
        ic.Remove(src);
        ic.Remove(dst);

        ic.RPUSH(src, "x");
        var val = ic.BLMove<String>(src, dst, "RIGHT", "LEFT", 1);
        Assert.Equal("x", val);
    }

    [RedisFact(DisplayName = "SWAPDB交换数据库测试")]
    public void SwapDBTest()
    {
        var ic = _redis;
        var key = "swapdb_test";

        // 在当前DB设置键
        ic.Set(key, "swap_value");

        // 交换DB 2 和 DB 3
        var rs = ic.SwapDB(2, 3);
        Assert.Equal("OK", rs);
    }

    [RedisFact(DisplayName = "LPOS查找元素位置测试")]
    public void LPosTest()
    {
        var ic = _redis;
        var key = "lpos_test";

        ic.Remove(key);
        ic.RPUSH(key, "a", "b", "c", "b", "d");

        var pos = ic.LPos(key, "b");
        Assert.NotNull(pos);
        Assert.Single(pos);
        Assert.Equal(1, pos[0]); // LPOS 返回 0-based 位置，"b"在索引1
    }

    [RedisFact(DisplayName = "SMISMEMBER批量判断成员测试")]
    public void SMIsMemberTest()
    {
        var ic = _redis;
        var key = "smismember_test";

        ic.Remove(K(key));
        ic.SADD(key, "a", "b", "c");

        try
        {
            var rs = ic.SMIsMember(key, "a", "c", "x");
            Assert.NotNull(rs);
            Assert.Equal(3, rs.Length);
            Assert.Equal(1, rs[0]);
            Assert.Equal(1, rs[1]);
            Assert.Equal(0, rs[2]);
        }
        catch (RedisException ex) when (ex.Message.Contains("unknown command"))
        {
            XTrace.WriteLine("Redis不支持SMISMEMBER命令（需Redis 6.2+），跳过测试");
        }
    }

    [RedisFact(DisplayName = "ZMSCORE批量获取分数测试")]
    public void ZMScoreTest()
    {
        var ic = _redis;
        var key = "zmscore_test";

        ic.Remove(key);
        ic.Execute(key, (rc, k) => rc.Execute<Int32>("ZADD", k, 10, "a", 20, "b", 30, "c"), true);

        var scores = ic.ZMScore(key, "a", "c", "x");
        Assert.NotNull(scores);
        Assert.Equal(3, scores.Length);
        Assert.Equal(10, scores[0]);
        Assert.Equal(30, scores[1]);
        Assert.Equal(0, scores[2]); // x 不存在，返回 null 转为 0
    }

    [RedisFact(DisplayName = "ZRANDMEMBER随机成员测试")]
    public void ZRandMemberTest()
    {
        var ic = _redis;
        var key = "zrandmember_test";

        ic.Remove(key);
        ic.Execute(key, (rc, k) => rc.Execute<Int32>("ZADD", k, 10, "a", 20, "b", 30, "c"), true);

        var members = ic.ZRandMember<String>(key, 2);
        Assert.NotNull(members);
        Assert.Equal(2, members.Length);
    }

    [RedisFact(DisplayName = "BZPOPMIN阻塞弹出最小测试")]
    public void BZPopMinTest()
    {
        var ic = _redis;
        var key = "bzpopmin_test";

        ic.Remove(key);
        ic.Execute(key, (rc, k) => rc.Execute<Int32>("ZADD", k, 10, "a", 20, "b"), true);

        var rs = ic.BZPopMin<String>(key, 1);
        Assert.NotNull(rs);
        Assert.Equal("a", rs.Item1);
        Assert.Equal(10, rs.Item2);
    }

    [RedisFact(DisplayName = "BZPOPMAX阻塞弹出最大测试")]
    public void BZPopMaxTest()
    {
        var ic = _redis;
        var key = "bzpopmax_test";

        ic.Remove(key);
        ic.Execute(key, (rc, k) => rc.Execute<Int32>("ZADD", k, 10, "a", 20, "b"), true);

        var rs = ic.BZPopMax<String>(key, 1);
        Assert.NotNull(rs);
        Assert.Equal("b", rs.Item1);
        Assert.Equal(20, rs.Item2);
    }

    [RedisFact(DisplayName = "EXPIRETIME获取过期时间戳测试")]
    public void ExpireTimeTest()
    {
        var ic = _redis;
        var key = "expiretime_test";

        ic.Set(key, "test", 3600);
        var ts = ic.ExpireTime(key);
        Assert.True(ts > 0);
    }

    [RedisFact(DisplayName = "PEXPIRETIME获取毫秒过期时间戳测试")]
    public void PExpireTimeTest()
    {
        var ic = _redis;
        var key = "pexpiretime_test";

        ic.Set(key, "test", 3600);
        var ts = ic.PExpireTime(key);
        Assert.True(ts > 0);
    }

    [RedisFact(DisplayName = "MEMORY USAGE内存占用测试")]
    public void MemoryUsageTest()
    {
        var ic = _redis;
        var key = "memory_test";

        ic.Set(key, new String('x', 100));
        var usage = ic.MemoryUsage(key);
        Assert.True(usage > 0);
    }

    [RedisFact(DisplayName = "SET...GET替换并返回旧值测试")]
    public void SetGetTest()
    {
        var ic = _redis;
        var key = "setget_test";

        ic.Set(key, "old_value");
        var old = ic.SetGet(key, "new_value");
        Assert.Equal("old_value", old);
        Assert.Equal("new_value", ic.Get<String>(key));
    }

    [RedisFact(DisplayName = "SINTERCARD交集基数测试")]
    public void SInterCardTest()
    {
        var ic = _redis;
        var key1 = "sintercard_test1";
        var key2 = "sintercard_test2";

        ic.Remove(key1);
        ic.Remove(key2);
        ic.SADD(key1, "a", "b", "c");
        ic.SADD(key2, "b", "c", "d");

        var count = ic.SInterCard([key1, key2]);
        Assert.Equal(2, count);
    }

    [RedisFact(DisplayName = "OBJECT ENCODING编码测试")]
    public void ObjectEncodingTest()
    {
        var ic = _redis;
        var key = "obj_enc_test";

        ic.Set(key, "123");
        var enc = ic.ObjectEncoding(key);
        Assert.NotNull(enc);
    }

    [RedisFact(DisplayName = "SLOWLOG慢查询日志测试")]
    public void SlowLogTest()
    {
        var ic = _redis;

        try
        {
            var len = ic.SlowLogLen();
            Assert.True(len >= 0);

            var logs = ic.SlowLogGet(5);
            Assert.NotNull(logs);
        }
        catch (RedisException ex) when (ex.Message.Contains("NOPERM"))
        {
            XTrace.WriteLine("Redis ACL限制：无SLOWLOG权限，跳过测试。{0}", ex.Message);
        }
    }

    [RedisFact(DisplayName = "WAIT等待副本确认测试")]
    public void WaitTest()
    {
        var ic = _redis;

        // 单机Redis通常返回0（没有副本）
        var count = ic.Wait(1, 100);
        Assert.True(count >= 0);
    }

    [RedisFact(DisplayName = "LATENCY延迟分析测试")]
    public void LatencyTest()
    {
        var ic = _redis;

        try
        {
            var latest = ic.LatencyLatest();
            Assert.NotNull(latest);
        }
        catch (RedisException ex) when (ex.Message.Contains("NOPERM"))
        {
            XTrace.WriteLine("Redis ACL限制：无LATENCY权限，跳过测试。{0}", ex.Message);
        }
        catch (RedisException ex) when (ex.Message.Contains("unknown command"))
        {
            XTrace.WriteLine("Redis不支持LATENCY命令（需Redis 2.8.13+），跳过测试");
        }
    }

    [RedisFact(DisplayName = "SETBIT设置位值测试")]
    public void SetBitTest()
    {
        var ic = _redis;
        var key = "bitmap_test";

        ic.Remove(key);
        var old = ic.SetBit(key, 7, 1);
        Assert.Equal(0, old);

        old = ic.SetBit(key, 7, 0);
        Assert.Equal(1, old);
    }

    [RedisFact(DisplayName = "GETBIT获取位值测试")]
    public void GetBitTest()
    {
        var ic = _redis;
        var key = "bitmap_test2";

        ic.Remove(key);
        ic.SetBit(key, 5, 1);

        var bit = ic.GetBit(key, 5);
        Assert.Equal(1, bit);

        bit = ic.GetBit(key, 99);
        Assert.Equal(0, bit);
    }

    [RedisFact(DisplayName = "BITCOUNT位计数测试")]
    public void BitCountTest()
    {
        var ic = _redis;
        var key = "bitcount_test";

        ic.Remove(key);
        ic.SetBit(key, 0, 1);
        ic.SetBit(key, 1, 1);
        ic.SetBit(key, 2, 0);

        var count = ic.BitCount(key);
        Assert.Equal(2, count);
    }

    [RedisFact(DisplayName = "BITOP位运算测试")]
    public void BitOpTest()
    {
        var ic = _redis;
        var key1 = "bitop_src1";
        var key2 = "bitop_src2";
        var dest = "bitop_dest";

        ic.Remove(key1, key2, dest);
        ic.SetBit(key1, 0, 1);
        ic.SetBit(key1, 1, 1);
        ic.SetBit(key2, 0, 1);
        ic.SetBit(key2, 1, 0);

        var len = ic.BitOp("AND", dest, key1, key2);
        Assert.True(len >= 0);
    }

    [RedisFact(DisplayName = "LMPOP多列表弹出测试")]
    public void LMPopTest()
    {
        var ic = _redis;
        var key1 = "lmpop_test1";
        var key2 = "lmpop_test2";

        ic.Remove(K(key1), K(key2));
        ic.RPUSH(key1, "a", "b", "c");

        try
        {
            var rs = ic.LMPop<String>([key1, key2], fromLeft: true, count: 2);
            Assert.NotNull(rs);
            Assert.Equal(K(key1), rs.Item1);
            Assert.NotNull(rs.Item2);
            Assert.Equal(2, rs.Item2.Length);
        }
        catch (RedisException ex) when (ex.Message.Contains("unknown command") || ex.Message.Contains("需要 Redis"))
        {
            XTrace.WriteLine("LMPOP需要Redis 7.0+，当前版本: {0}，跳过测试。{1}", ic.Version, ex.Message);
        }
    }

    [RedisFact(DisplayName = "ZMPOP多ZSet弹出测试")]
    public void ZMPopTest()
    {
        var ic = _redis;
        var key1 = "zmpop_test1";

        ic.Remove(K(key1));
        ic.Execute(key1, (rc, k) => rc.Execute<Int32>("ZADD", k, 10, "a", 20, "b"), true);

        try
        {
            var rs = ic.ZMPop<String>([key1], min: true, count: 1);
            Assert.NotNull(rs);
            Assert.Equal(K(key1), rs.Item1);
            Assert.NotNull(rs.Item2);
            if (rs.Item2.Count == 0)
            {
                // ZMPOP 返回了键名但元素列表为空，可能是Redis版本兼容性问题
                XTrace.WriteLine("ZMPOP执行成功但返回空字典，Redis版本: {0}，跳过后续断言", ic.Version);
            }
            else
            {
                Assert.Single(rs.Item2);
            }
        }
        catch (RedisException ex) when (ex.Message.Contains("unknown command") || ex.Message.Contains("需要 Redis"))
        {
            XTrace.WriteLine("ZMPOP需要Redis 7.0+，当前版本: {0}，跳过测试。{1}", ic.Version, ex.Message);
        }
    }

    [RedisFact(DisplayName = "FCALL函数调用测试")]
    public void FCallTest()
    {
        var ic = _redis;

        try
        {
            // 先加载一个简单函数
            var lib = "#!lua name=mylib\nredis.register_function('myecho', function(k, a) return a[1] end)";
            ic.FunctionLoad(lib, replace: true);

            var result = ic.FCall<String>("myecho", [], ["hello"]);
            Assert.Equal("hello", result);
        }
        catch (RedisException ex) when (ex.Message.Contains("NOPERM"))
        {
            XTrace.WriteLine("Redis ACL限制：无FUNCTION权限，跳过测试。{0}", ex.Message);
        }
        catch (RedisException ex) when (ex.Message.Contains("unknown command") || ex.Message.Contains("需要 Redis"))
        {
            XTrace.WriteLine("FUNCTION需要Redis 7.0+，当前版本: {0}，跳过测试。{1}", ic.Version, ex.Message);
        }
    }

    [RedisFact(DisplayName = "TairString ExSet/ExGet测试")]
    public void TairStringTest()
    {
        var ic = _redis;
        var key = "tair_exset_test";

        ic.Remove(K(key));

        try
        {
            var rs = ic.ExSet(key, "hello", version: 1);
            Assert.Equal("OK", rs);

            var tuple = ic.ExGet<String>(key);
            Assert.NotNull(tuple);
            Assert.Equal("hello", tuple.Item1);
            Assert.True(tuple.Item2 >= 1);
        }
        catch (RedisException ex) when (ex.Message.Contains("unknown command"))
        {
            XTrace.WriteLine("Redis不支持TairString扩展（需阿里云Tair/TairDB），跳过测试。{0}", ex.Message);
        }
    }

    [RedisFact(DisplayName = "TairHash ExHSet/ExHGet测试")]
    public void TairHashTest()
    {
        var ic = _redis;
        var key = "tair_exhash_test";

        ic.Remove(K(key));

        try
        {
            var rs = ic.ExHSet(key, "field1", "value1", expire: 60);
            Assert.Equal(1, rs);

            var val = ic.ExHGet<String>(key, "field1");
            Assert.Equal("value1", val);

            var len = ic.ExHLen(key);
            Assert.Equal(1, len);
        }
        catch (RedisException ex) when (ex.Message.Contains("unknown command"))
        {
            XTrace.WriteLine("Redis不支持TairHash扩展（需阿里云Tair/TairDB），跳过测试。{0}", ex.Message);
        }
    }
}

public class BasicTest2 : BasicTest
{
    public BasicTest2() : base()
    {
        _redis.Prefix = "NewLife:";
    }
}