using Dan.Common.Exceptions;
using Dan.Common.Models;
using Dan.Core.Config;
using Dan.Core.Exceptions;
using System.Net;
using Microsoft.Azure.Functions.Worker;

namespace Dan.Core.Extensions;

/// <summary>
/// Exception extensions
/// </summary>
public static class ExceptionExtensions
{
    /// <summary>
    /// Get error model
    /// </summary>
    /// <returns>An ErrorModel</returns>
    /// <param name="exception">The Nadobe Exception</param>
    /// <param name="context">The function execution context</param>
    public static ErrorModel GetErrorModel(this DanException exception, FunctionContext context)
    {
        string? detailedErrorCode = null;
        if (exception.DetailErrorCode.HasValue)
        {
            detailedErrorCode = string.IsNullOrEmpty(exception.DetailErrorSource)
                ? $"ES-{exception.DetailErrorCode}"
                : $"{exception.DetailErrorSource}-{exception.DetailErrorCode}";
        }

        var model = new ErrorModel()
        {
            Code = (int)exception.ExceptionErrorCode,
            Description = exception.Message,
            DetailCode = detailedErrorCode,
            DetailDescription = exception.DetailErrorDescription,
            InvocationId = context.InvocationId
        };

        if (Settings.IsDevEnvironment || Settings.IsUnitTest)
        {
            model.Stacktrace = exception.StackTrace;
            if (exception.InnerException != null)
            {
                model.InnerExceptionMessage = exception.InnerException.GetType().FullName + ": " + exception.InnerException.Message;
                model.InnerExceptionStackTrace = string.IsNullOrEmpty(exception.InnerStackTrace) ? exception.InnerException.StackTrace : exception.InnerStackTrace;
            }
        }

        return model;
    }

    /// <summary>
    /// Get error model
    /// </summary>
    /// <returns>An ErrorModel</returns>
    /// <param name="exception">The Nadobe Exception</param>
    public static HttpStatusCode GetErrorCode(this DanException exception)
    {
        // Each DanException should be mapped to a httpStatusCode here
        switch (exception)
        {
            case InvalidRequestorException _:
            case InvalidSubjectException _:
            case ExpiredAccreditationException _:
            case UnknownEvidenceCodeException _:
            case InvalidEvidenceRequestException _:
            case InvalidEvidenceRequestParameterException _:
            case InvalidLegalBasisException _:
            case ErrorInLegalBasisReferenceException _:
            case ExpiredConsentException _:
            case InvalidValidToDateTimeException _:
            case InvalidAuthorizationRequestException _:
            case EvidenceSourcePermanentClientException _:
            case ConsentAlreadyHandledException _:
            case InvalidJmesPathExpressionException _:
                return HttpStatusCode.BadRequest; // 400
            case MissingAuthenticationException _:
            case InvalidAccessTokenException _:
            case InvalidCertificateException _:
                return HttpStatusCode.Unauthorized; // 401
            case RequiresConsentException _:
            case AuthorizationFailedException _:
                return HttpStatusCode.Forbidden; // 403
            case NonExistentAccreditationException _:
            case DeletedSubjectException _:
            case DeletedConsentException _:
                return HttpStatusCode.NotFound; // 404
            case EvidenceSourcePermanentServerException _:
                return HttpStatusCode.BadGateway;  // 502
            case ServiceNotAvailableException _:
            case AsyncEvidenceStillWaitingException _:
            case EvidenceSourceTransientException _:
                return HttpStatusCode.ServiceUnavailable; // 503

        }

        // TODO! As all DanExceptions should be mapped to something, this should be logged.
        return HttpStatusCode.InternalServerError;
    }
}