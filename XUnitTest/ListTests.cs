using NewLife.Caching;
using NewLife.Log;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace XUnitTest
{
    public class ListTests
    {
        private readonly FullRedis _redis;

        public ListTests()
        {
            var config = BasicTest.GetConfig();

            _redis = new FullRedis();
            _redis.Init(config);
#if DEBUG
            _redis.Log = NewLife.Log.XTrace.Log;
#endif
        }

        [Fact]
        public void List_Normal()
        {
            var key = "lkey";

            // 删除已有
            _redis.Remove(key);
            var l = _redis.GetList<String>(key);
            _redis.SetExpire(key, TimeSpan.FromSeconds(60));

            var list = l as RedisList<String>;
            Assert.NotNull(list);

            // 取出个数
            var count = list.Count;
            Assert.Equal(0, count);

            // 添加
            var vs = new[] { "1234", "abcd", "新生命团队", "ABEF" };
            list.Add(vs[0]);
            list.AddRange(vs.Skip(1));

            // 对比个数
            var count2 = list.Count;
            Assert.Equal(count + vs.Length, count2);
            Assert.False(l.IsReadOnly);

            // 取出来
            Assert.Equal(vs.Length, list.Count);
            Assert.Equal(vs[0], list[0]);
            Assert.Equal(vs[1], list[1]);
            Assert.Equal(vs[2], list[2]);
            Assert.Equal(vs[3], list[3]);

            var exist = list.Contains(vs[3]);
            Assert.True(exist);
        }

        [Fact]
        public void List_Copy()
        {
            var key = "lkey_copy";

            // 删除已有
            _redis.Remove(key);
            var l = _redis.GetList<String>(key);
            _redis.SetExpire(key, TimeSpan.FromSeconds(60));

            var list = l as RedisList<String>;

            // 添加
            var vs = new[] { "1234", "abcd", "新生命团队", "ABEF" };
            list.AddRange(vs);

            // 拷贝
            var vs3 = new String[2];
            list.CopyTo(vs3, 0);
            Assert.Equal(2, vs3.Length);
            Assert.Equal(vs[0], vs3[0]);
            Assert.Equal(vs[1], vs3[1]);
        }

        [Fact]
        public void List_Remove()
        {
            var key = "lkey_remove";

            // 删除已有
            _redis.Remove(key);
            var l = _redis.GetList<String>(key);
            _redis.SetExpire(key, TimeSpan.FromSeconds(60));

            var list = l as RedisList<String>;

            // 添加
            var vs = new[] { "1234", "abcd", "新生命团队", "ABEF" };
            list.AddRange(vs);

            // 索引、删除
            var idx = list.IndexOf("abcd");
            Assert.Equal(1, idx);
            list.RemoveAt(3);
            list.Remove("1234");
            Assert.Equal(2, list.Count);
            Assert.Equal("abcd", list[0]);

            // 插入
            list.Insert(1, "12345");
            Assert.Equal("abcd", list[0]);
            Assert.Equal("12345", list[1]);
            Assert.Equal("新生命团队", list[2]);
            Assert.Equal(3, list.Count);

            // 修剪
            var rs = list.LTrim(1, 2);
            Assert.True(rs);
            Assert.Equal(2, list.Count);
            Assert.Equal("12345", list[0]);
            Assert.Equal("新生命团队", list[1]);
        }

        [Fact]
        public void List_Advance()
        {
            var key = "lkey_advance";

            // 删除已有
            _redis.Remove(key);
            var l = _redis.GetList<String>(key);
            _redis.SetExpire(key, TimeSpan.FromSeconds(60));

            var list = l as RedisList<String>;
            Assert.NotNull(list);

            // 添加
            var vs = new[] { "1234", "abcd", "新生命团队", "ABEF" };
            list.AddRange(vs);

            // 压入
            var vs2 = new[] { "0000", "1111" };
            var n2 = list.LPUSH(vs2);
            Assert.Equal(vs.Length + vs2.Length, n2);
            Assert.Equal(vs2[1], list[0]);
            Assert.Equal(vs2[0], list[1]);

            var vs3 = new[] { "0000", "1111" };
            var n3 = list.RPUSH(vs3);
            Assert.Equal(vs.Length + vs2.Length + vs3.Length, n3);
            Assert.Equal(vs3[1], list[0]);
            Assert.Equal(vs3[0], list[1]);

            // 弹出
            var item1 = list.LPOP();
            Assert.Equal(vs2[1], item1);
            var item2 = list.RPOP();
            Assert.Equal(vs3[1], item2);
        }

        [Fact]
        public void RPOPLPUSH_Test()
        {
            var key = "lkey_rpoplpush";
            var key2 = "lkey_rpoplpush2";

            // 删除已有
            _redis.Remove(key);
            _redis.Remove(key2);

            var l = _redis.GetList<String>(key);
            _redis.SetExpire(key, TimeSpan.FromSeconds(60));

            var list = l as RedisList<String>;
            Assert.NotNull(list);

            // 添加
            var vs = new[] { "1234", "abcd", "新生命团队", "ABEF" };
            list.AddRange(vs);

            // 原子消费
            list.RPOPLPUSH(key2);
            Assert.Equal(vs.Length - 1, list.Count);
            Assert.Equal(vs[2], list.RPOP());

            // 第二列表
            var l2 = _redis.GetList<String>(key2);
            _redis.SetExpire(key2, TimeSpan.FromSeconds(60));

            Assert.Equal(vs[3], l2[0]);
            Assert.Equal(1, l2.Count);
        }

        [Fact]
        public void BRPOPLPUSH_Test()
        {
            // 一个队列多个消费，阻塞是否叠加
            var key = "lkey_brpoplpush";
            _redis.Timeout = 15_000;

            XTrace.WriteLine("BRPOPLPUSH_Test");
            var sw = Stopwatch.StartNew();

            var t1 = Task.Run(() =>
            {
                var queue = _redis.GetList<String>(key) as RedisList<String>;
                queue.BRPOPLPUSH("lkey_ack1", 3);
                XTrace.WriteLine("lkey_ack1");
            });

            var t2 = Task.Run(() =>
            {
                var queue = _redis.GetList<String>(key) as RedisList<String>;
                queue.BRPOPLPUSH("lkey_ack2", 3);
                XTrace.WriteLine("lkey_ack2");
            });

            var t3 = Task.Run(() =>
            {
                var queue = _redis.GetList<String>(key) as RedisList<String>;
                queue.BRPOPLPUSH("lkey_ack3", 3);
                XTrace.WriteLine("lkey_ack3");
            });

            Task.WaitAll(t1, t2, t3);

            sw.Stop();
            XTrace.WriteLine("BRPOPLPUSH_BlockTest: {0}", sw.Elapsed);
            //Assert.True(sw.ElapsedMilliseconds < 3_000 + 500);
        }
    }
}