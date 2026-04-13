using System.Globalization;
using System.Windows.Data;

namespace CNC_Drf.Core;

[ValueConversion(typeof(bool), typeof(string))]
public class BoolToSpindleLabelConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is true ? "Spindle ON" : "Spindle";
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

[ValueConversion(typeof(bool), typeof(string))]
public class BoolToConnectLabelConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is true ? "Ligado" : "Ligar";
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

[ValueConversion(typeof(bool), typeof(Brush))]
public class BoolToBgConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is true
            ? new SolidColorBrush(Color.FromRgb(0x2e, 0x7d, 0x32))
            : new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44));
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

[ValueConversion(typeof(bool), typeof(bool))]
public class InvertBoolConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is bool b ? !b : false;
    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => value is bool b ? !b : false;
}

[ValueConversion(typeof(string), typeof(Visibility))]
public class EmptyToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is string s && (s == "Drop here" || string.IsNullOrEmpty(s))
            ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

[ValueConversion(typeof(bool), typeof(Visibility))]
public class BoolToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is true ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

[ValueConversion(typeof(bool), typeof(Visibility))]
public class InvertBoolToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is true ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

[ValueConversion(typeof(bool), typeof(Brush))]
public class CommentColorConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is true
            ? new SolidColorBrush(Color.FromRgb(0x6a, 0x99, 0x55))  // comentários: verde
            : new SolidColorBrush(Color.FromRgb(0xe0, 0xe0, 0xe0)); // código: branco claro
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}
