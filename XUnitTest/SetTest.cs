using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NewLife.Caching;
using NewLife.Log;
using Xunit;

namespace XUnitTest
{
    public class SetTest
    {
        private readonly FullRedis _redis;

        public SetTest()
        {
            _redis = new FullRedis("127.0.0.1:6379", null, 2);
#if DEBUG
            _redis.Log = XTrace.Log;
#endif
        }

        [Fact]
        public void Search()
        {
            var rkey = "set_Search";

            // 删除已有
            _redis.Remove(rkey);

            var set = _redis.GetSet<String>(rkey);
            var set2 = set as RedisSet<String>;

            // 插入数据
            set.Add("stone1");
            set.Add("stone2");
            set.Add("stone3");
            set.Add("stone4");
            Assert.Equal(4, set.Count);

            // 搜索。这里为了Assert每一项，要排序，因为输出顺序可能不确定
            var dic = set2.Search("*one?", 4).OrderBy(e => e).ToList();
            Assert.Equal(4, dic.Count);
            Assert.Equal("stone1", dic[0]);
            Assert.Equal("stone2", dic[1]);
            Assert.Equal("stone3", dic[2]);
        }
    }
}