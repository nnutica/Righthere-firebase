using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Firebasemauiapp.Converters;

public class CalendarDayColorConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length < 2)
            return Colors.Transparent;

        bool isSelected = values[0] is bool selected && selected;

        if (isSelected)
        {
            // Selected day - dark orange
            return Color.FromArgb("#FF8C00");
        }

        // Unselected days - gradient from light cream to light orange
        if (values[1] is DateTime date)
        {
            // Create gradient effect based on day of week (position)
            int dayOfWeek = (int)date.DayOfWeek;

            return dayOfWeek switch
            {
                0 => Color.FromArgb("#FFE4C4"), // Sunday - lightest cream
                1 => Color.FromArgb("#FFD8A8"), // Monday
                2 => Color.FromArgb("#FFCC9C"), // Tuesday
                3 => Color.FromArgb("#FFC090"), // Wednesday
                4 => Color.FromArgb("#FFD8A8"), // Thursday
                5 => Color.FromArgb("#FFE4C4"), // Friday
                6 => Color.FromArgb("#FFF0DC"), // Saturday - very light
                _ => Color.FromArgb("#FFE4C4")
            };
        }

        return Color.FromArgb("#FFE4C4");
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
