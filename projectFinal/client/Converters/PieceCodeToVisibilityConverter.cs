using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CheckersClient.Converters
{
    // Visible iff the cell holds a piece (not '.').
    public class PieceCodeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            char c = value is char ch ? ch : '.';
            return c == '.' ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
