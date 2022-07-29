namespace Dan.Common.Helpers.Util;

/// <summary>
/// Helper class to do check if a ssn is well formed
/// </summary>
public static class SSNValidator
{
    /// <summary>
    /// Validate a ssn
    /// </summary>                
    /// <param name="ssn">
    /// The social security number
    /// </param>
    /// <returns>
    /// True if validated, false if invalid
    /// </returns>
    public static bool ValidateSSN(string ssn)
    {
        if (ssn.Length != 11)
        {
            return false;
        }

        /* Too strict for tenor numbers 
        string date = (ssn[0] <= '3') ? ssn.Substring(0, 6) : ((ssn[0] - '4') + ssn.Substring(1, 5));
        DateTime tmp;
        if (DateTime.TryParseExact(date, "ddMMyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out tmp) == false)
        {
            return false;
        } */

        int[] n = new int[11];
        int tmp2 = 0;
        for (int i = 0; i < 11; i++)
        {
            if (int.TryParse(ssn[i].ToString(), out tmp2))
            {
                n[i] = tmp2;
            }
            else
            {
                return false;
            }
        }

        // Control number 1
        int k1 = 11 - (3 * n[0] + 7 * n[1] + 6 * n[2] + 1 * n[3] + 8 * n[4] + 9 * n[5] + 4 * n[6] + 5 * n[7] + 2 * n[8]) % 11;
        if (k1 == 11)
        {
            k1 = 0;
        }

        if (k1 == 10 || k1 != n[9])
        {
            return false;
        }

        // Control number 2
        int k2 = 11 - (5 * n[0] + 4 * n[1] + 3 * n[2] + 2 * n[3] + 7 * n[4] + 6 * n[5] + 5 * n[6] + 4 * n[7] + 3 * n[8] + 2 * k1) % 11;
        if (k2 == 11)
        {
            k2 = 0;
        }

        if (k2 == 10 || k2 != n[10])
        {
            return false;
        }

        // Sex: yes
        return true;
    }

    /// <summary>
    /// Check if ssn has correct length
    /// </summary>       
    /// <param name="ssn">
    /// The social security number
    /// </param>
    /// <returns>
    /// True if validated, false if invalid
    /// </returns>
    private static bool CorrectLength(String ssn)
    {
        if (ssn.Length == 11)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Check that ssn contains only numbers
    /// </summary>
    /// <param name="ssn">
    /// The social security number
    /// </param>
    /// <returns>
    /// True if validated, false if invalid
    /// </returns>
    private static bool IsNumeric(String ssn)
    {
        for (int i = 0; i < ssn.Length; i++)
        {
            if (!char.IsDigit(ssn[i]))
            {
                return false;
            }
        }

        return true;
    }
}