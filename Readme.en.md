# NewLife.Redis - Redis Client Component

![GitHub top language](https://img.shields.io/github/languages/top/newlifex/newlife.redis?logo=github)
![GitHub License](https://img.shields.io/github/license/newlifex/newlife.redis?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/newlife.redis?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/newlife.redis?logo=nuget)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/newlife.redis?label=dev%20nuget&logo=nuget)


`NewLife.Redis` is a Redis client component designed for high-performance real-time big data processing.  
The Redis protocol is implemented in the Redis/RedisClient located in the [X component](https://github.com/NewLifeX/X), and this library extends it with advanced features such as list structures, hash structures, and queues.

Source: https://github.com/NewLifeX/NewLife.Redis  
Nuget: NewLife.Redis  
Tutorial: [https://newlifex.com/core/redis](https://newlifex.com/core/redis)

---

### Features
* Widely used for real-time big data processing at ZTO since 2017. More than 200 Redis instances have been running stably for over a year, processing nearly 100 million package data entries daily, with 8 billion calls per day.
* Low latency, with Get/Set operations averaging 200-600μs (including round-trip network communication).
* High throughput with an in-built connection pool, supporting up to 100,000 concurrent connections.
* High performance, supports binary serialization.

---

### Redis Experience Sharing
* Deploy multiple instances on Linux, with the number of instances equal to the number of processors. Each instance's maximum memory equals the physical memory of the machine to avoid memory overflow in a single instance.
* Store massive data (over 1 billion entries) across multiple instances by hashing keys (Crc16/Crc32), greatly improving read and write performance.
* Use binary serialization instead of common JSON serialization.
* Design the size of each key-value pair carefully, including but not limited to batch retrievals. The goal is to keep each network packet around 1.4k bytes to reduce communication overhead.
* Redis Get/Set operations average 200-600μs (including round-trip network communication). Use this as a benchmark to assess the network environment and Redis client components.
* Use pipelining to combine a batch of commands.
* Redis’s main performance bottlenecks are serialization, network bandwidth, and memory size. Excessive use can also cause processor bottlenecks.
* Other optimizations can be explored.

The above experiences come from over a year of stable operation with over 300 instances and more than 4TB of space, ordered by importance and should be applied as needed based on the scenario.

---

### Recommended Usage
It is recommended to use the Singleton pattern. Redis internally uses a connection pool and supports multi-threaded concurrent access.
```csharp
public static class RedisHelper
{
    /// <summary>
    /// Redis instance
    /// </summary>
    public static FullRedis redisConnection { get; set; } = new FullRedis("127.0.0.1:6379", "123456", 4);
}

Console.WriteLine(RedisHelper.redisConnection.Keys);
``` 

---

### Basic Redis
The Redis implementation follows the standard protocol and basic string operations, with the complete implementation provided by the independent open-source project [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis).  
It uses a connection pool and synchronous blocking architecture, offering ultra-low latency (200-600μs) and extremely high throughput.  
It is widely used in real-time big data processing in the logistics industry and has been validated with daily call volumes of 10 billion.

```csharp
// Instantiate Redis, default port 6379 can be omitted, two ways to write the password
//var rds = new FullRedis("127.0.0.1", null, 7);
var rds = new FullRedis("127.0.0.1:6379", "pass", 7);
//var rds = new FullRedis();
//rds.Init("server=127.0.0.1:6379;password=pass;db=7");
rds.Log = XTrace.Log;
```

### Basic Operations
Before performing basic operations, we need to do some preparation:
+ Create a new console project and add `XTrace.UseConsole();` at the beginning of the main function to easily view debug logs.
+ Before testing specific code, ensure you have instantiated either MemoryCache or Redis.
+ Prepare a model class `User`:
```csharp
class User
{
    public String Name { get; set; }
    public DateTime CreateTime { get; set; }
}
```

Add, Delete, Update, Query:
```csharp
var rds = new FullRedis("127.0.0.1", null, 7);
rds.Log = XTrace.Log;
rds.ClientLog = XTrace.Log; // Debug log. Comment out for production use.
var user = new User { Name = "NewLife", CreateTime = DateTime.Now };
rds.Set("user", user, 3600);
var user2 = rds.Get<User>("user");
XTrace.WriteLine("Json: {0}", user2.ToJson());
XTrace.WriteLine("Json: {0}", rds.Get<String>("user"));
if (rds.ContainsKey("user")) XTrace.WriteLine("Exists!");
rds.Remove("user");
```

Execution Result:
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
14:14:26.068  1 N - Exists!
14:14:26.069  1 N - DEL user
14:14:26.070  1 N - => 1
```

When saving complex objects, the default serialization method is JSON. Therefore, when retrieving the result as a string, you'll find it is in JSON format.  
Redis strings are essentially binary data with length prefixes, where [53] indicates 53 bytes of binary data.

### Collection Operations
GetAll/SetAll are commonly used batch operations in Redis, allowing you to get or set multiple keys simultaneously, often achieving more than 10x throughput.

Batch operations:
```csharp
var rds = new FullRedis("127.0.0.1", null, 7);
rds.Log = XTrace.Log;
rds.ClientLog = XTrace.Log; // Debug log. Comment out for production use.
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

Execution Result:
```csharp
MSET name NewLife time 2018-09-25 15:56:26 count 1234
=> OK
EXPIRE name 120
EXPIRE time 120
EXPIRE count 120
MGET name time count
name=NewLife,time=2018-09-25 15:56:26,count=1234
```

In collection operations, there are also `GetList/GetDictionary/GetQueue/GetSet` for various types of collections like Redis lists, hashes, queues, and sets.  
The basic Redis version does not support these collections, but the full version [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) does, while MemoryCache directly supports them.

### Advanced Operations
+ Add: Adds a key if it does not exist, returns false if the key already exists.  
+ Replace: Replaces an existing value with a new value and returns the old value.  
+ Increment: Increments a value atomically.  
+ Decrement: Decrements a value atomically.

Advanced operations:
```csharp
var rds = new FullRedis("127.0.0.1", null, 7);
rds.Log = XTrace.Log;
rds.ClientLog = XTrace.Log; // Debug log. Comment out for production use.
var flag = rds.Add("count", 5678);
XTrace.WriteLine(flag ? "Add Success" : "Add Failed");
var ori = rds.Replace("count", 777);
var count = rds.Get<Int32>("count");
XTrace.WriteLine("count changed from {0} to {1}", ori, count);

rds.Increment("count", 11);
var count2 = rds.Decrement("count", 10);
XTrace.WriteLine("count={0}", count2);
```

Execution Result:
```csharp
SETNX count 5678
=> 0
Add Failed
GETSET count 777
=> 1234
GET count
=> 777
count changed from 1234 to 777
INCRBY count 11
=> 788
DECRBY count 10
=> 778
count=778
```

---

### Performance Testing

The **Bench** tool will perform pressure testing by dividing the workload into multiple groups based on the number of threads.  
Parameters:
- **rand**: Whether to randomly generate keys/values.
- **batch**: Batch size, which optimizes read/write operations using GetAll/SetAll.

By default, Redis sets **AutoPipeline=100**, meaning it automatically pipelines operations for better performance during read and write operations when there’s no batching.

---

### Redis's Siblings

Redis implements the **ICache** interface, with its sibling, **MemoryCache**, being an in-memory cache with a throughput in the tens of millions.  
It is highly recommended to design applications using the **ICache** interface. Use **MemoryCache** for small data, and when the data grows (to 100,000 entries), switch to **Redis** without needing to modify the business logic code.

### Redis Server Support

1. **Version 3.2 and above**:  
   From version 3.2 onwards, **FullRedis** supports all Redis versions.

2. **Stream Data Type**:  
   FullRedis supports the **Stream** data type, which was introduced in Redis 5.0, allowing the storage and manipulation of message streams.

3. **LUA Not Supported**:  
   FullRedis **does not support** LUA scripting, meaning you cannot execute Lua scripts through this client.
