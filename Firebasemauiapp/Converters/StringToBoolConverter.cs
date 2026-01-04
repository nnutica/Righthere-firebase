using System;
using System.Globalization;

namespace Firebasemauiapp.Converters
{
    /// <summary>
    /// Converts string to bool - true if string is not null/empty
    /// </summary>
    public class StringToBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                var result = !string.IsNullOrWhiteSpace(str);
                System.Diagnostics.Debug.WriteLine($"[DiaryViewModel.StringToBoolConverter] value: '{str}' -> result: {result}");
                return result;
            }
            System.Diagnostics.Debug.WriteLine($"[DiaryViewModel.StringToBoolConverter] value is not string: {value?.GetType().Name}");
            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
