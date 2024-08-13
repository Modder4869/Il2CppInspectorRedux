using VersionedSerialization.Generator.Utils;

namespace VersionedSerialization.Generator.Models;

public sealed record PropertySerializationInfo(
    string Name,
    string ReadMethod,
    string SizeExpression,
    int Alignment,
    ImmutableEquatableArray<VersionCondition> VersionConditions
);