using System;

[Serializable]
public struct EntityId : IEquatable<EntityId>
{
    public int value;
    public string debugName;

    public bool IsValid => value > 0;

    public EntityId(int value, string debugName = null)
    {
        this.value = value;
        this.debugName = debugName;
    }

    public bool Equals(EntityId other)
    {
        return value == other.value;
    }

    public override bool Equals(object obj)
    {
        return obj is EntityId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return value;
    }

    public override string ToString()
    {
        return string.IsNullOrEmpty(debugName) ? value.ToString() : debugName + "(" + value + ")";
    }
}
