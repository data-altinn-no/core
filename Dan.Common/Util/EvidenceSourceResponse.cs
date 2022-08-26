using Dan.Common.Exceptions;
using Microsoft.Azure.Functions.Worker.Http;

namespace Dan.Common.Util;

/// <summary>
/// Helper class for evidence sources communicating with core
/// </summary>
public static class EvidenceSourceResponse
{

    public static async Task<HttpResponseData> CreateResponse(HttpRequestData req, Func<Task<List<EvidenceValue>>> func)
    {
        try
        {
            return await CreateSuccessResponse(req, await func.Invoke());
        }
        catch (EvidenceSourceTransientException e)
        {
            return await CreateTransientErrorResponse(req, e);
        }
        catch (EvidenceSourcePermanentClientException e)
        {
            return await CreatePermanentClientErrorResponse(req, e);
        }
        catch (EvidenceSourcePermanentServerException e)
        {
            return await CreatePermanentServerErrorResponse(req, e);
        }
    }

    /// <summary>
    /// Helper method for wrapping a harvesting method returning a premade HTTP response message. Will handle evidence source exceptions and create the correct response to core
    /// </summary>
    /// <param name="req">The HTTP request from core</param>
    /// <param name="func">The delegate containing the call to the function</param>
    /// <returns>A HTTP response message</returns>
    public static async Task<HttpResponseData> CreateResponse(HttpRequestData req, Func<Task<HttpResponseData>> func)
    {
        try
        {
            return await func.Invoke();
        }
        catch (EvidenceSourceTransientException e)
        {
            return await CreateTransientErrorResponse(req, e);
        }
        catch (EvidenceSourcePermanentClientException e)
        {
            return await CreatePermanentClientErrorResponse(req, e);
        }
        catch (EvidenceSourcePermanentServerException e)
        {
            return await CreatePermanentServerErrorResponse(req, e);
        }
    }

    private static async Task<HttpResponseData> CreateSuccessResponse(HttpRequestData req, List<EvidenceValue> evidenceValues)
    {
        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(evidenceValues);
        return response;
    }

    private static async Task<HttpResponseData> CreateTransientErrorResponse(HttpRequestData req, EvidenceSourceException exception)
    {
        var response = req.CreateResponse();
        response.StatusCode = HttpStatusCode.ServiceUnavailable;
        await response.WriteAsJsonAsync(GetErrorModel(exception));
        return response;
    }

    private static async Task<HttpResponseData> CreatePermanentClientErrorResponse(HttpRequestData req, EvidenceSourceException exception)
    {
        var response = req.CreateResponse();
        response.StatusCode = HttpStatusCode.BadRequest;
        await response.WriteAsJsonAsync(GetErrorModel(exception));
        return response;
    }

    private static async Task<HttpResponseData> CreatePermanentServerErrorResponse(HttpRequestData req, EvidenceSourceException exception)
    {
        var response = req.CreateResponse();
        response.StatusCode = HttpStatusCode.BadGateway;
        await response.WriteAsJsonAsync(GetErrorModel(exception));
        return response;
    }

    private static ErrorModel GetErrorModel(EvidenceSourceException exception)
    {
        var model = new ErrorModel
        {
            Code = (int)exception.ExceptionErrorCode,
            DetailCode = exception.DetailErrorCode?.ToString(),
            Description = exception.Message,
            Stacktrace = exception.StackTrace
        };

        if (exception.InnerException == null)
        {
            return model;
        }

        model.InnerExceptionMessage = exception.InnerException.GetType().FullName + ": " + exception.InnerException.Message;
        model.InnerExceptionStackTrace = exception.InnerException.StackTrace;

        return model;
    }
}
