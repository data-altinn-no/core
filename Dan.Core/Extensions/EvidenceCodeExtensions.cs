using Dan.Common.Models;
using Dan.Core.Config;

namespace Dan.Core.Extensions;

/// <summary>
/// Object extensions
/// </summary>
public static class EvidenceCodeExtensions
{
    /// <summary>
    /// Gets the full URL to the evidence code 
    /// </summary>
    /// <param name="evidenceCode">The evidence code instance</param>
    /// <returns>A fully qualified URL</returns>
    public static string GetEvidenceSourceUrl(this EvidenceCode evidenceCode)
    {
        return Settings.GetEvidenceSourceUrl(evidenceCode.EvidenceSource).Replace("/api/evidencecodes", $"/api/{evidenceCode.EvidenceCodeName}?code={Settings.FunctionKeyValue}");
    }

    /// <summary>
    /// Helper method wrapping both legacy ServiceContext and BelongsToServiceContext
    /// </summary>
    /// <param name="evidenceCode"></param>
    /// <returns>The full list of service contexts</returns>
    public static List<string> GetBelongsToServiceContexts(this EvidenceCode evidenceCode)
    {
        var listOfServiceContexts = new List<string>();
        listOfServiceContexts.AddRange(evidenceCode.BelongsToServiceContexts);

        if (evidenceCode.ServiceContext != null && !listOfServiceContexts.Contains(evidenceCode.ServiceContext))
        {
            listOfServiceContexts.Add(evidenceCode.ServiceContext);
        }

        return listOfServiceContexts;
    }

    /// <summary>
    /// Returns whether or not the supplied service context has access to the evidence code.
    /// If the evidence code does not define any service context binding, any service context is allowed.
    /// </summary>
    /// <param name="evidenceCode">The evidence code</param>
    /// <param name="serviceContextName">The name of the service context</param>
    /// <returns>True if available; otherwise false</returns>
    public static bool IsValidServiceContext(this EvidenceCode evidenceCode, string serviceContextName)
    {
        if (evidenceCode.BelongsToServiceContexts.Count == 0 && evidenceCode.ServiceContext == null)
        {
            return true;
        }

        return evidenceCode.GetBelongsToServiceContexts().Contains(serviceContextName);
    }

    /// <summary>
    /// Returns whether or not the supplied service context has access to the evidence code 
    /// If the evidence code does not define any service context binding, any service context is allowed.
    /// </summary>
    /// <param name="evidenceCode">The evidence code</param>
    /// <param name="serviceContext">The service context</param>
    /// <returns>True if available; otherwise false</returns>
    public static bool IsValidServiceContext(this EvidenceCode evidenceCode, ServiceContext serviceContext) => evidenceCode.IsValidServiceContext(serviceContext.Name);
}