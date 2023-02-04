using System;
using Microsoft.Extensions.DependencyInjection;
using NewLife.Caching;
using Xunit;

namespace XUnitTest
{
    [Collection("Basic")]
    public class DependencyInjectionTest
    {
        public readonly ServiceProvider provider;

        public DependencyInjectionTest()
        {
            var services = new ServiceCollection();
            services.AddRedis("server=127.0.0.1;passowrd=;db=9");
            provider = services.BuildServiceProvider();
        }

        [Fact]
        public void SetAndGet()
        {
            var fullRedis = provider.GetService<FullRedis>();

            _ = fullRedis.Set<String>("test", "123456");

            var str = fullRedis.Get<String>("test");

            Assert.Equal("123456", str);
        }
    }
}