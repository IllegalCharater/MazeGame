using System;

/// <summary>
/// 全局事件键：类型安全的"枚举类"（readonly struct）。
/// C# 不支持 partial enum；Unity 2021.3 为 C# 9，struct 没有无参构造函数/字段初始化器，
/// 因此用静态工厂 Next() 实现自动自增。
/// 新增事件：新建 EventType.&lt;模块&gt;.cs partial 文件，用 Next() 自动取下一个值；
/// 需要精确起始值时用 new EventType(3000) 显式指定。
/// 数据传递约定：
///   单值事件：  DispatchEvent(EventType.X, new object[]{ 值 })  +  BindEvent&lt;T&gt;(EventType.X, handler)
///   多字段事件：DispatchEvent(EventType.X, new DataBag { { key, value }, ... })  +  BindEvent&lt;DataBag&gt;(EventType.X, handler)
/// </summary>
public readonly partial struct EventType : IEquatable<EventType> {
    private static int _nextValue = 2000;

    public readonly int Value;

    private EventType(int value) {
        Value = value;
    }

    /// <summary>创建下一个自动自增的事件键。</summary>
    private static EventType Next() => new EventType(_nextValue++);

    public bool Equals(EventType other) => Value == other.Value;
    public override bool Equals(object obj) => obj is EventType other && Equals(other);
    public override int GetHashCode() => Value;
    public override string ToString() => Value.ToString();

    public static bool operator ==(EventType a, EventType b) => a.Value == b.Value;
    public static bool operator !=(EventType a, EventType b) => a.Value != b.Value;
    public static explicit operator int(EventType e) => e.Value;
    public static explicit operator EventType(int value) => new EventType(value);

}
