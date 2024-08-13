using System;

namespace VersionedSerialization.Generator.Models;

public enum PropertyType
{
    Unsupported = -1,
    None,
    Boolean,
    UInt8,
    UInt16,
    UInt32,
    UInt64,
    Int8,
    Int16,
    Int32,
    Int64,
    String,
}

public static class PropertyTypeExtensions
{
    public static string GetTypeName(this PropertyType type)
        => type switch
        {
            PropertyType.Unsupported => nameof(PropertyType.Unsupported),
            PropertyType.None => nameof(PropertyType.None),
            PropertyType.UInt8 => nameof(Byte),
            PropertyType.Int8 => nameof(SByte),
            PropertyType.Boolean => nameof(PropertyType.Boolean),
            PropertyType.UInt16 => nameof(PropertyType.UInt16),
            PropertyType.UInt32 => nameof(PropertyType.UInt32),
            PropertyType.UInt64 => nameof(PropertyType.UInt64),
            PropertyType.Int16 => nameof(PropertyType.Int16),
            PropertyType.Int32 => nameof(PropertyType.Int32),
            PropertyType.Int64 => nameof(PropertyType.Int64),
            PropertyType.String => nameof(String),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

    public static bool IsSeperateMethod(this PropertyType type)
        => type switch
        {
            PropertyType.Boolean => true,
            PropertyType.String => true,
            _ => false
        };
}