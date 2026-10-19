#if IOS
using Foundation;
using UIKit;

namespace MobileSmoke;

/// <summary>iOS 应用入口</summary>
public class Application
{
    /// <summary>主入口</summary>
    /// <param name="args">命令行参数</param>
    public static void Main(String[] args) => UIApplication.Main(args, null, typeof(AppDelegate));
}

/// <summary>iOS 冒烟入口：启动即执行检查并写入 smoke.json</summary>
[Register("AppDelegate")]
public class AppDelegate : UIApplicationDelegate
{
    /// <summary>应用启动完成</summary>
    /// <param name="application">应用</param>
    /// <param name="launchOptions">启动参数</param>
    /// <returns>是否继续启动</returns>
    public override Boolean FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        var (pass, fail, _) = Smoke.Run();
        Console.WriteLine(fail == 0 ? $"SMOKE_OK pass={pass}" : $"SMOKE_FAIL pass={pass} fail={fail}");
        return true;
    }
}
#endif
