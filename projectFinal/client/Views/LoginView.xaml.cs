using System.Windows;
using System.Windows.Controls;
using CheckersClient.ViewModels;

namespace CheckersClient.Views
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
        }

        // PasswordBox.Password is intentionally not bindable. We push it
        // into the view-model right before invoking the command so the
        // password never lives in a property where binding bots could
        // read it.
        private void OnLoginClick(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as LoginViewModel;
            if (vm != null) vm.LoginPassword = LoginPasswordBox.Password;
        }

        private void OnRegisterClick(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as LoginViewModel;
            if (vm != null) vm.RegPassword = RegPasswordBox.Password;
        }
    }
}
