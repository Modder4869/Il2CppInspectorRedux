/*
    Copyright 2017 Perfare - https://github.com/Perfare/Il2CppDumper
    Copyright 2017-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Il2CppInspector.Next;
using Il2CppInspector.Next.Metadata;
using NoisyCowStudios.Bin2Object;
using VersionedSerialization;

namespace Il2CppInspector
{
    public class Metadata : BinaryObjectStream
    {
        public Il2CppGlobalMetadataHeader Header { get; set; }

        public Il2CppAssemblyDefinition[] Assemblies { get; set; }
        public Il2CppImageDefinition[] Images { get; set; }
        public Il2CppTypeDefinition[] Types { get; set; }
        public Il2CppMethodDefinition[] Methods { get; set; }
        public Il2CppParameterDefinition[] Params { get; set; }
        public Il2CppFieldDefinition[] Fields { get; set; }
        public Il2CppFieldDefaultValue[] FieldDefaultValues { get; set; }
        public Il2CppParameterDefaultValue[] ParameterDefaultValues { get; set; }
        public Il2CppPropertyDefinition[] Properties { get; set; }
        public Il2CppEventDefinition[] Events { get; set; }
        public Il2CppGenericContainer[] GenericContainers { get; set; }
        public Il2CppGenericParameter[] GenericParameters { get; set; }
        public Il2CppCustomAttributeTypeRange[] AttributeTypeRanges { get; set; }
        public Il2CppCustomAttributeDataRange[] AttributeDataRanges { get; set; }
        public Il2CppInterfaceOffsetPair[] InterfaceOffsets { get; set; }
        public Il2CppMetadataUsageList[] MetadataUsageLists { get; set; }
        public Il2CppMetadataUsagePair[] MetadataUsagePairs { get; set; }
        public Il2CppFieldRef[] FieldRefs { get; set; }

        public int[] InterfaceUsageIndices { get; set; }
        public int[] NestedTypeIndices { get; set; }
        public int[] AttributeTypeIndices { get; set; }
        public int[] GenericConstraintIndices { get; set; }
        public uint[] VTableMethodIndices { get; set; }
        public string[] StringLiterals { get; set; }

        public Dictionary<int, string> Strings { get; private set; } = new Dictionary<int, string>();

        // Set if something in the metadata has been modified / decrypted
        public bool IsModified { get; private set; } = false;

        // Status update callback
        private EventHandler<string> OnStatusUpdate { get; set; }
        private void StatusUpdate(string status) => OnStatusUpdate?.Invoke(this, status);

        // Initialize metadata object from a stream
        public static Metadata FromStream(MemoryStream stream, EventHandler<string> statusCallback = null) {
            // TODO: This should really be placed before the Metadata object is created,
            // but for now this ensures it is called regardless of which client is in use
            PluginHooks.LoadPipelineStarting();

            var metadata = new Metadata(statusCallback);
            stream.Position = 0;
            stream.CopyTo(metadata);
            metadata.Position = 0;
            metadata.Initialize();
            return metadata;
        }

        private Metadata(EventHandler<string> statusCallback = null) : base() => OnStatusUpdate = statusCallback;

        private void Initialize()
        {
            // Pre-processing hook
            var pluginResult = PluginHooks.PreProcessMetadata(this);
            IsModified = pluginResult.IsStreamModified;

            StatusUpdate("Processing metadata");

            // Read metadata header
            Header = ReadObject<Il2CppGlobalMetadataHeader>(0);

            // Check for correct magic bytes
            if (!Header.SanityValid) {
                throw new InvalidOperationException("The supplied metadata file is not valid.");
            }

            // Set object versioning for Bin2Object from metadata version
            Version = new StructVersion(Header.Version);

            if (Version < MetadataVersions.V160 || Version > MetadataVersions.V310) {
                throw new InvalidOperationException($"The supplied metadata file is not of a supported version ({Header.Version}).");
            }

            // Rewind and read metadata header with the correct version settings
            Header = ReadObject<Il2CppGlobalMetadataHeader>(0);

            // Sanity checking
            // Unity.IL2CPP.MetadataCacheWriter.WriteLibIl2CppMetadata always writes the metadata information in the same order it appears in the header,
            // with each block always coming directly after the previous block, 4-byte aligned. We can use this to check the integrity of the data and
            // detect sub-versions.

            // For metadata v24.0, the header can either be either 0x110 (24.0, 24.1) or 0x108 (24.2) bytes long. Since 'stringLiteralOffset' is the first thing
            // in the header after the sanity and version fields, and since it will always point directly to the first byte after the end of the header,
            // we can use this value to determine the actual header length and therefore narrow down the metadata version to 24.0/24.1 or 24.2.

            if (!pluginResult.SkipValidation) {
                var realHeaderLength = Header.StringLiteralOffset;

                if (realHeaderLength != Sizeof(typeof(Il2CppGlobalMetadataHeader))) {
                    if (Version == MetadataVersions.V240) {
                        Version = MetadataVersions.V242;
                        Header = ReadObject<Il2CppGlobalMetadataHeader>(0);
                    }
                }

                if (realHeaderLength != Sizeof(typeof(Il2CppGlobalMetadataHeader))) {
                    throw new InvalidOperationException("Could not verify the integrity of the metadata file or accurately identify the metadata sub-version");
                }
            }
            
            // Load all the relevant metadata using offsets provided in the header
            if (Version >= MetadataVersions.V160)
                Images = ReadArray<Il2CppImageDefinition>(Header.ImagesOffset,  Header.ImagesSize / Sizeof(typeof(Il2CppImageDefinition)));

            // As an additional sanity check, all images in the metadata should have Mono.Cecil.MetadataToken == 1
            // In metadata v24.1, two extra fields were added which will cause the below test to fail.
            // In that case, we can then adjust the version number and reload
            // Tokens were introduced in v19 - we don't bother testing earlier versions
            if (Version >= MetadataVersions.V190 && Images.Any(x => x.Token != 1))
                if (Version == MetadataVersions.V240) {
                    Version = MetadataVersions.V241;

                    // No need to re-read the header, it's the same for both sub-versions
                    Images = ReadArray<Il2CppImageDefinition>(Header.ImagesOffset, Header.ImagesSize / Sizeof(typeof(Il2CppImageDefinition)));

                    if (Images.Any(x => x.Token != 1))
                        throw new InvalidOperationException("Could not verify the integrity of the metadata file image list");
                }

            Types = ReadArray<Il2CppTypeDefinition>(Header.TypeDefinitionsOffset, Header.TypeDefinitionsSize / Sizeof(typeof(Il2CppTypeDefinition)));
            Methods = ReadArray<Il2CppMethodDefinition>(Header.MethodsOffset, Header.MethodsSize / Sizeof(typeof(Il2CppMethodDefinition)));
            Params = ReadArray<Il2CppParameterDefinition>(Header.ParametersOffset, Header.ParametersSize / Sizeof(typeof(Il2CppParameterDefinition)));
            Fields = ReadArray<Il2CppFieldDefinition>(Header.FieldsOffset, Header.FieldsSize / Sizeof(typeof(Il2CppFieldDefinition)));
            FieldDefaultValues = ReadArray<Il2CppFieldDefaultValue>(Header.FieldDefaultValuesOffset, Header.FieldDefaultValuesSize / Sizeof(typeof(Il2CppFieldDefaultValue)));
            Properties = ReadArray<Il2CppPropertyDefinition>(Header.PropertiesOffset, Header.PropertiesSize / Sizeof(typeof(Il2CppPropertyDefinition)));
            Events = ReadArray<Il2CppEventDefinition>(Header.EventsOffset, Header.EventsSize / Sizeof(typeof(Il2CppEventDefinition)));
            InterfaceUsageIndices = ReadArray<int>(Header.InterfacesOffset, Header.InterfacesSize / sizeof(int));
            NestedTypeIndices = ReadArray<int>(Header.NestedTypesOffset, Header.NestedTypesSize / sizeof(int));
            GenericContainers = ReadArray<Il2CppGenericContainer>(Header.GenericContainersOffset, Header.GenericContainersSize / Sizeof(typeof(Il2CppGenericContainer)));
            GenericParameters = ReadArray<Il2CppGenericParameter>(Header.GenericParametersOffset, Header.GenericParametersSize / Sizeof(typeof(Il2CppGenericParameter)));
            GenericConstraintIndices = ReadArray<int>(Header.GenericParameterConstraintsOffset, Header.GenericParameterConstraintsSize / sizeof(int));
            InterfaceOffsets = ReadArray<Il2CppInterfaceOffsetPair>(Header.InterfaceOffsetsOffset, Header.InterfaceOffsetsSize / Sizeof(typeof(Il2CppInterfaceOffsetPair)));
            VTableMethodIndices = ReadArray<uint>(Header.VTableMethodsOffset, Header.VTableMethodsSize / sizeof(uint));

            if (Version >= MetadataVersions.V160) {
                // In v24.4 hashValueIndex was removed from Il2CppAssemblyNameDefinition, which is a field in Il2CppAssemblyDefinition
                // The number of images and assemblies should be the same. If they are not, we deduce that we are using v24.4
                // Note the version comparison matches both 24.2 and 24.3 here since 24.3 is tested for during binary loading
                var assemblyCount = Header.AssembliesSize / Sizeof(typeof(Il2CppAssemblyDefinition));
                var changedAssemblyDefStruct = false;
                if ((Version == MetadataVersions.V241 || Version == MetadataVersions.V242 || Version == MetadataVersions.V243) && assemblyCount < Images.Length)
                {
                    if (Version == MetadataVersions.V241)
                        changedAssemblyDefStruct = true;
                    Version = MetadataVersions.V244;
                }

                Assemblies = ReadArray<Il2CppAssemblyDefinition>(Header.AssembliesOffset, Images.Length);

                if (changedAssemblyDefStruct)
                    Version = MetadataVersions.V241;

                ParameterDefaultValues = ReadArray<Il2CppParameterDefaultValue>(Header.ParameterDefaultValuesOffset, Header.ParameterDefaultValuesSize / Sizeof(typeof(Il2CppParameterDefaultValue)));
            }
            if (Version >= MetadataVersions.V190 && Version < MetadataVersions.V270) {
                MetadataUsageLists = ReadArray<Il2CppMetadataUsageList>(Header.MetadataUsageListsOffset, Header.MetadataUsageListsCount / Sizeof(typeof(Il2CppMetadataUsageList)));
                MetadataUsagePairs = ReadArray<Il2CppMetadataUsagePair>(Header.MetadataUsagePairsOffset, Header.MetadataUsagePairsCount / Sizeof(typeof(Il2CppMetadataUsagePair)));
            }
            if (Version >= MetadataVersions.V190) {
                FieldRefs = ReadArray<Il2CppFieldRef>(Header.FieldRefsOffset, Header.FieldRefsSize / Sizeof(typeof(Il2CppFieldRef)));
            }
            if (Version >= MetadataVersions.V210 && Version < MetadataVersions.V290) {
                AttributeTypeIndices = ReadArray<int>(Header.AttributesTypesOffset, Header.AttributesTypesCount / sizeof(int));
                AttributeTypeRanges = ReadArray<Il2CppCustomAttributeTypeRange>(Header.AttributesInfoOffset, Header.AttributesInfoCount / Sizeof(typeof(Il2CppCustomAttributeTypeRange)));
            }

            if (Version >= MetadataVersions.V290)
            {
                AttributeDataRanges = ReadArray<Il2CppCustomAttributeDataRange>(Header.AttributeDataRangeOffset,
                    Header.AttributeDataRangeSize / Sizeof(typeof(Il2CppCustomAttributeDataRange)));
            }

            if (Version == MetadataVersions.V290 || Version == MetadataVersions.V310)
            {
                // 29.2/31.2 added a new isUnmanagedCallersOnly flag to Il2CppMethodDefinition.
                // This offsets all subsequent entries by one - we can detect this by checking the
                // top token byte (which should always be 0x06).

                if (Methods.Length >= 2)
                {
                    var secondToken = Methods[1].Token;
                    if (secondToken >> 24 != 0x6)
                    {
                        Version = new StructVersion(Version.Major, 1, Version.Tag);

                        Methods = ReadArray<Il2CppMethodDefinition>(Header.MethodsOffset,
                            Header.MethodsSize / Sizeof(typeof(Il2CppMethodDefinition)));
                    }
                }
            }

            // Get all metadata strings
            var pluginGetStringsResult = PluginHooks.GetStrings(this);
            if (pluginGetStringsResult.IsDataModified && !pluginGetStringsResult.IsInvalid)
                Strings = pluginGetStringsResult.Strings;

            else {
                Position = Header.StringOffset;

                while (Position < Header.StringOffset + Header.StringSize)
                    Strings.Add((int) Position - Header.StringOffset, ReadNullTerminatedString());
            }

            // Get all string literals
            var pluginGetStringLiteralsResult = PluginHooks.GetStringLiterals(this);
            if (pluginGetStringLiteralsResult.IsDataModified)
                StringLiterals = pluginGetStringLiteralsResult.StringLiterals.ToArray();

            else {
                var stringLiteralList = ReadArray<Il2CppStringLiteral>(Header.StringLiteralOffset, Header.StringLiteralSize / Sizeof(typeof(Il2CppStringLiteral)));

                StringLiterals = new string[stringLiteralList.Length];
                for (var i = 0; i < stringLiteralList.Length; i++)
                    StringLiterals[i] = ReadFixedLengthString(Header.StringLiteralDataOffset + stringLiteralList[i].DataIndex, (int)stringLiteralList[i].Length);
            }

            // Post-processing hook
            IsModified |= PluginHooks.PostProcessMetadata(this).IsStreamModified;
        }

        // Save metadata to file, overwriting if necessary
        public void SaveToFile(string pathname) {
            Position = 0;
            using (var outFile = new FileStream(pathname, FileMode.Create, FileAccess.Write))
                CopyTo(outFile);
        }

        public int Sizeof(Type type) => Sizeof(type, Version);
        
        public int Sizeof(Type type, StructVersion metadataVersion, int longSizeBytes = 8)
        {
            var doubleRepresentation = metadataVersion.AsDouble;

            if (Reader.ObjectMappings.TryGetValue(type, out var streamType))
                type = streamType;

            int size = 0;
            foreach (var i in type.GetTypeInfo().GetFields())
            {
                // Only process fields for our selected object versioning (always process if none supplied)
                var versions = i.GetCustomAttributes<VersionAttribute>(false).Select(v => (v.Min, v.Max)).ToList();
                if (versions.Any() && !versions.Any(v => (v.Min <= doubleRepresentation || v.Min == -1) && (v.Max >= doubleRepresentation || v.Max == -1)))
                    continue;

                if (i.FieldType == typeof(long) || i.FieldType == typeof(ulong))
                    size += longSizeBytes;
                else if (i.FieldType == typeof(int) || i.FieldType == typeof(uint))
                    size += 4;
                else if (i.FieldType == typeof(short) || i.FieldType == typeof(ushort))
                    size += 2;

                // Fixed-length array
                else if (i.FieldType.IsArray) {
                    var attr = i.GetCustomAttribute<ArrayLengthAttribute>(false) ??
                               throw new InvalidOperationException("Array field " + i.Name + " must have ArrayLength attribute");
                    size += attr.FixedSize;
                }

                // Embedded object
                else
                    size += Sizeof(i.FieldType, metadataVersion);
            }
            return size;
        }
    }
}
