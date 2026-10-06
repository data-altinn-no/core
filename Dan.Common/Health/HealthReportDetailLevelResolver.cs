using Altinn.AspNet.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Dan.Common.Health;

/// <summary>
/// Decides how much detail health responses expose. Mirrors the environment mapping in
/// Altinn.AspNet.HealthChecks (Development → Full, Production → Summary, otherwise Diagnostic),
/// with an explicit configuration override.
/// </summary>
public static class HealthReportDetailLevelResolver
{
    /// <summary>
    /// Configuration key (app setting) that overrides the environment-based default.
    /// Accepts any <see cref="HealthReportDetailLevel"/> name, e.g. <c>Diagnostic</c>.
    /// </summary>
    public const string ConfigurationKey = "HealthReportDetailLevel";

    /// <summary>
    /// Resolves the detail level for the current host.
    /// </summary>
    public static HealthReportDetailLevel Resolve(IHostEnvironment environment, IConfiguration configuration)
    {
        if (Enum.TryParse<HealthReportDetailLevel>(configuration[ConfigurationKey], ignoreCase: true, out var configured))
        {
            return configured;
        }

        // Dan.Core marks local runs with ASPNETCORE_ENVIRONMENT=LocalDevelopment; the Functions host itself
        // derives IHostEnvironment from AZURE_FUNCTIONS_ENVIRONMENT and defaults to Production.
        var isLocal = string.Equals(
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            "LocalDevelopment",
            StringComparison.OrdinalIgnoreCase);

        if (isLocal || environment.IsDevelopment())
        {
            return HealthReportDetailLevel.Full;
        }

        return environment.IsProduction()
            ? HealthReportDetailLevel.Summary
            : HealthReportDetailLevel.Diagnostic;
    }
}
