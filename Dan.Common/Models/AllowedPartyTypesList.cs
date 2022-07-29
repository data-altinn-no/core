namespace Dan.Common.Models;

/// <summary>
/// Helper type for lists
/// </summary>
[DataContract]
public class AllowedPartyTypesList : List<KeyValuePair<AccreditationPartyTypes, PartyTypeConstraint>>
{
}