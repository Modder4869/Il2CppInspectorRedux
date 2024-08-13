using Il2CppInspector.Next.Metadata;
using VersionedSerialization.Attributes;

namespace Il2CppInspector.Next.BinaryMetadata;

using FieldIndex = int;
using TypeDefinitionIndex = int;

[VersionedStruct]
public partial struct Il2CppMetadataRegistration
{
    [Aligned(0)]
    public int GenericClassesCount;

    public Pointer<Pointer<Il2CppGenericClass>> GenericClasses;

    [Aligned(0)]
    public int GenericInstsCount;

    public Pointer<Pointer<Il2CppGenericInst>> GenericInsts;

    [Aligned(0)]
    public int GenericMethodTableCount;

    public Pointer<Il2CppGenericMethodFunctionsDefinitions> GenericMethodTable;

    [Aligned(0)]
    public int TypesCount;

    public Pointer<Pointer<Il2CppType>> Types;

    [Aligned(0)]
    public int MethodSpecsCount;

    public Pointer<Il2CppMethodSpec> MethodSpecs;

    [VersionCondition(LessThan = "16.0")]
    public int MethodReferencesCount;

    [VersionCondition(LessThan = "16.0")]
    [CustomSerialization("reader.ReadNUInt();", "is32Bit ? 4 : 8")]
    public ulong MethodReferences; // uint**

    [Aligned(0)]
    public FieldIndex FieldOffsetsCount;

    [CustomSerialization("reader.ReadNUInt();", "is32Bit ? 4 : 8")]
    public ulong FieldOffsets; // int**

    [Aligned(0)]
    public TypeDefinitionIndex TypeDefinitionsSizesCount;
    public Pointer<Pointer<Il2CppTypeDefinitionSizes>> TypeDefinitionsSizes;

    [Aligned(0)]
    [VersionCondition(GreaterThan = "19.0")]
    public ulong MetadataUsagesCount;

    [VersionCondition(GreaterThan = "19.0")]
    public Pointer<Pointer<Il2CppMetadataUsage>> MetadataUsages;
}