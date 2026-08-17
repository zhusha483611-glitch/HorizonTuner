using System.Windows.Data;
using System.Globalization;
using HorizonTuner.Resources;

namespace HorizonTuner.Converters;

public class TypeToInstanceConverter : IValueConverter
{
    private readonly Dictionary<Type, object> CachedInstances = new();

    private object GetPage(Type pageType)
    {
        if (pageType == null)
        {
            return null!;
        }

        if (CachedInstances.TryGetValue(pageType, out var cachedInstance))
        {
            return cachedInstance;
        }

        try
        {
            var newInstance = Activator.CreateInstance(pageType);
            if (newInstance == null)
            {
                return null!;
            }
            CachedInstances[pageType] = newInstance!;
            return newInstance!;
        }
        catch (Exception)
        {
            return null!;
        }
    }
    
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is not Type pageType)
        {
            return null!;
        }
        return GetPage(pageType);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
