using System;
using System.Net.Sockets;
using Xunit;

namespace XUnitTest;

/// <summary>拓扑集成测试特性。本机 Redis 拓扑（主从复制、哨兵等）不可用时自动标记为跳过</summary>
/// <remarks>
/// 用于依赖本机固定端口拓扑的集成测试。在测试发现阶段探测 127.0.0.1 上指定端口，
/// 全部就绪才运行，否则跳过，避免在未搭建拓扑的机器上误报“连接失败”。
/// </remarks>
public class RedisTopologyFactAttribute : FactAttribute
{
    /// <summary>实例化拓扑测试特性</summary>
    /// <param name="ports">拓扑端口数组</param>
    public RedisTopologyFactAttribute(params Int32[] ports)
    {
        if (ports == null || ports.Length == 0) return;

        foreach (var port in ports)
        {
            if (!IsPortOpen(port))
            {
                Skip = $"本机 Redis 拓扑未就绪（127.0.0.1:{port} 未监听），跳过集成测试。";
                return;
            }
        }
    }

    private static Boolean IsPortOpen(Int32 port)
    {
        try
        {
            using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            var ias = socket.BeginConnect("127.0.0.1", port, null, null);
            if (!ias.AsyncWaitHandle.WaitOne(500)) return false;

            socket.EndConnect(ias);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
