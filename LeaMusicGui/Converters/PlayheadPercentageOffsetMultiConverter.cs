using System.Globalization;
using System.Windows.Data;

namespace LeaMusicGui.Converters
{
    public class PlayheadPercentageOffsetMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 &&
                double.TryParse(values[0]?.ToString(), out double percentage) &&
                double.TryParse(values[1]?.ToString(), out double parentWidth))
            {
                return (percentage / 100.0) * parentWidth;
            }

            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
