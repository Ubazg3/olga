using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.CheckersServer;
using WebApplication1.Services;

namespace WebApplication1.Pages.Admin;

public class GamesModel : PageModel
{
    private readonly CheckersBackend _backend;
    public GamesModel(CheckersBackend backend) { _backend = backend; }

    public List<GameRecord> RecentGames { get; private set; } = new();

    // The single game selected by /Admin/Games/{gameId}.
    public GameRecord? SelectedGame { get; private set; }
    public List<Move> Moves { get; private set; } = new();
    public string InitialBoard { get; private set; } = "";

    private Dictionary<int, string> _userNames = new();
    public string? Error { get; private set; }

    public async Task OnGetAsync(int? gameId)
    {
        var token = AdminAuth.RequireToken(User);
        try
        {
            // Always load the recent-games list — used both for the
            // top-level table and as a name lookup for the replay page.
            RecentGames = await _backend.AdminGetRecentGamesAsync(token, 50);

            // Resolve every distinct player id we'll need to render.
            var ids = new HashSet<int>();
            foreach (var g in RecentGames)
            {
                ids.Add(g.WhitePlayerId);
                ids.Add(g.BlackPlayerId);
            }

            if (gameId.HasValue)
            {
                SelectedGame = RecentGames.FirstOrDefault(g => g.Id == gameId.Value);
                if (SelectedGame == null)
                {
                    // Not in the most recent — fetch by user profile is
                    // the only WCF op that returns a game by id; for
                    // simplicity, we just show "not found" rather than
                    // adding a new server op.
                    Error = "Game not found in recent set.";
                    return;
                }

                Moves = await _backend.GetMoveHistoryAsync(SelectedGame.Id);
                InitialBoard = StartingBoardSerialized();

                ids.Add(SelectedGame.WhitePlayerId);
                ids.Add(SelectedGame.BlackPlayerId);
            }

            // Look up usernames for everyone we've referenced.
            foreach (int id in ids)
            {
                if (_userNames.ContainsKey(id)) continue;
                var profile = await _backend.GetUserProfileAsync(id);
                if (profile?.User != null)
                    _userNames[id] = profile.User.Username;
            }
        }
        catch (Exception ex)
        {
            Error = "Could not load games: " + ex.Message;
        }
    }

    public string UserName(int userId)
    {
        return _userNames.TryGetValue(userId, out var name) ? name : "#" + userId;
    }

    // Serialised the same way Board.Serialize() does it on the server
    // — 32 dark-square chars + " w 0" — so the JS replay viewer can
    // render the start position directly.
    private static string StartingBoardSerialized()
    {
        // r=0..7, dark squares only (file+rank even). Ranks 0-2 white,
        // ranks 5-7 black, 3-4 empty.
        var sb = new System.Text.StringBuilder(32);
        for (int r = 0; r < 8; r++)
            for (int f = 0; f < 8; f++)
            {
                if (((f + r) & 1) != 0) continue;
                if (r <= 2)      sb.Append('w');
                else if (r >= 5) sb.Append('b');
                else             sb.Append('.');
            }
        return sb.ToString() + " w 0";
    }

    // Compact projection passed to the JS replay engine — everything
    // it needs to render each step.
    public List<object> MovesForJs()
    {
        return Moves.Select(m => (object)new
        {
            n  = m.Notation,
            ba = m.BoardAfter,
            ff = m.FromFile, fr = m.FromRank,
            tf = m.ToFile,   tr = m.ToRank
        }).ToList();
    }
}
