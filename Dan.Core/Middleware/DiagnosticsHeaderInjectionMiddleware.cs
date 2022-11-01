using Dan.Core.Helpers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace Dan.Core.Middleware;

public class DiagnosticsHeaderInjectionMiddleware : IFunctionsWorkerMiddleware
{
    private static string? _versionInfo;

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        await next(context);
        var response = context.GetHttpResponseData();
        if (response == null) return;
        response.Headers.Add("x-invocationId", context.InvocationId);

        if (_versionInfo == null)
        {
            var versionInfo = VersionHelper.GetVersionInfo();
            _versionInfo = versionInfo.Built + "-" + versionInfo.Commit;
        }

        response.Headers.Add("x-version", _versionInfo);
    }
}