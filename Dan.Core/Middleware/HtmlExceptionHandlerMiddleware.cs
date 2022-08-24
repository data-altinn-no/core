using System.Net;
using Dan.Common.Exceptions;
using Dan.Core.Config;
using Dan.Core.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace Dan.Core.Middleware;

/// <summary>
/// Error Handler Filter
/// </summary>
public class HtmlExceptionHandlerMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<HtmlExceptionHandlerMiddleware> _logger;

    public HtmlExceptionHandlerMiddleware(ILogger<HtmlExceptionHandlerMiddleware> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {

            await next(context);
        }
        catch (Exception rootException)
        {
            // We only deal with a single exception (the first one) if it's an aggregate exception
            var exception = rootException is AggregateException aggregateException
                ? aggregateException.Flatten().InnerExceptions.First()
                : rootException;

            _logger.LogError($"HtmlExceptionHandlerMiddleware triggered. Function '{context.FunctionDefinition.Name}:{context.InvocationId} failed:\n{exception}");

            var statusCode = HttpStatusCode.InternalServerError;
            var reason = exception.Message;

            if (exception is DanException danException)
            {
                statusCode = danException.GetErrorCode();
                reason += $" ({(int)danException.ExceptionErrorCode})";
            }

            var trace = WebUtility.HtmlEncode(exception.StackTrace);
            var title = $"{(int)statusCode} {statusCode}";
            var message = Settings.IsDevEnvironment ? $"{reason}<br /><br/><pre>{trace}</pre>" : $"{reason}";

            var request = await context.GetHttpRequestDataAsync();
            var response = request!.CreateHtmlResponse(statusCode, "Error.html", new { title, message });
            context.GetInvocationResult().Value = response;
        }
    }
}