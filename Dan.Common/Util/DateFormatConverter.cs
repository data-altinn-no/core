namespace Dan.Common.Util;

/// <summary>
/// IsoDateTimeConverter
/// </summary>
public class DateFormatConverter : IsoDateTimeConverter
{
    /// <summary>
    /// Constructor for setting DateTimeFormat for IsoDateTimeConverter
    /// </summary>
    public DateFormatConverter(string format)
    {
        DateTimeFormat = format;
    }
}
