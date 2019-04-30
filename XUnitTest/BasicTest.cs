using System;
using System.Threading;
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

        [Fact(DisplayName = "ÐÅÏ¢²âÊÔ", Timeout = 1000)]
        public void InfoTest()
        {
            var inf = Cache.Execute<String>(null, client => client.Execute<String>("info"));
            Assert.NotNull(inf);
        }

        [Fact(DisplayName = "×Ö·û´®²âÊÔ")]
        public void GetSet()
        {
            var ic = Cache;
            var key = "Name";

            // Ìí¼ÓÉ¾³ý
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

        [Fact(DisplayName = "ËÑË÷²âÊÔ")]
        public void SearchTest()
        {
            var ic = Cache;
            var key = "Name";
            var key2 = "Name2";

            // Ìí¼ÓÉ¾³ý
            ic.Set(key, Environment.UserName);
            ic.Rename(key, key2);
            Assert.True(ic.ContainsKey(key2));
            Assert.False(ic.ContainsKey(key));

            var ss = ic.Search("*");
            Assert.True(ss.Length > 0);

            var n = 0;
            var ss2 = ic.Search("*", 10, ref n);
            Assert.True(ss2.Length > 0);
        }
    }
}