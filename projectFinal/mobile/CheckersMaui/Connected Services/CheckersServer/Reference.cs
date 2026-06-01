//------------------------------------------------------------------------------
// Hand-written WCF proxy for the MAUI client.
//
// This file is the moral equivalent of `Reference.cs` produced by
// dotnet-svcutil — it pairs the [DataContract] DTOs the server exposes
// (matching their type names AND DataContract namespace exactly) with
// a `ClientBase`-derived class that calls the operations.
//
// Why hand-rolled instead of svcutil-generated?
//   * The whole proxy fits on one screen; svcutil bloats it 10x.
//   * Keeping it in source means the build doesn't depend on the
//     server being live during a clean build.
//   * Every type has the exact DataContract namespace the server uses
//     (`http://schemas.datacontract.org/2004/07/Model`), so the same
//     wire format the WPF client speaks works here too.
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Threading.Tasks;

namespace CheckersMaui.Services.CheckersServer
{
    // ---- Enums ----

    [DataContract(Name = "PieceColor", Namespace = "http://schemas.datacontract.org/2004/07/Model")]
    public enum PieceColor
    {
        [EnumMember] White = 0,
        [EnumMember] Black = 1,
    }

    [DataContract(Name = "PieceType", Namespace = "http://schemas.datacontract.org/2004/07/Model")]
    public enum PieceType
    {
        [EnumMember] None = 0,
        [EnumMember] Man  = 1,
        [EnumMember] King = 2,
    }

    [DataContract(Name = "UserRole", Namespace = "http://schemas.datacontract.org/2004/07/Model")]
    public enum UserRole
    {
        [EnumMember] Player = 0,
        [EnumMember] Admin  = 1,
    }

    [DataContract(Name = "GameStatus", Namespace = "http://schemas.datacontract.org/2004/07/Model")]
    public enum GameStatus
    {
        [EnumMember] Waiting    = 0,
        [EnumMember] InProgress = 1,
        [EnumMember] WhiteWins  = 2,
        [EnumMember] BlackWins  = 3,
        [EnumMember] Draw       = 4,
        [EnumMember] Aborted    = 5,
    }

    [DataContract(Name = "GameEndReason", Namespace = "http://schemas.datacontract.org/2004/07/Model")]
    public enum GameEndReason
    {
        [EnumMember] None           = 0,
        [EnumMember] NoPieces       = 1,
        [EnumMember] NoLegalMoves   = 2,
        [EnumMember] Resignation    = 3,
        [EnumMember] DrawAgreement  = 4,
        [EnumMember] Disconnection  = 5,
    }

    // ---- Base + entities ----

    [DataContract(Name = "Base", Namespace = "http://schemas.datacontract.org/2004/07/Model")]
    [KnownType(typeof(User))]
    [KnownType(typeof(GameRecord))]
    [KnownType(typeof(Move))]
    public class Base
    {
        [DataMember] public int Id { get; set; }
        public Base() { Id = -1; }
    }

    [DataContract(Name = "User", Namespace = "http://schemas.datacontract.org/2004/07/Model")]
    public class User : Base
    {
        [DataMember] public string?    Username       { get; set; }
        [DataMember] public string?    PasswordHash   { get; set; }
        [DataMember] public string?    Email          { get; set; }
        [DataMember] public UserRole   Role           { get; set; }
        [DataMember] public int        Wins           { get; set; }
        [DataMember] public int        Losses         { get; set; }
        [DataMember] public int        Draws          { get; set; }
        [DataMember] public int        Rating         { get; set; }
        [DataMember] public DateTime   CreatedAt      { get; set; }
        [DataMember] public bool       IsBanned       { get; set; }
        [DataMember] public byte[]?    ProfilePicture { get; set; }
        [DataMember] public DateTime?  BirthDate      { get; set; }
        [DataMember] public string?    Country        { get; set; }

        public int GamesPlayed => Wins + Losses + Draws;
    }

    [DataContract(Name = "GameRecord", Namespace = "http://schemas.datacontract.org/2004/07/Model")]
    public class GameRecord : Base
    {
        [DataMember] public int           WhitePlayerId { get; set; }
        [DataMember] public int           BlackPlayerId { get; set; }
        [DataMember] public DateTime      StartedAt     { get; set; }
        [DataMember] public DateTime?     EndedAt       { get; set; }
        [DataMember] public GameStatus    Status        { get; set; }
        [DataMember] public GameEndReason EndReason     { get; set; }
        [DataMember] public int?          WinnerId      { get; set; }
        [DataMember] public string?       FinalBoard    { get; set; }
        [DataMember] public int           MoveCount     { get; set; }
    }

    [DataContract(Name = "Move", Namespace = "http://schemas.datacontract.org/2004/07/Model")]
    public class Move : Base
    {
        [DataMember] public int        GameId         { get; set; }
        [DataMember] public int        MoveNumber     { get; set; }
        [DataMember] public PieceColor MoverColor     { get; set; }
        [DataMember] public int        FromFile       { get; set; }
        [DataMember] public int        FromRank       { get; set; }
        [DataMember] public int        ToFile         { get; set; }
        [DataMember] public int        ToRank         { get; set; }
        [DataMember] public string?    PathSerialized { get; set; }
        [DataMember] public PieceType  Piece          { get; set; }
        [DataMember] public bool       IsCapture      { get; set; }
        [DataMember] public int        CapturedCount  { get; set; }
        [DataMember] public bool       BecameKing     { get; set; }
        [DataMember] public string?    Notation       { get; set; }
        [DataMember] public string?    BoardAfter     { get; set; }
        [DataMember] public DateTime   PlayedAt       { get; set; }
    }

    [DataContract(Name = "Square", Namespace = "http://schemas.datacontract.org/2004/07/Model")]
    public struct Square
    {
        [DataMember] public int File { get; set; }
        [DataMember] public int Rank { get; set; }
        public Square(int f, int r) { File = f; Rank = r; }
    }

    [DataContract(Name = "MoveResult", Namespace = "http://schemas.datacontract.org/2004/07/Model")]
    public class MoveResult
    {
        [DataMember] public bool          Accepted     { get; set; }
        [DataMember] public string?       ErrorMessage { get; set; }
        [DataMember] public Move?         PlayedMove   { get; set; }
        [DataMember] public GameStatus    NewStatus    { get; set; }
        [DataMember] public GameEndReason EndReason    { get; set; }
        [DataMember] public string?       FEN          { get; set; }
    }

    [DataContract(Name = "GameStateDto", Namespace = "http://schemas.datacontract.org/2004/07/Model")]
    public class GameStateDto
    {
        [DataMember] public int           GameId               { get; set; }
        [DataMember] public int           WhitePlayerId        { get; set; }
        [DataMember] public int           BlackPlayerId        { get; set; }
        [DataMember] public string?       WhiteUsername        { get; set; }
        [DataMember] public string?       BlackUsername        { get; set; }
        [DataMember] public string?       Board                { get; set; }
        [DataMember] public GameStatus    Status               { get; set; }
        [DataMember] public GameEndReason EndReason            { get; set; }
        [DataMember] public int?          WinnerId             { get; set; }
        [DataMember] public int           MoveCount            { get; set; }
        [DataMember] public string?       LastMoveNotation     { get; set; }
        [DataMember] public int           WhitePiecesLeft      { get; set; }
        [DataMember] public int           BlackPiecesLeft      { get; set; }
        [DataMember] public int?          DrawOfferFromUserId  { get; set; }
    }

    [DataContract(Name = "LoginResult", Namespace = "http://schemas.datacontract.org/2004/07/Model")]
    public class LoginResult
    {
        [DataMember] public bool    Success { get; set; }
        [DataMember] public string? Error   { get; set; }
        [DataMember] public Guid    Token   { get; set; }
        [DataMember] public User?   User    { get; set; }
    }

    [DataContract(Name = "UserProfileDto", Namespace = "http://schemas.datacontract.org/2004/07/Model")]
    public class UserProfileDto
    {
        [DataMember] public User?                  User        { get; set; }
        [DataMember] public List<GameRecord>?      RecentGames { get; set; }
        [DataMember] public Dictionary<int, string>? Usernames { get; set; }
    }

    // ---- Service contract ----
    //
    // This must list every operation we plan to call on the WCF
    // server, and the names + parameter types must match the server's
    // [OperationContract]s exactly. The [ServiceContract] Namespace is
    // left at the .NET default ("http://tempuri.org/"), which is what
    // the server uses too — go check ICheckersService on the server
    // side if a contract mismatch turns up.
    [ServiceContract(ConfigurationName = "CheckersMaui.Services.CheckersServer.ICheckersService")]
    public interface ICheckersService
    {
        // Auth
        [OperationContract] LoginResult Login   (string username, string password);
        [OperationContract] LoginResult Register(string username, string password, string email,
                                                  DateTime? birthDate, string country);
        [OperationContract] void        Logout  (Guid token);

        // Profile
        [OperationContract] UserProfileDto GetUserProfile  (int userId);
        [OperationContract] void           UpdateMyEmail   (Guid token, string email);
        [OperationContract] void           ChangeMyPassword(Guid token, string currentPassword, string newPassword);

        // Read-only feeds
        [OperationContract] List<User>       GetTopPlayers   (int count);
        [OperationContract] List<GameRecord> GetMyGameHistory(Guid token);
        [OperationContract] List<User>       GetOnlinePlayers();

        // Game state
        [OperationContract] GameStateDto GetCurrentGame(Guid token);
        [OperationContract] List<Move>   GetMoveHistory(int gameId);
    }

    public interface ICheckersServiceChannel : ICheckersService, IClientChannel { }

    // Client. Sync methods delegate straight to the channel; async
    // wrappers spin them onto a thread-pool thread so the UI stays
    // responsive — same pattern as the WPF client.
    public class CheckersServiceClient : ClientBase<ICheckersService>, ICheckersService
    {
        public CheckersServiceClient(System.ServiceModel.Channels.Binding binding,
                                     EndpointAddress address)
            : base(binding, address) { }

        // Sync (used internally by the async wrappers).
        public LoginResult Login(string u, string p) => Channel.Login(u, p);
        public LoginResult Register(string u, string p, string e, DateTime? b, string c)
            => Channel.Register(u, p, e, b, c);
        public void Logout(Guid token) => Channel.Logout(token);

        public UserProfileDto GetUserProfile(int id) => Channel.GetUserProfile(id);
        public void UpdateMyEmail(Guid t, string e) => Channel.UpdateMyEmail(t, e);
        public void ChangeMyPassword(Guid t, string cur, string nw) => Channel.ChangeMyPassword(t, cur, nw);

        public List<User>       GetTopPlayers(int n)        => Channel.GetTopPlayers(n);
        public List<GameRecord> GetMyGameHistory(Guid t)    => Channel.GetMyGameHistory(t);
        public List<User>       GetOnlinePlayers()          => Channel.GetOnlinePlayers();

        public GameStateDto GetCurrentGame(Guid t) => Channel.GetCurrentGame(t);
        public List<Move>   GetMoveHistory(int gameId) => Channel.GetMoveHistory(gameId);

        // Async wrappers.
        public Task<LoginResult> LoginAsync(string u, string p)
            => Task.Run(() => Channel.Login(u, p));
        public Task<LoginResult> RegisterAsync(string u, string p, string e, DateTime? b, string c)
            => Task.Run(() => Channel.Register(u, p, e, b, c));
        public Task LogoutAsync(Guid t)
            => Task.Run(() => Channel.Logout(t));

        public Task<UserProfileDto> GetUserProfileAsync(int id)
            => Task.Run(() => Channel.GetUserProfile(id));
        public Task UpdateMyEmailAsync(Guid t, string e)
            => Task.Run(() => Channel.UpdateMyEmail(t, e));
        public Task ChangeMyPasswordAsync(Guid t, string cur, string nw)
            => Task.Run(() => Channel.ChangeMyPassword(t, cur, nw));

        public Task<List<User>>       GetTopPlayersAsync(int n)     => Task.Run(() => Channel.GetTopPlayers(n));
        public Task<List<GameRecord>> GetMyGameHistoryAsync(Guid t) => Task.Run(() => Channel.GetMyGameHistory(t));
        public Task<List<User>>       GetOnlinePlayersAsync()       => Task.Run(() => Channel.GetOnlinePlayers());

        public Task<GameStateDto> GetCurrentGameAsync(Guid t) => Task.Run(() => Channel.GetCurrentGame(t));
        public Task<List<Move>>   GetMoveHistoryAsync(int gameId) => Task.Run(() => Channel.GetMoveHistory(gameId));

        // ClientBase already exposes a CloseAsync via ICommunicationObject,
        // so no override is needed — ServiceHelper just calls .CloseAsync().
    }
}
