using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace Firebasemauiapp.Converters;

public class TextLengthToFontSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string text)
        {
            int length = text.Length;

            // Adjust font size based on text length
            if (length <= 20)
                return 16.0;
            else if (length <= 40)
                return 14.0;
            else if (length <= 60)
                return 12.0;
            else
                return 10.0;
        }
        return 14.0; // Default font size
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
