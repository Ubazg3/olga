using System.Collections.ObjectModel;
using CheckersMaui.Services;
using CheckersMaui.Services.CheckersServer;

namespace CheckersMaui.Pages;

public partial class LeaderboardPage : ContentPage
{
    public ObservableCollection<LeaderRow> Rows { get; } = new();

    public LeaderboardPage()
    {
        InitializeComponent();
        List.ItemsSource = Rows;
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

    private void OnRefreshing(object? sender, EventArgs e) => _ = ReloadAsync();

    private async Task ReloadAsync()
    {
        try
        {
            var players = await ServiceHelper.CallAsync(s => s.GetTopPlayersAsync(20));
            Rows.Clear();
            for (int i = 0; i < players.Count; i++)
                Rows.Add(LeaderRow.From(players[i], i + 1));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Could not load leaderboard: " + ex.Message, "OK");
        }
        finally
        {
            Refresher.IsRefreshing = false;
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//Home");
}

public class LeaderRow
{
    public string Username    { get; init; } = "";
    public string RankText    { get; init; } = "";
    public Color  RankColor   { get; init; } = Colors.SlateGray;
    public string RatingText  { get; init; } = "";
    public string RecordText  { get; init; } = "";

    public static LeaderRow From(User u, int rank) => new()
    {
        Username   = u.Username ?? "(anonymous)",
        RankText   = rank.ToString(),
        // Gold / silver / bronze for the top three; muted for the rest.
        RankColor  = rank switch
        {
            1 => Color.FromArgb("#F4D060"),
            2 => Color.FromArgb("#C0C0C0"),
            3 => Color.FromArgb("#CD7F32"),
            _ => Color.FromArgb("#3D4860"),
        },
        RatingText = u.Rating.ToString(),
        RecordText = $"{u.Wins}W · {u.Losses}L · {u.Draws}D",
    };
}
