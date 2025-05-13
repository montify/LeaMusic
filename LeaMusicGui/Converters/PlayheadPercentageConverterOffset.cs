using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

namespace LeaMusicGui.Converters
{
    public class PlayheadPercentageConverterOffset : IValueConverter
    {
       
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (double.TryParse(value?.ToString(), out double percentage))
            {
                return ((percentage / 100.0f) * 1200);
            }

            return 0.0;
        }

 

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
