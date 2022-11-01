using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace Dan.Core.Middleware;

public class InvocationIdHeaderMiddleware : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        await next(context);
        context.GetHttpResponseData()?.Headers.Add("x-invocationId", context.InvocationId);
    }
}