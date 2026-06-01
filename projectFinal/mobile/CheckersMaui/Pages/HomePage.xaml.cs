using CheckersMaui.Services;
using CheckersMaui.Services.CheckersServer;

namespace CheckersMaui.Pages;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!AppState.IsLoggedIn)
        {
            // Defensive — somehow landed here without logging in.
            await Shell.Current.GoToAsync("//Login");
            return;
        }

        BindCurrentUser();
        await LoadOnlineAsync();
    }

    private void OnRefreshing(object? sender, EventArgs e)
    {
        // Pull-to-refresh: re-fetch the user profile (so wins/losses
        // update after a game) and the online list.
        _ = ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        try
        {
            // Refresh stats — the User cached in AppState is from
            // login, so its win/loss numbers grow stale. Pull a fresh
            // profile so the UI matches the server.
            var profile = await ServiceHelper.CallAsync(s =>
                s.GetUserProfileAsync(AppState.UserId));
            if (profile?.User != null)
            {
                AppState.User = profile.User;
                BindCurrentUser();
            }
            await LoadOnlineAsync();
        }
        catch (Exception ex)
        {
            ShowStatus("Refresh failed: " + ex.Message);
        }
        finally
        {
            Refresher.IsRefreshing = false;
        }
    }

    private void BindCurrentUser()
    {
        var u = AppState.User!;
        WelcomeLabel.Text = $"Hi, {u.Username}";
        MetaLabel.Text    = u.Role == UserRole.Admin
            ? "Admin · " + (u.Country ?? "—")
            : "Player · " + (string.IsNullOrEmpty(u.Country) ? "—" : u.Country);
        RatingLabel.Text  = u.Rating.ToString();
        WinsLabel.Text    = u.Wins.ToString();
        LossesLabel.Text  = u.Losses.ToString();
        DrawsLabel.Text   = u.Draws.ToString();
    }

    private async Task LoadOnlineAsync()
    {
        try
        {
            var players = await ServiceHelper.CallAsync(s => s.GetOnlinePlayersAsync());

            // Filter ourselves out — "online now" makes more sense as
            // "everyone else online" on the home screen.
            var others = players.Where(p => p.Id != AppState.UserId).ToList();
            OnlineList.ItemsSource = others;
            NoOnlineLabel.IsVisible = others.Count == 0;
        }
        catch (Exception ex)
        {
            // Don't crash the home screen if the feed fails — just
            // show a status hint.
            ShowStatus("Could not load online players: " + ex.Message);
            OnlineList.ItemsSource = Array.Empty<User>();
            NoOnlineLabel.IsVisible = true;
        }
    }

    private async void OnProfile     (object s, EventArgs e) => await Shell.Current.GoToAsync("//Profile");
    private async void OnGameHistory (object s, EventArgs e) => await Shell.Current.GoToAsync("//GameHistory");
    private async void OnLeaderboard (object s, EventArgs e) => await Shell.Current.GoToAsync("//Leaderboard");

    private async void OnLogout(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Sign out", "Sign out of this account?", "Yes", "Cancel");
        if (!confirm) return;

        var token = AppState.Token;
        AppState.Clear();
        try { await ServiceHelper.CallAsync(s => s.LogoutAsync(token)); } catch { /* best-effort */ }
        await Shell.Current.GoToAsync("//Login");
    }

    private void ShowStatus(string msg)
    {
        StatusLabel.Text      = msg;
        StatusLabel.IsVisible = true;
    }
}
