using Benchmark;
using BenchmarkDotNet.Running;
using NewLife.Caching;
using Stardust;

//!!! 标准后台服务项目模板，新生命团队强烈推荐

// 启用控制台日志，拦截所有异常
XTrace.UseConsole();

// 初始化对象容器，提供依赖注入能力
var services = ObjectContainer.Current;
services.AddSingleton(XTrace.Log);

// 配置星尘。自动读取配置文件 config/star.config 中的服务器地址
var star = services.AddStardust();

var summary = BenchmarkRunner.Run<BasicBenchmark>();

Console.ReadLine();