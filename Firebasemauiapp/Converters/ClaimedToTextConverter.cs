using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace Firebasemauiapp.Converters;

public class ClaimedToTextConverter : IValueConverter
{
    public string TrueText { get; set; } = "Completed";
    public string FalseText { get; set; } = "Not completed";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b)
            return TrueText;
        return FalseText;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
