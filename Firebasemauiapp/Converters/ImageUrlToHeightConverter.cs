using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace Firebasemauiapp.Converters;

public class ImageUrlToHeightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // parameter format: "heightWithImage,heightWithoutImage"
        // e.g., "340,220" or "230,420"

        var hasImage = !string.IsNullOrEmpty(value as string);

        if (parameter is string paramStr)
        {
            var parts = paramStr.Split(',');
            if (parts.Length == 2 &&
                double.TryParse(parts[0], out double withImage) &&
                double.TryParse(parts[1], out double withoutImage))
            {
                return hasImage ? withImage : withoutImage;
            }
        }

        return 220; // default
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
