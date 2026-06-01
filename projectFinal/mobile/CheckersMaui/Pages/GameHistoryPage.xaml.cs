using System.Collections.ObjectModel;
using CheckersMaui.Services;
using CheckersMaui.Services.CheckersServer;

namespace CheckersMaui.Pages;

public partial class GameHistoryPage : ContentPage
{
    public ObservableCollection<GameRow> Rows { get; } = new();

    public GameHistoryPage()
    {
        InitializeComponent();
        GamesList.ItemsSource = Rows;
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
            var games = await ServiceHelper.CallAsync(s =>
                s.GetMyGameHistoryAsync(AppState.Token));

            Rows.Clear();
            foreach (var g in games)
                Rows.Add(GameRow.From(g, AppState.UserId));

            // Resolver will add opponent usernames lazily — fetch
            // each unique opponent profile once.
            await ResolveOpponentsAsync(games);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Could not load history: " + ex.Message, "OK");
        }
        finally
        {
            Refresher.IsRefreshing = false;
        }
    }

    private async Task ResolveOpponentsAsync(List<GameRecord> games)
    {
        // Collect distinct opponent ids.
        var ids = new HashSet<int>();
        foreach (var g in games)
        {
            int oppId = g.WhitePlayerId == AppState.UserId ? g.BlackPlayerId : g.WhitePlayerId;
            if (oppId > 0) ids.Add(oppId);
        }

        var nameById = new Dictionary<int, string>();
        foreach (int id in ids)
        {
            try
            {
                var p = await ServiceHelper.CallAsync(s => s.GetUserProfileAsync(id));
                if (p?.User?.Username is { } name) nameById[id] = name;
            }
            catch { /* ignore one bad lookup; default "#id" remains */ }
        }

        // Patch the rows in place.
        for (int i = 0; i < Rows.Count; i++)
        {
            var r = Rows[i];
            if (nameById.TryGetValue(r.OpponentId, out var name))
                Rows[i] = r.WithOpponent(name);
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//Home");
}

// Flat view-model row so the XAML stays simple. Captures only what
// the template needs — outcome chip text/colour, opponent label, and
// the date/moves subtitle.
public class GameRow
{
    public int    OpponentId  { get; init; }
    public string Title       { get; init; } = "";
    public string Subtitle    { get; init; } = "";
    public string MovesText   { get; init; } = "";
    public string OutcomeText { get; init; } = "";
    public Color  OutcomeColor{ get; init; } = Colors.Gray;

    public static GameRow From(GameRecord g, int meId)
    {
        bool meWhite = g.WhitePlayerId == meId;
        int  oppId   = meWhite ? g.BlackPlayerId : g.WhitePlayerId;

        // Outcome from this user's perspective.
        string outcome;
        Color  color;
        if (g.Status == GameStatus.InProgress)            { outcome = "LIVE";  color = Colors.SteelBlue; }
        else if (g.Status == GameStatus.Draw)             { outcome = "DRAW";  color = Colors.SlateGray; }
        else if (g.WinnerId == meId)                      { outcome = "WIN";   color = Colors.MediumSeaGreen; }
        else if (g.Status == GameStatus.Aborted)          { outcome = "ABRT";  color = Colors.Gray; }
        else                                              { outcome = "LOSS";  color = Colors.IndianRed; }

        string title    = $"vs #{oppId}";
        string subtitle = $"{g.StartedAt:dd MMM yyyy HH:mm}";
        string moves    = $"{g.MoveCount} moves";

        return new GameRow
        {
            OpponentId   = oppId,
            Title        = title,
            Subtitle     = subtitle,
            MovesText    = moves,
            OutcomeText  = outcome,
            OutcomeColor = color,
        };
    }

    public GameRow WithOpponent(string name) => new()
    {
        OpponentId   = OpponentId,
        Title        = $"vs {name}",
        Subtitle     = Subtitle,
        MovesText    = MovesText,
        OutcomeText  = OutcomeText,
        OutcomeColor = OutcomeColor,
    };
}
