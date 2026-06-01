using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CheckersClient.Converters
{
    // null → Collapsed, non-null → Visible. Pass parameter "Invert" to
    // flip the meaning.
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool invert = string.Equals(parameter as string, "Invert", StringComparison.OrdinalIgnoreCase);
            bool isNull = value == null
                          || (value is string s && string.IsNullOrEmpty(s));
            bool visible = invert ? isNull : !isNull;
            return visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
