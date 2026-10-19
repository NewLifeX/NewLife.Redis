#if ANDROID
using Android.App;
using Android.OS;
using Android.Util;

namespace MobileSmoke;

/// <summary>Android 冒烟入口：启动即执行检查，结果同时写入 logcat 与 smoke.json</summary>
[Activity(Label = "MobileSmoke", MainLauncher = true, Exported = true)]
public class MainActivity : Activity
{
    /// <summary>创建活动并执行冒烟</summary>
    /// <param name="savedInstanceState">活动状态</param>
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        var (pass, fail, _) = Smoke.Run();
        if (fail == 0)
            Log.Info("MobileSmoke", $"SMOKE_OK pass={pass}");
        else
            Log.Error("MobileSmoke", $"SMOKE_FAIL pass={pass} fail={fail}");
    }
}
#endif
