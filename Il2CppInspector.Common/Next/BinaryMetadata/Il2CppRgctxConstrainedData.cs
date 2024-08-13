using Il2CppInspector.Next.Metadata;
using VersionedSerialization.Attributes;

namespace Il2CppInspector.Next.BinaryMetadata;

using TypeIndex = int;

[VersionedStruct]
public partial struct Il2CppRgctxConstrainedData
{
    public TypeIndex TypeIndex;
    public Il2CppMetadataUsage EncodedMethodIndex;
}