using NewLife;
using NewLife.Caching.Clusters;
using NewLife.Caching.Models;
using Xunit;

namespace XUnitTest.Models;

public class ReplicationInfoTests
{
    [Fact]
    public void Parse()
    {
        var str = """
                # Replication
                role:master
                connected_slaves:3
                slave0:ip=127.0.0.1,port=6004,state=online,offset=5547321,lag=0
                slave1:ip=127.0.0.1,port=6002,state=online,offset=5547321,lag=0
                slave2:ip=127.0.0.1,port=6003,state=online,offset=5547321,lag=0
                master_replid:fde76bb564010e613e8688a9c363ef0236d57fe0
                master_replid2:c8e18bb64c3d7ea0d24f9af1e499f781d10e4559
                master_repl_offset:5547321
                second_repl_offset:2975511
                repl_backlog_active:1
                repl_backlog_size:1048576
                repl_backlog_first_byte_offset:4498746
                repl_backlog_histlen:1048576
                """;

        var dic = str.SplitAsDictionary(":", "\n");

        var rep = new ReplicationInfo();
        rep.Load(dic);

        Assert.Equal("master", rep.Role);
        Assert.NotNull(rep.Slaves);
        Assert.Equal(3, rep.Slaves.Length);

        var inf = rep.Slaves[1];
        Assert.NotNull(inf);
        Assert.Equal("127.0.0.1", inf.IP);
        Assert.Equal(6002, inf.Port);
        Assert.Equal("online", inf.State);
        Assert.Equal(5547321, inf.Offset);
        Assert.Equal(0, inf.Lag);
    }

    [Fact]
    public void Parse2()
    {
        var str = """
                # Replication
                role:slave
                master_host:127.0.0.1
                master_port:6379
                master_link_status:up
                master_last_io_seconds_ago:0
                master_sync_in_progress:0
                slave_repl_offset:5552381
                slave_priority:100
                slave_read_only:1
                connected_slaves:0
                master_replid:fde76bb564010e613e8688a9c363ef0236d57fe0
                master_replid2:0000000000000000000000000000000000000000
                master_repl_offset:5552381
                second_repl_offset:-1
                repl_backlog_active:1
                repl_backlog_size:1048576
                repl_backlog_first_byte_offset:5060704
                repl_backlog_histlen:491678
                """;

        var dic = str.SplitAsDictionary(":", "\n");

        var rep = new ReplicationInfo();
        rep.Load(dic);

        Assert.Equal("slave", rep.Role);
        Assert.Null(rep.Slaves);
        Assert.Equal("127.0.0.1", rep.MasterHost);
        Assert.Equal(6379, rep.MasterPort);
    }
}
