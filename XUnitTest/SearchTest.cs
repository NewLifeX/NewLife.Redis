using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Caching;
using NewLife.Log;
using Xunit;

namespace XUnitTest
{
    [Collection("Basic")]
    public class SearchTest
    {
        protected readonly FullRedis _redis;

        public SearchTest()
        {
            var config = BasicTest.GetConfig();

            _redis = new FullRedis();
            _redis.Init(config);
            _redis.Db = 2;
            _redis.Retry = 0;
            _redis.Log = XTrace.Log;

#if DEBUG
            _redis.ClientLog = XTrace.Log;
#endif
        }

        [Fact(DisplayName = "搜索测试")]
        public void GetSearchTest()
        {
            var ic = _redis;
            for (int i = 0; i < 1000; i++)
            {
                PlayGameVo playGameVo = new PlayGameVo()
                {
                    MemberId = (i + 1).ToString(),
                    GameMode = 1,
                    Num = 10000
                };
                //cache.Cache.Set(RedisConst.PlayGameKey + playGameVo.MemberId, playGameVo);
                //redis.Prefix=RedisConst.PlayGameKey;
                ic.Set("jinshi:member:battle-royale:play-game:" + playGameVo.MemberId, playGameVo);
            }
            IDictionary<string, string> all = new Dictionary<string, string>();
            List<string> list = ic.Search("jinshi:member:battle-royale:play-game:*", 1000).ToList();
            all = ic.GetAll<string>(list);
            Assert.True(list.Count==1000);
            Assert.False(list.Count < 1000);
        }
    }

    public class PlayGameVo
    {
        public string MemberId { get; set; }
        public int GameMode { get; set; }
        public int Num { get; set; }
    }
}
