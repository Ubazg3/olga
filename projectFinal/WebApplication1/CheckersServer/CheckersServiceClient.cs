using System.ServiceModel;
using System.ServiceModel.Channels;

namespace WebApplication1.CheckersServer;

// Strongly-typed WCF proxy. Use it via DI through the higher-level
// CheckersBackend wrapper — pages don't talk to ChannelBase directly.
public partial class CheckersServiceClient
    : ClientBase<ICheckersService>, ICheckersService
{
    public CheckersServiceClient(Binding binding, EndpointAddress remoteAddress)
        : base(binding, remoteAddress) { }

    // --- sync passthrough ---
    public LoginResult Login   (string u, string p)            => Channel.Login(u, p);
    public LoginResult Register(string u, string p, string e, System.DateTime? bd, string c)
        => Channel.Register(u, p, e, bd, c);
    public void        Logout  (System.Guid t)                  => Channel.Logout(t);

    public string JoinQueue (System.Guid t) => Channel.JoinQueue(t);
    public void   LeaveQueue(System.Guid t) => Channel.LeaveQueue(t);

    public GameStateDto       GetCurrentGame        (System.Guid t)             => Channel.GetCurrentGame(t);
    public List<List<Square>> GetLegalMovesForSquare(System.Guid t, int f, int r) => Channel.GetLegalMovesForSquare(t, f, r);
    public List<Move>         GetMoveHistory        (int gameId)                => Channel.GetMoveHistory(gameId);

    public MoveResult MakeMove(System.Guid t, List<Square> path) => Channel.MakeMove(t, path);
    public MoveResult Resign  (System.Guid t)                    => Channel.Resign(t);

    public UserProfileDto GetUserProfile         (int userId)                                            => Channel.GetUserProfile(userId);
    public void           UpdateMyEmail          (System.Guid t, string email)                            => Channel.UpdateMyEmail(t, email);
    public void           ChangeMyPassword       (System.Guid t, string current, string fresh)            => Channel.ChangeMyPassword(t, current, fresh);
    public void           UpdateMyProfilePicture (System.Guid t, byte[] image)                            => Channel.UpdateMyProfilePicture(t, image);

    public List<User>       GetTopPlayers   (int count)        => Channel.GetTopPlayers(count);
    public List<GameRecord> GetMyGameHistory(System.Guid t)    => Channel.GetMyGameHistory(t);
    public List<User>       GetOnlinePlayers()                 => Channel.GetOnlinePlayers();

    public List<User>       AdminGetAllUsers   (System.Guid t)                            => Channel.AdminGetAllUsers(t);
    public bool             AdminBanUser       (System.Guid t, int userId, bool banned)   => Channel.AdminBanUser(t, userId, banned);
    public List<GameRecord> AdminGetRecentGames(System.Guid t, int count)                 => Channel.AdminGetRecentGames(t, count);
    public ServerStatsDto   AdminGetServerStats(System.Guid t)                            => Channel.AdminGetServerStats(t);
}
