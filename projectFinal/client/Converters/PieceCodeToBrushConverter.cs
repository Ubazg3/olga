using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CheckersClient.Converters
{
    // Piece code character ('w'/'W'/'b'/'B'/'.') → solid brush used to
    // paint the disc on the board. Used by CheckersBoardControl.
    public class PieceCodeToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            char c = value is char ch ? ch : '.';
            switch (c)
            {
                case 'w':
                case 'W':
                    return new LinearGradientBrush(
                        Color.FromRgb(0xFF, 0xF8, 0xE6),
                        Color.FromRgb(0xE0, 0xC0, 0x80),
                        90);
                case 'b':
                case 'B':
                    return new LinearGradientBrush(
                        Color.FromRgb(0x33, 0x33, 0x33),
                        Color.FromRgb(0x10, 0x10, 0x10),
                        90);
                default:
                    return Brushes.Transparent;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
