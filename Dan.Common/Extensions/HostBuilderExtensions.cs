using System.Reflection;
using Azure.Core.Serialization;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using Dan.Common.Handlers;
using Dan.Common.Interfaces;
using Dan.Common.Services;
using Dan.Common.Telemetry;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using Polly;
using Polly.Extensions.Http;
using Polly.Registry;

namespace Dan.Common.Extensions;

/// <summary>
/// HostBuilder extensions for setting up Dan plugin default configurations
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    ///     Sets up the isolated worker function with default configuration and wiring with ConfigureFunctionsWorkerDefaults(),
    ///     handling application insights, logging and correct JSON serialization settings. Also adds defaults services;
    ///     HttpClientFactory with a circuit-breaker enabled named client (use Constants.SafeHttpClient) which should be
    ///     used for outbound requests to the data source. Also expects to find a service implementing IEvidenceSourceMetadata.
    /// </summary>
    /// <param name="builder">The host builder</param>
    /// <returns>The host builder for additional chaining</returns>
    /// <exception cref="NotImplementedException">Thrown if IEvidenceSourceMetadata is not implemented in the same assembly</exception>
    public static IHostBuilder ConfigureDanPluginDefaults(this IHostBuilder builder)
    {
        builder
            .ConfigureFunctionsWorkerDefaults(_ =>{}, options =>
            {
                options.Serializer = new NewtonsoftJsonObjectSerializer(
                    // Use Newtonsoft.Json for serializing in order to support TypeNameHandling and other annotations on. This should be ported to System.Text.Json at some point.
                    new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Auto,
                        NullValueHandling = NullValueHandling.Ignore
                    });
            })
            .ConfigureAppConfiguration((config) =>
            {
                config.AddJsonFile("host.json", optional: true);
                config.AddJsonFile("worker.json", optional: true);
            })
            .ConfigureLogging(logging =>
            {
                logging.AddOpenTelemetry(options =>
                {
                    options.IncludeFormattedMessage = true;
                    options.IncludeScopes = true;
                });

                // The HttpClient factory logs "Start processing HTTP request GET <url>" at Information level, with the
                // full URL. Many data sources put the subject's national identity number in the URL, so keep these lines
                // out of the logs. Outgoing calls are still traced as dependencies (masked by SsnRedactingProcessor).
                // The factory's logging handlers are also removed below; this filter additionally covers HttpClients
                // whose loggers are added back by a plugin.
                // Note: logLevel in host.json does not apply here, as worker logs are exported directly via OpenTelemetry.
                logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddLogging();

                // Remove the HttpClient factory's logging handlers from all clients. Besides the request/response log
                // lines (see the filter above), they open a logging scope "HTTP GET <full url>" around the whole request.
                // With IncludeScopes enabled, every log record written inside the pipeline (e.g. by the Maskinporten
                // token handler) would carry that URL, including any national identity number in it. Filtering the
                // category does not prevent the scope, so the handlers are removed entirely.
                services.ConfigureHttpClientDefaults(httpClient => httpClient.RemoveAllLoggers());
                services.AddHttpClient();

                // UseAzureMonitorExporter() throws at startup if no connection string is configured,
                // so it's gated here to keep plugins working locally without one.
                var appInsightsConnectionString = context.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
                var instrumentationKey = context.Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"];

                if(string.IsNullOrWhiteSpace(appInsightsConnectionString) && !string.IsNullOrWhiteSpace(instrumentationKey))
                {
                    appInsightsConnectionString = $"InstrumentationKey={instrumentationKey}";
                }

                if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
                {
                    services.AddOpenTelemetry()
                        // SsnRedactingProcessor masks national identity numbers in span URLs and names. It is added
                        // here, before UseAzureMonitorExporter, so it runs ahead of the export processor.
                        .WithTracing(tracing => tracing
                            .AddProcessor<SsnRedactingProcessor>()
                            .AddHttpClientInstrumentation())
                        .UseAzureMonitorExporter(options =>
                        {
                            options.ConnectionString = appInsightsConnectionString;
                            // Export logs even when the trace they belong to is not sampled,
                            // so error logs are never silently dropped.
                            options.EnableTraceBasedLogsSampler = false;
                            // Disable sampling (default is rate-limited to 5 traces/sec) so all
                            // telemetry is exported, matching the previous Application Insights setup.
                            options.SamplingRatio = 1.0f;
                            options.TracesPerSecond = null;
                        })
                        .UseFunctionsWorkerDefaults();
                }

                var openCircuitTimeSeconds =
                    int.TryParse(context.Configuration["DefaultCircuitBreakerOpenCircuitTimeSeconds"], out var result)
                        ? result
                        : 10;
                var failuresBeforeTripping =
                    int.TryParse(context.Configuration["DefaultCircuitBreakerFailureBeforeTripping"], out result)
                        ? result
                        : 4;

                var registry = new PolicyRegistry
                {
                    {
                        Constants.SafeHttpClientPolicy,
                        HttpPolicyExtensions.HandleTransientHttpError()
                            .CircuitBreakerAsync(
                                failuresBeforeTripping,
                                TimeSpan.FromSeconds(openCircuitTimeSeconds))
                    }
                };
                services.AddPolicyRegistry(registry);

                var httpClientTimeoutSeconds = int.TryParse(context.Configuration["SafeHttpClientTimeout"], out result)
                    ? result
                    : 30;

                // Client configured with circuit breaker policies
                services.AddHttpClient(Constants.SafeHttpClient,
                        client => { client.Timeout = TimeSpan.FromSeconds(httpClientTimeoutSeconds); })
                    .AddPolicyHandlerFromRegistry(Constants.SafeHttpClientPolicy);
                
                // Using safehttpclient settings, but will add auth handler for talking with plugins
                services.AddHttpClient(Constants.PluginHttpClient,
                        client => { client.Timeout = TimeSpan.FromSeconds(httpClientTimeoutSeconds); })
                    .AddPolicyHandlerFromRegistry(Constants.SafeHttpClientPolicy)
                    .AddHttpMessageHandler<PluginAuthorizationMessageHandler>();

                // Add a common service to fetch information from the CCR ("Enhetsregisteret"). Using a default API-client (which just wraps a HttpClient), which
                // calls a proxy in Core by default. Core uses the same service, but a different IEntityRegistryApiClientService which utilizes a distributed cache.
                services.AddSingleton<IEntityRegistryService, EntityRegistryService>();
                services.AddSingleton<IEntityRegistryApiClientService, DefaultEntityRegistryApiClientService>();
                services.AddSingleton<IPluginCredentialService, PluginCredentialService>();

                services.AddMemoryCache();
                services.AddTransient<PluginAuthorizationMessageHandler>();
                services.AddTransient<IDanPluginClientService, DanPluginClientService>();
                services.AddTransient<ICcrClientService, CcrClientService>();

                // This can be overwritten by plugins by doing their own registrations if needing special options
                var defaultCredentials = new DefaultAzureCredential();
                services.AddSingleton(defaultCredentials);

                // Try to add the first IEvidenceSourceMetadata implementation we can find in the entry assembly
                var evidenceSourceMetadataServiceType = typeof(IEvidenceSourceMetadata);
                var assembly = Assembly.GetEntryAssembly();

                var implementationType = assembly?.GetTypes()
                    .FirstOrDefault(t => t.GetInterfaces().Any(i => i == evidenceSourceMetadataServiceType));

                if (implementationType == null)
                    throw new NotImplementedException(
                        $"Missing implementation of {nameof(IEvidenceSourceMetadata)} in entry assembly {assembly?.FullName ?? "(unmanaged assembly)"}");

                if (!services.Any(s =>
                        s.ServiceType == evidenceSourceMetadataServiceType &&
                        s.ImplementationType == implementationType))
                    services.Add(new ServiceDescriptor(evidenceSourceMetadataServiceType, implementationType,
                        ServiceLifetime.Singleton));
            });

        return builder;
    }
}