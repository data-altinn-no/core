using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Core.Serialization;
using Dan.Common.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Extensions.Http;
using Polly.Registry;

namespace Dan.Common.Extensions;

public static class HostBuilderExtensions
{
    /// <summary>
    /// Sets up the isolated worker function with default configuration and wiring with ConfigureFunctionsWorkerDefaults(), handling application insights
    /// logging and correct JSON serialization settings. Also adds defaults services; HttpClientFactory with a circuit-breaker enabled named client (use Constants.SafeHttpClient)
    /// which should be used for outbound requests to the data source. Also expects to find a service implementing IEvidenceSourceMetadata.
    /// </summary>
    /// <param name="builder">The host builder</param>
    /// <returns>The host builder for additional chaining</returns>
    /// <exception cref="NotImplementedException">Thrown if IEvidenceSourceMetadata is not implemented in the same assembly</exception>
    public static IHostBuilder ConfigureDanPluginDefaults(this IHostBuilder builder)
    {
        builder.ConfigureFunctionsWorkerDefaults(workerBuilder =>
        {
            workerBuilder
                // Using preview package Microsoft.Azure.Functions.Worker.ApplicationInsights, see https://github.com/Azure/azure-functions-dotnet-worker/pull/944
                // Requires APPLICATIONINSIGHTS_CONNECTION_STRING being set. Note that host.json logging settings will have to be replicated to worker.json
                .AddApplicationInsights()
                .AddApplicationInsightsLogger();
        }, options => { options.Serializer = new NewtonsoftJsonObjectSerializer(); })
            .ConfigureServices((context, services) =>
            {
                services.AddLogging();
                services.AddHttpClient();

                var openCircuitTimeSeconds = int.TryParse(context.Configuration["DefaultCircuitBreakerOpenCircuitTimeSeconds"], out var result) ? result : 10;
                var failuresBeforeTripping = int.TryParse(context.Configuration["DefaultCircuitBreakerFailureBeforeTripping"], out result) ? result : 4;

                var registry = new PolicyRegistry()
                {
                    {
                        Constants.SafeHttpClientPolicy,
                        HttpPolicyExtensions.HandleTransientHttpError()
                            .CircuitBreakerAsync(
                                handledEventsAllowedBeforeBreaking: failuresBeforeTripping,
                                durationOfBreak: TimeSpan.FromSeconds(openCircuitTimeSeconds))
                    }
                };
                services.AddPolicyRegistry(registry);

                var httpClientTimeoutSeconds = int.TryParse(context.Configuration["SafeHttpClientTimeout"], out result) ? result : 30;

                // Client configured with circuit breaker policies
                services.AddHttpClient(Constants.SafeHttpClient,
                        client => { client.Timeout = TimeSpan.FromSeconds(httpClientTimeoutSeconds); })
                    .AddPolicyHandlerFromRegistry(Constants.SafeHttpClientPolicy);

                services.Configure<JsonSerializerOptions>(options =>
                {
                    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                    options.Converters.Add(new JsonStringEnumConverter());
                });

                // Try to add the first IEvidenceSourceMetadata implementation we can find
                var evidenceSourceMetadataServiceType = typeof(IEvidenceSourceMetadata);
                var assembly = Assembly.GetExecutingAssembly();

                var implementationType = assembly.GetTypes()
                   .FirstOrDefault(t => t.GetInterfaces().Any(i => i == evidenceSourceMetadataServiceType));

                if (implementationType == null)
                {
                    throw new NotImplementedException(
                        $"Missing implementation of {nameof(IEvidenceSourceMetadata)}");
                }

                if (!services.Any(s => s.ServiceType == evidenceSourceMetadataServiceType && s.ImplementationType == implementationType))
                {
                    services.Add(new ServiceDescriptor(evidenceSourceMetadataServiceType, implementationType, ServiceLifetime.Singleton));
                }
            });

        return builder;
    }
}