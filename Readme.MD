## 新生命Redis组件
`NewLife.Redis` 是一个Redis客户端组件，以高性能处理大数据实时计算为目标。  
Redis协议基础实现Redis/RedisClient位于[X组件](https://github.com/NewLifeX/X)，本库为扩展实现，主要增加列表结构、哈希结构、队列等高级功能。  

源码： https://github.com/NewLifeX/NewLife.Redis  
Nuget：NewLife.Redis  

---
### 特性
* 在ZTO大数据实时计算广泛应用，200多个Redis实例稳定工作一年多，每天处理近1亿包裹数据，日均调用量80亿次
* 低延迟，Get/Set操作平均耗时200~600us（含往返网络通信）
* 大吞吐，自带连接池，最大支持1000并发 
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

## 新生命开源项目矩阵
各项目默认支持net4.5/net4.0/netstandard2.0  

|                               项目                               | 年份  |  状态  | .NET Core | 说明                                                |
| :--------------------------------------------------------------: | :---: | :----: | :-------: | --------------------------------------------------- |
|                             基础组件                             |       |        |           | 支撑其它中间件以及产品项目                          |
|          [NewLife.Core](https://github.com/NewLifeX/X)           | 2002  | 维护中 |     √     | 算法、日志、网络、RPC、序列化、缓存、多线程         |
|              [XCode](https://github.com/NewLifeX/X)              | 2005  | 维护中 |     √     | 数据中间件，MySQL、SQLite、SqlServer、Oracle        |
|      [NewLife.Net](https://github.com/NewLifeX/NewLife.Net)      | 2005  | 维护中 |     √     | 网络库，千万级吞吐率，学习gRPC、Thrift              |
|     [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube)     | 2010  | 维护中 |     √     | Web魔方，企业级快速开发框架，集成OAuth              |
|    [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent)    | 2008  | 维护中 |     √     | 服务管理框架，Windows服务、Linux的Systemd           |
|                              中间件                              |       |        |           | 对接各知名中间件平台                                |
|    [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis)    | 2017  | 维护中 |     √     | Redis客户端，微秒级延迟，百亿级项目验证             |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | 2018  | 维护中 |     √     | 支持Apache RocketMQ和阿里云消息队列，十亿级项目验证 |
|     [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT)     | 2019  | 维护中 |     √     | 物联网消息协议，客户端支持阿里云物联网              |
|     [NewLife.LoRa](https://github.com/NewLifeX/NewLife.LoRa)     | 2016  | 维护中 |     √     | 超低功耗的物联网远程通信协议LoRaWAN                 |
|   [NewLife.Thrift](https://github.com/NewLifeX/NewLife.Thrift)   | 2019  | 维护中 |     √     | Thrift协议实现                                      |
|     [NewLife.Hive](https://github.com/NewLifeX/NewLife.Hive)     | 2019  | 维护中 |     √     | 纯托管读写Hive，Hadoop数据仓库，基于Thrift协议      |
|             [NoDb](https://github.com/NewLifeX/NoDb)             | 2017  | 开发中 |     √     | NoSQL数据库，百万级kv读写性能，持久化               |
|      [NewLife.Ftp](https://github.com/NewLifeX/NewLife.Ftp)      | 2008  | 维护中 |     √     | Ftp客户端实现                                       |
|                             产品平台                             |       |        |           | 产品平台级，编译部署即用，个性化自定义              |
|           [AntJob](https://github.com/NewLifeX/AntJob)           | 2019  | 维护中 |     √     | 蚂蚁调度系统，大数据实时计算平台                    |
|         [Stardust](https://github.com/NewLifeX/Stardust)         | 2018  | 维护中 |     √     | 星尘，微服务平台，分布式平台                        |
|            [XLink](https://github.com/NewLifeX/XLink)            | 2016  | 维护中 |     √     | 物联网云平台                                        |
|           [XProxy](https://github.com/NewLifeX/XProxy)           | 2005  | 维护中 |     √     | 产品级反向代理                                      |
|          [XScript](https://github.com/NewLifeX/XScript)          | 2010  | 维护中 |     ×     | C#脚本引擎                                          |
|          [SmartOS](https://github.com/NewLifeX/SmartOS)          | 2014  | 维护中 |   C++11   | 嵌入式操作系统，完全独立自主，ARM Cortex-M芯片架构  |
|         [GitCandy](https://github.com/NewLifeX/GitCandy)         | 2015  | 维护中 |     ×     | Git管理系统                                         |
|                               其它                               |       |        |           |                                                     |
|           [XCoder](https://github.com/NewLifeX/XCoder)           | 2006  | 维护中 |     √     | 码神工具，开发者必备                                |
|        [XTemplate](https://github.com/NewLifeX/XTemplate)        | 2008  | 维护中 |     √     | 模版引擎，T4(Text Template)语法                     |
|       [X组件 .NET2.0](https://github.com/NewLifeX/X_NET20)       | 2002  | 存档中 |  .NET2.0  | 日志、网络、RPC、序列化、缓存、Windows服务、多线程  |

---
### 基础 Redis
Redis实现标准协议以及基础字符串操作，完整实现由独立开源项目[NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis)提供。  
采取连接池加同步阻塞架构，具有超低延迟（200~600us）以及超高吞吐量的特点。  
在物流行业大数据实时计算中广泛应有，经过日均100亿次调用量验证。  

```csharp
// 实例化Redis，默认端口6379可以省略，密码有两种写法
//var rds = Redis.Create("127.0.0.1", 7);
var rds = Redis.Create("pass@127.0.0.1:6379", 7);
//var rds = Redis.Create("server=127.0.0.1:6379;password=pass", 7);
rds.Log = XTrace.Log; // 调试日志。正式使用时注释
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
