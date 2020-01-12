using NewLife.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace XUnitTest
{
    public class StackTests
    {
        private FullRedis _redis;

        public StackTests()
        {
            _redis = new FullRedis("127.0.0.1:6379", null, 2);
        }

        [Fact]
        public void Stack_Normal()
        {
            var key = "skey";

            // 删除已有
            _redis.Remove(key);
            var s = _redis.GetStack<String>(key);
            _redis.SetExpire(key, TimeSpan.FromMinutes(60));

            var stack = s as RedisStack<String>;
            Assert.NotNull(stack);

            // 取出个数
            var count = stack.Count;
            Assert.True(stack.IsEmpty);
            Assert.Equal(0, count);

            // 添加
            var vs = new[] { "1234", "abcd", "新生命团队", "ABEF" };
            s.Add(vs);

            // 对比个数
            var count2 = stack.Count;
            Assert.False(stack.IsEmpty);
            Assert.Equal(count + vs.Length, count2);

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
            var count3 = stack.Count;
            Assert.True(stack.IsEmpty);
            Assert.Equal(count, count3);
        }
    }
}