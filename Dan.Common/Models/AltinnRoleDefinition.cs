namespace Dan.Common.Models;

/// <summary>
/// The Altinn response model for Roles 
/// </summary>
public class AltinnRoleDefinition
{
    /// <summary>
    /// Type of Role (altinn or other)
    /// </summary>
    public string RoleType { get; set; }

    /// <summary>
    /// The unique id
    /// </summary>
    public int RoleDefinitionId { get; set; }

    /// <summary>
    /// The name of the role in Altinn
    /// </summary>
    public string RoleName { get; set; }

    /// <summary>
    /// Role definition description
    /// </summary>
    public string RoleDescription { get; set; }

    /// <summary>
    /// The unique role code - same across all of Altinn's environments, whilst the id may differ
    /// </summary>
    public string RoleDefinitionCode { get; set; }
}