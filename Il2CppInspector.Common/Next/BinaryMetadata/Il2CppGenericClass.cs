using VersionedSerialization.Attributes;

namespace Il2CppInspector.Next.BinaryMetadata;

[VersionedStruct]
public partial struct Il2CppGenericClass
{
    [VersionCondition(LessThan = "24.5"), Aligned(0)]
    public int TypeDefinitionIndex;

    [VersionCondition(GreaterThan = "27.0")]
    public Pointer<Il2CppType> Type;

    public Il2CppGenericContext Context;

    [CustomSerialization("reader.ReadNUInt();", "is32Bit ? 4 : 8")]
    public ulong CachedClass; // Il2CppClass*, optional
}