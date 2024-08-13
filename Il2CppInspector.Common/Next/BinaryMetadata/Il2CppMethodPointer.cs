using VersionedSerialization.Attributes;

namespace Il2CppInspector.Next.BinaryMetadata;

[VersionedStruct]
public partial struct Il2CppMethodPointer(ulong addr = 0)
{
    public static readonly Il2CppMethodPointer Null = new();

    [CustomSerialization("reader.ReadNUInt();", "is32Bit ? 4 : 8")]
    public ulong Value { get; set; } = addr;

    public readonly bool IsNull => Value == 0;

    public readonly override string ToString() => $"0x{Value:X}";

    public static implicit operator ulong(Il2CppMethodPointer ptr) => ptr.Value;
    public static implicit operator Il2CppMethodPointer(ulong ptr) => new(ptr);
}