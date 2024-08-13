namespace VersionedSerialization.Attributes;

#pragma warning disable CS9113 // Parameter is unread.
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class AlignedAttribute(int alignment) : Attribute;
#pragma warning restore CS9113 // Parameter is unread.
