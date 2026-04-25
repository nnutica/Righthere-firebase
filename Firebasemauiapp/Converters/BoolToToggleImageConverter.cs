using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace Firebasemauiapp.Converters;

public class BoolToToggleImageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isImageSectionVisible)
        {
            // If Image Section is visible (open), show close icon
            // Otherwise, show image icon
            return isImageSectionVisible ? "close.png" : "imgicon.png";
        }
        return "imgicon.png";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
