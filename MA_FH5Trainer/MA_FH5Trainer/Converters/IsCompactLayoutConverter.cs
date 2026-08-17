using System.Globalization;
using System.Windows.Data;

namespace HorizonTuner.Converters;

public sealed class IsCompactLayoutConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double width || double.IsNaN(width) || double.IsInfinity(width))
        {
            return false;
        }

        var threshold = 820d;
        if (parameter is string s && double.TryParse(s, out var parsed))
        {
            threshold = parsed;
        }

        return width < threshold;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

