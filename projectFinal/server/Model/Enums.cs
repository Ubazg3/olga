using System.Runtime.Serialization;

namespace Model
{
    [DataContract]
    public enum PieceColor
    {
        [EnumMember] White = 0,
        [EnumMember] Black = 1
    }

    // Two piece kinds in checkers: Man (the starting piece) and King
    // (a Man that has reached the opponent's back rank). Kings move and
    // capture in all four diagonal directions; men only forward.
    [DataContract]
    public enum PieceType
    {
        [EnumMember] None = 0,
        [EnumMember] Man  = 1,
        [EnumMember] King = 2
    }

    [DataContract]
    public enum UserRole
    {
        [EnumMember] Player = 0,
        [EnumMember] Admin  = 1
    }

    [DataContract]
    public enum GameStatus
    {
        [EnumMember] Waiting    = 0,
        [EnumMember] InProgress = 1,
        [EnumMember] WhiteWins  = 2,
        [EnumMember] BlackWins  = 3,
        [EnumMember] Draw       = 4,
        [EnumMember] Aborted    = 5
    }

    [DataContract]
    public enum GameEndReason
    {
        [EnumMember] None           = 0,
        [EnumMember] NoPieces       = 1,
        [EnumMember] NoLegalMoves   = 2,
        [EnumMember] Resignation    = 3,
        [EnumMember] DrawAgreement  = 4,
        [EnumMember] Disconnection  = 5
    }
}
