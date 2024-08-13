using VersionedSerialization.Attributes;

namespace Il2CppInspector.Next.BinaryMetadata;

[VersionedStruct]
public partial struct Il2CppArrayType
{
    public Pointer<Il2CppType> ElementType;
    public byte Rank;
    public byte NumSizes;
    public byte NumLowerBound;

    [CustomSerialization("reader.ReadNUInt();", "is32Bit ? 4 : 8")]
    public ulong Sizes; // int*

    [CustomSerialization("reader.ReadNUInt();", "is32Bit ? 4 : 8")]
    public ulong LoBounds; // int*
}