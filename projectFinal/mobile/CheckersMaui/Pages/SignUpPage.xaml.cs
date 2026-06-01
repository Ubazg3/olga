using CheckersMaui.Services;
using CheckersMaui.Services.CheckersServer;

namespace CheckersMaui.Pages;

public partial class SignUpPage : ContentPage
{
    public SignUpPage()
    {
        InitializeComponent();
        // Date picker bounds + initial value. Doing this in code keeps
        // the XAML free of System.DateTime namespace plumbing.
        BirthDatePicker.MinimumDate = new DateTime(1900, 1, 1);
        BirthDatePicker.MaximumDate = DateTime.Today;
        BirthDatePicker.Date        = DateTime.Today.AddYears(-25);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        StatusLabel.IsVisible = false;
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        StatusLabel.IsVisible = false;

        string user    = UsernameEntry.Text?.Trim()    ?? "";
        string pass    = PasswordEntry.Text            ?? "";
        string email   = EmailEntry.Text?.Trim()       ?? "";
        string country = CountryEntry.Text?.Trim()     ?? "";
        DateTime? bd   = BirthDatePicker.Date;

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            ShowError("Username and password are required.");
            return;
        }
        if (pass.Length < 6)
        {
            ShowError("Password must be at least 6 characters long.");
            return;
        }

        SetBusy(true);
        try
        {
            LoginResult res = await ServiceHelper.CallAsync(s =>
                s.RegisterAsync(user, pass, email, bd, country));

            if (!res.Success)
            {
                ShowError(res.Error ?? "Registration failed.");
                return;
            }

            // Server logs us in as part of registration, just like
            // the WPF client does — store the token and head home.
            AppState.Token = res.Token;
            AppState.User  = res.User;
            await Shell.Current.GoToAsync("//Home");
        }
        catch (Exception ex)
        {
            ShowError("Could not reach the server: " + ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//Login");
    }

    private void SetBusy(bool busy)
    {
        Spinner.IsRunning      = busy;
        Spinner.IsVisible      = busy;
        RegisterButton.IsEnabled = !busy;
    }

    private void ShowError(string message)
    {
        StatusLabel.Text      = message;
        StatusLabel.TextColor = Colors.IndianRed;
        StatusLabel.IsVisible = true;
    }
}
