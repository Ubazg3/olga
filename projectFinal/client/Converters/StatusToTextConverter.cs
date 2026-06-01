using System;
using System.Globalization;
using System.Windows.Data;
using Model;

namespace CheckersClient.Converters
{
    // GameStatus enum → human-readable label shown in the game header.
    public class StatusToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is GameStatus s)) return string.Empty;
            switch (s)
            {
                case GameStatus.Waiting:    return "Waiting for opponent…";
                case GameStatus.InProgress: return "Game in progress";
                case GameStatus.WhiteWins:  return "White wins";
                case GameStatus.BlackWins:  return "Black wins";
                case GameStatus.Draw:       return "Draw";
                case GameStatus.Aborted:    return "Aborted";
                default:                    return s.ToString();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
