using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CheckersClient.Validation;
using Model;

namespace CheckersClient.ViewModels
{
    // Login + Sign-up screen. The view binds two tabs against this VM
    // and one password field is read directly from the view because
    // PasswordBox.Password is not bindable.
    public class LoginViewModel : ViewModelBase
    {
        private readonly AppViewModel _app;

        public LoginViewModel(AppViewModel app)
        {
            _app = app;
            LoginCommand    = new RelayCommand(async _ => await DoLoginAsync(),    _ => !IsBusy);
            RegisterCommand = new RelayCommand(async _ => await DoRegisterAsync(), _ => !IsBusy);
        }

        // ----- Login fields -----
        private string _loginUsername = "";
        public  string LoginUsername  { get { return _loginUsername; } set { SetProperty(ref _loginUsername, value); } }

        // ----- Register fields -----
        private string _regUsername = "";
        public  string RegUsername  { get { return _regUsername; } set { SetProperty(ref _regUsername, value); } }

        private string _regEmail = "";
        public  string RegEmail  { get { return _regEmail; } set { SetProperty(ref _regEmail, value); } }

        // Optional profile-extra fields collected during sign-up.
        private DateTime? _regBirthDate;
        public  DateTime? RegBirthDate { get { return _regBirthDate; } set { SetProperty(ref _regBirthDate, value); } }

        private string _regCountry = "";
        public  string RegCountry  { get { return _regCountry; } set { SetProperty(ref _regCountry, value); } }

        // ----- UI state -----
        private bool _isBusy;
        public  bool IsBusy { get { return _isBusy; } set { SetProperty(ref _isBusy, value); } }

        private string _statusMessage;
        public  string StatusMessage { get { return _statusMessage; } set { SetProperty(ref _statusMessage, value); } }

        private bool _isError;
        public  bool IsError { get { return _isError; } set { SetProperty(ref _isError, value); } }

        public ICommand LoginCommand    { get; }
        public ICommand RegisterCommand { get; }

        // The view sets these from PasswordBox.Password before calling
        // the command, since PasswordBox.Password isn't bindable.
        public string LoginPassword    { get; set; } = "";
        public string RegPassword      { get; set; } = "";

        // ----- Operations -----

        private async Task DoLoginAsync()
        {
            // Login is lenient: any non-empty username + password is
            // forwarded to the server, which is the actual authority on
            // whether they're valid (sign-up enforces the format rules).
            // This lets short legacy accounts log in (e.g. seeded admin).
            if (string.IsNullOrWhiteSpace(LoginUsername))
            {
                Show("Username is required.", true); return;
            }
            if (string.IsNullOrEmpty(LoginPassword))
            {
                Show("Password is required.", true); return;
            }

            IsBusy = true;
            Show("Connecting…", false);
            try
            {
                LoginResult res = await _app.Client.LoginAsync(LoginUsername.Trim(), LoginPassword);
                if (res == null || !res.Success)
                {
                    Show(res?.Error ?? "Login failed.", true);
                    return;
                }
                _app.GoToLobby();
            }
            catch (System.Exception ex)
            {
                Show("Could not reach the server: " + ex.Message, true);
            }
            finally { IsBusy = false; }
        }

        private async Task DoRegisterAsync()
        {
            string err = InputValidator.ValidateUsername(RegUsername)
                      ?? InputValidator.ValidatePassword(RegPassword)
                      ?? InputValidator.ValidateEmail(RegEmail)
                      ?? ValidateBirthDate(RegBirthDate);
            if (err != null) { Show(err, true); return; }

            IsBusy = true;
            Show("Creating your account…", false);
            try
            {
                LoginResult res = await _app.Client.RegisterAsync(
                    RegUsername.Trim(),
                    RegPassword,
                    RegEmail?.Trim(),
                    RegBirthDate,
                    RegCountry?.Trim());
                if (res == null || !res.Success)
                {
                    Show(res?.Error ?? "Registration failed.", true);
                    return;
                }
                _app.GoToLobby();
            }
            catch (System.Exception ex)
            {
                Show("Could not reach the server: " + ex.Message, true);
            }
            finally { IsBusy = false; }
        }

        private void Show(string msg, bool isError)
        {
            StatusMessage = msg;
            IsError = isError;
        }

        // Birth date is optional, but if supplied we sanity-check it.
        private static string ValidateBirthDate(DateTime? date)
        {
            if (!date.HasValue) return null;
            DateTime today = DateTime.Today;
            if (date.Value > today)        return "Birth date can't be in the future.";
            if (date.Value.Year < 1900)    return "Birth date is too far in the past.";
            int years = today.Year - date.Value.Year;
            if (date.Value.Date > today.AddYears(-years)) years--;
            if (years < 5)  return "You must be at least 5 years old to register.";
            return null;
        }
    }
}
