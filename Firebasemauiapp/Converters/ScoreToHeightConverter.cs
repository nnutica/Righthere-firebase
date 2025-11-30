using System;
using System.Globalization;

namespace Firebasemauiapp.Converters
{
    public class ScoreToHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double score)
            {
                // Scale score (0-10) to height (0-100) for better visibility
                return score * 10;
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
