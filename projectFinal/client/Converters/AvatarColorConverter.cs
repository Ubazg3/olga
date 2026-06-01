using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CheckersClient.Converters
{
    // Username → deterministic background colour for the avatar circle.
    // Same username always lands on the same colour, so users recognise
    // each other across the lobby.
    public class AvatarColorConverter : IValueConverter
    {
        // Hand-picked palette — readable on dark backgrounds.
        private static readonly Color[] Palette =
        {
            Color.FromRgb(0xE5, 0x73, 0x73), // red
            Color.FromRgb(0xF0, 0x6E, 0x4E), // orange
            Color.FromRgb(0xE5, 0xB1, 0x4D), // amber
            Color.FromRgb(0x6F, 0xC2, 0x76), // green
            Color.FromRgb(0x4E, 0xB7, 0xC2), // teal
            Color.FromRgb(0x66, 0x99, 0xE5), // blue
            Color.FromRgb(0x9B, 0x77, 0xE5), // violet
            Color.FromRgb(0xE5, 0x6E, 0xC2), // pink
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string s = value as string ?? string.Empty;
            int hash = 0;
            foreach (char c in s) hash = (hash * 31 + c) & 0x7FFFFFFF;
            return new SolidColorBrush(Palette[hash % Palette.Length]);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
