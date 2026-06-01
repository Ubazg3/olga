using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;
using BusinessLogic;
using Model;
using ViewDB;

namespace CheckersServer
{
    // Stateless service. Each request authenticates with a token issued
    // by Login / Register; the server keeps a shared
    // ConcurrentDictionary<Guid,int> mapping token → userId. There are
    // no WCF sessions and no duplex callbacks — the protocol matches
    // the school's reference style and the requirement that the network
    // service be stateless.
    [ServiceBehavior(
        InstanceContextMode       = InstanceContextMode.PerCall,
        ConcurrencyMode           = ConcurrencyMode.Multiple,
        UseSynchronizationContext = false)]
    public class CheckersService : ICheckersService
    {
        // Runs exactly once per AppDomain, the first time WCF touches
        // this type — i.e. before any service operation can be
        // dispatched. Initialises the database and seeds the default
        // admin without needing an explicit Main method.
        static CheckersService()
        {
            Bootstrap.EnsureInitialised();
        }

        // Process-wide singletons — every request uses the same hub.
        public  static GameManager Manager { get; } = new GameManager();
        private static readonly AuthService _auth = new AuthService();
        private static readonly UsersDB _users    = new UsersDB();
        private static readonly GamesDB _games    = new GamesDB();
        private static readonly MovesDB _moves    = new MovesDB();

        // token → userId. Tokens are random Guids issued at login.
        private static readonly ConcurrentDictionary<Guid, int> _tokens =
            new ConcurrentDictionary<Guid, int>();

        // ===== Auth =====

        public LoginResult Login(string username, string password)
        {
            return LoginAsync(username, password).GetAwaiter().GetResult();
        }

        public async Task<LoginResult> LoginAsync(string username, string password)
        {
            AuthResult res = await _auth.LoginAsync(username, password);
            if (!res.Success) return LoginResult.Fail(res.Error);

            Guid token = Guid.NewGuid();
            _tokens[token] = res.User.Id;
            Manager.RegisterOnline(res.User);
            return LoginResult.Ok(token, SafeUser(res.User));
        }

        public LoginResult Register(string username, string password, string email,
                                    DateTime? birthDate, string country)
        {
            return RegisterAsync(username, password, email, birthDate, country)
                .GetAwaiter().GetResult();
        }

        public async Task<LoginResult> RegisterAsync(string username, string password, string email,
                                                     DateTime? birthDate, string country)
        {
            AuthResult res = await _auth.RegisterAsync(username, password, email, birthDate, country);
            if (!res.Success) return LoginResult.Fail(res.Error);

            Guid token = Guid.NewGuid();
            _tokens[token] = res.User.Id;
            Manager.RegisterOnline(res.User);
            return LoginResult.Ok(token, SafeUser(res.User));
        }

        public void Logout(Guid token)
        {
            LogoutAsync(token).GetAwaiter().GetResult();
        }

        public async Task LogoutAsync(Guid token)
        {
            if (_tokens.TryRemove(token, out int userId))
                await Manager.HandleDisconnectAsync(userId);
        }

        // ===== Matchmaking =====

        public string JoinQueue(Guid token)
        {
            return JoinQueueAsync(token).GetAwaiter().GetResult();
        }

        public async Task<string> JoinQueueAsync(Guid token)
        {
            User u = await RequireAuthAsync(token);
            JoinResult res = await Manager.JoinMatchmakingAsync(u);
            return res.Started ? "Game started!" : res.Message;
        }

        public void LeaveQueue(Guid token)
        {
            LeaveQueueAsync(token).GetAwaiter().GetResult();
        }

        public string JoinBotGame(Guid token)
        {
            return JoinBotGameAsync(token).GetAwaiter().GetResult();
        }

        public async Task<string> JoinBotGameAsync(Guid token)
        {
            User u = await RequireAuthAsync(token);
            JoinResult res = await Manager.JoinBotGameAsync(u);
            return res.Started ? "Bot game started!" : res.Message;
        }

        public async Task LeaveQueueAsync(Guid token)
        {
            User u = await RequireAuthAsync(token);
            Manager.LeaveMatchmaking(u.Id);
        }

        // ===== Game state =====

        public GameStateDto GetCurrentGame(Guid token)
        {
            return GetCurrentGameAsync(token).GetAwaiter().GetResult();
        }

        public async Task<GameStateDto> GetCurrentGameAsync(Guid token)
        {
            User u = await RequireAuthAsync(token);
            GameSession s = Manager.FindByUserId(u.Id);
            return s?.ToDto();
        }

        public List<List<Square>> GetLegalMovesForSquare(Guid token, int file, int rank)
        {
            return GetLegalMovesForSquareAsync(token, file, rank).GetAwaiter().GetResult();
        }

        public Task<List<List<Square>>> GetLegalMovesForSquareAsync(Guid token, int file, int rank)
        {
            return Task.Run(() =>
            {
                User u = RequireAuthAsync(token).GetAwaiter().GetResult();
                GameSession s = Manager.FindByUserId(u.Id);
                List<List<Square>> result = new List<List<Square>>();
                if (s == null) return result;
                if (s.ColorOf(u.Id) != s.Board.SideToMove) return result;

                foreach (Move m in CheckersEngine.GenerateLegalMoves(s.Board))
                {
                    if (m.FromFile == file && m.FromRank == rank)
                        result.Add(m.Path);
                }
                return result;
            });
        }

        public List<Move> GetMoveHistory(int gameId)
        {
            return GetMoveHistoryAsync(gameId).GetAwaiter().GetResult();
        }

        public Task<List<Move>> GetMoveHistoryAsync(int gameId)
        {
            return _moves.GetByGameAsync(gameId);
        }

        // ===== Game actions =====

        public MoveResult MakeMove(Guid token, List<Square> path)
        {
            return MakeMoveAsync(token, path).GetAwaiter().GetResult();
        }

        public async Task<MoveResult> MakeMoveAsync(Guid token, List<Square> path)
        {
            User u = await RequireAuthAsync(token);
            if (path == null || path.Count < 2)
                return MoveResult.Fail("Move path must contain at least a start and an end.");
            return await Manager.SubmitMoveAsync(u.Id, path);
        }

        public MoveResult Resign(Guid token)
        {
            return ResignAsync(token).GetAwaiter().GetResult();
        }

        public async Task<MoveResult> ResignAsync(Guid token)
        {
            User u = await RequireAuthAsync(token);
            return await Manager.ResignAsync(u.Id);
        }

        public void OfferDraw(Guid token)
        {
            User u = RequireAuthAsync(token).GetAwaiter().GetResult();
            Manager.OfferDraw(u.Id);
        }

        public MoveResult RespondToDraw(Guid token, bool accept)
        {
            User u = RequireAuthAsync(token).GetAwaiter().GetResult();
            return Manager.RespondToDrawAsync(u.Id, accept).GetAwaiter().GetResult();
        }

        // ===== Profiles =====

        public UserProfileDto GetUserProfile(int userId)
        {
            return GetUserProfileAsync(userId).GetAwaiter().GetResult();
        }

        public async Task<UserProfileDto> GetUserProfileAsync(int userId)
        {
            User target = await _users.GetByIdAsync(userId);
            if (target == null) return null;

            List<GameRecord> games = await _games.GetByPlayerAsync(userId);
            // Cap at 15 most recent — already ordered DESC by StartedAt.
            if (games.Count > 15) games = games.GetRange(0, 15);

            // Resolve every distinct opponent username for the games we
            // ship back, so the client can render "vs alice" rows
            // without making N extra round-trips.
            Dictionary<int, string> names = new Dictionary<int, string>();
            HashSet<int> needed = new HashSet<int>();
            foreach (GameRecord g in games)
            {
                needed.Add(g.WhitePlayerId);
                needed.Add(g.BlackPlayerId);
            }
            foreach (int id in needed)
            {
                if (id == userId) { names[id] = target.Username; continue; }
                User u = await _users.GetByIdAsync(id);
                if (u != null) names[id] = u.Username;
            }

            return new UserProfileDto
            {
                User        = SafeUser(target),
                RecentGames = games,
                Usernames   = names
            };
        }

        public void UpdateMyProfilePicture(Guid token, byte[] image)
        {
            UpdateMyProfilePictureAsync(token, image).GetAwaiter().GetResult();
        }

        public async Task UpdateMyProfilePictureAsync(Guid token, byte[] image)
        {
            User u = await RequireAuthAsync(token);

            // Cap the upload at ~1.5 MB so a malicious client can't fill
            // the database with one row. Clients should resize before
            // sending; this is just a backstop.
            const int maxBytes = 1_500_000;
            if (image != null && image.Length > maxBytes)
                throw new FaultException("Image too large (limit ~1.5 MB).");

            await _users.UpdateProfilePictureAsync(u.Id, image);
        }

        public void UpdateMyEmail(Guid token, string email)
        {
            UpdateMyEmailAsync(token, email).GetAwaiter().GetResult();
        }

        public async Task UpdateMyEmailAsync(Guid token, string email)
        {
            User u = await RequireAuthAsync(token);
            await _users.UpdateEmailAsync(u.Id, email ?? string.Empty);
        }

        public void ChangeMyPassword(Guid token, string currentPassword, string newPassword)
        {
            ChangeMyPasswordAsync(token, currentPassword, newPassword).GetAwaiter().GetResult();
        }

        public async Task ChangeMyPasswordAsync(Guid token, string currentPassword, string newPassword)
        {
            User u = await RequireAuthAsync(token);
            string err = await _auth.ChangePasswordAsync(u.Id, currentPassword, newPassword);
            if (err != null) throw new FaultException(err);
        }

        // ===== Read-only stats =====

        public List<User> GetTopPlayers(int count)
        {
            return GetTopPlayersAsync(count).GetAwaiter().GetResult();
        }

        public async Task<List<User>> GetTopPlayersAsync(int count)
        {
            return SafeUsers(await _users.GetTopByRatingAsync(count));
        }

        public List<GameRecord> GetMyGameHistory(Guid token)
        {
            return GetMyGameHistoryAsync(token).GetAwaiter().GetResult();
        }

        public async Task<List<GameRecord>> GetMyGameHistoryAsync(Guid token)
        {
            User u = await RequireAuthAsync(token);
            return await _games.GetByPlayerAsync(u.Id);
        }

        public List<User> GetOnlinePlayers()
        {
            return GetOnlinePlayersAsync().GetAwaiter().GetResult();
        }

        public Task<List<User>> GetOnlinePlayersAsync()
        {
            return Task.FromResult(SafeUsers(Manager.GetOnlineUsers()));
        }

        // ===== Admin =====

        public List<User> AdminGetAllUsers(Guid token)
        {
            return AdminGetAllUsersAsync(token).GetAwaiter().GetResult();
        }

        public async Task<List<User>> AdminGetAllUsersAsync(Guid token)
        {
            await RequireAdminAsync(token);
            return await _users.GetAllAsync();
        }

        public bool AdminBanUser(Guid token, int userId, bool banned)
        {
            return AdminBanUserAsync(token, userId, banned).GetAwaiter().GetResult();
        }

        public async Task<bool> AdminBanUserAsync(Guid token, int userId, bool banned)
        {
            await RequireAdminAsync(token);
            int affected = await _users.SetBannedAsync(userId, banned);
            return affected > 0;
        }

        public List<GameRecord> AdminGetRecentGames(Guid token, int count)
        {
            return AdminGetRecentGamesAsync(token, count).GetAwaiter().GetResult();
        }

        public async Task<List<GameRecord>> AdminGetRecentGamesAsync(Guid token, int count)
        {
            await RequireAdminAsync(token);
            return await _games.GetRecentAsync(count);
        }

        public ServerStatsDto AdminGetServerStats(Guid token)
        {
            return AdminGetServerStatsAsync(token).GetAwaiter().GetResult();
        }

        // Aggregate counters for the admin dashboard. All numbers come
        // straight from a small set of COUNT queries, so refreshing the
        // dashboard is cheap.
        public async Task<ServerStatsDto> AdminGetServerStatsAsync(Guid token)
        {
            await RequireAdminAsync(token);
            DateTime startOfDay = DateTime.Today;
            int totalUsers      = await _users.CountTotalAsync();
            int bannedUsers     = await _users.CountBannedAsync();
            int adminUsers      = await _users.CountAdminsAsync();
            int registeredToday = await _users.CountRegisteredSinceAsync(startOfDay);
            int totalGames      = await _games.CountTotalAsync();
            int gamesToday      = await _games.CountSinceAsync(startOfDay);

            return new ServerStatsDto
            {
                TotalUsers      = totalUsers,
                BannedUsers     = bannedUsers,
                AdminUsers      = adminUsers,
                RegisteredToday = registeredToday,
                TotalGames      = totalGames,
                GamesToday      = gamesToday,
                OnlineUsers     = Manager.GetOnlineUsers().Count,
                ActiveGames     = Manager.GetActiveGameCount()
            };
        }

        // ===== Helpers =====

        private async Task<User> RequireAuthAsync(Guid token)
        {
            if (token == Guid.Empty || !_tokens.TryGetValue(token, out int userId))
                throw new FaultException("Not authenticated. Call Login first.");

            User u = await _users.GetByIdAsync(userId);
            if (u == null)
                throw new FaultException("User no longer exists.");
            if (u.IsBanned)
                throw new FaultException("Account is banned.");

            // Refresh the activity timestamp so the stale-player
            // sweep doesn't kill an active session. Cheap — single
            // dictionary write under a short lock.
            Manager.TouchLastSeen(u.Id);
            return u;
        }

        private async Task RequireAdminAsync(Guid token)
        {
            User u = await RequireAuthAsync(token);
            if (u.Role != UserRole.Admin)
                throw new FaultException("Admin role required.");
        }

        // Strip the password hash before any User leaves the server.
        // ProfilePicture is left in — it's how avatars get to the
        // client side without an extra round-trip per card.
        private static User SafeUser(User u)
        {
            if (u == null) return null;
            return new User
            {
                Id             = u.Id,
                Username       = u.Username,
                Email          = u.Email,
                Role           = u.Role,
                Wins           = u.Wins,
                Losses         = u.Losses,
                Draws          = u.Draws,
                Rating         = u.Rating,
                CreatedAt      = u.CreatedAt,
                IsBanned       = u.IsBanned,
                ProfilePicture = u.ProfilePicture,
                BirthDate      = u.BirthDate,
                Country        = u.Country
            };
        }

        private static List<User> SafeUsers(List<User> src)
        {
            List<User> dst = new List<User>(src.Count);
            foreach (User u in src) dst.Add(SafeUser(u));
            return dst;
        }
    }
}
