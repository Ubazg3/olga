using CheckersMaui.Services;
using CheckersMaui.Services.CheckersServer;

namespace CheckersMaui.Pages;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Clear any leftover state when returning to the login screen
        // (e.g. after Logout). The username field is cleared too so a
        // second user on the same device starts fresh.
        UsernameEntry.Text = string.Empty;
        PasswordEntry.Text = string.Empty;
        StatusLabel.IsVisible = false;
        AppState.Clear();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        StatusLabel.IsVisible = false;
        string user = UsernameEntry.Text?.Trim() ?? "";
        string pass = PasswordEntry.Text ?? "";

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            ShowError("Fill in both username and password.");
            return;
        }

        SetBusy(true);
        try
        {
            LoginResult res = await ServiceHelper.CallAsync(s => s.LoginAsync(user, pass));
            if (!res.Success)
            {
                ShowError(res.Error ?? "Login failed.");
                return;
            }

            AppState.Token = res.Token;
            AppState.User  = res.User;
            await Shell.Current.GoToAsync("//Home");
        }
        catch (Exception ex)
        {
            // Most likely cause: server isn't running, or ServiceHost
            // value in ServiceHelper doesn't match the dev machine.
            ShowError("Could not reach the server: " + ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnSignUpClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//SignUp");
    }

    private void SetBusy(bool busy)
    {
        Spinner.IsRunning   = busy;
        Spinner.IsVisible   = busy;
        LoginButton.IsEnabled = !busy;
    }

    private void ShowError(string message)
    {
        StatusLabel.Text      = message;
        StatusLabel.TextColor = Colors.IndianRed;
        StatusLabel.IsVisible = true;
    }
}
