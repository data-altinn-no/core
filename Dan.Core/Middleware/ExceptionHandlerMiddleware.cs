using System.Diagnostics;
using System.Net;
using Dan.Common.Exceptions;
using Dan.Common.Models;
using Dan.Core.Config;
using Dan.Core.Exceptions;
using Dan.Core.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace Dan.Core.Middleware;

/// <summary>
/// Middleware logging exceptions and returning a nice error model if HTTP
/// </summary>
public class ExceptionHandlerMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;

    public ExceptionHandlerMiddleware(ILogger<ExceptionHandlerMiddleware> logger)
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

            DanException nex;
            LogLevel logLevel;
            string cert = string.Empty;
            var request = await context.GetHttpRequestDataAsync();

            // Occurs when any of the Altinn integration services fail
            if (exception is ServiceNotAvailableException serviceNotAvailableException)
            {
                // Handle nested errors
                if (serviceNotAvailableException.InnerException is AltinnServiceException altinnServiceException)
                {
                    serviceNotAvailableException =
                        new ServiceNotAvailableException(altinnServiceException.Message, altinnServiceException);
                }

                nex = serviceNotAvailableException;
                logLevel = LogLevel.Error;
            }
            else if (exception is DanException danException)
            {
                // Use the exception verbatim
                nex = danException;

                // Most errors are just functional user errors, but EvidenceSourcePermanentServerException indicates a serverside misconfiguration or similar
                logLevel = exception is EvidenceSourcePermanentServerException ? LogLevel.Error : LogLevel.Information;

                if (exception is InvalidCertificateException)
                {
                    cert = request?.Headers.Get(Settings.CertificateHeader) ?? "none supplied";
                }
            }
            else if (exception is HttpRequestException || exception is TimeoutException)
            {
                // These indicate an upstream server being down
                nex = new ServiceNotAvailableException(null, exception);
                logLevel = LogLevel.Warning;
            }
            else
            {
                // All other exceptions are to be treated as internal errors
                nex = new InternalServerErrorException(null, exception);
                logLevel = LogLevel.Error;
            }

            ErrorModel errorModel = nex.GetErrorModel(context);
            HttpStatusCode statusCode = nex.GetErrorCode();

            string message =
                "Core OnException handler: {status={statusCode} ex={exception} nex={DanException} msg={message} nexMsg={nexMessage} innerEx={innerEx} innerExMsg={innerExMsg} func={functionName} invocationId={invocationId} cert={cert} detailDescription={detailDescription} detailedErrorCode={detailedErrorCode}";
            object[] args =
            {
                statusCode, // statusCode
                exception.GetType().Name, // exception
                nex.GetType().Name, // DanException
                exception.Message, // message
                nex.Message, // nexMessage,
                exception.InnerException?.GetType().Name!, // innerEx
                exception.InnerException?.Message!, // innerExMessage
                context.FunctionDefinition.Name, // functionName
                context.InvocationId, // invocationId
                cert, // cert
                errorModel.DetailDescription ?? string.Empty, // detailDescription
                errorModel.DetailCode ?? string.Empty // detailedErrorCode
            };

            _logger.Log(logLevel, 0, exception, message, args);

            if (request == null) return;

            var response = request.CreateResponse(statusCode);

            if (statusCode == HttpStatusCode.Unauthorized) // 401 requires that we set a WWW-Authenticate header
            {
                var schemeAndRealm = "Bearer realm=\"data.altinn.no\"";
                response.Headers.Add("WWW-Authenticate",
                    nex is InvalidAccessTokenException ? schemeAndRealm + ",error=\"invalid_token\"" : schemeAndRealm);
            }

            // You need to explicitly pass the status code in WriteAsJsonAsync method.
            // https://github.com/Azure/azure-functions-dotnet-worker/issues/776
            await response.WriteAsJsonAsync(errorModel, statusCode);

            var invocationResult = context.GetInvocationResult();

            var httpOutputBindingFromMultipleOutputBindings = GetHttpOutputBindingFromMultipleOutputBinding(context);
            if (httpOutputBindingFromMultipleOutputBindings is not null)
            {
                httpOutputBindingFromMultipleOutputBindings.Value = response;
            }
            else
            {
                invocationResult.Value = response;
            }
        }
    }
    private OutputBindingData<HttpResponseData>? GetHttpOutputBindingFromMultipleOutputBinding(FunctionContext context)
    {
        // The output binding entry name will be "$return" only when the function return type is HttpResponseData
        var httpOutputBinding = context.GetOutputBindings<HttpResponseData>()
            .FirstOrDefault(b => b.BindingType == "http" && b.Name != "$return");

        return httpOutputBinding;
    }

}