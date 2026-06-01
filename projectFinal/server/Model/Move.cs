using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Model
{
    // A single turn in checkers. For a simple slide the move has a
    // start square and one destination. For a jump the move stores the
    // entire sequence of landing squares in PathSerialized so that
    // multi-jumps stay one row in the Moves table (still the link
    // table — every move is tied to a Game).
    [DataContract]
    public class Move : Base
    {
        [DataMember] public int        GameId        { get; set; }
        [DataMember] public int        MoveNumber    { get; set; }
        [DataMember] public PieceColor MoverColor    { get; set; }

        // Start of the move.
        [DataMember] public int FromFile { get; set; }
        [DataMember] public int FromRank { get; set; }

        // Final landing square (after all jumps in this turn).
        [DataMember] public int ToFile   { get; set; }
        [DataMember] public int ToRank   { get; set; }

        // For jumps with two or more captures, the intermediate
        // landing squares serialised as "f,r;f,r;..." (excluding the
        // starting square; the last entry equals (ToFile,ToRank)).
        // Empty for simple slides and single jumps.
        [DataMember] public string PathSerialized { get; set; }

        [DataMember] public PieceType Piece         { get; set; }   // Man or King at move start
        [DataMember] public bool      IsCapture     { get; set; }   // any jumps in this move?
        [DataMember] public int       CapturedCount { get; set; }   // number of pieces taken
        [DataMember] public bool      BecameKing    { get; set; }   // promoted on this move?
        [DataMember] public string    Notation      { get; set; }   // e.g. "a3-b4" or "a3xc5xe7"
        [DataMember] public string    BoardAfter    { get; set; }   // Board.Serialize() snapshot
        [DataMember] public DateTime  PlayedAt      { get; set; }

        public Move()
        {
            Piece          = PieceType.None;
            PathSerialized = string.Empty;
            Notation       = string.Empty;
            BoardAfter     = string.Empty;
            PlayedAt       = DateTime.Now;
        }

        public Square From { get { return new Square(FromFile, FromRank); } }
        public Square To   { get { return new Square(ToFile,   ToRank);   } }

        // Reconstructs the full landing path: [from, hop1, hop2, ..., to].
        // For a simple slide returns [from, to].
        public List<Square> Path
        {
            get
            {
                List<Square> path = new List<Square> { From };
                if (!string.IsNullOrEmpty(PathSerialized))
                {
                    string[] parts = PathSerialized.Split(';');
                    foreach (string p in parts)
                    {
                        if (string.IsNullOrEmpty(p)) continue;
                        string[] xy = p.Split(',');
                        path.Add(new Square(int.Parse(xy[0]), int.Parse(xy[1])));
                    }
                }
                else
                {
                    path.Add(To);
                }
                return path;
            }
        }

        // Helper used by the engine when building a move record.
        public static string SerializePath(List<Square> hops)
        {
            if (hops == null || hops.Count == 0) return string.Empty;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hops.Count; i++)
            {
                if (i > 0) sb.Append(';');
                sb.Append(hops[i].File);
                sb.Append(',');
                sb.Append(hops[i].Rank);
            }
            return sb.ToString();
        }
    }
}
