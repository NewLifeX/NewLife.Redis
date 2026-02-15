using NewLife.Caching;
using NewLife.Log;
using RedisSwitch.Models;
using RedisSwitch.Services;

XTrace.UseConsole();

var builder = WebApplication.CreateBuilder(args);

// 配置选项
var redisConfig = builder.Configuration.GetSection("Redis");
var switchOptions = builder.Configuration.GetSection("RedisSwitch").Get<RedisSwitchOptions>() ?? new RedisSwitchOptions();

// 初始化Redis
var redis = new FullRedis();
var connStr = $"server={redisConfig["Server"]}";
if (!String.IsNullOrEmpty(redisConfig["Password"]))
    connStr += $";password={redisConfig["Password"]}";
connStr += $";db={redisConfig["Db"]}";

redis.Timeout = redisConfig.GetValue<Int32>("Timeout", 15000);
redis.Init(connStr);
redis.Log = XTrace.Log;
#if DEBUG
redis.ClientLog = XTrace.Log;
#endif

builder.Services.AddSingleton(redis);
builder.Services.AddSingleton(switchOptions);
builder.Services.AddSingleton<RedisQueueService>();

// 添加HTTP客户端
builder.Services.AddHttpClient();

// 添加后台服务
builder.Services.AddHostedService<MessageConsumerService>();

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
