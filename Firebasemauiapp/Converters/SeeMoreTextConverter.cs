using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace Firebasemauiapp.Converters
{
    public class SeeMoreTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isExpanded)
                return isExpanded ? "See less" : "See more";
            return "See more";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
