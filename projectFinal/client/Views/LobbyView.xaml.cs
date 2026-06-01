using System.Windows;
using System.Windows.Controls;
using CheckersClient.UserControls;
using CheckersClient.ViewModels;

namespace CheckersClient.Views
{
    public partial class LobbyView : UserControl
    {
        public LobbyView()
        {
            InitializeComponent();
        }

        // Forwards a card-click to the lobby's parent app, navigating
        // to the profile screen for the clicked user.
        private void OnPlayerCardClicked(object sender, CardClickedEventArgs e)
        {
            var vm = DataContext as LobbyViewModel;
            if (vm == null || e?.User == null) return;
            vm.OpenProfile(e.User.Id);
        }

        // Opens the modal About dialog; sets Owner so it inherits
        // the main window's icon/centering.
        private void OnAboutClick(object sender, RoutedEventArgs e)
        {
            var about = new AboutWindow { Owner = Window.GetWindow(this) };
            about.ShowDialog();
        }
    }
}

