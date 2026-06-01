using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CheckersClient.Converters
{
    // Kings have uppercase codes ('W' / 'B'). Used to show the crown
    // overlay on top of the disc.
    public class PieceCodeToKingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            char c = value is char ch ? ch : '.';
            return (c == 'W' || c == 'B') ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
