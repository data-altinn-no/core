using Microsoft.Extensions.Configuration;

namespace Dan.Core.Helpers;
public static class ConfigurationHelper
{
    public static IConfigurationRoot ConfigurationRoot = null!;
    public static void Initialize(IConfigurationRoot configurationRoot)
    {
        ConfigurationRoot = configurationRoot;
    }
}
