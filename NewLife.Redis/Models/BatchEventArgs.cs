namespace NewLife.Caching.Models;

/// <summary>批计算事件参数</summary>
public class BatchEventArgs : EventArgs
{
    /// <summary>需要批计算的键</summary>
    public String[] Keys { get; set; } = null!;
}