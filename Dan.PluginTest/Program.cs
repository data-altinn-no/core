using Microsoft.Extensions.Hosting;
using Dan.Common.Extensions;
using Dan.PluginTest.Config;
using Microsoft.Extensions.DependencyInjection;

var host = new HostBuilder()
    .ConfigureDanPluginDefaults()
    .ConfigureAppConfiguration((_, _) =>
    {
    })
    .ConfigureServices((context, services) =>
    {
        var configurationRoot = context.Configuration;
        services.Configure<Settings>(configurationRoot);
    })
    .Build();

await host.RunAsync();