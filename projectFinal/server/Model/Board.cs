using System;
using System.Runtime.Serialization;
using System.Text;

namespace Model
{
    // 8x8 checkers board. Pieces only ever occupy "dark" squares —
    // squares where (file + rank) is even. The other 32 squares are
    // unused. Conventions:
    //   * White pieces start on ranks 0-2 and move toward higher ranks.
    //   * Black pieces start on ranks 5-7 and move toward lower ranks.
    //   * White makes the first move.
    //
    // The board has no chess-specific state (castling, en passant);
    // only the side to move and a "moves since capture/promotion"
    // counter for tracking quiet phases.
    [DataContract]
    public class Board
    {
        [DataMember] private Piece[][] _grid;            // jagged for DataContract
        [DataMember] public PieceColor SideToMove { get; set; }
        [DataMember] public int MovesSinceProgress { get; set; }
        [DataMember] public int FullmoveNumber { get; set; }

        public Board()
        {
            _grid = new Piece[8][];
            for (int f = 0; f < 8; f++) _grid[f] = new Piece[8];
            FullmoveNumber = 1;
        }

        public Piece this[int file, int rank]
        {
            get { return _grid[file][rank]; }
            set { _grid[file][rank] = value; }
        }

        public Piece this[Square s]
        {
            get { return _grid[s.File][s.Rank]; }
            set { _grid[s.File][s.Rank] = value; }
        }

        // Dark squares are where the parity of file+rank is even (with
        // a1 = (0,0) treated as a dark square — the standard checkers
        // setup). Pieces may only stand on these.
        public static bool IsPlayable(int file, int rank)
        {
            return ((file + rank) & 1) == 0;
        }

        public static bool IsPlayable(Square s) { return IsPlayable(s.File, s.Rank); }

        public static Board StartingPosition()
        {
            Board b = new Board();
            for (int r = 0; r < 8; r++)
                for (int f = 0; f < 8; f++)
                {
                    if (!IsPlayable(f, r)) continue;
                    if (r <= 2) b[f, r] = new Piece(PieceType.Man, PieceColor.White);
                    else if (r >= 5) b[f, r] = new Piece(PieceType.Man, PieceColor.Black);
                }
            b.SideToMove = PieceColor.White;
            return b;
        }

        public Board Clone()
        {
            Board b = new Board();
            for (int f = 0; f < 8; f++)
                for (int r = 0; r < 8; r++)
                {
                    Piece p = _grid[f][r];
                    if (p != null) b[f, r] = new Piece(p.Type, p.Color);
                }
            b.SideToMove         = SideToMove;
            b.MovesSinceProgress = MovesSinceProgress;
            b.FullmoveNumber     = FullmoveNumber;
            return b;
        }

        // Compact textual snapshot — stored in the DB and used by
        // clients to render a board without re-walking all 64 squares.
        // Format: 32 characters (one per dark square, ordered by rank
        // then file), then a space, then 'w' or 'b' for side-to-move,
        // then a space and MovesSinceProgress.
        public string Serialize()
        {
            StringBuilder sb = new StringBuilder(40);
            for (int r = 0; r < 8; r++)
                for (int f = 0; f < 8; f++)
                {
                    if (!IsPlayable(f, r)) continue;
                    Piece p = _grid[f][r];
                    sb.Append(p == null ? '.' : p.Code);
                }
            sb.Append(' ');
            sb.Append(SideToMove == PieceColor.White ? 'w' : 'b');
            sb.Append(' ');
            sb.Append(MovesSinceProgress);
            return sb.ToString();
        }

        public static Board Deserialize(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                throw new ArgumentException("Empty board state.");
            string[] parts = s.Split(' ');
            if (parts.Length < 2 || parts[0].Length != 32)
                throw new ArgumentException("Bad checkers board encoding.");

            Board b = new Board();
            int idx = 0;
            for (int r = 0; r < 8; r++)
                for (int f = 0; f < 8; f++)
                {
                    if (!IsPlayable(f, r)) continue;
                    char c = parts[0][idx++];
                    if (c == '.') continue;
                    PieceType t = (c == 'w' || c == 'b') ? PieceType.Man : PieceType.King;
                    PieceColor col = (c == 'w' || c == 'W') ? PieceColor.White : PieceColor.Black;
                    b[f, r] = new Piece(t, col);
                }
            b.SideToMove = parts[1] == "w" ? PieceColor.White : PieceColor.Black;
            if (parts.Length > 2 && int.TryParse(parts[2], out int v)) b.MovesSinceProgress = v;
            return b;
        }

        public int CountPieces(PieceColor color)
        {
            int n = 0;
            for (int f = 0; f < 8; f++)
                for (int r = 0; r < 8; r++)
                {
                    Piece p = _grid[f][r];
                    if (p != null && p.Color == color) n++;
                }
            return n;
        }
    }
}
