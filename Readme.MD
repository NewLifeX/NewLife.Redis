# NewLife.Redis - Redis客户端组件

![GitHub top language](https://img.shields.io/github/languages/top/newlifex/newlife.redis?logo=github)
![GitHub License](https://img.shields.io/github/license/newlifex/newlife.redis?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/newlife.redis?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/newlife.redis?logo=nuget)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/newlife.redis?label=dev%20nuget&logo=nuget)

## [[English]](https://github.com/NewLifeX/NewLife.Redis/blob/master/Readme.en.md)

`NewLife.Redis` 是一个Redis客户端组件，以高性能处理大数据实时计算为目标。  
Redis协议基础实现Redis/RedisClient位于Redis类，FullRedis为扩展实现，主要增加列表结构、哈希结构、队列等高级功能。  

源码： https://github.com/NewLifeX/NewLife.Redis  
Nuget：NewLife.Redis  
教程：[https://newlifex.com/core/redis](https://newlifex.com/core/redis)  

---
### 特性
* 2017年在ZTO大数据实时计算广泛应用，200多个Redis实例稳定工作一年多，每天处理近1亿条包裹数据，日均调用量80亿次
* 低延迟，Get/Set操作平均耗时200~600us（含往返网络通信）
* 大吞吐，自带连接池，最大支持100000并发 
* 高性能，支持二进制序列化

---
### Redis经验分享
* 在Linux上多实例部署，实例个数等于处理器个数，各实例最大内存直接为本机物理内存，避免单个实例内存撑爆
* 把海量数据（10亿+）根据key哈希（Crc16/Crc32）存放在多个实例上，读写性能成倍增长 
* 采用二进制序列化，而非常见Json序列化 
* 合理设计每一对Key的Value大小，包括但不限于使用批量获取，原则是让每次网络包控制在1.4k字节附近，减少通信次数
* Redis客户端的Get/Set操作平均耗时200~600us（含往返网络通信），以此为参考评估网络环境和Redis客户端组件
* 使用管道Pipeline合并一批命令
* Redis的主要性能瓶颈是序列化、网络带宽和内存大小，滥用时处理器也会达到瓶颈
* 其它可查优化技巧
以上经验，源自于300多个实例4T以上空间一年多稳定工作的经验，并按照重要程度排了先后顺序，可根据场景需要酌情采用！

---
### 推荐用法
推荐使用单例模式,Redis内部有连接池并且支持多线程并发访问
```csharp
public static class RedisHelper
{
    /// <summary>
    /// Redis实例
    /// </summary>
    public static FullRedis redisConnection { get; set; } = new FullRedis("127.0.0.1:6379", "123456", 4);
}

Console.WriteLine(RedisHelper.redisConnection.Keys);
``` 

---
### 基础 Redis
Redis实现标准协议以及基础字符串操作，完整实现由独立开源项目[NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis)提供。  
采取连接池加同步阻塞架构，具有超低延迟（200~600us）以及超高吞吐量的特点。  
在物流行业大数据实时计算中广泛应有，经过日均100亿次调用量验证。  

```csharp
// 实例化Redis，默认端口6379可以省略，密码有两种写法
//var rds = new FullRedis("127.0.0.1", null, 7);
var rds = new FullRedis("127.0.0.1:6379", "pass", 7);
//var rds = new FullRedis();
//rds.Init("server=127.0.0.1:6379;password=pass;db=7");
rds.Log = XTrace.Log;
```

### 基本操作
在基本操作之前，我们先做一些准备工作：
+ 新建控制台项目，并在入口函数开头加上 `XTrace.UseConsole();` ，这是为了方便查看调试日志  
+ 具体测试代码之前，需要加上前面MemoryCache或Redis的实例化代码   
+ 准备一个模型类User  
```csharp
class User
{
    public String Name { get; set; }
    public DateTime CreateTime { get; set; }
}
```

添删改查：  
```csharp
var rds = new FullRedis("127.0.0.1", null, 7);
rds.Log = XTrace.Log;
rds.ClientLog = XTrace.Log; // 调试日志。正式使用时注释
var user = new User { Name = "NewLife", CreateTime = DateTime.Now };
rds.Set("user", user, 3600);
var user2 = rds.Get<User>("user");
XTrace.WriteLine("Json: {0}", user2.ToJson());
XTrace.WriteLine("Json: {0}", rds.Get<String>("user"));
if (rds.ContainsKey("user")) XTrace.WriteLine("存在！");
rds.Remove("user");
```

执行结果：  
```csharp
14:14:25.990  1 N - SELECT 7
14:14:25.992  1 N - => OK
14:14:26.008  1 N - SETEX user 3600 [53]
14:14:26.021  1 N - => OK
14:14:26.042  1 N - GET user
14:14:26.048  1 N - => [53]
14:14:26.064  1 N - GET user
14:14:26.065  1 N - => [53]
14:14:26.066  1 N - Json: {"Name":"NewLife","CreateTime":"2018-09-25 14:14:25"}
14:14:26.067  1 N - EXISTS user
14:14:26.068  1 N - => 1
14:14:26.068  1 N - 存在！
14:14:26.069  1 N - DEL user
14:14:26.070  1 N - => 1
```

保存复杂对象时，默认采用Json序列化，所以上面可以按字符串把结果取回来，发现正是Json字符串。  
Redis的strings，实质上就是带有长度前缀的二进制数据，[53]表示一段53字节长度的二进制数据。

### 集合操作
GetAll/SetAll 在Redis上是很常用的批量操作，同时获取或设置多个key，一般有10倍以上吞吐量。   

批量操作：
```csharp
var rds = new FullRedis("127.0.0.1", null, 7);
rds.Log = XTrace.Log;
rds.ClientLog = XTrace.Log; // 调试日志。正式使用时注释
var dic = new Dictionary<String, Object>
{
    ["name"] = "NewLife",
    ["time"] = DateTime.Now,
    ["count"] = 1234
};
rds.SetAll(dic, 120);

var vs = rds.GetAll<String>(dic.Keys);
XTrace.WriteLine(vs.Join(",", e => $"{e.Key}={e.Value}"));
```

执行结果：
```csharp
MSET name NewLife time 2018-09-25 15:56:26 count 1234
=> OK
EXPIRE name 120
EXPIRE time 120
EXPIRE count 120
MGET name time count
name=NewLife,time=2018-09-25 15:56:26,count=1234
```

集合操作里面还有 `GetList/GetDictionary/GetQueue/GetSet` 四个类型集合，分别代表Redis的列表、哈希、队列、Set集合等。  
基础版Redis不支持这四个集合，完整版[NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis)支持，MemoryCache则直接支持。  

### 高级操作
+ Add 添加，当key不存在时添加，已存在时返回false。  
+ Replace 替换，替换已有值为新值，返回旧值。  
+ Increment 累加，原子操作  
+ Decrement 递减，原子操作  

高级操作：  
```csharp
var rds = new FullRedis("127.0.0.1", null, 7);
rds.Log = XTrace.Log;
rds.ClientLog = XTrace.Log; // 调试日志。正式使用时注释
var flag = rds.Add("count", 5678);
XTrace.WriteLine(flag ? "Add成功" : "Add失败");
var ori = rds.Replace("count", 777);
var count = rds.Get<Int32>("count");
XTrace.WriteLine("count由{0}替换为{1}", ori, count);

rds.Increment("count", 11);
var count2 = rds.Decrement("count", 10);
XTrace.WriteLine("count={0}", count2);
```

执行结果：  
```csharp
SETNX count 5678
=> 0
Add失败
GETSET count 777
=> 1234
GET count
=> 777
count由1234替换为777
INCRBY count 11
=> 788
DECRBY count 10
=> 778
count=778
```

### 性能测试
Bench 会分根据线程数分多组进行添删改压力测试。    
rand 参数，是否随机产生key/value。  
batch 批大小，分批执行读写操作，借助GetAll/SetAll进行优化。  

Redis默认设置AutoPipeline=100，无分批时打开管道操作，对添删改优化。  

### Redis的兄弟姐妹
Redis实现ICache接口，它的孪生兄弟MemoryCache，内存缓存，千万级吞吐率。  
各应用强烈建议使用ICache接口编码设计，小数据时使用MemoryCache实现；  
数据增大（10万）以后，改用Redis实现，不需要修改业务代码。  

## 快速拥有

​	使用NewLife组件的最简便方式是从Nuget引用，例如在项目Nuget管理中搜索`NewLife.Redis` 并引入。

​	NewLife组件由社区共创20多年，使用MIT开源协议，**任何人可任意修改并再次发行**（无需声明来源）！许多企业基于此构建内部开发框架时，甚至可通过批量替换源码中所有`NewLife`字符串为贵公司名实现私有化定制。

​	团队始终秉承开放态度，不仅支持VisualStudio（最新正式版）打开解决方案编译，也兼容`dotnet build`命令行编译，项目文件摒弃复杂功能以追求简单易用，真正做到开箱即用。

​	我们公开强命名证书`newlife.snk`以支持独自编译替换程序集。



​	命令行中运行以下命令快速体验NewLife组件：

```
dotnet new install NewLife.Templates
dotnet new nconsole --name test
cd test
dotnet run
```


## 新生命项目矩阵
各项目默认支持net9.0/netstandard2.1/netstandard2.0/net4.62/net4.5，旧版（2024.0801）支持net4.0/net2.0  

|                               项目                               | 年份  | 说明                                                                                        |
| :--------------------------------------------------------------: | :---: | ------------------------------------------------------------------------------------------- |
|                             基础组件                             |       | 支撑其它中间件以及产品项目                                                                  |
|          [NewLife.Core](https://github.com/NewLifeX/X)           | 2002  | 核心库，日志、配置、缓存、网络、序列化、APM性能追踪                                         |
|    [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode)    | 2005  | 大数据中间件，单表百亿级，MySql/SQLite/SqlServer/Oracle/PostgreSql/达梦，自动分表，读写分离 |
|      [NewLife.Net](https://github.com/NewLifeX/NewLife.Net)      | 2005  | 网络库，单机千万级吞吐率（2266万tps），单机百万级连接（400万Tcp长连接）                     |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | 2011  | 协议通信库，提供CS应用通信框架，支持Http/RPC通信框架，高吞吐，物联网设备低开销易接入        |
|     [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube)     | 2010  | 魔方快速开发平台，集成了用户权限、SSO登录、OAuth服务端等，单表100亿级项目验证               |
|    [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent)    | 2008  | 服务管理组件，把应用安装成为操作系统守护进程，Windows服务、Linux的Systemd                   |
|     [NewLife.Zero](https://github.com/NewLifeX/NewLife.Zero)     | 2020  | Zero零代脚手架，基于NewLife组件生态的项目模板NewLife.Templates，Web、WebApi、Service        |
|                              中间件                              |       | 对接知名中间件平台                                                                          |
|    [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis)    | 2017  | Redis客户端，微秒级延迟，百万级吞吐，丰富的消息队列，百亿级数据量项目验证                   |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | 2018  | RocketMQ纯托管客户端，支持Apache RocketMQ和阿里云消息队列，十亿级项目验                     |
|     [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT)     | 2019  | 物联网消息协议，MqttClient/MqttServer，客户端支持阿里云物联网                               |
|      [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT)      | 2022  | IoT标准库，定义物联网领域的各种通信协议标准规范                                             |
|   [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus)   | 2022  | ModbusTcp/ModbusRTU/ModbusASCII，基于IoT标准库实现，支持ZeroIoT平台和IoTEdge网关            |
|  [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens)  | 2022  | 西门子PLC协议，基于IoT标准库实现，支持IoT平台和IoTEdge                                      |
|      [NewLife.Map](https://github.com/NewLifeX/NewLife.Map)      | 2022  | 地图组件库，封装百度地图、高德地图、腾讯地图、天地图                                        |
|    [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio)    | 2023  | 音频编解码库，PCM/ADPCMA/G711A/G722U/WAV/AAC                                                |
|                             产品平台                             |       | 产品平台级，编译部署即用，个性化自定义                                                      |
|         [Stardust](https://github.com/NewLifeX/Stardust)         | 2018  | 星尘，分布式服务平台，节点管理、APM监控中心、配置中心、注册中心、发布中心                   |
|           [AntJob](https://github.com/NewLifeX/AntJob)           | 2019  | 蚂蚁调度，分布式大数据计算平台（实时/离线），蚂蚁搬家分片思想，万亿级数据量项目验证         |
|      [NewLife.ERP](https://github.com/NewLifeX/NewLife.ERP)      | 2021  | 企业ERP，产品管理、客户管理、销售管理、供应商管理                                           |
|         [CrazyCoder](https://github.com/NewLifeX/XCoder)         | 2006  | 码神工具，众多开发者工具，网络、串口、加解密、正则表达式、Modbus、MQTT                      |
|           [EasyIO](https://github.com/NewLifeX/EasyIO)           | 2023  | 简易文件存储，支持分布式系统中文件集中存储。                                                |
|           [XProxy](https://github.com/NewLifeX/XProxy)           | 2005  | 产品级反向代理，NAT代理、Http代理                                                           |
|        [HttpMeter](https://github.com/NewLifeX/HttpMeter)        | 2022  | Http压力测试工具                                                                            |
|         [GitCandy](https://github.com/NewLifeX/GitCandy)         | 2015  | Git源代码管理系统                                                                           |
|          [SmartOS](https://github.com/NewLifeX/SmartOS)          | 2014  | 嵌入式操作系统，完全独立自主，支持ARM Cortex-M芯片架构                                      |
|          [SmartA2](https://github.com/NewLifeX/SmartA2)          | 2019  | 嵌入式工业计算机，物联网边缘网关，高性能.NET8主机，应用于工业、农业、交通、医疗             |
|                          FIoT物联网平台                          | 2020  | 物联网整体解决方案，建筑、环保、农业，软硬件及大数据分析一体化，单机十万级点位项目验证      |
|                        UWB高精度室内定位                         | 2020  | 厘米级（10~20cm）高精度室内定位，软硬件一体化，与其它系统联动，大型展厅项目验证             |



## 新生命开发团队
![XCode](https://newlifex.com/logo.png)  

新生命团队（NewLife）成立于2002年，是新时代物联网行业解决方案提供者，致力于提供软硬件应用方案咨询、系统架构规划与开发服务。  
团队主导的80多个开源项目已被广泛应用于各行业，Nuget累计下载量高达400余万次。  
团队开发的大数据中间件NewLife.XCode、蚂蚁调度计算平台AntJob、星尘分布式平台Stardust、缓存队列组件NewLife.Redis以及物联网平台FIoT，均成功应用于电力、高校、互联网、电信、交通、物流、工控、医疗、文博等行业，为客户提供了大量先进、可靠、安全、高质量、易扩展的产品和系统集成服务。  

我们将不断通过服务的持续改进，成为客户长期信赖的合作伙伴，通过不断的创新和发展，成为国内优秀的IoT服务供应商。  

`新生命团队始于2002年，部分开源项目具有20年以上漫长历史，源码库保留有2010年以来所有修改记录`  
网站：https://newlifex.com  
开源：https://github.com/newlifex  
QQ群：1600800/1600838  
微信公众号：  
![智能大石头](https://newlifex.com/stone.jpg)  
