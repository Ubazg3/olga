using System;
using System.Globalization;
using System.Windows.Data;
using Model;

namespace CheckersClient.Converters
{
    // GameEndReason enum → human-readable explanation shown on the
    // game-over screen.
    public class GameResultConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is GameEndReason r)) return string.Empty;
            switch (r)
            {
                case GameEndReason.NoPieces:      return "by capturing all pieces";
                case GameEndReason.NoLegalMoves:  return "by trapping the opponent";
                case GameEndReason.Resignation:   return "by resignation";
                case GameEndReason.DrawAgreement: return "by mutual agreement";
                case GameEndReason.Disconnection: return "the opponent disconnected";
                case GameEndReason.None:          return "";
                default:                          return r.ToString();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
