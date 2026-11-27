using System.Diagnostics;
using Azure.Monitor.OpenTelemetry.Exporter;
using Dan.Common.Telemetry;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;

namespace Dan.Common.Extensions;

/// <summary>
/// Telemetry setup shared by the plugins, which get it through
/// <see cref="HostBuilderExtensions.ConfigureDanPluginDefaults"/>, and Core, which has its own host setup
/// </summary>
public static class TelemetryExtensions
{
    /// <summary>
    ///     Sends log records to OpenTelemetry, with national identity numbers masked and without the request lines of
    ///     the HttpClient factory. Where they are exported is set up by <see cref="AddDanOpenTelemetry"/>.
    /// </summary>
    /// <param name="logging">The logging builder</param>
    /// <returns>The logging builder for additional chaining</returns>
    public static ILoggingBuilder AddDanOpenTelemetryLogging(this ILoggingBuilder logging)
    {
        logging.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            // Mask national identity numbers in log messages and attributes before export. Added here, before
            // UseAzureMonitorExporter registers its export processor, so it runs first.
            options.AddProcessor(new SsnRedactingLogProcessor());
        });

        // The HttpClient factory logs "Start processing HTTP request GET <url>" at Information level, with the
        // full URL. Many data sources put the subject's national identity number in the URL, so keep these lines
        // out of the logs. Outgoing calls are still traced as dependencies (masked by SsnRedactingProcessor).
        // The factory's logging handlers are also removed in AddDanOpenTelemetry; this filter additionally covers
        // HttpClients whose loggers are added back.
        // The rule is bound to the OpenTelemetry provider as well, because a provider-specific rule (e.g. from a
        // "Logging:OpenTelemetry:LogLevel" section bound by Host.CreateDefaultBuilder) would otherwise win over a
        // rule without a provider.
        logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
        logging.AddFilter<OpenTelemetryLoggerProvider>("System.Net.Http.HttpClient", LogLevel.Warning);

        return logging;
    }

    /// <summary>
    ///     Exports traces, metrics and logs to Azure Monitor/Application Insights through OpenTelemetry, with national
    ///     identity numbers masked. Log lines are never sampled, so every one is exported, including warnings, errors
    ///     and logged exceptions. Requests and dependencies are all exported too unless
    ///     <paramref name="maxTracesPerSecond"/> is given. Without a connection string or instrumentation key in
    ///     configuration nothing is exported, which is the normal case in a local run.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">
    ///     Configuration holding APPLICATIONINSIGHTS_CONNECTION_STRING or APPINSIGHTS_INSTRUMENTATIONKEY
    /// </param>
    /// <param name="maxTracesPerSecond">
    ///     The number of operations per second, in each instance, that are exported with their requests and
    ///     dependencies. An operation is kept or left out as a whole. Null, the default, exports every one.
    /// </param>
    /// <returns>The service collection for additional chaining</returns>
    public static IServiceCollection AddDanOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        double? maxTracesPerSecond = null)
    {
        // Remove the HttpClient factory's logging handlers from all clients. Besides the request/response log
        // lines (see the filter in AddDanOpenTelemetryLogging), they open a logging scope "HTTP GET <full url>"
        // around the whole request. With IncludeScopes enabled, every log record written inside the pipeline (e.g.
        // by the Maskinporten token handler) would carry that URL, including any national identity number in it.
        // Filtering the category does not prevent the scope, so the handlers are removed entirely.
        services.ConfigureHttpClientDefaults(httpClient => httpClient.RemoveAllLoggers());

        // UseAzureMonitorExporter() throws at startup if no connection string is configured,
        // so it's gated here to keep things working locally without one.
        var appInsightsConnectionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
        var instrumentationKey = configuration["APPINSIGHTS_INSTRUMENTATIONKEY"];

        if (string.IsNullOrWhiteSpace(appInsightsConnectionString) && !string.IsNullOrWhiteSpace(instrumentationKey))
        {
            appInsightsConnectionString = $"InstrumentationKey={instrumentationKey}";
        }

        if (string.IsNullOrWhiteSpace(appInsightsConnectionString))
        {
            return services;
        }

        // Azure SDK clients (Key Vault, Cosmos, Storage, ...) only emit spans through ActivitySource when this
        // switch is on. The classic Application Insights SDK collected these dependencies automatically.
        AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);

        services.AddOpenTelemetry()
            // SsnRedactingProcessor masks national identity numbers in span tags and names. It is added
            // here, before UseAzureMonitorExporter, so it runs ahead of the export processor.
            .WithTracing(tracing => tracing
                .AddProcessor<SsnRedactingProcessor>()
                .AddHttpClientInstrumentation(httpClient =>
                {
                    // Azure SDK clients create their own HTTP span (source "Azure.Core.Http") around the
                    // HttpClient call. Skip the generic HttpClient span in that case, so each wire request
                    // gives one dependency record and keeps the Azure request identifiers.
                    httpClient.FilterHttpRequestMessage = _ =>
                        Activity.Current?.Parent?.Source.Name != "Azure.Core.Http";
                })
                .AddSource("Azure.*"))
            .UseAzureMonitorExporter(options =>
            {
                options.ConnectionString = appInsightsConnectionString;
                // Export logs even when the trace they belong to is not sampled,
                // so error logs are never silently dropped.
                options.EnableTraceBasedLogsSampler = false;
                // Without a rate, sampling is off (the exporter's default is rate-limited to 5 traces/sec) so
                // all telemetry is exported. Note: this is a deliberate change from the classic Application
                // Insights SDK, which had adaptive sampling on by default in the worker. Expect higher
                // ingestion volume. With a rate, the exporter keeps that many traces per second and leaves out
                // the rest, each trace as a whole. Sampling in the Functions host is controlled separately by
                // the OTEL_TRACES_SAMPLER and OTEL_TRACES_SAMPLER_ARG app settings.
                options.SamplingRatio = 1.0f;
                options.TracesPerSecond = maxTracesPerSecond;
            })
            .UseFunctionsWorkerDefaults();

        return services;
    }
}
