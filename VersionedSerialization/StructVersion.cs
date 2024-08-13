namespace VersionedSerialization;

public readonly struct StructVersion(int major = 0, int minor = 0, string? tag = null) : IEquatable<StructVersion>
{
    public readonly int Major = major;
    public readonly int Minor = minor;
    public readonly string? Tag = tag;

    #region Equality operators

    public static bool operator ==(StructVersion left, StructVersion right)
        => left.Major == right.Major && left.Minor == right.Minor;

    public static bool operator !=(StructVersion left, StructVersion right)
        => !(left == right);

    public static bool operator >(StructVersion left, StructVersion right)
        => left.Major > right.Major || (left.Major == right.Major && left.Minor > right.Minor);

    public static bool operator <(StructVersion left, StructVersion right)
        => left.Major < right.Major || (left.Major == right.Major && left.Minor < right.Minor);

    public static bool operator >=(StructVersion left, StructVersion right)
        => left.Major > right.Major || (left.Major == right.Major && left.Minor >= right.Minor);

    public static bool operator <=(StructVersion left, StructVersion right)
        => left.Major < right.Major || (left.Major == right.Major && left.Minor <= right.Minor);

    public override bool Equals(object? obj)
        => obj is StructVersion other && Equals(other);

    public bool Equals(StructVersion other)
        => Major == other.Major && Minor == other.Minor;

    public override int GetHashCode()
        => HashCode.Combine(Major, Minor);

    #endregion

    public override string ToString() => $"{Major}.{Minor}{(Tag != null ? $"-{Tag}" : "")}";

    public static implicit operator StructVersion(string value)
    {
        var versionParts = value.Split('.');
        if (versionParts.Length is 1 or > 2)
            throw new InvalidOperationException("Invalid version string.");

        var tagParts = versionParts[1].Split("-");
        if (tagParts.Length > 2) 
            throw new InvalidOperationException("Invalid version string.");

        var major = int.Parse(versionParts[0]);
        var minor = int.Parse(tagParts[0]);
        var tag = tagParts.Length == 1 ? null : tagParts[1];

        return new StructVersion(major, minor, tag);
    }
}