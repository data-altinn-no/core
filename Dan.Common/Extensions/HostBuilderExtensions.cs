using System.Reflection;
using Azure.Core.Serialization;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using Dan.Common.Handlers;
using Dan.Common.Interfaces;
using Dan.Common.Services;
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
            })
            .ConfigureServices((context, services) =>
            {
                services.AddLogging();
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
                        .WithTracing(tracing => tracing.AddHttpClientInstrumentation())
                        .UseAzureMonitorExporter(options => options.ConnectionString = appInsightsConnectionString)
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