using System.Runtime.CompilerServices;
using NewLife;
using NewLife.Caching;
using NewLife.Caching.Queues;
using NewLife.Serialization;

var fail = 0;

void Check(String name, Action action)
{
    try
    {
        action();
        Console.WriteLine($"[PASS] {name}");
    }
    catch (Exception ex)
    {
        fail++;
        Console.WriteLine($"[FAIL] {name}: {ex.GetType().Name} - {ex.Message}");
    }
}

Console.WriteLine("== NewLife.Redis NativeAOT 冒烟 ==");
Console.WriteLine($"IsDynamicCodeSupported = {RuntimeFeature.IsDynamicCodeSupported}");
Console.WriteLine($"JSON 主机 = {new RedisJsonEncoder().JsonHost?.GetType().FullName}");

var redis = new FullRedis("127.0.0.1:6379", null!, 14) { Timeout = 3000 };

Check("连接与服务器信息", () =>
{
    var ver = redis.Version;
    if (ver == null || ver.Major == 0) throw new InvalidOperationException("未取到服务器版本");
    Console.WriteLine($"       服务器版本：{ver}");
});

Check("字符串 Set/Get", () =>
{
    redis.Set("aot:str", "hello-aot");
    var v = redis.Get<String>("aot:str");
    if (v != "hello-aot") throw new InvalidOperationException($"读回值不符：{v}");
});

Check("整数 INCR", () =>
{
    redis.Remove("aot:num");
    var n = redis.Increment("aot:num", 2);
    if (n != 2) throw new InvalidOperationException($"自增结果不符：{n}");
});

Check("Hash 字典", () =>
{
    redis.Remove("aot:hash");
    var hash = redis.GetDictionary<String>("aot:hash");
    hash["k1"] = "v1";
    hash["k2"] = "v2";
    if (hash["k1"] != "v1") throw new InvalidOperationException("HGET 读回失败");
});

Check("List 列表", () =>
{
    redis.Remove("aot:list");
    var list = redis.GetList<String>("aot:list");
    list.Add("a");
    list.Add("b");
    if (list.Count != 2) throw new InvalidOperationException($"LLEN={list.Count}");
});

Check("队列 生产/消费", () =>
{
    redis.Remove("aot:queue");
    var queue = redis.GetQueue<String>("aot:queue");
    queue.Add("job-1");
    var item = queue.TakeOne(2);
    if (item != "job-1") throw new InvalidOperationException($"取出：{item ?? "<null>"}");
});

Check("可靠队列 生产/消费", () =>
{
    redis.Remove("aot:rq*");
    var queue = redis.GetReliableQueue<String>("aot:rq");
    queue.Add("job-r1");
    var item = queue.TakeOne(2);
    if (item != "job-r1") throw new InvalidOperationException($"取出：{item ?? "<null>"}");
});

Check("JsonHelper 泛型往返", () =>
{
    var order = new Order { Id = 7, Name = "序列化", Time = new DateTime(2026, 10, 9) };
    var json = JsonHelper.ToJson(order);
    var order2 = JsonHelper.ToJsonEntity<Order>(json);
    if (order2?.Id != 7 || order2?.Name != "序列化") throw new InvalidOperationException($"往返失败：{json}");
});

Check("自定义对象 Redis 存取（编码器链路）", () =>
{
    var order = new Order { Id = 9, Name = "对象", Time = new DateTime(2026, 10, 9) };
    redis.Set("aot:order", order);
    var order2 = redis.Get<Order>("aot:order");
    if (order2?.Id != 9) throw new InvalidOperationException($"读回 Id={order2?.Id}");
});

Check("复杂类型 字典 JSON 存取", () =>
{
    var dic = new Dictionary<String, Int32> { ["a"] = 1, ["b"] = 2 };
    redis.Set("aot:dic", dic);
    var dic2 = redis.Get<Dictionary<String, Int32>>("aot:dic");
    if (dic2 == null || dic2.Count != 2 || dic2["b"] != 2) throw new InvalidOperationException("字典读回失败");
});

Check("Object 值字典 JSON 存取", () =>
{
    var dic = new Dictionary<String, Object> { ["n"] = 1, ["s"] = "x", ["b"] = true };
    redis.Set("aot:objdic", dic);
    var dic2 = redis.Get<Dictionary<String, Object>>("aot:objdic");
    if (dic2 == null || dic2.Count != 3) throw new InvalidOperationException("Object 字典读回失败");
});

Check("Stream 追加/读取", () =>
{
    redis.Remove("aot:stream");
    var stream = redis.GetStream<Order>("aot:stream");
    var id = stream.Add(new Order { Id = 11, Name = "流" });
    if (id.IsNullOrEmpty()) throw new InvalidOperationException("未返回消息 ID");
    var items = stream.Take(1).ToList();
    if (items.Count != 1 || items[0].Id != 11) throw new InvalidOperationException($"读取：{items.Count} 条");
});

Check("列表范围读取（LRANGE→T[]）", () =>
{
    redis.Remove("aot:list2");
    var list = (RedisList<Order>)redis.GetList<Order>("aot:list2");
    list.Add(new Order { Id = 21, Name = "数组" });
    list.Add(new Order { Id = 22, Name = "数组2" });
    var arr = list.GetAll();
    if (arr.Length != 2 || arr[1].Id != 22) throw new InvalidOperationException($"读取：{arr.Length}");
});

Check("JSON 数组 存取", () =>
{
    var orders = new[] { new Order { Id = 31 }, new Order { Id = 32 } };
    redis.Set("aot:arr", orders);
    var arr2 = redis.Get<Order[]>("aot:arr");
    if (arr2 == null || arr2.Length != 2 || arr2[1].Id != 32) throw new InvalidOperationException($"读回：{arr2?.Length}");
});

Check("Message 字段解码（GetBody 反射路径）", () =>
{
    var detail = new Detail { Sku = 9 };
    // 泛型入口保根：AOT 下反射解码嵌套类型要求其成员元数据被保留（等价于静态引用/上下文声明）
    _ = JsonHelper.ToJsonEntity<Detail>(JsonHelper.ToJson(detail));
    var msg = new Message { Body = ["Id", "42", "Name", "字段", "Detail", JsonHelper.ToJson(detail)] };
    var order = msg.GetBody<OrderDetail>();
    if (order?.Id != 42 || order?.Name != "字段" || order?.Detail?.Sku != 9) throw new InvalidOperationException("字段解码失败");
});

redis.Remove("aot:str");
redis.Remove("aot:num");
redis.Remove("aot:hash");
redis.Remove("aot:list");
redis.Remove("aot:queue");
redis.Remove("aot:rq*");
redis.Remove("aot:order");
redis.Remove("aot:dic");
redis.Remove("aot:objdic");
redis.Remove("aot:stream");
redis.Remove("aot:list2");
redis.Remove("aot:arr");

Console.WriteLine($"== 结果：{(fail == 0 ? "全部通过" : $"{fail} 项失败")} ==");
return fail == 0 ? 0 : 1;

public class Order
{
    public Int32 Id { get; set; }
    public String? Name { get; set; }
    public DateTime Time { get; set; }
}

public class OrderDetail
{
    public Int32 Id { get; set; }
    public String? Name { get; set; }
    public Detail? Detail { get; set; }
}

public class Detail
{
    public Int32 Sku { get; set; }
}
