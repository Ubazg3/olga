using System;
using System.Globalization;
using System.Windows.Data;

namespace CheckersClient.Converters
{
    // Username → up to two-letter initials shown over the avatar circle.
    public class InitialsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string s = value as string;
            if (string.IsNullOrEmpty(s)) return string.Empty;

            string[] parts = s.Split(new[] { ' ', '_', '-', '.' },
                StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return string.Empty;
            if (parts.Length == 1) return parts[0].Substring(0, 1).ToUpperInvariant();
            return (parts[0].Substring(0, 1) + parts[parts.Length - 1].Substring(0, 1))
                .ToUpperInvariant();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
