namespace Dan.Common.Enums;

/// <summary>
/// The type of legal basis, usually. ESPD
/// </summary>
[Flags]
public enum LegalBasisType
{
    /// <summary>
    /// Legal basis is a ESPD document
    /// </summary>
    [EnumMember(Value = "espd")]
    Espd = 1,

    /// <summary>
    /// Legal basis is a ESPD document
    /// </summary>
    [EnumMember(Value = "cpv")]
    Cpv = 2
}