using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using NewLife;
using NewLife.Caching;
using NewLife.Log;
using NewLife.Security;
using NewLife.Serialization;
using Xunit;

namespace XUnitTest
{
    public class StreamTests
    {
        private FullRedis _redis;

        public StreamTests()
        {
            //_redis = new FullRedis("127.0.0.1:6379", null, 2);

            var config = "";
            var file = @"config\redis.config";
            if (File.Exists(file)) config = File.ReadAllText(file.GetFullPath())?.Trim();
            if (config.IsNullOrEmpty()) config = "server=127.0.0.1:6379;db=3";
            if (!File.Exists(file)) File.WriteAllText(file.GetFullPath(), config);

            _redis = new FullRedis();
            _redis.Init(config);
#if DEBUG
            _redis.Log = NewLife.Log.XTrace.Log;
#endif
        }

        [Fact]
        public void Stream_Primitive()
        {
            var key = "stream_Primitive";

            // 删除已有
            _redis.Remove(key);
            var s = _redis.GetStream<Int32>(key);
            _redis.SetExpire(key, TimeSpan.FromMinutes(60));

            // 取出个数
            var count = s.Count;
            Assert.True(s.IsEmpty);
            Assert.Equal(0, count);

            // 添加基础类型
            var id = s.Add(1234);

            // 对比个数
            var count2 = s.Count;
            Assert.False(s.IsEmpty);
            Assert.Equal(count + 1, count2);

            // 范围
            var rs = s.Range(null, null);
            Assert.Equal(count + 1, rs.Count);

            // 尾部消费
            var vs1 = s.Read(null, 3);
            Assert.Null(vs1);

            // 原始读取
            vs1 = s.Read("0-0", 3);
            Assert.NotNull(vs1);
            Assert.Single(vs1);

            var message = vs1[0];
            Assert.Equal(id, message.Id);

            Assert.NotNull(message.Body);
            Assert.Equal(2, message.Body.Length);
            Assert.Equal(s.PrimitiveKey, message.Body[0]);
            Assert.Equal(1234, message.Body[1].ToInt());

            // 智能读取
            var vs3 = s.Take(5).ToList();
            Assert.Single(vs3);
            Assert.Equal(1234, vs3[0]);

            // 指针已经前移
            var ss = id.Split('-');
            Assert.Equal($"{ss[0]}-{ss[1].ToInt() + 1}", s.StartId);

            // 删除
            var rs2 = s.Delete(rs[0].Id);
            Assert.Equal(1, rs2);
            Assert.Equal(count + 1, s.Count + 1);
        }

        class UserInfo
        {
            public String Name { get; set; }
            public Int32 Age { get; set; }
        }

        [Fact]
        public void Stream_Normal()
        {
            var key = "stream_key";

            // 删除已有
            _redis.Remove(key);
            var s = _redis.GetStream<UserInfo>(key);
            _redis.SetExpire(key, TimeSpan.FromMinutes(60));

            // 取出个数
            var count = s.Count;
            Assert.True(s.IsEmpty);
            Assert.Equal(0, count);

            // 添加空对象
            Assert.Throws<ArgumentNullException>(() => s.Add(default));

            // 添加复杂对象
            var id = s.Add(new UserInfo { Name = "smartStone", Age = 36 });

            var queue = s as IProducerConsumer<UserInfo>;
            var vs = new[] {
                new UserInfo{ Name = "1234" },
                new UserInfo{ Name = "abcd" },
                new UserInfo{ Name = "新生命团队" },
                new UserInfo{ Name = "ABEF" }
            };
            queue.Add(vs);

            // 对比个数
            var count2 = s.Count;
            Assert.False(s.IsEmpty);
            Assert.Equal(count + 1 + vs.Length, count2);

            // 独立消费
            var vs1 = s.Read(null, 3);
            Assert.Null(vs1);

            vs1 = s.Read("0-0", 3);
            Assert.Equal(3, vs1.Count);
            Assert.Equal(id, vs1[0].Id);

            // 取出来
            var vs2 = s.Take(2).ToList();
            Assert.Equal(2, vs2.Count);
            Assert.Equal("smartStone", vs2[0].Name);
            Assert.Equal(36, vs2[0].Age);
            Assert.Equal(vs[0].Name, vs2[1].Name);

            id = s.StartId;

            var vs3 = s.Take(7).ToList();
            Assert.Equal(3, vs3.Count);
            Assert.Equal(vs[1].Name, vs3[0].Name);
            Assert.Equal(vs[2].Name, vs3[1].Name);
            Assert.Equal(vs[3].Name, vs3[2].Name);

            // 开始编号改变
            Assert.NotEqual(id, s.StartId);
        }

        [Fact]
        public void CreateGroup()
        {
            var key = "stream_group";

            // 删除已有
            _redis.Remove(key);
            var s = _redis.GetStream<Int32>(key);
            _redis.SetExpire(key, TimeSpan.FromMinutes(60));

            // 添加基础类型
            var id = s.Add(1234);

            // 创建
            s.GroupCreate("mygroup");
            s.GroupDeleteConsumer("mygroup", "stone");
            s.GroupSetId("mygroup", "0-0");

            // 删除
            s.GroupDestroy("mygroup");
        }

        [Fact]
        public void GetInfo()
        {
            var key = "stream_info";

            // 删除已有
            _redis.Remove(key);
            var s = _redis.GetStream<String>(key);
            _redis.SetExpire(key, TimeSpan.FromMinutes(60));

            // 添加基础类型
            for (var i = 0; i < 7; i++)
            {
                s.Add(Rand.NextString(8));
            }

            // 创建
            s.GroupCreate("mygroup", "0");
            s.GroupCreate("mygroup2");
            s.GroupCreate("mygroup3");

            // 消费
            var rs = s.ReadGroup("mygroup", "stone", 3);

            // 消息流
            var info = s.GetInfo();
            Assert.NotNull(info);

            XTrace.WriteLine(info.ToJson(true));

            // 消费者
            var groups = s.GetGroups();
            Assert.Equal(3, groups.Length);
            XTrace.WriteLine(groups.ToJson(true));

            // 消费者
            var consumers = s.GetConsumers("mygroup");
            Assert.Equal(1, consumers.Length);
            XTrace.WriteLine(consumers.ToJson(true));
        }

        [Fact]
        public async void GlobalConsume()
        {
            var key = "stream_GlobalConsume";

            // 删除已有
            _redis.Remove(key);
            var queue = _redis.GetStream<String>(key);
            _redis.SetExpire(key, TimeSpan.FromMinutes(60));

            // 添加基础类型
            for (var i = 0; i < 7; i++)
            {
                queue.Add(Rand.NextString(8));
            }

            // 消费
            var rs = queue.Take(6).ToList();
            Assert.Equal(6, rs.Count);

            var rs2 = queue.TakeOne();
            Assert.NotNull(rs2);

            // 延迟2秒生产消息
            ThreadPool.QueueUserWorkItem(s => { Thread.Sleep(2000); queue.Add("xxyy"); });
            var sw = Stopwatch.StartNew();
            var rs3 = await queue.TakeOneAsync(3);
            sw.Stop();
            Assert.Equal("xxyy", rs3);
            Assert.True(sw.ElapsedMilliseconds >= 2000);
        }

        [Fact]
        public async void GroupConsume()
        {
            var key = "stream_GroupConsume";

            // 删除已有
            _redis.Remove(key);
            var queue = _redis.GetStream<String>(key);
            _redis.SetExpire(key, TimeSpan.FromMinutes(60));
            queue.Group = "mygroup";

            // 添加基础类型
            var ids = new List<String>();
            for (var i = 0; i < 7; i++)
            {
                var msgId = queue.Add(Rand.NextString(8));
                ids.Add(msgId);
            }

            // 创建
            queue.GroupCreate(queue.Group);

            // 消费
            var rs = queue.Take(6).ToList();
            Assert.Equal(6, rs.Count);

            var rs2 = queue.TakeOne();
            Assert.NotNull(rs2);

            // 延迟2秒生产消息
            ThreadPool.QueueUserWorkItem(s => { Thread.Sleep(2000); queue.Add("xxyy"); });
            var sw = Stopwatch.StartNew();
            var msg = await queue.TakeMessageAsync(3);
            sw.Stop();
            Assert.Equal("xxyy", msg.GetBody<String>());
            Assert.True(sw.ElapsedMilliseconds >= 2000);

            // 等待队列
            var pi = queue.GetPending(queue.Group);
            Assert.NotNull(pi);
            Assert.Equal(7 + 1, pi.Count);
            Assert.Equal(ids[0], pi.StartId);
            //Assert.Equal(ids[0], pi.EndId);
            Assert.Equal(1, pi.Consumers.Count);
            var kv = pi.Consumers.First();
            Assert.Equal(7 + 1, kv.Value);
            Assert.Equal($"{Environment.MachineName}@{Process.GetCurrentProcess().Id}", kv.Key);

            var ps = queue.Pending(queue.Group, null, null, 5);
            Assert.NotNull(ps);
            Assert.Equal(5, ps.Length);

            // 确认消费
            //Assert.False(true, "Acknowledge");
            queue.Acknowledge(msg.Id);

            // 等待队列
            pi = queue.GetPending(queue.Group);
            Assert.NotNull(pi);
            Assert.Equal(7, pi.Count);

            ps = queue.Pending(queue.Group, null, null, 8);
            Assert.NotNull(ps);
            Assert.Equal(7, ps.Length);
        }
    }
}