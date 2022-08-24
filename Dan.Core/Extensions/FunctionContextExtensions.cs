using System.Reflection;
using Dan.Core.Exceptions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Dan.Core.Extensions;
public static class FunctionContextExtensions
{
    public static MethodInfo GetTargetFunctionMethod(this FunctionContext context)
    {
        // More terrible reflection code..
        // Would be nice if this was available out of the box on FunctionContext

        // This contains the fully qualified name of the method
        // E.g. IsolatedFunctionAuth.TestFunctions.ScopesAndAppRoles
        var entryPoint = context.FunctionDefinition.EntryPoint;

        var assemblyPath = context.FunctionDefinition.PathToAssembly;
        var assembly = Assembly.LoadFrom(assemblyPath);
        var typeName = entryPoint.Substring(0, entryPoint.LastIndexOf('.'));
        var type = assembly.GetType(typeName);
        if (type == null)
        {
            throw new InternalServerErrorException($"{nameof(GetTargetFunctionMethod)} failed loading type {typeName}");
        }

        var methodName = entryPoint.Substring(entryPoint.LastIndexOf('.') + 1);
        var method = type.GetMethod(methodName);
        if (method == null)
        {
            throw new InternalServerErrorException($"{nameof(GetTargetFunctionMethod)} failed loading method {typeName}");
        }

        return method;
    }

    public static bool HasAttribute(this FunctionContext context, Type attributeType)
    {
        var methodInfo = GetTargetFunctionMethod(context);
        return methodInfo.CustomAttributes.Any(attr => attr.AttributeType == attributeType);
    }

    // This is a horrible hack, see https://github.com/Azure/azure-functions-dotnet-worker/issues/936#issuecomment-1196676710
    public static void SetInvocationResult(this FunctionContext context, HttpResponseData response)
    {
        var httpResponseDataOutputBinding = context.GetOutputBindings<HttpResponseData>().FirstOrDefault();
        if (httpResponseDataOutputBinding != null)
        {
            httpResponseDataOutputBinding.Value = response;
        }
        else 
        {
            context.GetInvocationResult().Value = response;
        }

        var keyValuePair = context.Features.SingleOrDefault(f => f.Key.Name == "IFunctionBindingsFeature");
        var functionBindingsFeature = keyValuePair.Value;
        var bindingFeatureType = functionBindingsFeature.GetType();
        
        var outputBindingsInfo = bindingFeatureType
            .GetProperty("OutputBindingsInfo")!
            .GetValue(functionBindingsFeature);
        var outputPropertyNames = (List<string>?)outputBindingsInfo?
            .GetType()
            .GetField("_propertyNames", BindingFlags.NonPublic | BindingFlags.Instance)?
            .GetValue(outputBindingsInfo);

        // Assume the complex type has a field named "HttpResponse"
        if (outputPropertyNames != null && outputPropertyNames.Contains("HttpResponse"))
        {
            var outputBindings = (Dictionary<string, object>)bindingFeatureType
                .GetProperty("OutputBindingData")!
                .GetValue(functionBindingsFeature)!;
            outputBindings.Add("HttpResponse", response);
        }
        // Not output bindings found
        else
        {
            context.GetInvocationResult().Value = response;
        }
    }
}
