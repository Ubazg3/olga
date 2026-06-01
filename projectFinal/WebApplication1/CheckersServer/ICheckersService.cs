// Mirrors the server-side ICheckersService contract — sync operations
// only, same Action URIs (default "http://tempuri.org/ICheckersService/...").
//
// Hand-written instead of dotnet-svcutil-generated so the build is
// self-contained: no external tool needed at restore time, and the
// file lives under /CheckersServer/ alongside the WPF client's
// Connected Services equivalent.

using System.Collections.Generic;
using System.ServiceModel;

namespace WebApplication1.CheckersServer;

[ServiceContract(ConfigurationName = "WebApplication1.CheckersServer.ICheckersService")]
public interface ICheckersService
{
    [OperationContract] LoginResult Login   (string username, string password);
    [OperationContract] LoginResult Register(string username, string password, string email,
                                              System.DateTime? birthDate, string country);
    [OperationContract] void        Logout  (System.Guid token);

    [OperationContract] string JoinQueue (System.Guid token);
    [OperationContract] void   LeaveQueue(System.Guid token);

    [OperationContract] GameStateDto       GetCurrentGame        (System.Guid token);
    [OperationContract] List<List<Square>> GetLegalMovesForSquare(System.Guid token, int file, int rank);
    [OperationContract] List<Move>         GetMoveHistory        (int gameId);

    [OperationContract] MoveResult MakeMove(System.Guid token, List<Square> path);
    [OperationContract] MoveResult Resign  (System.Guid token);

    [OperationContract] UserProfileDto GetUserProfile         (int userId);
    [OperationContract] void           UpdateMyEmail          (System.Guid token, string email);
    [OperationContract] void           ChangeMyPassword       (System.Guid token, string currentPassword, string newPassword);
    [OperationContract] void           UpdateMyProfilePicture (System.Guid token, byte[] image);

    [OperationContract] List<User>       GetTopPlayers   (int count);
    [OperationContract] List<GameRecord> GetMyGameHistory(System.Guid token);
    [OperationContract] List<User>       GetOnlinePlayers();

    [OperationContract] List<User>       AdminGetAllUsers   (System.Guid token);
    [OperationContract] bool             AdminBanUser       (System.Guid token, int userId, bool banned);
    [OperationContract] List<GameRecord> AdminGetRecentGames(System.Guid token, int count);
    [OperationContract] ServerStatsDto   AdminGetServerStats(System.Guid token);
}
