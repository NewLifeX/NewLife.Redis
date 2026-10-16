using System;
using Microsoft.Extensions.DependencyInjection;
using NewLife.Caching;
using Xunit;

namespace XUnitTest
{
    public class DependencyInjectionTest
    {
        public readonly ServiceProvider provider;

        public DependencyInjectionTest()
        {
            var services = new ServiceCollection();
            services.AddRedis(BasicTest.GetConfig());
            provider = services.BuildServiceProvider();
        }

        [RedisFact]
        public void SetAndGet()
        {
            var fullRedis = provider.GetService<FullRedis>();

            _ = fullRedis.Set<String>("test", "123456");

            var str = fullRedis.Get<String>("test");

            Assert.Equal("123456", str);
        }
    }
}