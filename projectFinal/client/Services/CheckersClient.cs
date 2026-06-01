using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;
using CheckersClient.CheckersServer;     // <- generated Service Reference
using Model;

namespace CheckersClient.Services
{
    // Thin façade over the generated WCF proxy. Everything in the UI
    // talks to this single object instead of taking a direct dependency
    // on the CheckersServer namespace.
    //
    // Responsibilities:
    //   • Build the proxy from the App.config <client> endpoint.
    //   • Hold the auth token returned by Login / Register so call
    //     sites don't have to thread it through every method.
    //   • Recreate the proxy transparently when WCF puts it into the
    //     Faulted state (e.g. on transient network errors).
    public class CheckersServiceClient
    {
        private readonly object _lock = new object();
        // The generated proxy class — sits under
        // Connected Services / CheckersServer / Reference.cs.
        private CheckersServer.CheckersServiceClient _proxy;

        public Guid Token        { get; private set; }
        public User CurrentUser  { get; private set; }
        public bool IsLoggedIn   { get { return Token != Guid.Empty; } }

        public CheckersServiceClient()
        {
            _proxy = new CheckersServer.CheckersServiceClient("CheckersEndpoint");
        }

        // Re-creates the proxy if it has faulted. WCF channels become
        // unusable after a transport error and must be replaced.
        private CheckersServer.CheckersServiceClient Proxy
        {
            get
            {
                lock (_lock)
                {
                    var co = (ICommunicationObject)_proxy;
                    if (co.State == CommunicationState.Faulted
                        || co.State == CommunicationState.Closed)
                    {
                        try { co.Abort(); } catch { /* best-effort */ }
                        _proxy = new CheckersServer.CheckersServiceClient("CheckersEndpoint");
                    }
                    return _proxy;
                }
            }
        }

        // ===== Auth =====

        public async Task<LoginResult> LoginAsync(string username, string password)
        {
            LoginResult res = await Proxy.LoginAsync(username, password);
            if (res != null && res.Success)
            {
                Token       = res.Token;
                CurrentUser = res.User;
            }
            return res;
        }

        public async Task<LoginResult> RegisterAsync(
            string username, string password, string email,
            DateTime? birthDate, string country)
        {
            LoginResult res = await Proxy.RegisterAsync(username, password, email, birthDate, country);
            if (res != null && res.Success)
            {
                Token       = res.Token;
                CurrentUser = res.User;
            }
            return res;
        }

        public async Task LogoutAsync()
        {
            if (Token == Guid.Empty) return;
            try { await Proxy.LogoutAsync(Token); }
            catch { /* server unreachable — log out locally anyway */ }
            Token       = Guid.Empty;
            CurrentUser = null;
        }

        // ===== Matchmaking =====

        public Task<string> JoinQueueAsync()   { return Proxy.JoinQueueAsync(Token); }
        public Task         LeaveQueueAsync()  { return Proxy.LeaveQueueAsync(Token); }
        public Task<string> JoinBotGameAsync() { return Proxy.JoinBotGameAsync(Token); }

        // ===== Game state =====

        public Task<GameStateDto>       GetCurrentGameAsync() => Proxy.GetCurrentGameAsync(Token);
        public Task<List<List<Square>>> GetLegalMovesForSquareAsync(int file, int rank)
            => Proxy.GetLegalMovesForSquareAsync(Token, file, rank);
        public Task<List<Move>>         GetMoveHistoryAsync(int gameId)
            => Proxy.GetMoveHistoryAsync(gameId);

        // ===== Game actions =====

        public Task<MoveResult> MakeMoveAsync(List<Square> path) => Proxy.MakeMoveAsync(Token, path);
        public Task<MoveResult> ResignAsync()                     => Proxy.ResignAsync(Token);
        public Task             OfferDrawAsync()                  => Proxy.OfferDrawAsync(Token);
        public Task<MoveResult> RespondToDrawAsync(bool accept)   => Proxy.RespondToDrawAsync(Token, accept);

        // ===== Profiles =====

        public Task<UserProfileDto> GetUserProfileAsync(int userId)
            => Proxy.GetUserProfileAsync(userId);

        public Task UpdateMyEmailAsync(string email)
            => Proxy.UpdateMyEmailAsync(Token, email);

        public async Task ChangeMyPasswordAsync(string currentPassword, string newPassword)
        {
            await Proxy.ChangeMyPasswordAsync(Token, currentPassword, newPassword);
            // If we got here without an exception, the password change
            // succeeded — no further bookkeeping needed (the token is
            // still valid; the server doesn't invalidate it on rotation).
        }

        // Pass null/empty to clear the picture.
        public Task UpdateMyProfilePictureAsync(byte[] image)
            => Proxy.UpdateMyProfilePictureAsync(Token, image);

        // ===== Read-only stats =====

        public Task<List<User>>       GetTopPlayersAsync(int count) => Proxy.GetTopPlayersAsync(count);
        public Task<List<GameRecord>> GetMyGameHistoryAsync()       => Proxy.GetMyGameHistoryAsync(Token);
        public Task<List<User>>       GetOnlinePlayersAsync()        => Proxy.GetOnlinePlayersAsync();

        // ===== Admin =====

        public Task<List<User>>       AdminGetAllUsersAsync()                    => Proxy.AdminGetAllUsersAsync(Token);
        public Task<bool>             AdminBanUserAsync(int userId, bool banned) => Proxy.AdminBanUserAsync(Token, userId, banned);
        public Task<List<GameRecord>> AdminGetRecentGamesAsync(int count)        => Proxy.AdminGetRecentGamesAsync(Token, count);
    }
}
