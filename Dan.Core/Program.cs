using System.Reflection;
using Azure.Core.Serialization;
using Dan.Common.Interfaces;
using Dan.Common.Models;
using Dan.Common.Services;
using Dan.Core.Attributes;
using Dan.Core.Config;
using Dan.Core.Extensions;
using Dan.Core.Helpers;
using Dan.Core.Middleware;
using Dan.Core.Services;
using Dan.Core.Services.Interfaces;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using Polly.Caching;
using Polly.Caching.Distributed;
using Polly.Caching.Serialization.Json;
using Polly.Extensions.Http;
using Polly.Registry;

IHostEnvironment danHostingEnvironment = new HostingEnvironment();
var host = new HostBuilder()
    .ConfigureAppConfiguration((hostContext, config) =>
    {
        config
            .AddEnvironmentVariables()
            .AddJsonFile("worker-logging.json", optional: true);

        danHostingEnvironment = hostContext.HostingEnvironment;
        if (danHostingEnvironment.IsLocalDevelopment())
        {
            config.AddUserSecrets(Assembly.GetExecutingAssembly(), true);
        }

        ConfigurationHelper.ConfigurationRoot = config.Build();
    })
    .ConfigureLogging((hostingContext, logging) =>
    {
        // This is required for the logging-options in worker-logging.json to ble applied
        logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
    })
    .ConfigureFunctionsWorkerDefaults(builder =>
    {
        builder
            .UseWhen<ExceptionHandlerMiddleware>(context => !context.HasAttribute(typeof(HtmlErrorAttribute)))
            .UseWhen<HtmlExceptionHandlerMiddleware>(context => context.HasAttribute(typeof(HtmlErrorAttribute)))
            .UseWhen<AuthenticationMiddleware>(context => !context.HasAttribute(typeof(NoAuthenticationAttribute)));

        builder.UseMiddleware<DiagnosticsHeaderInjectionMiddleware>();
        builder.UseMiddleware<FunctionContextAccessorMiddleware>();

        if (!danHostingEnvironment.IsLocalDevelopment())
        {
            // Using preview package Microsoft.Azure.Functions.Worker.ApplicationInsights, see https://github.com/Azure/azure-functions-dotnet-worker/pull/944
            // Requires APPLICATIONINSIGHTS_CONNECTION_STRING being set. Note that host.json logging settings only affects the host, not the workers. 
            // See worker-logging.json for other logging settings, and the discussion on https://github.com/Azure/azure-functions-dotnet-worker/issues/1182
            builder
                .AddApplicationInsights()
                .AddApplicationInsightsLogger();
        }

    }, options =>
    {
        options.Serializer = new NewtonsoftJsonObjectSerializer();
    })
    .ConfigureServices((_, services) =>
    {
        // You will need extra configuration because above will only log per default Warning (default AI configuration). As this is a provider-specific
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

        services.AddStackExchangeRedisCache(option =>
        {
            option.Configuration = Settings.RedisCacheConnectionString;
        });

        var sp = services.BuildServiceProvider();
        var distributedCache = sp.GetRequiredService<IDistributedCache>();

        services.AddSingleton(_ => new CosmosClientBuilder(Settings.CosmosDbConnection).Build());
        services.AddSingleton<IChannelManagerService, ChannelManagerService>();
        services.AddSingleton<IAltinnCorrespondenceService, AltinnCorrespondenceService>();
        services.AddSingleton<IAvailableEvidenceCodesService, AvailableEvidenceCodesService>();
        services.AddSingleton<IAltinnServiceOwnerApiService, AltinnServiceOwnerApiService>();
        services.AddSingleton<ITokenRequesterService, TokenRequesterService>();
        services.AddSingleton<IServiceContextService, ServiceContextService>();
        services.AddSingleton<IAccreditationRepository, CosmosDbAccreditationRepository>();
        services.AddSingleton<IEntityRegistryService, EntityRegistryService>();
        services.AddSingleton<IEntityRegistryApiClientService, CachingEntityRegistryApiClientService>();
        services.AddSingleton<IFunctionContextAccessor, FunctionContextAccessor>();

        services.AddScoped<IEvidenceStatusService, EvidenceStatusService>();
        services.AddScoped<IEvidenceHarvesterService, EvidenceHarvesterService>();
        services.AddScoped<IConsentService, ConsentService>();
        services.AddScoped<IRequirementValidationService, RequirementValidationService>();
        services.AddScoped<IAuthorizationRequestValidatorService, AuthorizationRequestValidatorService>();
        services.AddScoped<IRequestContextService, RequestContextService>();

        services.AddTransient<ExceptionDelegatingHandler>();

        services.AddPolicyRegistry(new PolicyRegistry()
            {
                {
                    CachingEntityRegistryApiClientService.EntityRegistryCachePolicy, Policy.CacheAsync(
                        distributedCache.AsAsyncCacheProvider<string>().WithSerializer(
                            new JsonSerializer<EntityRegistryUnit?>(new JsonSerializerSettings())),
                        TimeSpan.FromHours(12))
                },
                {
                    "EvidenceCodesCachePolicy", Policy.CacheAsync(
                        distributedCache.AsAsyncCacheProvider<string>().WithSerializer(
                            new JsonSerializer<List<EvidenceCode>>(new JsonSerializerSettings
                            {
                                TypeNameHandling = TypeNameHandling.All
                            })
                        ), AvailableEvidenceCodesService.DistributedCacheTtl)
                },
                {
                    "MaskinportenTokenPolicy", Policy.CacheAsync(
                        distributedCache.AsAsyncCacheProvider<string>(),
                        new Oauth2AccessTokenCachingStrategy())
                },
                {
                    "DefaultCircuitBreaker", HttpPolicyExtensions.HandleTransientHttpError().
                        CircuitBreakerAsync(Settings.BreakerFailureCountThreshold, Settings.BreakerRetryWaitTime)
                }
            });

        // Default client to use in harvesting
        services.AddHttpClient("SafeHttpClient", client =>
            {
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.BaseAddress = new Uri(Settings.ApiUrl);
            })
            .AddPolicyHandlerFromRegistry("DefaultCircuitBreaker")
            .AddHttpMessageHandler<ExceptionDelegatingHandler>();

        // Client used for getting evidence code lists from data sources
        services.AddHttpClient("EvidenceCodesClient", client =>
            {
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(25);
            })
            .AddHttpMessageHandler<ExceptionDelegatingHandler>();

        // Client with enterprise certificate authentication
        services.AddHttpClient("ECHttpClient", client =>
            {
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .AddPolicyHandlerFromRegistry("DefaultCircuitBreaker")
            .AddHttpMessageHandler<ExceptionDelegatingHandler>()
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new HttpClientHandler();
                handler.ClientCertificates.Add(Settings.AltinnCertificate);
                return handler;
            });

        // Client used for Entity Registry lookups
        services.AddHttpClient("EntityRegistryClient", client =>
            {
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(5);
            })
            .AddHttpMessageHandler<ExceptionDelegatingHandler>();

    })
    .Build();

await host.RunAsync();
