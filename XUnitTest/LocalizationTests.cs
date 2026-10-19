using System;
using System.Collections.Generic;
using System.Globalization;
using NewLife;
using NewLife.Caching;
using NewLife.Log;
using Xunit;

namespace XUnitTest;

/// <summary>本地化集合。串行执行，避免修改全局语言影响并行用例</summary>
[CollectionDefinition("Localization", DisableParallelization = true)]
public class LocalizationCollection { }

/// <summary>运行时本地化测试</summary>
[Collection("Localization")]
public class LocalizationTests
{
    private sealed class CaptureLog : Logger
    {
        public List<String> Messages { get; } = [];

        protected override void OnWrite(LogLevel level, String format, params Object[] args) => Messages.Add(format);
    }

    private static void UseCulture(String culture, String logCulture)
    {
        Localization.Culture = culture == null ? null : new CultureInfo(culture);
        Localization.LogCulture = logCulture == null ? null : new CultureInfo(logCulture);
    }

    [Fact]
    public void ExceptionMessageIsTranslated()
    {
        UseCulture("zh-Hans", null);
        try
        {
            var redis = new Redis();
            var ex = Assert.Throws<NotSupportedException>(() => redis.GetDictionary<String>("key"));

            Assert.Equal("Redis未支持该功能，需要new FullRedis", ex.Message);
        }
        finally
        {
            UseCulture(null, null);
        }
    }

    [Fact]
    public void LogMessageIsTranslatedWithPrefix()
    {
        UseCulture("zh-Hans", "zh-Hans");
        try
        {
            var log = new CaptureLog();
            var redis = new Redis { Name = "r1", Log = log };

            redis.WriteLog("Using Redis server: {0}", "127.0.0.1:6379");

            Assert.Single(log.Messages);
            Assert.Equal("[r1]使用 Redis 服务器：127.0.0.1:6379", log.Messages[0]);
        }
        finally
        {
            UseCulture(null, null);
        }
    }

    [Fact]
    public void LogCultureIsIndependentFromUICulture()
    {
        UseCulture("zh-Hans", "en-US");
        try
        {
            var log = new CaptureLog();
            var redis = new Redis { Name = "r1", Log = log };

            redis.WriteLog("Using Redis server: {0}", "127.0.0.1:6379");

            Assert.Single(log.Messages);
            Assert.Equal("[r1]Using Redis server: 127.0.0.1:6379", log.Messages[0]);
        }
        finally
        {
            UseCulture(null, null);
        }
    }

    [Fact]
    public void PrefixedLogKeepsPrefixOutOfTranslation()
    {
        UseCulture("zh-Hans", "zh-Hans");
        try
        {
            var log = new CaptureLog();

            log.ErrorPrefixed("[topic/1]", "Error processing message: {0} {1}", "body", "error");

            Assert.Single(log.Messages);
            Assert.Equal("[topic/1]消息处理异常：body error", log.Messages[0]);
        }
        finally
        {
            UseCulture(null, null);
        }
    }

    [Fact]
    public void UnknownCultureFallsBackToEnglish()
    {
        UseCulture("tlh", "tlh");
        try
        {
            var log = new CaptureLog();
            var redis = new Redis { Name = "r1", Log = log };

            redis.WriteLog("Using Redis server: {0}", "127.0.0.1:6379");

            Assert.Single(log.Messages);
            Assert.Equal("[r1]Using Redis server: 127.0.0.1:6379", log.Messages[0]);
        }
        finally
        {
            UseCulture(null, null);
        }
    }
}
