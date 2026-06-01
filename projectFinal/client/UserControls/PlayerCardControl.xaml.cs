using System;
using System.Windows.Controls;
using System.Windows.Input;
using Model;

namespace CheckersClient.UserControls
{
    public partial class PlayerCardControl : UserControl
    {
        // Fired when the user clicks anywhere on the card. Carries the
        // bound User so callers don't have to dig into DataContext
        // themselves. A delegate-typed event — same pattern as
        // CheckersBoardControl.SquareClicked.
        public delegate void CardClickedHandler(object sender, CardClickedEventArgs e);
        public event CardClickedHandler Clicked;

        public PlayerCardControl()
        {
            InitializeComponent();
        }

        private void OnRootClicked(object sender, MouseButtonEventArgs e)
        {
            User user = DataContext as User;
            if (user == null) return;
            Clicked?.Invoke(this, new CardClickedEventArgs(user));
            e.Handled = true;
        }
    }

    public class CardClickedEventArgs : EventArgs
    {
        public User User { get; }
        public CardClickedEventArgs(User user) { User = user; }
    }
}
