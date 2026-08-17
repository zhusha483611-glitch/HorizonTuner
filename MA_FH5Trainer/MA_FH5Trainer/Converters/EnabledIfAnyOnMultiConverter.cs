namespace HorizonTuner.Converters;

using System;
using System.Globalization;
using System.Windows.Data;

public class EnabledIfAnyOnMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
        {
            return false;
        }

        if (values[0] is not bool enabled)
        {
            return false;
        }

        var anyOn = false;
        for (var i = 1; i < values.Length; i++)
        {
            if (values[i] is bool b && b)
            {
                anyOn = true;
                break;
            }
        }

        return enabled && anyOn;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

