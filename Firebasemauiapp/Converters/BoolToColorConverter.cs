using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace Firebasemauiapp.Converters;

public class BoolToColorConverter : IValueConverter
{
    public Color TrueColor { get; set; } = Colors.Green;
    public Color FalseColor { get; set; } = Colors.Transparent;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isTrue = value is bool b && b;

        // If parameter is provided, parse it as "trueColor|falseColor"
        if (parameter is string colorParam && !string.IsNullOrEmpty(colorParam))
        {
            var colors = colorParam.Split('|');
            if (colors.Length == 2)
            {
                var trueColorStr = colors[0].Trim();
                var falseColorStr = colors[1].Trim();

                Color trueColor = ParseColor(trueColorStr);
                Color falseColor = ParseColor(falseColorStr);

                return isTrue ? trueColor : falseColor;
            }
        }

        // Fallback to properties
        return isTrue ? TrueColor : FalseColor;
    }

    private Color ParseColor(string colorStr)
    {
        if (colorStr.Equals("Transparent", StringComparison.OrdinalIgnoreCase))
            return Colors.Transparent;
        if (colorStr.Equals("White", StringComparison.OrdinalIgnoreCase))
            return Colors.White;

        // Try to parse hex color
        if (colorStr.StartsWith("#"))
            return Color.FromArgb(colorStr);

        return Colors.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
