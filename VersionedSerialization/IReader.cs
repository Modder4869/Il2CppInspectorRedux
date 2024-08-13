using System.Collections.Immutable;

namespace VersionedSerialization;

public interface IReader
{
    bool Is32Bit { get; }

    bool ReadBoolean();
    long ReadNInt();
    ulong ReadNUInt();
    string ReadString();
    ReadOnlySpan<byte> ReadBytes(int length);

    T Read<T>() where T : unmanaged;
    ImmutableArray<T> ReadArray<T>(long count) where T : unmanaged;

    T ReadObject<T>(in StructVersion version = default) where T : IReadable, new();
    ImmutableArray<T> ReadObjectArray<T>(long count, in StructVersion version = default) where T : IReadable, new();

    public void Align(int alignment = 0);
}