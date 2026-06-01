using System;
using System.Globalization;
using System.Windows.Data;
using CheckersClient.Helpers;

namespace CheckersClient.Converters
{
    // byte[] (e.g. User.ProfilePicture) → ImageSource for an Image
    // control. Returns null when the input is null/empty so callers
    // can layer a NullToVisibility converter on top to hide the
    // <Image> and reveal the initials fallback.
    public class ByteArrayToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            byte[] bytes = value as byte[];
            return ImageProcessor.BytesToImage(bytes);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
