#if NETSTANDARD || NETCOREAPP
using System;
using Microsoft.Extensions.DependencyInjection;
using NewLife.Caching;
using NewLife.Log;

namespace Microsoft.Extensions.DependencyInjection
{ 
    /// <summary>
    /// DependencyInjectionExtensions
    /// </summary>
    public static class DependencyInjectionExtensions
    { 
        /// <summary>
        /// Adds services for FullRedis to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="config"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static IServiceCollection AddFullRedis(this IServiceCollection services,string config,int timeout = 15_000)
        {
            if (string.IsNullOrEmpty(config)) throw new ArgumentNullException(nameof(config)); 

            FullRedis.Register();

            var redis = new FullRedis { Timeout = 15_000, Log = XTrace.Log };
            redis.Init(config);

            services.AddSingleton<FullRedis>(redis);

            return services; 
        }


        /// <summary>
        /// Adds services for FullRedis to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="server"></param>
        /// <param name="psssword"></param>
        /// <param name="db"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static IServiceCollection AddFullRedis(this IServiceCollection services, string server,string psssword, int db, int timeout = 15_000)
        {
            if (string.IsNullOrEmpty(server)) throw new ArgumentNullException(nameof(server));

            FullRedis.Register();

            var redis = new FullRedis(server,psssword,db);

            services.AddSingleton<FullRedis>(redis);

            return services;
        }


    } 

}
# endif