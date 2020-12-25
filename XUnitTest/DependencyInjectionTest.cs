using System;
using System.Collections.Generic;
using System.Text;
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
            services.AddFullRedis("server=127.0.0.1;passowrd=;db=9");
            provider = services.BuildServiceProvider();
        }


        [Fact]
        public void SetAndGet()
        {
            var fullRedis = provider.GetService<FullRedis>();

            _ = fullRedis.Set<string>("test", "123456");

            var str = fullRedis.Get<string>("test");

            Assert.Equal("123456", str);
        }

    }
}