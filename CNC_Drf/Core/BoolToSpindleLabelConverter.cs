using System.Globalization;
using System.Windows.Data;

namespace CNC_Drf.Core;

[ValueConversion(typeof(bool), typeof(string))]
public class BoolToSpindleLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? "M3 ON" : "M3 OFF";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
