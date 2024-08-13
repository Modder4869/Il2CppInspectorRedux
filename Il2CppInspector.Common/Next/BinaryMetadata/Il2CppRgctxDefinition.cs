using VersionedSerialization.Attributes;

namespace Il2CppInspector.Next.BinaryMetadata;

[VersionedStruct]
public partial struct Il2CppRgctxDefinition
{
    [Aligned(0)]
    public Il2CppRgctxDataType Type;

    [CustomSerialization("reader.ReadNUInt();", "is32Bit ? 4 : 8")]
    public ulong Data; // void*

    public readonly Pointer<Il2CppRgctxDefinitionData> Definition => Data;
    public readonly Pointer<Il2CppRgctxConstrainedData> Constrained => Data;
}