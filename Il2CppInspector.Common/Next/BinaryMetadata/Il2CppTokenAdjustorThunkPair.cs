using VersionedSerialization.Attributes;

namespace Il2CppInspector.Next.BinaryMetadata;

[VersionedStruct]
public partial struct Il2CppTokenAdjustorThunkPair
{
    [Aligned(0)]
    public uint Token;

    public Il2CppMethodPointer AdjustorThunk;
}