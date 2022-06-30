# NewLife. Redis - redis client component

![GitHub top language](https://img.shields.io/github/languages/top/newlifex/newlife.redis?logo=github) ![GitHub License](https://img.shields.io/github/license/newlifex/newlife.redis?logo=github) ![Nuget Downloads](https://img.shields.io/nuget/dt/newlife.redis?logo=nuget) ![Nuget](https://img.shields.io/nuget/v/newlife.redis?logo=nuget) ![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/newlife.redis?label=dev%20nuget&amp;logo=nuget)

`NewLife. Redis`It is a redis client component, aiming at high-performance processing of big data real-time computing.  
Redis protocol basic implementation redis/redisclient is located in[X component](https://github.com/NewLifeX/X), this library is an extension implementation, mainly adding list structure, hash structure, queue and other advanced functions.

Source code:[https://github.com/NewLifeX/NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis)  
Nuget：NewLife. Redis  
course:[https://newlifex.com/core/redis](https://newlifex.com/core/redis)

* * *

### characteristic

- ZTO big data real-time computing is widely used. More than 200 redis instances have been working stably for more than a year, processing nearly 100million package data every day, and the average daily call volume is 8billion times
- Low delay, average time of get/set operation is 200~600us (including round-trip network communication)
- Large throughput, built-in connection pool, up to 1000 concurrent
- High performance, support binary serialization

* * *

### Redis experience sharing

- For multi instance deployment on Linux, the number of instances is equal to the number of processors, and the maximum memory of each instance is directly the physical memory of the machine to avoid the memory explosion of a single instance
- Store massive data (1billion +) on multiple instances according to key hash (crc16/crc32), and the read-write performance has doubled
- Binary serialization is adopted, while JSON serialization is not common
- Reasonably design the value size of each pair of keys, including but not limited to the use of batch acquisition. The principle is to control each network packet around 1.4k bytes and reduce the number of communications
- The average time of get/set operation of redis client is 200~600us (including round-trip network communication), which is used as a reference to evaluate the network environment and redis client components
- Merge a batch of commands using pipeline
- The main performance bottlenecks of redis are serialization, network bandwidth and memory size. The processor will also reach the bottleneck when abused
- Other verifiable optimization techniques
The above experience is derived from the experience of more than one year of stable work in more than 300 instances of 4T space. It is ranked in order of importance and can be adopted as appropriate according to the needs of the scene!

* * *

### Basic redis

Redis implements standard protocols and basic string operations, which are fully implemented by independent open source projects[NewLife. Redis](https://github.com/NewLifeX/NewLife.Redis)Provided.  
It adopts the connection pool and synchronous blocking architecture, which has the characteristics of ultra-low latency (200~600us) and ultra-high throughput.  
It is widely used in big data real-time computing in the logistics industry, and has been verified by an average of 10billion calls per day.

    // 实例化Redis，默认端口6379可以省略，密码有两种写法
    //var rds = new Redis("127.0.0.1", null, 7);
    var rds = new Redis("127.0.0.1:6379", "pass", 7);
    //var rds = new Redis();
    //rds.Init("server=127.0.0.1:6379;password=pass;db=7");
    rds.Log = XTrace.Log; // 调试日志。正式使用时注释

### basic operation 

Before the basic operation, we should make some preparations:

- Create a new console project and add`XTrace. UseConsole();`, this is to facilitate viewing the debug log

- Before specifically testing the code, you need to add the previous memorycache or redis instantiation code

- Prepare a model class user

- ```csharp
    cshar class User
    {
        public String Name { get; set; }
        public DateTime CreateTime { get; set; }
    }
    ```

Add, delete, modify and query:

```c#
var user = new User { Name = "NewLife", CreateTime = DateTime.Now };
rds.Set("user", user, 3600);
var user2 = rds.Get<User>("user");
XTrace.WriteLine("Json: {0}", user2.ToJson());
XTrace.WriteLine("Json: {0}", rds.Get<String>("user"));
if (rds.ContainsKey("user")) XTrace.WriteLine("存在！");
rds.Remove("user");
```

Execution result:

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

When saving complex objects, JSON serialization is used by default, so the results can be retrieved by string, and it is found that it is the JSON string.  
Redis' strings are actually binary data with a length prefix, [53] represents a 53 byte length of binary data.

### Collection operation

Getall/setall is a very common batch operation on redis. It can obtain or set multiple keys at the same time, which generally has a throughput of more than 10 times.

Batch operation:

```c#
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

Execution result:

    MSET name NewLife time 2018-09-25 15:56:26 count 1234
    => OK
    EXPIRE name 120
    EXPIRE time 120
    EXPIRE count 120
    MGET name time count
    name=NewLife,time=2018-09-25 15:56:26,count=1234

In the collection operation`GetList/GetDictionary/GetQueue/GetSet`Four types of collections represent redis' list, hash, queue, set collection, etc.  
The basic version of redis does not support these four sets. The full version[NewLife. Redis](https://github.com/NewLifeX/NewLife.Redis)Yes, memorycache directly supports.

### Advanced operations

- Add add: if the key does not exist, it will be added. If the key already exists, it will return false.
- Replace replace, replace the existing value with the new value, and return the old value.
- Increment accumulation, atomic operation
- Decrement, atomic operation

Advanced actions:

```c#
var flag = rds.Add("count", 5678);
XTrace.WriteLine(flag ? "Add success" : "Add fail");
var ori = rds.Replace("count", 777);
var count = rds.Get<Int32>("count");
XTrace.WriteLine("count由{0}替换为{1}", ori, count);

rds.Increment("count", 11);
var count2 = rds.Decrement("count", 10);
XTrace.WriteLine("count={0}", count2);
```

Execution result:

    SETNX count 5678
    => 0
    Add fail
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

### performance testing 

Bench will conduct the stress test of adding, deleting and modifying in groups according to the number of threads.  
Rand parameter, whether to generate key/value randomly.  
Batch batch size. Read / write operations are performed in batches. It is optimized with getall/setall.

The default setting of redis is autopipeline=100. When there is no batch, open the pipeline to optimize the addition, deletion and modification.

### Redis' brothers and sisters

Redis implements the Icache interface. Its twin brother, memorycache, has a memory cache and a throughput of tens of millions.  
It is strongly recommended that all applications use the Icache interface coding design, and use memorycache for small data;  
After the data is increased by 100000 yuan, redis is used instead of modifying the business code.

## New life project matrix

Each project supports net6.0/netstandard2.1 by default, and the old version (2021.1225) supports net5.0/netstandard2.0/net4.5/net4.0/net2.0
 <figure><table>
<thead>
<tr><th style='text-align:center;'>project</th><th style='text-align:center;'>particular year</th><th style='text-align:center;'>state</th><th style='text-align:center;'>. NET6</th><th>explain</th></tr></thead>
<tbody><tr><td style='text-align:center;'>Basic components</td><td style='text-align:center;'>&nbsp;</td><td style='text-align:center;'>&nbsp;</td><td style='text-align:center;'>&nbsp;</td><td>Support other middleware and product projects</td></tr><tr><td style='text-align:center;'><a href='https://github.com/NewLifeX/X'>NewLife. Core</a></td><td style='text-align:center;'>two thousand and two</td><td style='text-align:center;'>Under maintenance</td><td style='text-align:center;'>√</td><td>Log, configuration, cache, network, RPC, serialization, APM performance tracking</td></tr><tr><td style='text-align:center;'><a href='https://github.com/NewLifeX/X'>XCode</a></td><td style='text-align:center;'>two thousand and five</td><td style='text-align:center;'>Under maintenance</td><td style='text-align:center;'>√</td><td>Big data middleware, mysql/sqlite/sqlserver/oracle/tdengine/ Damon, automatic table creation and table splitting</td></tr><tr><td style='text-align:center;'><a href='https://github.com/NewLifeX/NewLife.Net'>NewLife. Net</a></td><td style='text-align:center;'>two thousand and five</td><td style='text-align:center;'>Under maintenance</td><td style='text-align:center;'>√</td><td>Network library, single machine 10 million level throughput (22.66 million TPS), single machine 1 million level connection (4million TCP)</td></tr><tr><td style='text-align:center;'><a href='https://github.com/NewLifeX/NewLife.Cube'>NewLife. Cube</a></td><td style='text-align:center;'>two thousand and ten</td><td style='text-align:center;'>Under maintenance</td><td style='text-align:center;'>√</td><td>Rubik&#39;s cube rapid development platform integrates user permissions, SSO login, OAuth server, etc., and single table 10billion level project verification</td></tr><tr><td style='text-align:center;'><a href='https://github.com/NewLifeX/NewLife.Agent'>NewLife. Agent</a></td><td style='text-align:center;'>two thousand and eight</td><td style='text-align:center;'>Under maintenance</td><td style='text-align:center;'>√</td><td>Service management framework to install applications as operating system daemons, windows services, and SYSTEMd of Linux</td></tr><tr><td style='text-align:center;'><a href='https://github.com/NewLifeX/NewLife.Zero'>NewLife. Zero</a></td><td style='text-align:center;'>two thousand and twenty</td><td style='text-align:center;'>Under maintenance</td><td style='text-align:center;'>√</td><td>Zero generation scaffolding, various types of project templates for copy and play, web applications, webapis, network services, and message services</td></tr><tr><td style='text-align:center;'>middleware </td><td style='text-align:center;'>&nbsp;</td><td style='text-align:center;'>&nbsp;</td><td style='text-align:center;'>&nbsp;</td><td>Docking with well-known middleware platforms</td></tr><tr><td style='text-align:center;'><a href='https://github.com/NewLifeX/NewLife.Redis'>NewLife. Redis</a></td><td style='text-align:center;'>two thousand and seventeen</td><td style='text-align:center;'>Under maintenance</td><td style='text-align:center;'>√</td><td>Redis client, microsecond delay, million level throughput, rich message queue, 10 billion level data volume project verification</td></tr><tr><td style='text-align:center;'><a href='https://github.com/NewLifeX/NewLife.RocketMQ'>NewLife. RocketMQ</a></td><td style='text-align:center;'>two thousand and eighteen</td><td style='text-align:center;'>Under maintenance</td><td style='text-align:center;'>√</td><td>Support Apache rocketmq and Alibaba cloud message queues, and verify billion level projects</td></tr><tr><td style='text-align:center;'><a href='https://github.com/NewLifeX/NewLife.MQTT'>NewLife. MQTT</a></td><td style='text-align:center;'>two thousand and nineteen</td><td style='text-align:center;'>Under maintenance</td><td style='text-align:center;'>√</td><td>Internet of things message protocol. The client supports Alibaba cloud Internet of things</td></tr><tr><td style='text-align:center;'><a href='https://github.com/NewLifeX/NewLife.LoRa'>NewLife. LoRa</a></td><td style='text-align:center;'>two thousand and sixteen</td><td style='text-align:center;'>Under maintenance</td><td style='text-align:center;'>√</td><td>Ultra low power Internet of things telecommunication protocol lorawan</td></tr><tr><td style='text-align:center;'>Product platform</td><td style='text-align:center;'>&nbsp;</td><td style='text-align:center;'>&nbsp;</td><td style='text-align:center;'>&nbsp;</td><td>Product platform level, compile, deploy and customize</td></tr><tr><td style='text-align:center;'><a href='https://github.com/NewLifeX/AntJob'>AntJob</a></td><td style='text-align:center;'>two thousand and nineteen</td><td style='text-align:center;'>Under maintenance</td><td style='text-align:center;'>√</td><td>Ant scheduling, distributed big data computing platform (real-time / offline), ant moving and slicing idea, project verification of trillions of data</td></tr><tr><td style='text-align:center;'><a href='https://github.com/NewLifeX/Stardust'>Stardust</a></td><td style='text-align:center;'>two thousand and eighteen</td><td style='text-align:center;'>Under maintenance</td><td style='text-align:center;'>√</td><td>Xingchen, distributed service platform, node management, APM monitoring center, configuration center, registration center, release center and message center</td></tr><tr><td style='text-align:center;'><a href='https://github.com/NewLifeX/XCoder'>CrazyCoder</a></td><td style='text-align:center;'>two thousand and six</td><td style='text-align:center;'>Under maintenance</td><td style='text-align:center;'>√</td><td>Codegod tools, many developer tools, network, serial port, encryption and decryption, regular expression, MODBUS</td></tr><tr><td style='text-align:center;'><a href='https://github.com/NewLifeX/XProxy'>XProxy</a></td><td style='text-align:center;'>two thousand and five</td><td style='text-align:center;'>Under maintenance</td><td style='text-align:center;'>√</td><td>Product level reverse proxy, NAT proxy, HTTP proxy</td></tr><tr><td style='text-align:center;'><a href='https://github.com/NewLifeX/SmartOS'>SmartOS</a></td><td style='text-align:center;'>two thousand and fourteen</td><td style='text-align:center;'>Under maintenance</td><td style='text-align:center;'>C++11</td><td>Embedded operating system, completely independent, arm Cortex-M chip architecture</td></tr><tr><td style='text-align:center;'><a href='https://github.com/NewLifeX/GitCandy'>GitCandy</a></td><td style='text-align:center;'>two thousand and fifteen</td><td style='text-align:center;'>Under maintenance</td><td style='text-align:center;'>&#215;</td><td>Git source code management system</td></tr><tr><td style='text-align:center;'>NewLife. A2</td><td style='text-align:center;'>two thousand and nineteen</td><td style='text-align:center;'>commercial</td><td style='text-align:center;'>√</td><td>Embedded industrial computer, Internet of things edge gateway, high performance Net host for industry, agriculture, transportation and medical treatment</td></tr><tr><td style='text-align:center;'>NewLife. IoT</td><td style='text-align:center;'>two thousand and twenty</td><td style='text-align:center;'>commercial</td><td style='text-align:center;'>√</td><td>The overall solution of the Internet of things, the integration of construction industry, environmental protection, agriculture, software and hardware and big data analysis, and the verification of 100000 level point-to-point projects</td></tr><tr><td style='text-align:center;'>NewLife. UWB</td><td style='text-align:center;'>two thousand and twenty</td><td style='text-align:center;'>commercial</td><td style='text-align:center;'>√</td><td>Centimeter level high-precision indoor positioning, software and hardware integration, linkage with other systems, large exhibition hall project verification</td></tr></tbody>
</table></figure> 
## New life development team

![XCode](https://newlifex.com/logo.png)

Founded in 2002, newlife is a solution provider for the Internet of things industry in the new era, committed to providing software and hardware application solution consulting, system architecture planning and development services.  
The open source newlife series components led by the team have been widely used in various industries, and nuget has been downloaded more than 600000 times.  
Newlife, the core component of big data developed by the team Xcode, ant scheduling computing platform antjob, Stardust distributed platform Stardust, cache queue component newlife Redis and Internet of things platform newlife IOT has been successfully applied in electric power, universities, Internet, telecommunications, transportation, logistics, industrial control, medical treatment, cultural and Expo industries, providing customers with a large number of advanced, reliable, safe, high-quality and easy to expand products and system integration services.

We will continue to become a long-term trusted partner of customers through continuous service improvement, and become an excellent IT service provider in China through continuous innovation and development.

`The new life team started in 2002. Some open source projects have a long history of more than 20 years. The source code library has retained all the modification records since 2010`  
Website:[https://newlifex.com](https://newlifex.com)  
Open source:[https://github.com/newlifex](https://github.com/newlifex)  
QQ group: 1600800/1600838  
Wechat official account:  
![智能大石头](https://newlifex.com/stone.jpg)