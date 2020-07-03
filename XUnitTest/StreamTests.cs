using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NewLife.Caching;
using NewLife.Data;
using NewLife.Log;
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
            if (config.IsNullOrEmpty()) config = "server=127.0.0.1;port=6379;db=3";

            _redis = new FullRedis();
            _redis.Init(config);
#if DEBUG
            _redis.Log = XTrace.Log;
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
            s.Add(new UserInfo { Name = "smartStone", Age = 36 });

            var queue = s as IProducerConsumer<Object>;
            var vs = new Object[] {
                new { aaa = "1234" },
                new { bbb = "abcd" },
                new { ccc = "新生命团队" },
                new { ddd = "ABEF" }
            };
            queue.Add(vs);

            // 对比个数
            var count2 = s.Count;
            Assert.False(s.IsEmpty);
            Assert.Equal(count + 1 + 1 + 1 + vs.Length, count2);

            // 独立消费
            var vs1 = s.Read(null, 3);
            Assert.Null(vs1);

            vs1 = s.Read("0-0", 3);
            Assert.Single(vs1);
            //var group = vs1[0] as Object[];
            //var k = group[0] as Packet;
            //var v = group[1] as Object[];
            //Assert.Equal(3, vs1.Length);

            // 取出来
            var vs2 = s.Take(2).ToArray();
            Assert.Equal(2, vs2.Length);
            Assert.Equal(vs[3], vs2[0]);
            Assert.Equal(vs[2], vs2[1]);

            var vs3 = s.Take(2).ToArray();
            Assert.Equal(2, vs3.Length);
            Assert.Equal(vs[1], vs3[0]);
            Assert.Equal(vs[0], vs3[1]);

            // 对比个数
            var count3 = s.Count;
            Assert.True(s.IsEmpty);
            Assert.Equal(count, count3);
        }
    }
}