using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace HwidSpoofer.Converters;

public class BoolToYesNoConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? "Yes" : "No";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class BoolToOnOffConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? "On" : "Off";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class BoolToStatusColorConverter : IValueConverter
{
    private static readonly SolidColorBrush Green = new(Color.FromRgb(0x4A, 0xDE, 0x80));
    private static readonly SolidColorBrush Red = new(Color.FromRgb(0xF8, 0x71, 0x71));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Green : Red;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
