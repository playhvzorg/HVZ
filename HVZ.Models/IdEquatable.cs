namespace HVZ.Models;

public abstract class IdEquatable<T> : IEquatable<T> where T : IdEquatable<T> {
    protected abstract object EqualityId { get; }

    public bool Equals(T? other)
    {
        return !ReferenceEquals(other, null) && EqualityId.Equals(other.EqualityId);
    }

    public static bool operator ==(IdEquatable<T>? a, IdEquatable<T>? b)
    {
        return ReferenceEquals(a, b) || !ReferenceEquals(a, null) && a.EqualityId.Equals(b?.EqualityId);
    }

    public static bool operator !=(IdEquatable<T>? a, IdEquatable<T>? b)
    {
        return !(a == b);
    }

    public override bool Equals(object? obj)
    {
        return obj as T == this;
    }

    public override int GetHashCode()
    {
        return EqualityId.GetHashCode();
    }
}