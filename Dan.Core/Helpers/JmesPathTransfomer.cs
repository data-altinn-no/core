using Dan.Core.Exceptions;
using DevLab.JmesPath;

namespace Dan.Core.Helpers;

public static class JmesPathTransfomer
{
    public const string QueryParameter = "query";
    public static void Apply(string? jmesExpression, ref string jsonResult)
    {
        if (string.IsNullOrEmpty(jmesExpression) || string.IsNullOrEmpty(jsonResult))
        {
            return;
        }

        try
        {
            var jmes = new JmesPath();
            jsonResult = jmes.Transform(jsonResult, jmesExpression);
        }
        catch (Exception e)
        {
            throw new InvalidJmesPathExpressionException(e.Message);
        }
    }
}