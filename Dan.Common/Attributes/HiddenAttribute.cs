namespace Dan.Common.Attributes;

/// <summary>
/// Attribute for marking properties as hidden. Will be present in serialized version in databases and other backing stores, but removed from output given the user
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class HiddenAttribute : Attribute
{
}