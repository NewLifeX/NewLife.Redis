// 自动选择最新的文件源
var srcs = new String[] { @"..\Bin", @"C:\X\DLL", @"C:\X\Bin", @"E:\X\DLL", @"E:\X\Bin" };
var cur = ".".GetFullPath();
foreach (var item in srcs)
{
    // 跳过当前目录
    if (item.EqualIgnoreCase(cur)) continue;

    Console.WriteLine("复制 {0} => {1}", item, cur);

    try
    {
        item.AsDirectory().CopyToIfNewer(cur, "*.dll;*.exe;*.xml;*.pdb;*.cs", false,
            name => Console.WriteLine("\t{1}\t{0}", name, item.CombinePath(name).AsFile().LastWriteTime.ToFullString()));
    }
    catch (Exception ex) { Console.WriteLine(" " + ex.Message); }
}