using System;
using NewLife.Caching;
using Xunit;

namespace XUnitTest
{
    public class BasicTest
    {
        public FullRedis Cache { get; set; }

        public BasicTest()
        {
            FullRedis.Register();
            var rds = FullRedis.Create("127.0.0.1:6379", 2);

            Cache = rds as FullRedis;
        }

        [Fact(DisplayName = "个数测试", Timeout = 1000)]
        public void CountTest()
        {
            var count = Cache.Count;
            Assert.NotEqual(0, count);
        }

        [Fact(DisplayName = "信息测试", Timeout = 1000)]
        public void InfoTest()
        {
            var inf = Cache.Execute<String>(null, client => client.Execute<String>("info"));
            Assert.NotNull(inf);
        }
    }
}