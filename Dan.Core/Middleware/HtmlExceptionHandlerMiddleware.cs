using System.Net;
using Dan.Common.Exceptions;
using Dan.Core.Config;
using Dan.Core.Extensions;
using Dan.Core.Helpers;
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

            var title = $"{(int)statusCode} {statusCode}";
            var message = CreateMessage(reason, exception.StackTrace, Settings.IsDevEnvironment);

            var request = await context.GetHttpRequestDataAsync();
            var response = request!.CreateHtmlResponse(statusCode, "Error.html", new { title, message });
            context.GetInvocationResult().Value = response;
        }
    }

    /// <summary>
    /// Creates the message for the error view. The reason is returned as plain text, which the view HTML encodes.
    /// With a stack trace the message is markup, where both the reason and the stack trace are HTML encoded.
    /// </summary>
    /// <param name="reason">The reason for the error</param>
    /// <param name="stackTrace">The stack trace of the exception</param>
    /// <param name="includeStackTrace">Whether the stack trace should be shown</param>
    /// <returns>The value for the message in the error view</returns>
    public static object CreateMessage(string reason, string? stackTrace, bool includeStackTrace)
    {
        return includeStackTrace
            ? new RawHtml($"{OutputEncoder.Html(reason)}<br /><br/><pre>{OutputEncoder.Html(stackTrace)}</pre>")
            : reason;
    }
}