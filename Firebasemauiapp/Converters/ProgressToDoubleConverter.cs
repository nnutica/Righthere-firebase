using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace Firebasemauiapp.Converters;

public class ProgressToDoubleConverter : IValueConverter
{
    public static ProgressToDoubleConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int i)
        {
            return Math.Clamp(i / 100.0, 0.0, 1.0);
        }
        return 0.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            return (int)Math.Round(Math.Clamp(d, 0.0, 1.0) * 100);
        }
        return 0;
    }
}