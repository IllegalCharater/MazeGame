/// <summary>
/// EventType 的框架内部扩展文件 —— 演示如何在另一个文件里给 EventType 追加常量（自动自增）。
/// 复制本文件结构，新建 EventType.&lt;模块&gt;.cs 即可；每个 Next() 自动取下一个值，保证唯一。
/// </summary>
public readonly partial struct EventType {
    // ---- 玩家数据（自动自增，从 2000 起）----
    public static readonly EventType CurrencyChanged = Next();   // 单值 int：金币数量
    public static readonly EventType EnergyChanged = Next();     // DataBag { energy, name }
}
