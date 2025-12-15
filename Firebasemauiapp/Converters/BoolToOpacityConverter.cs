using System.Globalization;

namespace Firebasemauiapp.Converters;

public class BoolToOpacityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            // If true (purchased), show with low opacity (0.5)
            // If false (not purchased), show with full opacity (1.0)
            return boolValue ? 0.5 : 1.0;
        }
        return 1.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
