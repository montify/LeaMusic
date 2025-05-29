using System.Globalization;
using System.Windows.Data;

namespace LeaMusicGui.Converters
{
    public class LoopWidthPercentageConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (double.TryParse(values[0].ToString(), out double loopStartPercentage))
                if (double.TryParse(values[1].ToString(), out double loopEndPercentage))
                    if (double.TryParse(values[2].ToString(), out double renderWidth))
                    {
                        var start = (loopStartPercentage / 100.0f) * renderWidth;
                        var end = (loopEndPercentage / 100.0f) * renderWidth;
                        var width = end - start;
                        return width;
                    }

            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


}
