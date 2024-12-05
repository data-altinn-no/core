using Microsoft.Extensions.Hosting;
using Dan.Common.Extensions;
using Dan.PluginTest.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

var host = new HostBuilder()
    .ConfigureDanPluginDefaults()
    .ConfigureAppConfiguration((_, _) =>
    {
    })
    .ConfigureServices((context, services) =>
    {
        var configurationRoot = context.Configuration;
        services.Configure<Settings>(configurationRoot);

        var applicationSettings = services.BuildServiceProvider().GetRequiredService<IOptions<Settings>>().Value;
    })
    .Build();

await host.RunAsync();