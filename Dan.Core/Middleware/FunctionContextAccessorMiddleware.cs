using Dan.Core.Services.Interfaces;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Azure.Functions.Worker;

// Copied from https://gist.github.com/dolphinspired/796d26ebe1237b78ee04a3bff0620ea0
// See also https://github.com/Azure/azure-functions-dotnet-worker/issues/950

namespace Dan.Core.Middleware;
public class FunctionContextAccessorMiddleware : IFunctionsWorkerMiddleware
{
    private IFunctionContextAccessor FunctionContextAccessor { get; }

    public FunctionContextAccessorMiddleware(IFunctionContextAccessor accessor)
    {
        FunctionContextAccessor = accessor;
    }

    public Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        if (FunctionContextAccessor.FunctionContext != null)
        {
            // This should never happen because the context should be localized to the current Task chain.
            // But if it does happen (perhaps the implementation is bugged), then we need to know immediately so it can be fixed.
            throw new InvalidOperationException($"Unable to initalize {nameof(IFunctionContextAccessor)}: context has already been initialized.");
        }

        FunctionContextAccessor.FunctionContext = context;

        return next(context);
    }
}