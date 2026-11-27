using System.Reflection;
using Azure.Core.Serialization;
using Azure.Identity;
using Dan.Common.Handlers;
using Dan.Common.Interfaces;
using Dan.Common.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
    ///     handling telemetry (OpenTelemetry exported to Azure Monitor/Application Insights, with national identity numbers
    ///     masked), logging and correct JSON serialization settings. Also adds defaults services;
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
            .ConfigureLogging(logging => logging.AddDanOpenTelemetryLogging())
            .ConfigureServices((context, services) =>
            {
                services.AddLogging();

                // Telemetry is shared with Core, see TelemetryExtensions
                services.AddDanOpenTelemetry(context.Configuration);
                services.AddHttpClient();

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