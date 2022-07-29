namespace Dan.Common.Helpers.Util;

/// <summary>
/// Helper class to do check if an organization is well formed
/// </summary>
public static class OrganizationNumberValidator
{
    /// <summary>
    /// Checks if the supplied organization number is well formed
    /// </summary>
    /// <param name="orgNo">The norwegian organization number</param>
    /// <returns>True if well formed, false if not</returns>
    public static bool IsWellFormed(string orgNo)
    {
        int[] weight = { 3, 2, 7, 6, 5, 4, 3, 2 };

        if (orgNo == null || orgNo.Length != 9)
        {
            return false;
        }

        try
        {
            int sum = 0;
            for (int i = 0; i < orgNo.Length - 1; i++)
            {
                var currentDigit = int.Parse(orgNo.Substring(i, 1));
                sum += currentDigit * weight[i];
            }

            int ctrlDigit = 11 - (sum % 11);
            if (ctrlDigit == 11)
            {
                ctrlDigit = 0;
            }

            return int.Parse(orgNo.Substring(orgNo.Length - 1)) == ctrlDigit;
        }
        catch
        {
            // ignored
        }

        return false;
    }
}