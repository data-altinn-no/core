using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;

namespace Dan.Core.Extensions;

public static class ApplicationInsightsExtensions
{
    /// <summary>
    /// The rate adaptive sampling aims for, which is the default of the Application Insights SDK
    /// </summary>
    private const double MaxTelemetryItemsPerSecond = 5;

    /// <summary>
    /// Adds Application Insights for the worker process, with adaptive sampling of everything but log lines and
    /// exceptions.
    ///
    /// By default the SDK samples log lines together with dependencies and requests. A burst of dependencies then
    /// makes log lines written shortly after disappear, as happened to the summary of the pending consent sync after
    /// its several hundred calls to Altinn. Log lines and exceptions are few compared to dependencies, and are what
    /// operations are followed up by, so they are always sent.
    /// </summary>
    public static IServiceCollection AddCoreApplicationInsights(this IServiceCollection services)
    {
        // The samplers of the SDK are replaced by the ones below, as their types cannot be changed
        services.AddApplicationInsightsTelemetryWorkerService(options => options.EnableAdaptiveSampling = false);
        services.ConfigureFunctionsApplicationInsights();

        services.Configure<TelemetryConfiguration>(configuration =>
        {
            // Same as the SDK, which samples events separately from the rest, except for the excluded types
            var builder = configuration.DefaultTelemetrySink.TelemetryProcessorChainBuilder;
            builder.UseAdaptiveSampling(MaxTelemetryItemsPerSecond, excludedTypes: "Trace;Exception;Event");
            builder.UseAdaptiveSampling(MaxTelemetryItemsPerSecond, includedTypes: "Event");
            builder.Build();
        });

        return services;
    }
}
