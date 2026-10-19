using System.Reflection;
using System.Text.Json;

namespace MobileSmoke;

/// <summary>移动端冒烟：验证主库在 Android/iOS 上可加载并执行核心 API</summary>
public static class Smoke
{
    /// <summary>执行全部检查</summary>
    /// <returns>通过数、失败数与结果文件路径</returns>
    public static (Int32 Pass, Int32 Fail, String Path) Run()
    {
        var log = new List<String>();
        var pass = 0;
        var fail = 0;

        void Check(String name, Action action)
        {
            try
            {
                action();
                log.Add($"OK {name}");
                pass++;
            }
            catch (Exception ex)
            {
                log.Add($"FAIL {name}: {ex.GetType().Name}: {ex.Message}");
                fail++;
            }
        }

        var asm = typeof(NewLife.Caching.FullRedis).Assembly;
        Check("assembly:" + asm.GetName().Name, () =>
        {
            if (CountTypes(asm) == 0) throw new Exception("no exported types");
        });
        Check("FullRedis ctor", () => { using var redis = new NewLife.Caching.FullRedis("127.0.0.1:6379", null, 0); });
        var dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        Directory.CreateDirectory(dir);
        var file = System.IO.Path.Combine(dir, "smoke.json");
        var json = JsonSerializer.Serialize(new
        {
            pass,
            fail,
            arch = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
            os = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            log,
        });
        File.WriteAllText(file, json);
        return (pass, fail, file);
    }

    private static Int32 CountTypes(Assembly asm)
    {
        try
        {
            return asm.GetExportedTypes().Length;
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Count(e => e != null);
        }
    }
}
