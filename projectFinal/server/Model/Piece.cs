using System.Runtime.Serialization;

namespace Model
{
    // A checkers piece: either a Man or a King, in one of two colours.
    // Promotion happens in the engine — a Man on the opponent's back
    // rank is replaced by a King with the same colour.
    [DataContract]
    public class Piece
    {
        [DataMember] public PieceType  Type  { get; set; }
        [DataMember] public PieceColor Color { get; set; }

        public Piece() { }
        public Piece(PieceType type, PieceColor color) { Type = type; Color = color; }

        // Compact one-character code used in board serialisation:
        //   '.' empty, 'w' white man, 'W' white king,
        //              'b' black man, 'B' black king.
        public char Code
        {
            get
            {
                if (Type == PieceType.None) return '.';
                bool king  = Type == PieceType.King;
                bool white = Color == PieceColor.White;
                if (white) return king ? 'W' : 'w';
                else       return king ? 'B' : 'b';
            }
        }
    }
}
