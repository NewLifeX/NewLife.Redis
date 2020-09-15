using System;
using System.IO;
using System.Linq;
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

            var kv = vs1.FirstOrDefault();
            Assert.Equal(id, kv.Key);

            var vs2 = kv.Value;
            Assert.NotNull(vs2);
            Assert.Equal(2, vs2.Length);
            Assert.Equal(s.PrimitiveKey, vs2[0]);
            Assert.Equal(1234, vs2[1].ToInt());

            // 智能读取
            var vs3 = s.Take(5);
            Assert.Single(vs3);
            Assert.Equal(1234, vs3[0]);

            // 指针已经前移
            var ss = id.Split('-');
            Assert.Equal($"{ss[0]}-{ss[1].ToInt() + 1}", s.StartId);

            // 删除
            var rs2 = s.Delete(rs.First().Key);
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
            Assert.Equal(id, vs1.FirstOrDefault().Key);

            // 取出来
            var vs2 = s.Take(2);
            Assert.Equal(2, vs2.Count);
            Assert.Equal("smartStone", vs2[0].Name);
            Assert.Equal(36, vs2[0].Age);
            Assert.Equal(vs[0].Name, vs2[1].Name);

            id = s.StartId;

            var vs3 = s.Take(7);
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
            s.GroupCreate("mygroup");
            s.GroupCreate("mygroup2");
            s.GroupCreate("mygroup3");

            var info = s.GetInfo();
            Assert.NotNull(info);

            XTrace.WriteLine(info.ToJson(true));
        }
    }
}