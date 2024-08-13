using VersionedSerialization.Generator.Utils;

namespace VersionedSerialization.Generator.Models;

public sealed record ObjectSerializationInfo(
    string Namespace,
    string Name,
    bool HasBaseType,
    bool IsStruct,
    bool ShouldGenerateSizeMethod,
    bool CanGenerateSizeMethod,
    ImmutableEquatableArray<PropertySerializationInfo> Properties
);