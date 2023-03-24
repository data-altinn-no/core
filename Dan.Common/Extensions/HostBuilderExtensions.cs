using System.Reflection;
using Azure.Core.Serialization;
using Dan.Common.Interfaces;
using Dan.Common.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Polly.Registry;

namespace Dan.Common.Extensions;

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
            .ConfigureFunctionsWorkerDefaults(workerBuilder =>
            {
                workerBuilder
                    // Using preview package Microsoft.Azure.Functions.Worker.ApplicationInsights, see https://github.com/Azure/azure-functions-dotnet-worker/pull/944
                    // Requires APPLICATIONINSIGHTS_CONNECTION_STRING being set. Note that host.json logging settings are not loaded to worker, and requires
                    .AddApplicationInsights()
                    .AddApplicationInsightsLogger();
            }, options =>
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
            .ConfigureServices((context, services) =>
            {
                services.AddLogging();
                services.AddHttpClient();

                // You will need extra configuration because AI will only log per default Warning (default AI configuration). As this is a provider-specific
                // setting, it will override all non-provider (Logging:LogLevel)-based configurations. 
                // https://github.com/microsoft/ApplicationInsights-dotnet/blob/main/NETCORE/src/Shared/Extensions/ApplicationInsightsExtensions.cs#L427
                // https://github.com/microsoft/ApplicationInsights-dotnet/issues/2610#issuecomment-1316672650
                // https://github.com/Azure/azure-functions-dotnet-worker/issues/1182#issuecomment-1319035412
                // So remove the default logger rule (warning and above). This will result that the default will be Information.
                services.Configure<LoggerFilterOptions>(options =>
                {
                    var toRemove = options.Rules.FirstOrDefault(rule => rule.ProviderName
                        == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");

                    if (toRemove is not null)
                    {
                        options.Rules.Remove(toRemove);
                    }
                });
                
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

                // Add a common service to fetch information from the CCR ("Enhetsregisteret"). Using a default API-client (which just wraps a HttpClient), which
                // calls a proxy in Core by default. Core uses the same service, but a different IEntityRegistryApiClientService which utilizes a distributed cache.
                services.AddSingleton<IEntityRegistryService, EntityRegistryService>();
                services.AddSingleton<IEntityRegistryApiClientService, DefaultEntityRegistryApiClientService>();

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