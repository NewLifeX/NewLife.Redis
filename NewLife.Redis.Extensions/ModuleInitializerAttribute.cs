#if NETSTANDARD2_0 || NETSTANDARD2_1 || NETCOREAPP3_1
// 旧目标框架的引用程序集把 ModuleInitializerAttribute 标记为 internal，
// 源生成器检测到类型存在却无法访问。此处补充本程序集可用的内部定义。
namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
internal sealed class ModuleInitializerAttribute : Attribute { }
#endif
