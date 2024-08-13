using VersionedSerialization.Attributes;

namespace Il2CppInspector.Next.BinaryMetadata;

using InvokerMethod = Il2CppMethodPointer;

[VersionedStruct]
public partial struct Il2CppCodeRegistration
{
    [VersionCondition(LessThan = "24.1"), Aligned(0)]
    public uint MethodPointersCount;

    [VersionCondition(LessThan = "24.1")]
    public Pointer<Il2CppMethodPointer> MethodPointers;

    [Aligned(0)]
    public uint ReversePInvokeWrapperCount;

    public Pointer<Il2CppMethodPointer> ReversePInvokeWrappers;

    [VersionCondition(LessThan = "22.0"), Aligned(0)]
    public uint DelegateWrappersFromManagedToNativeCount;

    [VersionCondition(LessThan = "22.0")]
    public Pointer<Il2CppMethodPointer> DelegateWrappersFromManagedToNative;

    [VersionCondition(LessThan = "22.0"), Aligned(0)]
    public uint MarshalingFunctionsCount;

    [VersionCondition(LessThan = "22.0")]
    public Pointer<Il2CppMethodPointer> MarshalingFunctions;

    [VersionCondition(GreaterThan = "21.0", LessThan = "22.0"), Aligned(0)]
    public uint CcwMarshalingFunctionsCount;

    [VersionCondition(GreaterThan = "21.0", LessThan = "22.0")]
    public Pointer<Il2CppMethodPointer> CcwMarshalingFunctions;

    [Aligned(0)]
    public uint GenericMethodPointersCount;

    public Pointer<Il2CppMethodPointer> GenericMethodPointers;

    [VersionCondition(EqualTo = "24.5")]
    [VersionCondition(GreaterThan = "27.1")]
    public Pointer<Il2CppMethodPointer> GenericAdjustorThunks;

    [Aligned(0)]
    public uint InvokerPointersCount;

    public Pointer<InvokerMethod> InvokerPointers;

    [VersionCondition(LessThan = "24.5"), Aligned(0)]
    public int CustomAttributeCount;

    [VersionCondition(LessThan = "24.5")]
    public Pointer<Il2CppMethodPointer> CustomAttributeGenerators;

    [VersionCondition(GreaterThan = "21.0", LessThan = "22.0"), Aligned(0)]
    public int GuidCount;

    [VersionCondition(GreaterThan = "21.0", LessThan = "22.0")]
    public Pointer<Il2CppGuid> Guids;

    [VersionCondition(GreaterThan = "22.0", LessThan = "29.0")]
    public int UnresolvedVirtualCallCount;

    [VersionCondition(EqualTo = "29.1"), VersionCondition(EqualTo = "31.1")]
    [VersionCondition(EqualTo = "29.2"), VersionCondition(EqualTo = "31.2")]
    [Aligned(0)]
    public uint UnresolvedIndirectCallCount; // UnresolvedVirtualCallCount pre 29.1

    [VersionCondition(GreaterThan = "22.0")]
    public Pointer<Il2CppMethodPointer> UnresolvedVirtualCallPointers;

    [VersionCondition(EqualTo = "29.1"), VersionCondition(EqualTo = "31.1")]
    [VersionCondition(EqualTo = "29.2"), VersionCondition(EqualTo = "31.2")]
    public Pointer<Il2CppMethodPointer> UnresolvedInstanceCallWrappers;

    [VersionCondition(EqualTo = "29.1"), VersionCondition(EqualTo = "31.1")]
    [VersionCondition(EqualTo = "29.2"), VersionCondition(EqualTo = "31.2")]
    public Pointer<Il2CppMethodPointer> UnresolvedStaticCallPointers;

    [VersionCondition(GreaterThan = "23.0"), Aligned(0)]
    public uint InteropDataCount;

    [VersionCondition(GreaterThan = "23.0")]
    public Pointer<Il2CppInteropData> InteropData;

    [VersionCondition(GreaterThan = "24.3"), Aligned(0)]
    public uint WindowsRuntimeFactoryCount;

    [VersionCondition(GreaterThan = "24.3")]
    public Pointer<Il2CppWindowsRuntimeFactoryTableEntry> WindowsRuntimeFactoryTable;

    [VersionCondition(GreaterThan = "24.2"), Aligned(0)]
    public uint CodeGenModulesCount;

    [VersionCondition(GreaterThan = "24.2")]
    public Pointer<Pointer<Il2CppCodeGenModule>> CodeGenModules;
}