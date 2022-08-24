global using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using Dan.Core.Helpers;
using Microsoft.Extensions.Configuration;

namespace Dan.Core.UnitTest;

[TestClass]
class GlobalTestInitializer
{
    [AssemblyInitialize()]
    public static void MyTestInitialize(TestContext testContext)
    {
        var config = new ConfigurationBuilder();

        config
            .AddEnvironmentVariables()
            .AddJsonFile("appsettings.unittest.json", true)
            .AddUserSecrets(Assembly.GetExecutingAssembly(), true);

        ConfigurationHelper.ConfigurationRoot = config.Build();
    }

    /*
    [AssemblyCleanup]
    public static void TearDown()
    {
        // The test framework will call this method once -AFTER- each test run.
    }
    */
}