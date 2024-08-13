using VersionedSerialization.Attributes;

namespace Il2CppInspector.Next.BinaryMetadata;

[VersionedStruct]
public partial struct Il2CppGenericInst
{
    public readonly bool Valid => TypeArgc > 0;

    [Aligned(0)]
    public uint TypeArgc;

    public Pointer<Pointer<Il2CppType>> TypeArgv;
}