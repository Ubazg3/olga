// Data contracts mirroring the server-side Model.dll types. Each one
// pins its DataContract Namespace to "http://schemas.datacontract.org/2004/07/Model"
// so the wire shape matches exactly — the server has them under
// namespace `Model`, and that's the default namespace WCF derives from
// the .NET namespace.
//
// Web app cannot reference the server's Model.dll directly because it
// targets net472 while we're on net8. Same fields, same serialised
// XML element names, separate CLR types.

using System.Runtime.Serialization;

namespace WebApplication1.CheckersServer;

[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Model")]
public enum PieceColor
{
    [EnumMember] White = 0,
    [EnumMember] Black = 1
}

[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Model")]
public enum PieceType
{
    [EnumMember] None = 0,
    [EnumMember] Man  = 1,
    [EnumMember] King = 2
}

[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Model")]
public enum UserRole
{
    [EnumMember] Player = 0,
    [EnumMember] Admin  = 1
}

[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Model")]
public enum GameStatus
{
    [EnumMember] Waiting    = 0,
    [EnumMember] InProgress = 1,
    [EnumMember] WhiteWins  = 2,
    [EnumMember] BlackWins  = 3,
    [EnumMember] Draw       = 4,
    [EnumMember] Aborted    = 5
}

[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Model")]
public enum GameEndReason
{
    [EnumMember] None           = 0,
    [EnumMember] NoPieces       = 1,
    [EnumMember] NoLegalMoves   = 2,
    [EnumMember] Resignation    = 3,
    [EnumMember] DrawAgreement  = 4,
    [EnumMember] Disconnection  = 5
}

[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Model")]
public class Base
{
    [DataMember] public int Id { get; set; }
}

[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Model")]
public class User : Base
{
    [DataMember] public string Username       { get; set; } = "";
    [DataMember] public string PasswordHash   { get; set; } = "";
    [DataMember] public string Email          { get; set; } = "";
    [DataMember] public UserRole Role         { get; set; }
    [DataMember] public int    Wins           { get; set; }
    [DataMember] public int    Losses         { get; set; }
    [DataMember] public int    Draws          { get; set; }
    [DataMember] public int    Rating         { get; set; }
    [DataMember] public DateTime CreatedAt    { get; set; }
    [DataMember] public bool   IsBanned       { get; set; }
    [DataMember] public byte[]? ProfilePicture { get; set; }
    [DataMember] public DateTime? BirthDate     { get; set; }
    [DataMember] public string?   Country       { get; set; }
}

[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Model")]
public class GameRecord : Base
{
    [DataMember] public int    WhitePlayerId { get; set; }
    [DataMember] public int    BlackPlayerId { get; set; }
    [DataMember] public DateTime StartedAt   { get; set; }
    [DataMember] public DateTime? EndedAt    { get; set; }
    [DataMember] public GameStatus Status    { get; set; }
    [DataMember] public GameEndReason EndReason { get; set; }
    [DataMember] public int?   WinnerId      { get; set; }
    [DataMember] public string FinalBoard    { get; set; } = "";
    [DataMember] public int    MoveCount     { get; set; }
}

[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Model")]
public struct Square
{
    [DataMember] public int File { get; set; }
    [DataMember] public int Rank { get; set; }
}

[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Model")]
public class Move : Base
{
    [DataMember] public int        GameId         { get; set; }
    [DataMember] public int        MoveNumber     { get; set; }
    [DataMember] public PieceColor MoverColor     { get; set; }
    [DataMember] public int        FromFile       { get; set; }
    [DataMember] public int        FromRank       { get; set; }
    [DataMember] public int        ToFile         { get; set; }
    [DataMember] public int        ToRank         { get; set; }
    [DataMember] public string     PathSerialized { get; set; } = "";
    [DataMember] public PieceType  Piece          { get; set; }
    [DataMember] public bool       IsCapture      { get; set; }
    [DataMember] public int        CapturedCount  { get; set; }
    [DataMember] public bool       BecameKing     { get; set; }
    [DataMember] public string     Notation       { get; set; } = "";
    [DataMember] public string     BoardAfter     { get; set; } = "";
    [DataMember] public DateTime   PlayedAt       { get; set; }
}

[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Model")]
public class MoveResult
{
    [DataMember] public bool          Accepted     { get; set; }
    [DataMember] public string        ErrorMessage { get; set; } = "";
    [DataMember] public Move?         PlayedMove   { get; set; }
    [DataMember] public GameStatus    NewStatus    { get; set; }
    [DataMember] public GameEndReason EndReason    { get; set; }
    [DataMember] public string        FEN          { get; set; } = "";
}

[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Model")]
public class GameStateDto
{
    [DataMember] public int     GameId            { get; set; }
    [DataMember] public int     WhitePlayerId     { get; set; }
    [DataMember] public int     BlackPlayerId     { get; set; }
    [DataMember] public string  WhiteUsername     { get; set; } = "";
    [DataMember] public string  BlackUsername     { get; set; } = "";
    [DataMember] public string  Board             { get; set; } = "";
    [DataMember] public GameStatus    Status      { get; set; }
    [DataMember] public GameEndReason EndReason   { get; set; }
    [DataMember] public int?    WinnerId          { get; set; }
    [DataMember] public int     MoveCount         { get; set; }
    [DataMember] public string  LastMoveNotation  { get; set; } = "";
    [DataMember] public int     WhitePiecesLeft   { get; set; }
    [DataMember] public int     BlackPiecesLeft   { get; set; }
}

[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Model")]
public class LoginResult
{
    [DataMember] public bool    Success { get; set; }
    [DataMember] public string  Error   { get; set; } = "";
    [DataMember] public Guid    Token   { get; set; }
    [DataMember] public User?   User    { get; set; }
}

[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Model")]
public class UserProfileDto
{
    [DataMember] public User? User { get; set; }
    [DataMember] public List<GameRecord> RecentGames { get; set; } = new();
    [DataMember] public Dictionary<int, string> Usernames { get; set; } = new();
}

[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Model")]
public class ServerStatsDto
{
    [DataMember] public int TotalUsers      { get; set; }
    [DataMember] public int TotalGames      { get; set; }
    [DataMember] public int OnlineUsers     { get; set; }
    [DataMember] public int ActiveGames     { get; set; }
    [DataMember] public int BannedUsers     { get; set; }
    [DataMember] public int AdminUsers      { get; set; }
    [DataMember] public int GamesToday      { get; set; }
    [DataMember] public int RegisteredToday { get; set; }
}
