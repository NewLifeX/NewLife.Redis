using System.Buffers;
using System.Collections.Concurrent;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;
using NewLife.Net;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Caching;

/// <summary>Redis客户端</summary>
/// <remarks>
/// 以极简原则进行设计，每个客户端不支持并行命令处理（非线程安全），可通过多客户端多线程解决。
/// </remarks>
public class RedisClient : DisposeBase
{
    #region 属性
    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>客户端</summary>
    public TcpClient? Client { get; set; }

    /// <summary>服务器地址</summary>
    public NetUri Server { get; set; }

    /// <summary>宿主</summary>
    public Redis Host { get; set; } = null!;

    /// <summary>读写超时时间。默认为0取Host.Timeout</summary>
    public Int32 Timeout { get; set; }

    /// <summary>是否已登录</summary>
    public Boolean Logined { get; private set; }

    /// <summary>登录时间</summary>
    public DateTime LoginTime { get; private set; }

    const Int32 MAX_POOL_SIZE = 1024 * 1024;
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    /// <param name="redis">宿主</param>
    /// <param name="server">服务器地址。一个redis对象可能有多服务器，例如Cluster集群</param>
    public RedisClient(Redis redis, NetUri server)
    {
        Name = redis.Name;
        Host = redis;
        Server = server;
    }

    /// <summary>销毁</summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        // 销毁时退出
        if (Logined)
        {
            try
            {
                var tc = Client;
                if (tc != null && tc.Connected && tc.GetStream() != null) Quit();
            }
            catch { }
        }

        Client.TryDispose();
    }

    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String ToString() => Server + "";
    #endregion

    #region 核心方法
    private Stream? _stream;
    /// <summary>新建连接获取数据流</summary>
    /// <param name="create">新建连接</param>
    /// <returns></returns>
    private async Task<Stream?> GetStreamAsync(Boolean create)
    {
        var tc = Client;
        var ns = _stream;

        // 判断连接是否可用
        var active = false;
        try
        {
            active = ns != null && tc != null && tc.Connected && ns.CanWrite && ns.CanRead;
        }
        catch { }

        // 如果连接不可用，则重新建立连接
        if (!active)
        {
            Logined = false;

            Client = null;
            tc.TryDispose();
            if (!create) return null;

            var timeout = Timeout;
            if (timeout == 0) timeout = Host.Timeout;
            tc = new TcpClient
            {
                SendTimeout = timeout,
                ReceiveTimeout = timeout
            };

            var uri = Server;
            await tc.ConnectAsync(uri.Address, uri.Port);

            Client = tc;
            ns = tc.GetStream();

            // 客户端SSL
            var sp = Host.SslProtocol;
            if (sp != SslProtocols.None)
            {
                var sslStream = new SslStream(ns, false, OnCertificateValidationCallback);
                sslStream.AuthenticateAsClient(uri.Host ?? uri.Address + "", [], sp, false);

                ns = sslStream;
            }

            _stream = ns;
        }

        return ns;
    }

    private Boolean OnCertificateValidationCallback(Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        // 如果没有证书，全部通过
        var cert = Host.Certificate;
        if (cert == null) return true;

        return chain.ChainElements
                .Cast<X509ChainElement>()
                .Any(x => x.Certificate.Thumbprint == cert.Thumbprint);
    }

    private static readonly Byte[] _NewLine = [(Byte)'\r', (Byte)'\n'];

    /// <summary>发出请求</summary>
    /// <param name="memory"></param>
    /// <param name="cmd"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    protected virtual Int32 GetRequest(Memory<Byte> memory, String cmd, Object[]? args)
    {
        // *<number of arguments>\r\n$<number of bytes of argument 1>\r\n<argument data>\r\n
        // *1\r\n$4\r\nINFO\r\n

        var log = Log == null || Log == Logger.Null ? null : Pool.StringBuilder.Get();
        log?.Append(cmd);

        /*
         * 一颗玲珑心
         * 九天下凡尘
         * 翩翩起菲舞
         * 霜摧砺石开
         */

        var writer = new SpanWriter(memory.Span);

        // 区分有参数和无参数
        if (args == null || args.Length == 0)
        {
            //var str = "*1\r\n${0}\r\n{1}\r\n".F(cmd.Length, cmd);
            //Encoding.UTF8.GetBytes(GetHeaderBytes(cmd, 0), memory.Span);
            writer.Write(GetHeaderBytes(cmd, 0));
        }
        else
        {
            //var str = "*{2}\r\n${0}\r\n{1}\r\n".F(cmd.Length, cmd, 1 + args.Length);
            //Encoding.UTF8.GetBytes(GetHeaderBytes(cmd, args.Length), memory.Span);
            writer.Write(GetHeaderBytes(cmd, args.Length));

            for (var i = 0; i < args!.Length; i++)
            {
                Byte[] buf = null!;
                var size = 0;
                var str = args[i] as String;
                if (str != null)
                {
                    size = Encoding.UTF8.GetByteCount(str);
                }
                else
                {
                    buf = (args[i] as Byte[])!;
                    size = buf.Length;
                }

                // 指令日志。简单类型显示原始值，复杂类型显示序列化后字符串
                if (log != null && args != null)
                {
                    log.Append(' ');
                    if (str != null)
                        log.Append(str);
                    else
                        log.AppendFormat("[{0}]{1}", size, buf.ToStr(null, 0, 1024)?.TrimEnd());
                }

                //str = "${0}\r\n".F(item.Length);
                writer.Write((Byte)'$');
                writer.Write(size.ToString());
                writer.Write(_NewLine);
                if (str != null)
                    writer.Write(str);
                else
                    writer.Write(buf);

                writer.Write(_NewLine);
            }
        }
        if (log != null) WriteLog("=> {0}", log.Return(true));

        return writer.WrittenCount;
    }

    /// <summary>异步接收响应</summary>
    /// <param name="ns">网络数据流</param>
    /// <param name="count">响应个数</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    protected virtual async Task<IList<Object?>> GetResponseAsync(Stream ns, Int32 count, CancellationToken cancellationToken = default)
    {
        /*
         * 响应格式
         * 1：简单字符串，非二进制安全字符串，一般是状态回复。  +开头，例：+OK\r\n 
         * 2: 错误信息。-开头， 例：-ERR unknown command 'mush'\r\n
         * 3: 整型数字。:开头， 例：:1\r\n
         * 4：大块回复值，最大512M。  $开头+数据长度。 例：$4\r\nmush\r\n
         * 5：多条回复。*开头， 例：*2\r\n$3\r\nfoo\r\n$3\r\nbar\r\n
         */

        var list = new List<Object?>();
        var ms = ns;
        var log = Log == null || Log == Logger.Null ? null : Pool.StringBuilder.Get();

        Char header;
        var buf = Pool.Shared.Rent(1);
        try
        {
            // 取巧进行异步操作，只要异步读取到第一个字节，后续同步读取
            //var buf = new Byte[1];
            if (cancellationToken == CancellationToken.None)
                cancellationToken = new CancellationTokenSource(Timeout > 0 ? Timeout : Host.Timeout).Token;
            var n = await ms.ReadAsync(buf, 0, 1, cancellationToken);
            if (n <= 0) return list;

            header = (Char)buf[0];
        }
        finally
        {
            Pool.Shared.Return(buf);
        }

        //var header = (Char)buf[0];

        // 多行响应
        for (var i = 0; i < count; i++)
        {
            // 解析响应
            if (i > 0)
            {
                var b = ms.ReadByte();
                if (b == -1) break;

                header = (Char)b;
            }

            log?.Append(header);
            if (header == '$')
            {
                list.Add(ReadBlock(ms, log));
            }
            else if (header == '*')
            {
                list.Add(ReadBlocks(ms, log));
            }
            else
            {
                // 字符串以换行为结束符
                var str = ReadLine(ms);
                log?.Append(str);

                if (header is '+' or ':')
                    list.Add(str);
                else if (header == '-')
                    throw new RedisException(str);
                else
                {
                    XTrace.WriteLine("无法解析响应[{0:X2}] {1}", (Byte)header, ms.ReadBytes(-1).ToHex("-"));
                    throw new InvalidDataException($"无法解析响应 [{header}]");
                }
            }
        }

        if (log != null) WriteLog("<= {0}", log.Return(true));

        return list;
    }

    /// <summary>异步执行命令，发请求，取响应</summary>
    /// <param name="cmd">命令</param>
    /// <param name="args">参数数组</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    protected virtual async Task<Object?> ExecuteCommandAsync(String cmd, Object[]? args, CancellationToken cancellationToken)
    {
        var isQuit = cmd == "QUIT";

        var ns = await GetStreamAsync(!isQuit);
        if (ns == null) return null;

        if (!cmd.IsNullOrEmpty())
        {
            // 验证登录
            CheckLogin(cmd);
            CheckSelect(cmd);

            // 参数编码为字符串或字节数组
            if (args != null)
            {
                for (var i = 0; i < args.Length; i++)
                {
                    args[i] = Host.Encoder.Encode(args[i]);
                }
            }

            // 估算数据包大小，从内存池借出
            var total = GetCommandSize(cmd, args);
            var buffer = total < MAX_POOL_SIZE ? Pool.Shared.Rent(total) : new Byte[total];
            var memory = buffer.AsMemory();

            var p = GetRequest(memory, cmd, args);
            memory = memory[..p];

            var max = Host.MaxMessageSize;
            if (max > 0 && memory.Length >= max) throw new InvalidOperationException($"命令[{cmd}]的数据包大小[{memory.Length}]超过最大限制[{max}]，大key会拖累整个Redis实例，可通过Redis.MaxMessageSize调节。");

            if (memory.Length > 0) await ns.WriteAsync(memory);

            if (total < MAX_POOL_SIZE) Pool.Shared.Return(buffer);

            await ns.FlushAsync(cancellationToken);
        }

        var rs = await GetResponseAsync(ns, 1, cancellationToken);

        if (isQuit) Logined = false;

        return rs.FirstOrDefault();
    }

    private void CheckLogin(String? cmd)
    {
        if (Logined) return;
        if (cmd.EqualIgnoreCase("Auth")) return;

        if (!Host.Password.IsNullOrEmpty() && !Auth(Host.UserName, Host.Password))
            throw new Exception("登录失败！");

        Logined = true;
        LoginTime = DateTime.Now;
    }

    private Int32 _selected = -1;
    private void CheckSelect(String? cmd)
    {
        var db = Host.Db;
        if (_selected == db) return;
        if (cmd.EqualIgnoreCase("Auth", "Select", "Info")) return;

        if (db > 0 && (Host is not FullRedis rds || !rds.Mode.EqualIgnoreCase("cluster", "sentinel"))) Select(db);

        _selected = db;
    }

    /// <summary>重置。干掉历史残留数据</summary>
    public void Reset()
    {
        var ns = GetStreamAsync(false).Result;
        if (ns == null) return;

        // 干掉历史残留数据
        if (ns is NetworkStream nss && nss.DataAvailable)
        {
            var buf = new Byte[1024];

            Int32 count;
            do
            {
                count = ns.Read(buf, 0, buf.Length);
            } while (count > 0 && nss.DataAvailable);
        }
    }

    private static IMemoryOwner<Byte>? ReadBlock(Stream ms, StringBuilder? log) => ReadPacket(ms, log);

    private Object?[] ReadBlocks(Stream ms, StringBuilder? log)
    {
        // 结果集数量
        var len = ReadLine(ms).ToInt(-1);
        log?.Append(len);
        if (len < 0) return [];

        var arr = new Object?[len];
        for (var i = 0; i < len; i++)
        {
            var b = ms.ReadByte();
            if (b == -1) break;

            var header = (Char)b;
            log?.Append(' ');
            log?.Append(header);
            if (header == '$')
            {
                arr[i] = ReadPacket(ms, log);
            }
            else if (header is '+' or ':')
            {
                arr[i] = ReadLine(ms);
                log?.Append(arr[i]);
            }
            else if (header == '*')
            {
                arr[i] = ReadBlocks(ms, log);
            }
        }

        return arr;
    }

    private static IMemoryOwner<Byte>? ReadPacket(Stream ms, StringBuilder? log)
    {
        var len = ReadLine(ms).ToInt(-1);
        log?.Append(len);
        if (len == 0)
        {
            // 某些字段即使长度是0，还是要把换行符读走
            ReadLine(ms);
            return null;
        }
        if (len <= 0) return null;
        //if (len <= 0) throw new InvalidDataException();

        len += 2;
        var owner = MemoryPool<Byte>.Shared.Rent(len);
        var span = owner.GetSpan();
        //var buf = new Byte[len];
        var p = 0;
        while (p < len)
        {
            // 等待，直到读完需要的数据，避免大包丢数据
            var count = ms.Read(span.Slice(p));
            if (count <= 0) break;

            p += count;
        }

        //var pk = new Packet(buf, 0, p - 2);
        //log?.AppendFormat(" {0}", pk.ToStr(null, 0, 1024)?.TrimEnd());

        //return pk;
        return owner;
    }

    private static String ReadLine(Stream ms)
    {
        var sb = Pool.StringBuilder.Get();
        while (true)
        {
            var b = ms.ReadByte();
            if (b < 0) break;

            if (b == '\r')
            {
                var b2 = ms.ReadByte();
                if (b2 < 0) break;

                if (b2 == '\n') break;

                sb.Append((Char)b);
                sb.Append((Char)b2);
            }
            else
                sb.Append((Char)b);
        }

        return sb.Return(true);
    }

    private Int32 GetCommandSize(String cmd, Object[]? args)
    {
        var total = 16 + cmd.Length;
        if (args != null)
        {
            foreach (var item in args)
            {
                if (item is String str)
                    total += 16 + Encoding.UTF8.GetByteCount(str);
                else if (item is Byte[] buf)
                    total += 16 + buf.Length;
            }
        }

        return total;
    }
    #endregion

    #region 主要方法
    /// <summary>执行命令。返回字符串、Packet、Packet[]</summary>
    /// <param name="cmd"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public virtual String? Execute(String cmd, params Object[] args) => Execute<String>(cmd, args);

    /// <summary>执行命令。返回基本类型、对象、对象数组</summary>
    /// <param name="cmd"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public virtual TResult? Execute<TResult>(String cmd, params Object[] args)
    {
        // 管道模式
        if (_ps != null)
        {
            _ps.Add(new Command(cmd, args, typeof(TResult)));
            return default;
        }

        var rs = ExecuteAsync(cmd, args).Result;
        if (rs == null) return default;
        if (rs is TResult rs2) return rs2;
        if (rs != null && TryChangeType(rs, typeof(TResult), out var target)) return (TResult?)target;

        return default;
    }

    /// <summary>尝试执行命令。返回基本类型、对象、对象数组</summary>
    /// <param name="cmd"></param>
    /// <param name="args"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public virtual Boolean TryExecute<TResult>(String cmd, Object[] args, out TResult? value)
    {
        var rs = Execute<TResult>(cmd, args);
        if (rs is TResult rs2)
        {
            value = rs2;
            return true;
        }

        value = default;
        if (rs == null) return false;

        if (rs != null && TryChangeType(rs, typeof(TResult), out var target)) value = (TResult?)target;

        return true;
    }

    /// <summary>异步执行命令。返回字符串、Packet、Packet[]</summary>
    /// <param name="cmd">命令</param>
    /// <param name="args">参数数组</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    public virtual async Task<Object?> ExecuteAsync(String cmd, Object[] args, CancellationToken cancellationToken = default)
    {
        // 埋点名称，支持二级命令
        var act = cmd.EqualIgnoreCase("cluster", "xinfo", "xgroup", "xreadgroup") ? $"{cmd}-{args?.FirstOrDefault()}" : cmd;
        using var span = cmd.IsNullOrEmpty() ? null : Host.Tracer?.NewSpan($"redis:{Name}:{act}", args);
        try
        {
            return await ExecuteCommandAsync(cmd, args, cancellationToken);
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
    }

    /// <summary>异步执行命令。返回基本类型、对象、对象数组</summary>
    /// <param name="cmd">命令</param>
    /// <param name="args">参数数组</param>
    /// <returns></returns>
    public virtual async Task<TResult?> ExecuteAsync<TResult>(String cmd, params Object[] args) => await ExecuteAsync<TResult>(cmd, args, CancellationToken.None);

    /// <summary>异步执行命令。返回基本类型、对象、对象数组</summary>
    /// <param name="cmd">命令</param>
    /// <param name="args">参数数组</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    public virtual async Task<TResult?> ExecuteAsync<TResult>(String cmd, Object[] args, CancellationToken cancellationToken)
    {
        // 管道模式
        if (_ps != null)
        {
            _ps.Add(new Command(cmd, args, typeof(TResult)));
            return default;
        }

        var rs = await ExecuteAsync(cmd, args, cancellationToken);
        if (rs == null) return default;
        if (rs is TResult rs2) return rs2;
        if (rs != null && TryChangeType(rs, typeof(TResult), out var target)) return (TResult?)target;

        return default;
    }

    /// <summary>读取更多。用于PubSub等多次读取命令</summary>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    public virtual async Task<TResult?> ReadMoreAsync<TResult>(CancellationToken cancellationToken)
    {
        var ns = await GetStreamAsync(false);
        if (ns == null) return default;

        var rss = await GetResponseAsync(ns, 1, cancellationToken);
        var rs = rss.FirstOrDefault();

        //var rs = ExecuteCommand(null, null, null);
        if (rs == null) return default;
        if (rs is TResult rs2) return rs2;
        if (rs != null && TryChangeType(rs, typeof(TResult), out var target)) return (TResult?)target;

        return default;
    }

    /// <summary>尝试转换类型</summary>
    /// <param name="value"></param>
    /// <param name="type"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public virtual Boolean TryChangeType(Object value, Type type, out Object? target)
    {
        target = null;

        if (value is String str)
        {
            try
            {
                //target = value.ChangeType(type);
                if (type == typeof(Boolean) && str == "OK")
                    target = true;
                else
                    target = Convert.ChangeType(str, type);
                return true;
            }
            catch (Exception ex)
            {
                //if (type.GetTypeCode() != TypeCode.Object)
                throw new Exception($"不能把字符串[{str}]转为类型[{type.FullName}]", ex);
            }
        }

        if (value is Packet pk)
        {
            target = Host.Encoder.Decode(pk, type);
            return true;
        }

        if (value is IMemoryOwner<Byte> owner)
        {
            target = Host.Encoder.Decode(owner.GetSpan(), type);
            owner.Dispose();

            return true;
        }

        if (value is Object[] objs)
        {
            if (type == typeof(Object[])) { target = value; return true; }
            if (type == typeof(Packet[])) { target = objs.Cast<Packet>().ToArray(); return true; }

            // 遇到空结果时返回默认值
            if (objs.Length == 0) return false;

            var elmType = type.GetElementTypeEx();
            if (elmType == null) return false;

            var arr = Array.CreateInstance(elmType, objs.Length);
            for (var i = 0; i < objs.Length; i++)
            {
                if (objs[i] is Packet pk3)
                    arr.SetValue(Host.Encoder.Decode(pk3, elmType), i);
                else if (objs[i] is IMemoryOwner<Byte> owner2)
                {
                    arr.SetValue(Host.Encoder.Decode(owner2.GetSpan(), elmType), i);
                    owner2.Dispose();
                }
                else if (objs[i] != null && objs[i].GetType().As(elmType))
                    arr.SetValue(objs[i], i);
            }
            target = arr;
            return true;
        }

        return false;
    }

    private IList<Command>? _ps;
    /// <summary>管道命令个数</summary>
    public Int32 PipelineCommands => _ps == null ? 0 : _ps.Count;

    /// <summary>开始管道模式</summary>
    public virtual void StartPipeline() => _ps ??= [];

    /// <summary>结束管道模式</summary>
    /// <param name="requireResult">要求结果</param>
    public virtual Object?[]? StopPipeline(Boolean requireResult)
    {
        var ps = _ps;
        if (ps == null) return null;

        _ps = null;

        var ns = GetStreamAsync(true).Result;
        if (ns == null) return null;

        using var span = Host.Tracer?.NewSpan($"redis:{Name}:Pipeline", null);
        try
        {
            // 验证登录
            CheckLogin(null);
            CheckSelect(null);

            // 估算数据包大小，从内存池借出
            var total = 0;
            foreach (var item in ps)
            {
                // 参数编码为字符串或字节数组
                var args = item.Args;
                if (args != null)
                {
                    for (var i = 0; i < args.Length; i++)
                    {
                        args[i] = Host.Encoder.Encode(args[i]);
                    }
                    item.Args = args;
                }

                total += GetCommandSize(item.Name, item.Args);
            }

            var buffer = total < MAX_POOL_SIZE ? Pool.Shared.Rent(total) : new Byte[total];
            var memory = buffer.AsMemory();
            var p = 0;

            // 整体打包所有命令
            var cmds = new List<String>(ps.Count);
            foreach (var item in ps)
            {
                cmds.Add(item.Name);
                p += GetRequest(memory.Slice(p), item.Name, item.Args);
            }
            memory = memory[..p];

            // 设置数据标签
            span?.SetTag(cmds);

            // 整体发出
            if (memory.Length > 0) ns.WriteAsync(memory).GetAwaiter().GetResult();
            if (total < MAX_POOL_SIZE) Pool.Shared.Return(buffer);

            if (!requireResult) return new Object[ps.Count];

            // 获取响应
            var list = GetResponseAsync(ns, ps.Count).Result;
            for (var i = 0; i < list.Count; i++)
            {
                var rs = list[i];
                if (rs != null && TryChangeType(rs, ps[i].Type, out var target) && target != null) list[i] = target;
            }

            return list.ToArray();
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
    }

    private class Command
    {
        public String Name { get; set; }
        public Object[] Args { get; set; }
        public Type Type { get; set; }

        public Command(String name, Object[] args, Type type)
        {
            Name = name;
            Args = args;
            Type = type;
        }
    }
    #endregion

    #region 基础功能
    /// <summary>心跳</summary>
    /// <returns></returns>
    public Boolean Ping() => Execute<String>("PING") == "PONG";

    /// <summary>选择Db</summary>
    /// <param name="db"></param>
    /// <returns></returns>
    public Boolean Select(Int32 db) => Execute<String>("SELECT", db + "") == "OK";

    /// <summary>验证密码</summary>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    public Boolean Auth(String? username, String password)
    {
        var rs = username.IsNullOrEmpty() ?
            Execute<String>("AUTH", password) :
            Execute<String>("AUTH", username, password);

        return rs == "OK";
    }

    /// <summary>退出</summary>
    /// <returns></returns>
    public Boolean Quit() => Execute<String>("QUIT") == "OK";
    #endregion

    #region 获取设置
    /// <summary>批量设置</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="values"></param>
    /// <returns></returns>
    public Boolean SetAll<T>(IDictionary<String, T> values)
    {
        if (values == null || values.Count == 0) throw new ArgumentNullException(nameof(values));

        var ps = new List<Object>();
        foreach (var item in values)
        {
            ps.Add(item.Key);

            if (item.Value == null) throw new NullReferenceException();
            ps.Add(item.Value);
        }

        var rs = Execute<String>("MSET", ps.ToArray());
        if (rs != "OK")
        {
            using var span = Host.Tracer?.NewSpan($"redis:{Name}:ErrorSetAll", values);
            if (Host.ThrowOnFailure) throw new XException("Redis.SetAll({0})失败。{1}", values.ToJson(), rs);
        }

        return rs == "OK";
    }

    /// <summary>批量获取</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="keys"></param>
    /// <returns></returns>
    public IDictionary<String, T?> GetAll<T>(IEnumerable<String> keys)
    {
        if (keys == null || !keys.Any()) throw new ArgumentNullException(nameof(keys));

        var ks = keys.ToArray();

        var dic = new Dictionary<String, T?>(ks.Length);
        if (Execute<Object[]>("MGET", ks) is not Object[] rs) return dic;

        for (var i = 0; i < ks.Length && i < rs.Length; i++)
        {
            if (rs[i] is Packet pk) dic[ks[i]] = (T?)Host.Encoder.Decode(pk, typeof(T));
        }

        return dic;
    }
    #endregion

    #region 辅助
    private static readonly ConcurrentDictionary<String, String> _cache0 = new();
    private static readonly ConcurrentDictionary<String, String> _cache1 = new();
    private static readonly ConcurrentDictionary<String, String> _cache2 = new();
    private static readonly ConcurrentDictionary<String, String> _cache3 = new();
    /// <summary>获取命令对应的字节数组，全局缓存</summary>
    /// <param name="cmd"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    private static String GetHeaderBytes(String cmd, Int32 args = 0)
    {
        if (args == 0) return _cache0.GetOrAdd(cmd, k => $"*1\r\n${k.Length}\r\n{k}\r\n");
        if (args == 1) return _cache1.GetOrAdd(cmd, k => $"*2\r\n${k.Length}\r\n{k}\r\n");
        if (args == 2) return _cache2.GetOrAdd(cmd, k => $"*3\r\n${k.Length}\r\n{k}\r\n");
        if (args == 3) return _cache3.GetOrAdd(cmd, k => $"*4\r\n${k.Length}\r\n{k}\r\n");

        return $"*{1 + args}\r\n${cmd.Length}\r\n{cmd}\r\n";
    }
    #endregion

    #region 日志
    /// <summary>日志</summary>
    public ILog Log { get; set; } = Logger.Null;

    /// <summary>写日志</summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public void WriteLog(String format, params Object[] args) => Log?.Info(format, args);
    #endregion
}
