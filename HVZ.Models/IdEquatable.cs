namespace HVZ.Models;

public abstract class IdEquatable<T> : IEquatable<T> where T : IdEquatable<T>
{
    protected abstract object EqualityId { get; }

    public bool Equals(T? other)
        => !ReferenceEquals(other, null) && EqualityId.Equals(other.EqualityId);

    public static bool operator ==(IdEquatable<T>? a, IdEquatable<T>? b)
        => ReferenceEquals(a, b) || !ReferenceEquals(a, null) && a.EqualityId.Equals(b?.EqualityId);

    public static bool operator !=(IdEquatable<T>? a, IdEquatable<T>? b)
        => !(a == b);

    public override bool Equals(object? obj)
        => obj as T == this;

    public override int GetHashCode()
        => EqualityId.GetHashCode();
}