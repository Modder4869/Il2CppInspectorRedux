using VersionedSerialization.Attributes;

namespace Il2CppInspector.Next.BinaryMetadata;

[VersionedStruct]
public partial struct Il2CppTokenIndexMethodTuple
{
    public uint Token;
    public int Index;

    [CustomSerialization("reader.ReadNUInt();", "is32Bit ? 4 : 8")]
    public ulong Method; // void**

    public uint GenericMethodIndex;
}