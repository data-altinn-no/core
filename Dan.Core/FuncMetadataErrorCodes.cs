using Dan.Common.Enums;
using Dan.Common.Exceptions;
using Dan.Common.Models;
using Dan.Core.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Dan.Core.Attributes;
using Dan.Core.Middleware;

namespace Dan.Core;

/// <summary>
/// Azure Function returning all available error codes.
/// </summary>
//[ErrorHandler]
public class FuncMetadataErrorCodes
{
    private static Type[]? _reflectedTypes;

    /// <summary>
    /// Entry point for error codes Azure function
    /// </summary>
    /// <param name="req">
    /// The HTTP request object
    /// </param>
    /// <returns>
    /// The <see cref="HttpResponseData"/>.
    /// </returns>
    [Function("MetadataErrorCodes"), NoAuthentication]
    public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "metadata/errorcodes")] HttpRequestData req)
    {
        var list = Enum.GetValues(typeof(ErrorCode))
            .Cast<ErrorCode>()
            .Where(x => (int)x < 2000 || (int)x >= 3000) // do not include OA error codes (2xxx)
            .Select(x => new ErrorModel()
            {
                Code = (int)x,
                Description = GetDefaultMessageForErrorCode(x)
            });

        return req.CreateExternalResponse(HttpStatusCode.OK, list);
    }

    /// <summary>
    /// Uses reflection to determine the default error message for the exception mapped to the given error code
    /// </summary>
    /// <param name="errorCode">The error code instance which we are to find the matching exception</param>
    /// <returns>The default error message</returns>
    private static string GetDefaultMessageForErrorCode(ErrorCode errorCode)
    {
        if (_reflectedTypes == null)
        {
            // Cache a fairly expensive call. Need to check both the Core and Common assemblies to get all exceptions
            _reflectedTypes = Assembly.GetExecutingAssembly().GetTypes();
            _reflectedTypes = _reflectedTypes.Concat(Assembly.GetAssembly(typeof(DanException))!.GetTypes()).ToArray();
        }

        var type = _reflectedTypes.FirstOrDefault(t => t.Name == errorCode.ToString() && t.IsSubclassOf(typeof(DanException)));
        if (type != null)
        {
            var ex = (DanException)Activator.CreateInstance(type)!;
            return ex.DefaultErrorMessage;
        }

        // In case we do not have a matching exception (should not happen), fall back to a generated description
        var rgx = new Regex("([a-z])([A-Z])");
        return rgx.Replace(errorCode.ToString().Replace("Exception", string.Empty), "$1 $2");
    }
}