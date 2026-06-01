using System.ServiceModel;
using WebApplication1.CheckersServer;

namespace WebApplication1.Services;

// One per request. Hides the WCF channel lifecycle (open / faulted /
// abort) so Razor Pages just call methods on this and get results.
//
// Singleton-friendly: builds the channel from the config-supplied
// endpoint URL and recreates it transparently when WCF marks it
// Faulted (e.g. transient server restart).
public class CheckersBackend : IDisposable
{
    private readonly EndpointAddress _address;
    private readonly NetTcpBinding   _binding;
    private readonly object          _lock = new();
    private CheckersServiceClient?   _client;

    public CheckersBackend(IConfiguration config)
    {
        string url = config["CheckersServer:Endpoint"]
            ?? "net.tcp://localhost:9100/CheckersService/";

        _address = new EndpointAddress(url);
        _binding = new NetTcpBinding(SecurityMode.None)
        {
            MaxReceivedMessageSize = 2_097_152,
            SendTimeout    = TimeSpan.FromMinutes(1),
            ReceiveTimeout = TimeSpan.FromMinutes(10)
        };
    }

    private CheckersServiceClient Client
    {
        get
        {
            lock (_lock)
            {
                if (_client == null
                    || _client.State == CommunicationState.Faulted
                    || _client.State == CommunicationState.Closed)
                {
                    try { _client?.Abort(); } catch { /* ignore */ }
                    _client = new CheckersServiceClient(_binding, _address);
                }
                return _client;
            }
        }
    }

    // Each method lifts a synchronous WCF call onto a thread-pool thread
    // so Razor Page handlers can `await` them without blocking the
    // request-processing thread.
    public Task<LoginResult>      LoginAsync   (string u, string p)             => Task.Run(() => Client.Login(u, p));
    public Task<LoginResult>      RegisterAsync(string u, string p, string e,
                                                  DateTime? bd = null, string? c = null)
        => Task.Run(() => Client.Register(u, p, e, bd, c ?? ""));
    public Task                   LogoutAsync  (Guid t)                          => Task.Run(() => Client.Logout(t));

    public Task<List<User>>       GetTopPlayersAsync   (int count)               => Task.Run(() => Client.GetTopPlayers(count));
    public Task<List<User>>       GetOnlinePlayersAsync()                        => Task.Run(() => Client.GetOnlinePlayers());

    public Task<List<User>>       AdminGetAllUsersAsync   (Guid t)               => Task.Run(() => Client.AdminGetAllUsers(t));
    public Task<bool>             AdminBanUserAsync       (Guid t, int id, bool b)
        => Task.Run(() => Client.AdminBanUser(t, id, b));
    public Task<List<GameRecord>> AdminGetRecentGamesAsync(Guid t, int count)    => Task.Run(() => Client.AdminGetRecentGames(t, count));
    public Task<ServerStatsDto>   AdminGetServerStatsAsync(Guid t)                => Task.Run(() => Client.AdminGetServerStats(t));

    public Task<UserProfileDto>   GetUserProfileAsync(int userId)                => Task.Run(() => Client.GetUserProfile(userId));
    public Task<List<Move>>       GetMoveHistoryAsync(int gameId)                => Task.Run(() => Client.GetMoveHistory(gameId));

    public void Dispose()
    {
        try
        {
            if (_client != null)
            {
                if (_client.State == CommunicationState.Opened) _client.Close();
                else                                            _client.Abort();
            }
        }
        catch { _client?.Abort(); }
    }
}
