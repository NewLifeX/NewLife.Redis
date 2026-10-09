#if !NET8_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis;

/// <summary>裁剪与 AOT 分析注解垫片。低版本目标框架缺少这些特性，补齐以便源码统一标注；net8.0+ 使用内置真实特性</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct, Inherited = false)]
internal sealed class DynamicallyAccessedMembersAttribute : Attribute
{
    /// <summary>实例化</summary>
    /// <param name="memberTypes">要保留的成员类型</param>
    public DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes memberTypes) => MemberTypes = memberTypes;

    /// <summary>要保留的成员类型</summary>
    public DynamicallyAccessedMemberTypes MemberTypes { get; }
}

/// <summary>成员保留类型</summary>
[Flags]
internal enum DynamicallyAccessedMemberTypes
{
    /// <summary>不保留</summary>
    None = 0,
    /// <summary>公共无参构造函数</summary>
    PublicParameterlessConstructor = 0x0001,
    /// <summary>公共构造函数</summary>
    PublicConstructors = 0x0003,
    /// <summary>非公共构造函数</summary>
    NonPublicConstructors = 0x0004,
    /// <summary>公共方法</summary>
    PublicMethods = 0x0008,
    /// <summary>非公共方法</summary>
    NonPublicMethods = 0x0010,
    /// <summary>公共字段</summary>
    PublicFields = 0x0020,
    /// <summary>非公共字段</summary>
    NonPublicFields = 0x0040,
    /// <summary>公共嵌套类型</summary>
    PublicNestedTypes = 0x0080,
    /// <summary>非公共嵌套类型</summary>
    NonPublicNestedTypes = 0x0100,
    /// <summary>公共属性</summary>
    PublicProperties = 0x0200,
    /// <summary>非公共属性</summary>
    NonPublicProperties = 0x0400,
    /// <summary>公共事件</summary>
    PublicEvents = 0x0800,
    /// <summary>非公共事件</summary>
    NonPublicEvents = 0x1000,
    /// <summary>全部成员</summary>
    All = -1,
}

/// <summary>无条件抑制分析告警。ILC 只识别特性声明、不处理 #pragma，用于运行期类型无法静态表达保留要求的边界</summary>
[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
internal sealed class UnconditionalSuppressMessageAttribute : Attribute
{
    /// <summary>实例化</summary>
    /// <param name="category">类别</param>
    /// <param name="checkId">检查编号</param>
    public UnconditionalSuppressMessageAttribute(String category, String checkId)
    {
        Category = category;
        CheckId = checkId;
    }

    /// <summary>类别</summary>
    public String Category { get; }

    /// <summary>检查编号</summary>
    public String CheckId { get; }

    /// <summary>范围</summary>
    public String? Scope { get; set; }

    /// <summary>目标</summary>
    public String? Target { get; set; }

    /// <summary>理由</summary>
    public String? Justification { get; set; }

    /// <summary>消息编号</summary>
    public String? MessageId { get; set; }
}
#endif
