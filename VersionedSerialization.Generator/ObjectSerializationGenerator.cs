using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VersionedSerialization.Generator.Models;
using VersionedSerialization.Generator.Utils;

namespace VersionedSerialization.Generator
{
    [Generator]
    public sealed class ObjectSerializationGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            //Debugger.Launch();
            
            var valueProvider = context.SyntaxProvider
                .ForAttributeWithMetadataName(Constants.VersionedStructAttribute,
                    static (node, _) => node is ClassDeclarationSyntax or StructDeclarationSyntax,
                    static (context, _) => (ContextClass: (TypeDeclarationSyntax)context.TargetNode, context.SemanticModel))
                .Combine(context.CompilationProvider)
                .Select(static (tuple, cancellationToken) => ParseSerializationInfo(tuple.Left.ContextClass, tuple.Left.SemanticModel, tuple.Right, cancellationToken))
                .WithTrackingName(nameof(ObjectSerializationGenerator));

            context.RegisterSourceOutput(valueProvider, EmitCode);
        }

        private static void EmitCode(SourceProductionContext sourceProductionContext, ObjectSerializationInfo info)
        {
            var generator = new CodeGenerator();
            generator.AppendLine("#nullable restore");
            generator.AppendLine("using VersionedSerialization;");
            generator.AppendLine();

            generator.AppendLine($"namespace {info.Namespace};");

            var versions = new HashSet<StructVersion>();
            foreach (var condition in info.Properties.SelectMany(static x => x.VersionConditions))
            {
                if (condition.LessThan.HasValue)
                    versions.Add(condition.LessThan.Value);

                if (condition.GreaterThan.HasValue)
                    versions.Add(condition.GreaterThan.Value);

                if (condition.EqualTo.HasValue)
                    versions.Add(condition.EqualTo.Value);
            }

            if (versions.Count > 0)
            {
                generator.EnterScope("file static class Versions");

                foreach (var version in versions)
                {
                    generator.AppendLine($"public static readonly StructVersion {GetVersionIdentifier(version)} = \"{version}\";");
                }

                generator.LeaveScope();
            }

            generator.EnterScope($"public partial {(info.IsStruct ? "struct" : "class")} {info.Name} : IReadable");
            GenerateReadMethod(generator, info);
            generator.AppendLine();
            GenerateSizeMethod(generator, info);
            generator.LeaveScope();

            sourceProductionContext.AddSource($"{info.Namespace}.{info.Name}.g.cs", generator.ToString());
        }

        private static void GenerateSizeMethod(CodeGenerator generator, ObjectSerializationInfo info)
        {
            generator.EnterScope("public static int Size(in StructVersion version = default, bool is32Bit = false)");

            if (!info.CanGenerateSizeMethod)
            {
                generator.AppendLine("throw new InvalidOperationException(\"No size can be calculated for this struct.\");");
            }
            else
            {
                generator.AppendLine("var size = 0;");
                if (info.HasBaseType)
                    generator.AppendLine("size += base.Size(in version, is32Bit);");

                foreach (var property in info.Properties)
                {
                    if (property.VersionConditions.Length > 0)
                        GenerateVersionCondition(property.VersionConditions, generator);

                    generator.EnterScope();

                    generator.AppendLine($"size += {property.SizeExpression};");

                    if (property.Alignment != 0)
                        generator.AppendLine($"size += size % {property.Alignment} == 0 ? 0 : {property.Alignment} - (size % {property.Alignment});");

                    generator.LeaveScope();
                }

                generator.AppendLine("return size;");
            }

            generator.LeaveScope();
        }

        private static void GenerateReadMethod(CodeGenerator generator, ObjectSerializationInfo info)
        {
            generator.EnterScope("public void Read<TReader>(ref TReader reader, in StructVersion version = default) where TReader : IReader, allows ref struct");

            if (info.HasBaseType)
                generator.AppendLine("base.Read(ref reader, in version);");

            foreach (var property in info.Properties)
            {
                if (property.VersionConditions.Length > 0)
                    GenerateVersionCondition(property.VersionConditions, generator);

                generator.EnterScope();
                generator.AppendLine($"this.{property.Name} = {property.ReadMethod}");

                if (property.Alignment != 0)
                    generator.AppendLine($"reader.Align({property.Alignment});");

                generator.LeaveScope();
            }

            generator.LeaveScope();
        }

        private static string GetVersionIdentifier(StructVersion version)
            => $"V{version.Major}_{version.Minor}{(version.Tag == null ? "" : $"_{version.Tag}")}";

        private static void GenerateVersionCondition(ImmutableEquatableArray<VersionCondition> conditions,
            CodeGenerator generator)
        {
            generator.AppendLine("if (");
            generator.IncreaseIndentation();

            for (var i = 0; i < conditions.Length; i++)
            {
                generator.AppendLine("(true");

                var condition = conditions[i];
                if (condition.LessThan.HasValue)
                    generator.AppendLine($"&& Versions.{GetVersionIdentifier(condition.LessThan.Value)} >= version");

                if (condition.GreaterThan.HasValue)
                    generator.AppendLine($"&& version >= Versions.{GetVersionIdentifier(condition.GreaterThan.Value)}");

                if (condition.EqualTo.HasValue)
                    generator.AppendLine($"&& version == Versions.{GetVersionIdentifier(condition.EqualTo.Value)}");

                if (condition.IncludingTag != null)
                    generator.AppendLine($"&& version.Tag == \"{condition.IncludingTag}\"");

                if (condition.ExcludingTag != null)
                    generator.AppendLine($"&& version.Tag != \"{condition.ExcludingTag}\"");

                generator.AppendLine(")");

                if (i != conditions.Length - 1)
                    generator.AppendLine("||");
            }

            generator.DecreaseIndentation();
            generator.AppendLine(")");
        }

        private static ObjectSerializationInfo ParseSerializationInfo(TypeDeclarationSyntax contextClass,
            SemanticModel model, Compilation compilation,
            CancellationToken cancellationToken)
        {
            var classSymbol = model.GetDeclaredSymbol(contextClass, cancellationToken) ?? throw new InvalidOperationException();

            var alignedAttribute = compilation.GetTypeByMetadataName(Constants.AlignedAttribute);
            var versionConditionAttribute = compilation.GetTypeByMetadataName(Constants.VersionConditionAttribute);
            var customSerializationAttribute = compilation.GetTypeByMetadataName(Constants.CustomSerializationAttribute);

            var canGenerateSizeMethod = true;

            var properties = new List<PropertySerializationInfo>();
            foreach (var member in classSymbol.GetMembers())
            {
                if (member.IsStatic 
                    || member is IFieldSymbol { AssociatedSymbol: not null } 
                    || member is IPropertySymbol { SetMethod: null })
                    continue;

                var alignment = 0;
                var versionConditions = new List<VersionCondition>();

                ITypeSymbol type;
                switch (member)
                {
                    case IFieldSymbol field:
                        type = field.Type;
                        break;
                    case IPropertySymbol property:
                        type = property.Type;
                        break;
                    default:
                        continue;
                }

                var typeInfo = ParseType(type);

                canGenerateSizeMethod &= typeInfo.Type != PropertyType.String;

                string readMethod;
                if (typeInfo.Type == PropertyType.None)
                {
                    readMethod = $"reader.ReadVersionedObject<{typeInfo.ComplexTypeName}>(in version);";
                }
                else
                {
                    readMethod = typeInfo.Type.IsSeperateMethod()
                        ? $"reader.Read{typeInfo.Type.GetTypeName()}();"
                        : $"reader.ReadPrimitive<{typeInfo.Type.GetTypeName()}>();";

                    if (typeInfo.ComplexTypeName != "")
                        readMethod = $"({typeInfo.ComplexTypeName}){readMethod}";
                }

                string sizeExpression;
                if (typeInfo.Type == PropertyType.None)
                {
                    sizeExpression = $"{typeInfo.ComplexTypeName}.Size(in version, is32Bit)";
                }
                else
                {
                    sizeExpression = $"sizeof({typeInfo.Type.GetTypeName()})";
                }

                foreach (var attribute in member.GetAttributes())
                {
                    if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, alignedAttribute))
                    {
                        alignment = (int)attribute.ConstructorArguments[0].Value!;
                    } 
                    else if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, versionConditionAttribute))
                    {
                        StructVersion? lessThan = null,
                            moreThan = null,
                            equalTo = null;

                        string? includingTag = null,
                            excludingTag = null;

                        foreach (var argument in attribute.NamedArguments)
                        {
                            switch (argument.Key)
                            {
                                case Constants.LessThan:
                                    lessThan = (StructVersion)(string)argument.Value.Value!;
                                    break;
                                case Constants.GreaterThan:
                                    moreThan = (StructVersion)(string)argument.Value.Value!;
                                    break;
                                case Constants.EqualTo:
                                    equalTo = (StructVersion)(string)argument.Value.Value!;
                                    break;
                                case Constants.IncludingTag:
                                    includingTag = (string)argument.Value.Value!;
                                    break;
                                case Constants.ExcludingTag:
                                    excludingTag = (string)argument.Value.Value!;
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }

                        versionConditions.Add(new VersionCondition(lessThan, moreThan, equalTo, includingTag, excludingTag));
                    }
                    else if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, customSerializationAttribute))
                    {
                        readMethod = (string)attribute.ConstructorArguments[0].Value!;
                        sizeExpression = (string)attribute.ConstructorArguments[1].Value!;
                    }
                }

                properties.Add(new PropertySerializationInfo(
                    member.Name,
                    readMethod,
                    sizeExpression,
                    alignment,
                    versionConditions.ToImmutableEquatableArray()
                ));
            }

            var hasBaseType = false;
            if (classSymbol.BaseType != null)
            {
                var objectSymbol = compilation.GetSpecialType(SpecialType.System_Object);
                var valueTypeSymbol = compilation.GetSpecialType(SpecialType.System_ValueType);

                if (!SymbolEqualityComparer.Default.Equals(objectSymbol, classSymbol.BaseType)
                    && !SymbolEqualityComparer.Default.Equals(valueTypeSymbol, classSymbol.BaseType))
                    hasBaseType = true;
            }

            return new ObjectSerializationInfo(
                classSymbol.ContainingNamespace.ToDisplayString(),
                classSymbol.Name,
                hasBaseType,
                contextClass.Kind() == SyntaxKind.StructDeclaration,
                true,
                canGenerateSizeMethod,
                properties.ToImmutableEquatableArray()
            );
        }

        private static (PropertyType Type, string ComplexTypeName, bool IsArray) ParseType(ITypeSymbol typeSymbol)
        {
            switch (typeSymbol)
            {
                case IArrayTypeSymbol arrayTypeSymbol:
                {
                    var elementType = ParseType(arrayTypeSymbol.ElementType);
                    return (elementType.Type, elementType.ComplexTypeName, true);
                }
                case INamedTypeSymbol { EnumUnderlyingType: not null } namedTypeSymbol:
                    var res = ParseType(namedTypeSymbol.EnumUnderlyingType);
                    return (res.Type, typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), false);
            }

            if (typeSymbol.SpecialType != SpecialType.None)
            {
                var type = typeSymbol.SpecialType switch
                {
                    SpecialType.System_Boolean => PropertyType.Boolean,
                    SpecialType.System_Byte => PropertyType.UInt8,
                    SpecialType.System_UInt16 => PropertyType.UInt16,
                    SpecialType.System_UInt32 => PropertyType.UInt32,
                    SpecialType.System_UInt64 => PropertyType.UInt64,
                    SpecialType.System_SByte => PropertyType.Int8,
                    SpecialType.System_Int16 => PropertyType.Int16,
                    SpecialType.System_Int32 => PropertyType.Int32,
                    SpecialType.System_Int64 => PropertyType.Int64,
                    SpecialType.System_String => PropertyType.String,
                    _ => PropertyType.Unsupported
                };

                return (type, "", false);
            }

            var complexType = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            return (PropertyType.None, complexType, false);
        }
    }
}
