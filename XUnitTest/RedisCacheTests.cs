using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using NewLife.Caching;
using NewLife.Redis.Extensions;
using Xunit;

namespace XUnitTest
{
    public class RedisCacheTests
    {
        public readonly ServiceProvider provider;
        private static readonly string prefix = "myPrefix:";
        public RedisCacheTests()
        {
            var services = new ServiceCollection();
            services.AddDistributedRedisCache(options =>
            {
                options.Server = "127.0.0.1";
                options.Db = 9;
                options.Prefix = prefix;
            });
            provider = services.BuildServiceProvider();
        }

        [Fact]
        public void Get()
        {
            var key = "key1";
            var value = Encoding.UTF8.GetBytes("value1");

            var cache = provider.GetService<IDistributedCache>();
            cache.Set(key, value, null);

            var rs = cache.Get(key);
            Assert.NotNull(rs);
            Assert.Equal(value, rs);
        }

        [Fact]
        public async Task GetAsync()
        {
            var key = "key2";
            var value = Encoding.UTF8.GetBytes("value2");

            var cache = provider.GetService<IDistributedCache>();
            await cache.SetAsync(key, value, null);

            var rs = await cache.GetAsync(key);
            Assert.NotNull(rs);
            Assert.Equal(value, rs);
        }

        [Fact]
        public void Set()
        {
            var key = "key3";
            var value = Encoding.UTF8.GetBytes("value3");

            var cache = provider.GetService<IDistributedCache>();
            cache.Set(key, value, null);

            var rs = cache.Get(key);
            Assert.NotNull(rs);
            Assert.Equal(value, rs);
        }

        [Fact]
        public async Task SetAsync()
        {
            var key = "key4";
            var value = Encoding.UTF8.GetBytes("value4");

            var cache = provider.GetService<IDistributedCache>();
            await cache.SetAsync(key, value, null);

            var rs = cache.Get(key);
            Assert.NotNull(rs);
            Assert.Equal(value, rs);
        }

        [Fact]
        public void Set_With_Options()
        {
            var key = "key5";
            var value = Encoding.UTF8.GetBytes("value5");

            var cache = provider.GetService<IDistributedCache>();
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTime.Now.AddSeconds(30)
            };
            cache.Set(key, value, options);

            var rs = cache.Get(key);
            Assert.NotNull(rs);
            Assert.Equal(value, rs);
        }

        [Fact]
        public async Task SetAsync_With_Options()
        {
            var key = "key6";
            var value = Encoding.UTF8.GetBytes("value6");

            var cache = provider.GetService<IDistributedCache>();
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTime.Now.AddSeconds(30)
            };
            await cache.SetAsync(key, value, options);

            var rs = cache.Get(key);
            Assert.NotNull(rs);
            Assert.Equal(value, rs);
        }

        [Fact]
        public void Set_With_Options_AbsoluteExpirationRelativeToNow()
        {
            var key = "key7";
            var value = Encoding.UTF8.GetBytes("value7");

            var cache = provider.GetService<IDistributedCache>();
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
            };
            cache.Set(key, value, options);

            var rs = cache.Get(key);
            Assert.NotNull(rs);
            Assert.Equal(value, rs);
        }

        /// <summary>
        /// 测试缓存Key前缀
        /// </summary>
        [Fact]
        public void PrefixTest()
        {
            var key = "key8";
            var value = Encoding.UTF8.GetBytes("value8");

            var cache = provider.GetService<IDistributedCache>();
            cache.Set(key, value, null);

            var rs = cache.Get(key);
            Assert.NotNull(rs);
            Assert.Equal(value, rs);

            var cache2 = provider.GetService<Redis>();
            var rs2 = cache.Get(prefix + key);
            Assert.NotNull(rs2);
            Assert.Equal(value, rs2);
        }

    }
}
