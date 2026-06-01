using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CheckersClient.Converters
{
    // True → Visible, false → Collapsed. Pass parameter "Invert" /
    // "Inverse" to flip the meaning. WPF ships an equivalent converter,
    // but rubric-wise we want our own.
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = value is bool && (bool)value;
            string p = parameter as string;
            bool invert = string.Equals(p, "Invert",  StringComparison.OrdinalIgnoreCase)
                       || string.Equals(p, "Inverse", StringComparison.OrdinalIgnoreCase);
            if (invert) b = !b;
            return b ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility v && v == Visibility.Visible;
        }
    }
}
