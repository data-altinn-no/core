using AwesomeAssertions;
using Dan.Common.Attributes;
using Dan.Common.Health;
using Dan.Core.Extensions;
using FakeItEasy;
using Microsoft.Azure.Functions.Worker;

namespace Dan.Core.UnitTest;

[TestClass]
public class FunctionContextExtensionsTest
{
    private static FunctionContext ContextFor(string entryPoint, string pathToAssembly)
    {
        var definition = A.Fake<FunctionDefinition>();
        A.CallTo(() => definition.EntryPoint).Returns(entryPoint);
        A.CallTo(() => definition.PathToAssembly).Returns(pathToAssembly);

        var context = A.Fake<FunctionContext>();
        A.CallTo(() => context.FunctionDefinition).Returns(definition);
        return context;
    }

    [TestMethod]
    public void HasAttribute_finds_NoAuthentication_on_function_declared_in_entry_assembly()
    {
        var context = ContextFor($"{typeof(FuncVersion).FullName}.{nameof(FuncVersion.RunAsync)}", typeof(FuncVersion).Assembly.Location);

        context.HasAttribute(typeof(NoAuthenticationAttribute)).Should().BeTrue();
    }

    [TestMethod]
    public void HasAttribute_finds_NoAuthentication_on_health_function_declared_in_Dan_Common_when_PathToAssembly_is_the_entry_assembly()
    {
        // The worker indexes referenced-assembly functions with the entry assembly (Dan.Core.dll) as PathToAssembly.
        var context = ContextFor($"{typeof(DanHealthFunctions).FullName}.{nameof(DanHealthFunctions.Alive)}", typeof(FuncVersion).Assembly.Location);

        context.HasAttribute(typeof(NoAuthenticationAttribute)).Should().BeTrue();
    }

    [TestMethod]
    public void HasAttribute_is_false_for_authenticated_function()
    {
        var context = ContextFor($"{typeof(FuncAccreditationList).FullName}.{nameof(FuncAccreditationList.RunAsync)}", typeof(FuncVersion).Assembly.Location);

        context.HasAttribute(typeof(NoAuthenticationAttribute)).Should().BeFalse();
    }
}
