using VersionedSerialization;

namespace Il2CppInspector.Next;

public static class MetadataVersions
{
    public static readonly StructVersion V160 = new(16);

    public static readonly StructVersion V190 = new(19);

    public static readonly StructVersion V210 = new(21);
    public static readonly StructVersion V220 = new(22);

    public static readonly StructVersion V240 = new(24);
    public static readonly StructVersion V241 = new(24, 1);
    public static readonly StructVersion V242 = new(24, 2);
    public static readonly StructVersion V243 = new(24, 3);
    public static readonly StructVersion V244 = new(24, 4);
    public static readonly StructVersion V245 = new(24, 5);

    public static readonly StructVersion V270 = new(27);
    public static readonly StructVersion V271 = new(27, 1);
    public static readonly StructVersion V272 = new(27, 2);

    public static readonly StructVersion V290 = new(29);
    public static readonly StructVersion V310 = new(31);

    public static readonly StructVersion V291 = new(29, 1);
    public static readonly StructVersion V311 = new(31, 1);

    public static readonly StructVersion V292 = new(29, 2);
    public static readonly StructVersion V312 = new(31, 2);
}