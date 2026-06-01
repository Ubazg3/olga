using CheckersMaui.Services;
using CheckersMaui.Services.CheckersServer;

namespace CheckersMaui.Pages;

public partial class ProfilePage : ContentPage
{
    public ProfilePage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!AppState.IsLoggedIn)
        {
            await Shell.Current.GoToAsync("//Login");
            return;
        }
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        SetBusy(true);
        try
        {
            // Pull a fresh profile so the displayed stats reflect any
            // games played since login.
            var profile = await ServiceHelper.CallAsync(s =>
                s.GetUserProfileAsync(AppState.UserId));
            if (profile?.User != null)
            {
                AppState.User = profile.User;
                Bind(profile.User);
            }
        }
        catch (Exception ex)
        {
            ShowStatus("Could not load profile: " + ex.Message, isError: true);
        }
        finally { SetBusy(false); }
    }

    private void Bind(User u)
    {
        UsernameLabel.Text = u.Username ?? "(unknown)";
        RoleLabel.Text     = u.Role == UserRole.Admin ? "Admin" : "Player";
        MemberLabel.Text   = "Member since " + u.CreatedAt.ToString("MMM yyyy");

        RatingValue.Text = u.Rating.ToString();
        WinsValue.Text   = u.Wins.ToString();
        LossesValue.Text = u.Losses.ToString();
        DrawsValue.Text  = u.Draws.ToString();

        EmailEntry.Text = u.Email ?? string.Empty;
    }

    private async void OnUpdateEmailClicked(object sender, EventArgs e)
    {
        SetBusy(true);
        StatusLabel.IsVisible = false;
        try
        {
            string email = EmailEntry.Text?.Trim() ?? "";
            await ServiceHelper.CallAsync(s => s.UpdateMyEmailAsync(AppState.Token, email));
            ShowStatus("Email updated.", isError: false);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            ShowStatus("Update failed: " + ex.Message, isError: true);
        }
        finally { SetBusy(false); }
    }

    private async void OnChangePasswordClicked(object sender, EventArgs e)
    {
        StatusLabel.IsVisible = false;
        string cur     = CurrentPasswordEntry.Text ?? "";
        string nw      = NewPasswordEntry.Text     ?? "";
        string confirm = ConfirmPasswordEntry.Text ?? "";

        if (string.IsNullOrEmpty(cur) || string.IsNullOrEmpty(nw))
        {
            ShowStatus("Fill in current and new password.", isError: true);
            return;
        }
        if (nw.Length < 6)
        {
            ShowStatus("New password must be at least 6 characters.", isError: true);
            return;
        }
        if (nw != confirm)
        {
            ShowStatus("New password and confirmation don't match.", isError: true);
            return;
        }

        SetBusy(true);
        try
        {
            await ServiceHelper.CallAsync(s =>
                s.ChangeMyPasswordAsync(AppState.Token, cur, nw));
            CurrentPasswordEntry.Text = "";
            NewPasswordEntry.Text     = "";
            ConfirmPasswordEntry.Text = "";
            ShowStatus("Password changed.", isError: false);
        }
        catch (Exception ex)
        {
            ShowStatus("Change failed: " + ex.Message, isError: true);
        }
        finally { SetBusy(false); }
    }

    private async void OnBackClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//Home");

    private void SetBusy(bool busy)
    {
        Spinner.IsRunning = busy;
        Spinner.IsVisible = busy;
    }

    private void ShowStatus(string msg, bool isError)
    {
        StatusLabel.Text      = msg;
        StatusLabel.TextColor = isError ? Colors.IndianRed : Colors.MediumSeaGreen;
        StatusLabel.IsVisible = true;
    }
}
