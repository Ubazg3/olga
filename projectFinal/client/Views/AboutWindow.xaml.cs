using System.Windows;

namespace CheckersClient.Views
{
    // Modal "About" dialog reachable from the lobby's ⓘ button. The
    // school project rubric mentions an "organisation description"
    // section; this is the in-app counterpart.
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
