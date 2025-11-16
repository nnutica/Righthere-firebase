using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace Firebasemauiapp.Converters
{
    public class MoodNameToImageConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var name = value as string;
            if (string.IsNullOrWhiteSpace(name))
                return "joy.png"; // default fallback
            return $"{name.ToLowerInvariant()}.png";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
