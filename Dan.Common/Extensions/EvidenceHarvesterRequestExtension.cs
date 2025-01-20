namespace Dan.Common.Extensions;

/// <summary>
/// Extension methods for evidence harvester requests
/// </summary>
public static class EvidenceHarvesterRequestExtension
{
    /// <summary>
    /// Tries to get the requested parameter as a string in the supplied out parameter. Returns false if not set.
    /// </summary>
    /// <param name="ehr">The evidence harvester request</param>
    /// <param name="paramName">The parameter name</param>
    /// <param name="value">The parameter value</param>
    /// <returns>True if the parameter was supplied and was valid; otherwise false</returns>
    public static bool TryGetParameter(this EvidenceHarvesterRequest ehr, string paramName, out string? value)
    {
        if (!ehr.TryGetParameter(paramName, out EvidenceParameter? parameter))
        {
            value = default;
            return false;
        }

        value = (string?)parameter?.Value;
        return true;
    }

    /// <summary>
    /// Tries to get the requested parameter as a integer in the supplied out parameter. Returns false if not set.
    /// </summary>
    /// <param name="ehr">The evidence harvester request</param>
    /// <param name="paramName">The parameter name</param>
    /// <param name="value">The parameter value</param>
    /// <returns>True if the parameter was supplied and was valid; otherwise false</returns>
    public static bool TryGetParameter(this EvidenceHarvesterRequest ehr, string paramName, out int value)
    {
        // Can't find parameter? Return false
        if (!ehr.TryGetParameter(paramName, out EvidenceParameter? parameter))
        {
            value = default;
            return false;
        }

        switch (parameter?.Value)
        {
            // Null? That's not a number, at least give a proper zero
            case null:
                value = default;
                return false;
            // If int, just return
            case int intValue:
                value = intValue;
                return true;
            // If long, return if not overflown above max int value
            case long and (> int.MaxValue or < int.MinValue):
                value = default;
                return false;
            case long longValue:
                value = Convert.ToInt32(longValue);
                return true;
            // If string, parse
            case string stringValue:
                return int.TryParse(stringValue, out value);
            // Otherwise return false, can't think of any other realistic value type, I dont want to do floats for
            // int values due to rounding, I'd rather just throw an invalid value because don't give us a decimal
            // number if we request a whole one
            default:
                value = default;
                return false;
        }
    }

    /// <summary>
    /// Tries to get the requested parameter as a decimal in the supplied out parameter. Returns false if not set.
    /// </summary>
    /// <param name="ehr">The evidence harvester request</param>
    /// <param name="paramName">The parameter name</param>
    /// <param name="value">The parameter value</param>
    /// <returns>True if the parameter was supplied and was valid; otherwise false</returns>
    public static bool TryGetParameter(this EvidenceHarvesterRequest ehr, string paramName, out decimal value)
    {
        if (!ehr.TryGetParameter(paramName, out EvidenceParameter? parameter))
        {
            value = default;
            return false;
        }
        
        switch (parameter?.Value)
        {
            // Null? That's not a number, at least give a proper zero
            case null:
                value = default;
                return false;
            // If decimal, just return
            case decimal decimalValue:
                value = decimalValue;
                return true;
            // In case of other number values, convert
            case int intValue:
                value = Convert.ToDecimal(intValue);
                return true;
            case long longValue:
                value = Convert.ToDecimal(longValue);
                return true;
            case float floatValue:
                value = Convert.ToDecimal(floatValue);
                return true;
            case double doubleValue:
                value = Convert.ToDecimal(doubleValue);
                return true;
            // If string, try to parse
            case string stringValue:
                return decimal.TryParse(stringValue, out value);
            // Otherwise return false, can't think of any other realistic value type that could be interpreted
            // as a decimal type
            default:
                value = default;
                return false;
        }
    }

    /// <summary>
    /// Tries to get the requested parameter as a decimal in the supplied out parameter. Returns false if not set.
    /// </summary>
    /// <param name="ehr">The evidence harvester request</param>
    /// <param name="paramName">The parameter name</param>
    /// <param name="value">The parameter value</param>
    /// <returns>True if the parameter was supplied and was valid; otherwise false</returns>
    public static bool TryGetParameter(this EvidenceHarvesterRequest ehr, string paramName, out DateTime value)
    {
        if (!ehr.TryGetParameter(paramName, out EvidenceParameter? parameter))
        {
            value = default;
            return false;
        }

        return DateTime.TryParse(parameter?.Value?.ToString(), out value);
    }


    /// <summary>
    /// Tries to get the requested parameter as a boolean in the supplied out parameter. Returns false if not set.
    /// </summary>
    /// <param name="ehr">The evidence harvester request</param>
    /// <param name="paramName">The parameter name</param>
    /// <param name="value">The parameter value</param>
    /// <returns>True if the parameter was supplied and was valid; otherwise false</returns>
    public static bool TryGetParameter(this EvidenceHarvesterRequest ehr, string paramName, out bool value)
    {
        if (!ehr.TryGetParameter(paramName, out EvidenceParameter? parameter))
        {
            value = default;
            return false;
        }

        return bool.TryParse((string?)parameter?.Value, out value);
    }

    /// <summary>
    /// Gets a parameter if set
    /// </summary>
    /// <param name="ehr">The evidence harvester request</param>
    /// <param name="paramName">The parameter name</param>
    /// <param name="parameter"></param>
    /// <returns>The parameter</returns>
    public static bool TryGetParameter(this EvidenceHarvesterRequest ehr, string paramName, out EvidenceParameter? parameter)
    {
        parameter = ehr.Parameters?.FirstOrDefault(x => x.EvidenceParamName == paramName);
        return parameter != null;
    }
}