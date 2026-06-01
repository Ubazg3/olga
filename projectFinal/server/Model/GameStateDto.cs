using System.Runtime.Serialization;

namespace Model
{
    // Snapshot of a live checkers game pushed to clients on join, on
    // every move, and on game end.
    [DataContract]
    public class GameStateDto
    {
        [DataMember] public int     GameId            { get; set; }
        [DataMember] public int     WhitePlayerId     { get; set; }
        [DataMember] public int     BlackPlayerId     { get; set; }
        [DataMember] public string  WhiteUsername     { get; set; }
        [DataMember] public string  BlackUsername     { get; set; }
        [DataMember] public string  Board             { get; set; }   // Board.Serialize()
        [DataMember] public GameStatus    Status     { get; set; }
        [DataMember] public GameEndReason EndReason  { get; set; }
        [DataMember] public int?    WinnerId          { get; set; }
        [DataMember] public int     MoveCount         { get; set; }
        [DataMember] public string  LastMoveNotation  { get; set; }
        [DataMember] public int     WhitePiecesLeft   { get; set; }
        [DataMember] public int     BlackPiecesLeft   { get; set; }
        // user id of whoever currently has a draw offer pending in
        // this game; null when no draw has been offered.
        [DataMember] public int?    DrawOfferFromUserId { get; set; }
    }
}
