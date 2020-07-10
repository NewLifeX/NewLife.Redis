using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Caching;
using NewLife.Log;
using Xunit;

namespace XUnitTest
{
    public class HashTest
    {
        private readonly FullRedis _redis;

        public HashTest()
        {
            _redis = new FullRedis("127.0.0.1:6379", null, 2);
#if DEBUG
            _redis.Log = XTrace.Log;
#endif
        }

        [Fact]
        public void HMSETTest()
        {
            var key = "hash_key";

            // 删除已有
            _redis.Remove(key);

            var hash = _redis.GetDictionary<String>(key) as RedisHash<String, String>;
            Assert.NotNull(hash);

            var dic = new Dictionary<String, String>
            {
                ["aaa"] = "123",
                ["bbb"] = "456"
            };
            var rs = hash.HMSet(dic);
            Assert.True(rs);
            Assert.Equal(2, hash.Count);
        }
    }
}