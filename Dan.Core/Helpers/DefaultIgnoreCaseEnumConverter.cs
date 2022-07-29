using System.ComponentModel;
using System.Globalization;

namespace Dan.Core.Helpers;

public class DefaultIgnoreCaseEnumConverter<T> : TypeConverter
       where T : struct
{
    /// <inheritdoc />
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    /// <inheritdoc />
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string str)
        {
            if (Enum.TryParse(str, true, out T enumResult))
            {
                return enumResult;
            }

            return default(T);
        }

        return base.ConvertFrom(context, culture, value);
    }
}
