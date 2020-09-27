using System;
using System.Linq;
using System.Threading;
using NewLife.Caching;
using NewLife.Log;
using Xunit;

namespace XUnitTest
{
    public class BasicTest
    {
        public FullRedis _redis { get; set; }

        public BasicTest()
        {
            _redis = new FullRedis("127.0.0.1:6379", null, 2);
#if DEBUG
            _redis.Log = XTrace.Log;
#endif
        }

        [Fact(DisplayName = "信息测试", Timeout = 1000)]
        public void InfoTest()
        {
            var inf = _redis.Execute(null, client => client.Execute<String>("info"));
            Assert.NotNull(inf);
        }

        [Fact(DisplayName = "字符串测试")]
        public void GetSet()
        {
            var ic = _redis;
            var key = "Name";

            // 添加删除
            ic.Set(key, Environment.UserName);
            ic.Append(key, "_XXX");
            var name = ic.Get<String>(key);
            Assert.Equal(Environment.UserName + "_XXX", name);

            var name2 = ic.GetRange(key, 0, Environment.UserName.Length - 1);
            Assert.Equal(Environment.UserName, name2);

            ic.SetRange(key, name.Length - 2, "YY");
            var name3 = ic.Get<String>(key);
            Assert.Equal(Environment.UserName + "_XYY", name3);

            var len = ic.StrLen(key);
            Assert.Equal((Environment.UserName + "_XYY").Length, len);
        }

        [Fact(DisplayName = "搜索测试")]
        public void SearchTest()
        {
            var ic = _redis;
            var key = "Name";
            var key2 = "Name2";

            // 添加删除
            ic.Set(key, Environment.UserName);
            ic.Rename(key, key2);
            Assert.True(ic.ContainsKey(key2));
            Assert.False(ic.ContainsKey(key));

            //var ss = ic.Search("*");
            //Assert.True(ss.Length > 0);

            var ss2 = ic.Search("Name*", 10).ToArray();
            Assert.True(ss2.Length > 0);

            //var ss3 = ic.Search("ReliableQueue:Status:*", 100).ToArray();
            //Assert.True(ss3.Length > 0);
        }
    }
}