using System.Reflection;
using Azure.Core.Serialization;
using Dan.Common.Models;
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
using Microsoft.Extensions.Logging.Console;
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
            .AddJsonFile("worker.json");

        danHostingEnvironment = hostContext.HostingEnvironment;
        if (danHostingEnvironment.IsLocalDevelopment())
        {
            config.AddUserSecrets(Assembly.GetExecutingAssembly(), true);
        }

        ConfigurationHelper.ConfigurationRoot = config.Build();
    })
    .ConfigureLogging(builder =>
    {
        if (danHostingEnvironment.IsLocalDevelopment())
        {
            builder.AddSimpleConsole(options =>
            {
                options.ColorBehavior = LoggerColorBehavior.Enabled;
                options.SingleLine = true;
                options.IncludeScopes = false;
            });
        }
    })
    .ConfigureFunctionsWorkerDefaults(builder =>
    {
        builder
            .UseWhen<ExceptionHandlerMiddleware>(context => !context.HasAttribute(typeof(HtmlErrorAttribute)))
            .UseWhen<HtmlExceptionHandlerMiddleware>(context => context.HasAttribute(typeof(HtmlErrorAttribute)))
            .UseWhen<AuthenticationMiddleware>(context => !context.HasAttribute(typeof(NoAuthenticationAttribute)));

        if (!danHostingEnvironment.IsLocalDevelopment())
        {
            // Using preview package Microsoft.Azure.Functions.Worker.ApplicationInsights, see https://github.com/Azure/azure-functions-dotnet-worker/pull/944
            // Requires APPLICATIONINSIGHTS_CONNECTION_STRING being set. Note that host.json logging settings will have to be replicated to worker.json
            builder
                .AddApplicationInsights()
                .AddApplicationInsightsLogger();
        }

    }, options =>
    {
        options.Serializer = new NewtonsoftJsonObjectSerializer();
    })
    .ConfigureServices((hostBuilderContext, services) =>
    {

        services.AddStackExchangeRedisCache(option =>
        {
            option.Configuration = Settings.RedisCacheConnectionString;
        });

        var sp = services.BuildServiceProvider();
        var distributedCache = sp.GetRequiredService<IDistributedCache>();

        services.AddSingleton(s => new CosmosClientBuilder(Settings.CosmosDbConnection).Build());
        services.AddSingleton<IChannelManagerService, ChannelManagerService>();
        services.AddSingleton<IAltinnCorrespondenceService, AltinnCorrespondenceService>();
        services.AddSingleton<IEntityRegistryService, EntityRegistryService>();
        services.AddSingleton<IAvailableEvidenceCodesService, AvailableEvidenceCodesService>();
        services.AddSingleton<IAltinnServiceOwnerApiService, AltinnServiceOwnerApiService>();
        services.AddSingleton<ITokenRequesterService, TokenRequesterService>();
        services.AddSingleton<IServiceContextService, ServiceContextService>();
        services.AddSingleton<IAccreditationRepository, CosmosDbAccreditationRepository>();

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
                    "ERCachePolicy", Policy.CacheAsync(
                        distributedCache.AsAsyncCacheProvider<string>(),
                        EntityRegistryService.DistributedCacheTtl)
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
                    "defaultCircuitBreaker", HttpPolicyExtensions.HandleTransientHttpError().
                        CircuitBreakerAsync(Settings.BreakerFailureCountThreshold, Settings.BreakerRetryWaitTime)
                }
            });

        // Default client to use in harvesting
        services.AddHttpClient("SafeHttpClient", client =>
        {
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.BaseAddress = new Uri(Settings.ApiUrl);
        })
        .AddPolicyHandlerFromRegistry("defaultCircuitBreaker")
        .AddHttpMessageHandler<ExceptionDelegatingHandler>();

        // Client used for getting evidence code lists from data sources
        services.AddHttpClient("EvidenceCodesClient", client =>
        {
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromSeconds(10);
        })
        .AddHttpMessageHandler<ExceptionDelegatingHandler>();

        // Client with enterprise certificate authentication
        services.AddHttpClient("ECHttpClient", client =>
        {
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .AddPolicyHandlerFromRegistry("defaultCircuitBreaker")
        .AddHttpMessageHandler<ExceptionDelegatingHandler>()
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
            handler.ClientCertificates.Add(Settings.AltinnCertificate);
            return handler;
        });


    })
    .Build();

await host.RunAsync();
