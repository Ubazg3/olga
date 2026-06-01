using System;
using System.Collections.Generic;
using System.ServiceModel;
using Model;

namespace CheckersServer
{
    // Service contract used by every WPF checkers client.
    //
    // Stateless — exposed over BasicHttpBinding (or NetTcpBinding) so it
    // shows up cleanly in WCF Test Client and matches the school's
    // reference style. Auth is token-based: Login returns a Guid the
    // client sends back on every subsequent call.
    //
    // Only synchronous operations are declared here. The svcutil-based
    // proxy generator (used by WCF Test Client and by Add Service
    // Reference) auto-generates a parallel set of *Async methods on the
    // client side — that's why Test Client shows both Login and
    // LoginAsync in the operation tree even though the contract has only
    // one of each.
    [ServiceContract]
    public interface ICheckersService
    {
        // ---- Auth ----
        [OperationContract] LoginResult Login   (string username, string password);
        [OperationContract] LoginResult Register(string username, string password, string email,
                                                 DateTime? birthDate, string country);
        [OperationContract] void        Logout  (Guid token);

        // ---- Matchmaking ----
        [OperationContract] string JoinQueue   (Guid token);
        [OperationContract] void   LeaveQueue  (Guid token);
        [OperationContract] string JoinBotGame (Guid token);

        // ---- Game state (clients poll these every ~500 ms) ----
        [OperationContract] GameStateDto       GetCurrentGame        (Guid token);
        [OperationContract] List<List<Square>> GetLegalMovesForSquare(Guid token, int file, int rank);
        [OperationContract] List<Move>         GetMoveHistory        (int gameId);

        // ---- Game actions ----
        // The path is the full sequence of squares the moving piece
        // visits this turn: [from, to] for a slide; [from, hop1, hop2, …]
        // for a (multi-)jump.
        [OperationContract] MoveResult MakeMove(Guid token, List<Square> path);
        [OperationContract] MoveResult Resign  (Guid token);
        [OperationContract] void       OfferDraw     (Guid token);
        [OperationContract] MoveResult RespondToDraw(Guid token, bool accept);

        // ---- Profiles ----
        [OperationContract] UserProfileDto GetUserProfile         (int userId);
        [OperationContract] void           UpdateMyEmail          (Guid token, string email);
        [OperationContract] void           ChangeMyPassword       (Guid token, string currentPassword, string newPassword);
        [OperationContract] void           UpdateMyProfilePicture (Guid token, byte[] image);

        // ---- Read-only stats / leaderboard ----
        [OperationContract] List<User>       GetTopPlayers   (int count);
        [OperationContract] List<GameRecord> GetMyGameHistory(Guid token);
        [OperationContract] List<User>       GetOnlinePlayers();

        // ---- Admin (gated by UserRole.Admin server-side) ----
        [OperationContract] List<User>       AdminGetAllUsers   (Guid token);
        [OperationContract] bool             AdminBanUser       (Guid token, int userId, bool banned);
        [OperationContract] List<GameRecord> AdminGetRecentGames(Guid token, int count);
        [OperationContract] ServerStatsDto   AdminGetServerStats(Guid token);
    }
}
