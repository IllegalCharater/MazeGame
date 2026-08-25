using System;

/// <summary>
/// 全局指令键：类型安全的"枚举类"（readonly struct），可跨文件用 partial 扩展。
/// 指令系统以该类型作键：ExecuteCommand(CommandType.X, payload)
/// 新增指令：新建 CommandType.&lt;模块&gt;.cs partial 文件，用 Next() 自动取下一个值。
/// </summary>
public readonly partial struct CommandType : IEquatable<CommandType> {
    private static int _nextValue = 1000;

    public readonly int Value;

    private CommandType(int value) {
        Value = value;
    }

    /// <summary>创建下一个自动自增的指令键。</summary>
    private static CommandType Next() => new CommandType(_nextValue++);

    public bool Equals(CommandType other) => Value == other.Value;
    public override bool Equals(object obj) => obj is CommandType other && Equals(other);
    public override int GetHashCode() => Value;
    public override string ToString() => Value.ToString();

    public static bool operator ==(CommandType a, CommandType b) => a.Value == b.Value;
    public static bool operator !=(CommandType a, CommandType b) => a.Value != b.Value;
    public static explicit operator int(CommandType e) => e.Value;
    public static explicit operator CommandType(int value) => new CommandType(value);


}
