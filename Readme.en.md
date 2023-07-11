# NewLife. Redis - redis client component

![GitHub top language](https://img.shields.io/github/languages/top/newlifex/newlife.redis?logo=github) ![GitHub License](https://img.shields.io/github/license/newlifex/newlife.redis?logo=github) ![Nuget Downloads](https://img.shields.io/nuget/dt/newlife.redis?logo=nuget) ![Nuget](https://img.shields.io/nuget/v/newlife.redis?logo=nuget) ![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/newlife.redis?label=dev%20nuget&amp;logo=nuget)

`NewLife.Redis` is a Redis client component targeting high-performance processing of big data real-time computations.  
Redis protocol base implementation Redis/RedisClient is located in [X components](https://github.com/NewLifeX/X), this library for the extension of the implementation, mainly to increase the list structure, hash structure, queues and other advanced features.  

Source : https://github.com/NewLifeX/NewLife.Redis
Nuget: NewLife.Redis
Tutorial: [https://newlifex.com/core/redis](https://newlifex.com/core/redis)

---
### Characteristics
* Widely used in ZTO big data real-time computing, more than 200 Redis instances have been working stably for more than a year, processing nearly 100 million parcels of data per day, with an average daily call volume of 8 billion times
* Low latency, with Get/Set operations taking an average of 200 to 600 us (including round-trip network communications)
* High throughput, with its own connection pool, supporting up to 1,000 concurrencies
* High-performance, with support for binary serialization

---
### Redis experience sharing
* Multi-instance deployment on Linux, where the number of instances is equal to the number of processors, and the maximum memory of each instance is directly localized to the physical memory of the machine, avoiding the bursting of the memory of a single instance
* Store massive data (1 billion+) on multiple instances based on key hash (Crc16/Crc32) for exponential growth in read/write performance
* Use binary serialization instead of the usual Json serialization
* Rationalize the design of the Value size of each pair of Keys, including but not limited to the use of bulk acquisition, with the principle of keeping each network packet in the vicinity of 1.4k bytes to reduce the number of communications
* Redis client Get/Set operations took an average of 200-600 us (including round-trip network communication), which was used as a reference to evaluate the network environment and Redis client components
* Consolidation of a batch of commands using the pipeline Pipeline
* The main performance bottlenecks in Redis are serialization, network bandwidth, and memory size, with processors also bottlenecking during abuse
* Other searchable optimization techniques
The above experience, derived from more than 300 instances of more than 4T space more than a year of stable work experience, and in accordance with the degree of importance of the ranking of the order, according to the needs of the scene can be used as appropriate! 

---
### Recommended usage
It is recommended to use the singleton pattern, Redis has an internal connection pool and supports multi-threaded concurrent access.
``` csharp
public static class RedisHelper
{
    /// <summary>
    /// Redis Instance
    /// </summary>
    public static FullRedis redisConnection=FullRedis.Create("server=127.0.0.1:6379;password=123456;db=4");
}

Console.WriteLine(RedisHelper.redisConnection.Keys);
```

---
### Basic Redis
Redis implements the standard protocols as well as basic string manipulation , the complete implementation of the independent open source project [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) provides .  
Adopting connection pooling and synchronous blocking architecture, it features ultra-low latency (200~600us) and ultra-high throughput.  
It should be widely used in the real-time calculation of big data in the logistics industry, and has been verified by 10 billion calls per day.  

```csharp 
// Instantiate Redis, the default port 6379 can be omitted, and the password can be written in two ways
//var rds = new FullRedis("127.0.0.1", null, 7);
var rds = new FullRedis("127.0.0.1:6379", "pass", 7);
//var rds = new FullRedis();
//rds.Init("server=127.0.0.1:6379;password=pass;db=7");
rds.Log = XTrace.Log; // Debug log. Comments for formal use
```

### Basic operations
Before the basic operation, let's do some preparation:
+ Create a new console project and add `XTrace.UseConsole();` at the beginning of the entry function to make it easier to view the debug logs.
+ Before you can test the code, you need to add the code that instantiates MemoryCache or Redis.
+ Prepare a model class User
``` csharp
class User
{
    public String Name { get; set; }
    public DateTime CreateTime { get; set; }
}
```

Add, delete and check:
``` csharp
var rds = new FullRedis("127.0.0.1", null, 7);
var user = new User { Name = "NewLife", CreateTime = DateTime.Now };
rds.Set("user", user, 3600);
var user2 = rds.Get<User>("user");
XTrace.WriteLine("Json: {0}", user2.ToJson());
XTrace.WriteLine("Json: {0}", rds.Get<String>("user"));
if (rds.ContainsKey("user")) XTrace.WriteLine("exists!");
rds.Remove("user");
``` 

Implementation results:
``` csharp
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

When saving complex objects, Json serialization is used by default, so above you can get the results back by string and find exactly the Json string.  
Redis strings are, essentially, binary data with a length prefix; [53] represents a 53-byte length piece of binary data. 

### Collection operations
GetAll/SetAll is a very common batch operation on Redis to get or set multiple keys at the same time, typically with 10x more throughput.   

Batch operation:
``` csharp
var rds = new FullRedis("127.0.0.1", null, 7);
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

Implementation results:
``` csharp
MSET name NewLife time 2018-09-25 15:56:26 count 1234
=> OK
EXPIRE name 120
EXPIRE time 120
EXPIRE count 120
MGET name time count
name=NewLife,time=2018-09-25 15:56:26,count=1234
``` 

There are also `GetList/GetDictionary/GetQueue/GetSet` four types of collections inside the set operation, which represent Redis lists, hashes, queues, Set collections, and so on.  
The base version of Redis does not support these four collections, the full version [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) does, and MemoryCache supports them directly.  

### Advanced operations
+ Add Adds the key when it does not exist, and returns false when it already exists.
+ Replace Replacement replaces the existing value with the new value and returns the old value.  
+ Increment Accumulation, atomic operation
+ Decrement, atomic operation

Advanced Operations:
``` csharp
var rds = new FullRedis("127.0.0.1", null, 7);
var flag = rds.Add("count", 5678);
XTrace.WriteLine(flag ? "Add Success" : "Add Failure");
var ori = rds.Replace("count", 777);
var count = rds.Get<Int32>("count");
XTrace.WriteLine("count replaced by {0} with {1}", ori, count);

rds.Increment("count", 11);
var count2 = rds.Decrement("count", 10);
XTrace.WriteLine("count={0}", count2);
``` 

Implementation results:
``` csharp
SETNX count 5678
=> 0
Add failed
GETSET count 777
=> 1234
GET count
=> 777
Replace count with 777 instead of 1234
INCRBY count 11
=> 788
DECRBY count 10
=> 778
count=778
``` 

### Performance testing
Bench conducts stress tests on additions, deletions, and modifications in multiple groups based on the number of threads.    
rand parameter, whether to generate random key/value.
batch Batch size, perform read and write operations in batches, optimized with GetAll/SetAll.  

Redis sets AutoPipeline=100 by default to turn on pipeline operations when there is no batching, optimized for add/delete.  

### Siblings of Redis ###
Redis implements the ICache interface, its twin MemoryCache, an in-memory cache with a ten million throughput rate.  
Each application is strongly recommended to use ICache interface coding design and MemoryCache implementation for small data;
After the data increase (100,000), switch to Redis implementation without modifying the business code.