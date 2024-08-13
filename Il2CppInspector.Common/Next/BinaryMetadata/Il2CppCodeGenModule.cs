using VersionedSerialization.Attributes;

namespace Il2CppInspector.Next.BinaryMetadata;

[VersionedStruct]
public partial struct Il2CppCodeGenModule
{
    [CustomSerialization("reader.ReadNUInt();", "is32Bit ? 4 : 8")]
    public ulong ModuleName; // const char*

    [Aligned(0)]
    public uint MethodPointerCount;
    
    public Pointer<Il2CppMethodPointer> MethodPointers;

    [Aligned(0)]
    [VersionCondition(EqualTo = "24.5")]
    [VersionCondition(GreaterThan = "27.1")]
    public uint AdjustorThunksCount;

    [VersionCondition(EqualTo = "24.5")]
    [VersionCondition(GreaterThan = "27.1")]
    public Pointer<Il2CppTokenAdjustorThunkPair> AdjustorThunks;

    [CustomSerialization("reader.ReadNUInt();", "is32Bit ? 4 : 8")]
    public ulong InvokerIndices; // int*

    [Aligned(0)]
    public uint ReversePInvokeWrapperCount;

    public Pointer<Il2CppTokenIndexMethodTuple> ReversePInvokeWrapperIndices;

    [Aligned(0)]
    public uint RgctxRangesCount;
    public Pointer<Il2CppTokenRangePair> RgctxRanges;

    [Aligned(0)]
    public uint RgctxsCount;
    public Pointer<Il2CppRgctxDefinition> Rgctxs;

    [CustomSerialization("reader.ReadNUInt();", "is32Bit ? 4 : 8")]
    public ulong DebuggerMetadata; // Pointer<Il2CppDebuggerMetadataRegistration> DebuggerMetadata;

    [VersionCondition(GreaterThan = "27.0", LessThan = "27.2")]
    public Pointer<Il2CppMethodPointer> CustomAttributeCacheGenerator;

    [VersionCondition(GreaterThan = "27.0")]
    public Il2CppMethodPointer ModuleInitializer;

    [VersionCondition(GreaterThan = "27.0")]
    [Aligned(0)]
    public ulong StaticConstructorTypeIndices; // TypeDefinitionIndex*

    [VersionCondition(GreaterThan = "27.0")]
    [CustomSerialization("reader.ReadNUInt();", "is32Bit ? 4 : 8")]
    public ulong MetadataRegistration; // Pointer<Il2CppMetadataRegistration>

    [VersionCondition(GreaterThan = "27.0")]
    [CustomSerialization("reader.ReadNUInt();", "is32Bit ? 4 : 8")]
    public ulong CodeRegistration; // Pointer<Il2CppCodeRegistration>
}