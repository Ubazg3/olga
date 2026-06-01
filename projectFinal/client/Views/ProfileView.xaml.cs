using System.Windows;
using System.Windows.Controls;
using CheckersClient.ViewModels;

namespace CheckersClient.Views
{
    public partial class ProfileView : UserControl
    {
        public ProfileView()
        {
            InitializeComponent();
        }

        // PasswordBox.Password is intentionally not bindable. We push
        // the values into the view-model just before invoking the Save
        // command — same pattern used by the login screen.
        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as ProfileViewModel;
            if (vm == null) return;
            vm.CurrentPassword = CurrentPasswordBox.Password;
            vm.NewPassword     = NewPasswordBox.Password;
            vm.ConfirmPassword = ConfirmPasswordBox.Password;
        }
    }
}
