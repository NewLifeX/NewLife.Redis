using System;
using System.Collections.Generic;
using System.Globalization;
using NewLife;
using NewLife.Caching;
using NewLife.Log;
using Xunit;

namespace XUnitTest;

/// <summary>本地化测试标记特性。invariant 全球化模式下自动跳过</summary>
public class LocalizationFactAttribute : FactAttribute
{
    public LocalizationFactAttribute()
    {
        if (!LocalizationTests.CulturesAvailable)
            Skip = "当前运行时仅支持 invariant 全球化，跳过依赖命名区域的本地化测试。";
    }
}

/// <summary>本地化集合。串行执行，避免修改全局语言影响并行用例</summary>
[CollectionDefinition("Localization", DisableParallelization = true)]
public class LocalizationCollection { }

/// <summary>运行时本地化测试</summary>
[Collection("Localization")]
public class LocalizationTests
{
    internal static Boolean CulturesAvailable { get; } = CheckCultures();

    private static readonly String[] _cultures =
    [
        "ar", "bn", "cs", "da", "de", "el", "es", "fa", "fi", "fr", "he", "hi", "hu", "id", "it", "ja",
        "ko", "ms", "nb", "nl", "pl", "pt", "ro", "ru", "sv", "th", "tr", "uk", "vi", "zh-Hans", "zh-Hant",
    ];

    private static Boolean CheckCultures()
    {
        try
        {
            _ = new CultureInfo("zh-Hans");
            _ = new CultureInfo("en-US");
            _ = new CultureInfo("tlh");
            return true;
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }

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

    [LocalizationFact]
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

    [LocalizationFact]
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

    [LocalizationFact]
    public void LogTraceTagIsTranslated()
    {
        UseCulture("zh-Hans", "zh-Hans");
        try
        {
            var log = new CaptureLog();
            var redis = new Redis { Name = "r1", Log = log, Tracer = new DefaultTracer { MaxSamples = 100 } };
            using var span = redis.Tracer.NewSpan("test");

            redis.WriteLog("Using Redis server: {0}", "127.0.0.1:6379");

            Assert.Single(log.Messages);
            Assert.Equal("[r1]使用 Redis 服务器：127.0.0.1:6379", log.Messages[0]);
            Assert.Contains("使用 Redis 服务器", span.Tag);
            Assert.Contains("127.0.0.1:6379", span.Tag);
        }
        finally
        {
            UseCulture(null, null);
        }
    }

    [LocalizationFact]
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

    [LocalizationFact]
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

    [LocalizationFact]
    public void AllLocaleTablesAreRegistered()
    {
        // 触发扩展程序集加载，确保两套词表都已注册
        Assert.NotNull(typeof(NewLife.Redis.Extensions.RedisXmlRepository).Assembly);

        foreach (var culture in _cultures)
        {
            UseCulture(culture, culture);
            try
            {
                var text = Localization.Translate("Using Redis server: {0}", "127.0.0.1:6379");
                Assert.NotEqual("Using Redis server: 127.0.0.1:6379", text);
                Assert.Contains("127.0.0.1:6379", text);

                var ext = Localization.Translate("Redis is not configured. Specify a connection string named RedisCache in the configuration file or configuration center.");
                Assert.NotEqual("Redis is not configured. Specify a connection string named RedisCache in the configuration file or configuration center.", ext);

                var redlock = Localization.Translate("RedLock.Acquire key={0} token={1} success={2}/{3} elapsed={4}ms validity={5}ms", "k", "t", 1, 2, 3, 4);
                Assert.NotEqual("RedLock.Acquire key=k token=t success=1/2 elapsed=3ms validity=4ms", redlock);
                Assert.Contains("k", redlock);
            }
            finally
            {
                UseCulture(null, null);
            }
        }
    }

    [LocalizationFact]
    public void RedLockLogMessagesAreTranslated()
    {
        UseCulture("zh-Hans", "zh-Hans");
        try
        {
            var acquired = Localization.Translate("RedLock.Acquire key={0} token={1} success={2}/{3} elapsed={4}ms validity={5}ms", "lock", "token", 2, 3, 10, 5000);
            Assert.Contains("成功", acquired);
            Assert.Contains("耗时", acquired);
            Assert.Contains("lock", acquired);

            var timeout = Localization.Translate("RedLock.Acquire key={0} timeout={1}ms elapsed={2}ms success={3}/{4}", "lock", 100, 10, 1, 3);
            Assert.Contains("超时", timeout);
            Assert.Contains("成功", timeout);
        }
        finally
        {
            UseCulture(null, null);
        }
    }

    [LocalizationFact]
    public void ExtensionsMessagesAreTranslated()
    {
        UseCulture("zh-Hans", null);
        try
        {
            // 触发扩展程序集加载，执行源生成器产出的模块初始化器
            Assert.NotNull(typeof(NewLife.Redis.Extensions.RedisXmlRepository).Assembly);

            var text = Localization.Translate("Redis is not configured. Specify a connection string named RedisCache in the configuration file or configuration center.");

            Assert.Equal("未配置 Redis。请在配置文件或配置中心指定名为 RedisCache 的连接字符串。", text);
        }
        finally
        {
            UseCulture(null, null);
        }
    }

    [LocalizationFact]
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
