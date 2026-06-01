using System;
using System.Collections.Generic;
using System.Text;
using Model;

namespace BusinessLogic
{
    // Checkers rules engine. Implements the standard 8x8 American /
    // English Draughts ruleset:
    //
    //   * Men move and capture diagonally forward only.
    //   * Kings move and capture diagonally in any direction, one
    //     square at a time (no flying king).
    //   * A jump captures the adjacent enemy piece and lands on the
    //     empty square beyond. If the jumping piece can immediately
    //     jump again from its landing square, it MUST continue.
    //   * A jump may not pass over the same piece twice (handled
    //     automatically — captured pieces are removed before the next
    //     hop is searched).
    //   * If any jump is available for the side to move, the player
    //     MUST make a jumping move; simple slides are forbidden.
    //   * A Man landing on the opponent's back rank promotes to a King;
    //     in American rules the move ends with the promotion (no extra
    //     jumps as a king on the same turn).
    //   * Side to move with no pieces, or no legal moves, loses.
    public static class CheckersEngine
    {
        // Diagonal directions, indexed by piece type & colour.
        private static readonly int[][] WhiteManDirs = { new[] { -1, 1 }, new[] { 1, 1 } };
        private static readonly int[][] BlackManDirs = { new[] { -1, -1 }, new[] { 1, -1 } };
        private static readonly int[][] KingDirs     =
        {
            new[] { -1, 1 }, new[] { 1, 1 }, new[] { -1, -1 }, new[] { 1, -1 }
        };

        // ----- Public API -----

        // Returns every legal move for the side currently on move,
        // observing forced-capture rules: if any jumps exist they are
        // the only legal options.
        public static List<Move> GenerateLegalMoves(Board board)
        {
            List<Move> jumps  = new List<Move>();
            List<Move> slides = new List<Move>();

            for (int f = 0; f < 8; f++)
                for (int r = 0; r < 8; r++)
                {
                    Piece p = board[f, r];
                    if (p == null || p.Color != board.SideToMove) continue;
                    GenerateJumpMovesFrom(board, new Square(f, r), p, jumps);
                    if (jumps.Count == 0)
                        GenerateSlideMovesFrom(board, new Square(f, r), p, slides);
                }

            // Forced capture: if any jump exists anywhere on the board,
            // discard all simple slides — those are illegal this turn.
            if (jumps.Count > 0)
            {
                // Re-run jump scan across all pieces (the early termination
                // above could miss jumps from later pieces).
                jumps.Clear();
                for (int f = 0; f < 8; f++)
                    for (int r = 0; r < 8; r++)
                    {
                        Piece p = board[f, r];
                        if (p == null || p.Color != board.SideToMove) continue;
                        GenerateJumpMovesFrom(board, new Square(f, r), p, jumps);
                    }
                return jumps;
            }
            return slides;
        }

        // Validates the candidate path supplied by a client and, if
        // legal, applies it to the board, returning a fully-populated
        // Move record. Returns null on illegal input.
        //
        // `path` is the full move path: [start, hop1, hop2, …]. For a
        // simple slide it is [from, to]; for a jump it is the full
        // sequence of landing squares.
        public static Move TryExecuteMove(Board board, IList<Square> path)
        {
            if (path == null || path.Count < 2) return null;

            List<Move> legal = GenerateLegalMoves(board);
            Move chosen = null;
            foreach (Move m in legal)
            {
                if (PathsEqual(m.Path, path)) { chosen = m; break; }
            }
            if (chosen == null) return null;

            ApplyMoveOnBoard(board, chosen);
            chosen.BoardAfter = board.Serialize();
            chosen.Notation   = BuildNotation(chosen);
            return chosen;
        }

        // Applies an already-validated move to `board`, mutating it.
        public static void ApplyMoveOnBoard(Board board, Move m)
        {
            Piece moving = board[m.From];
            board[m.From] = null;

            // Walk the path step by step so each captured piece is
            // removed at the right moment (this matters for unusual
            // multi-jumps where the choice of order could matter for
            // future endgames).
            Square cur = m.From;
            List<Square> hops = m.Path;
            for (int i = 1; i < hops.Count; i++)
            {
                Square next = hops[i];
                int df = Math.Sign(next.File - cur.File);
                int dr = Math.Sign(next.Rank - cur.Rank);
                int dist = Math.Abs(next.File - cur.File);
                if (dist == 2)
                {
                    Square mid = new Square(cur.File + df, cur.Rank + dr);
                    board[mid] = null;
                }
                cur = next;
            }

            // Promotion check on the final landing square.
            bool promote = moving.Type == PieceType.Man
                           && IsPromotionRank(m.To.Rank, moving.Color);
            if (promote)
                board[m.To] = new Piece(PieceType.King, moving.Color);
            else
                board[m.To] = new Piece(moving.Type, moving.Color);

            // Quiet-move counter — reset on capture or promotion.
            if (m.IsCapture || promote) board.MovesSinceProgress = 0;
            else                        board.MovesSinceProgress++;

            if (board.SideToMove == PieceColor.Black) board.FullmoveNumber++;
            board.SideToMove = (board.SideToMove == PieceColor.White)
                ? PieceColor.Black : PieceColor.White;
        }

        public static bool HasAnyLegalMove(Board board)
        {
            return GenerateLegalMoves(board).Count > 0;
        }

        // ----- Move generation -----

        private static void GenerateSlideMovesFrom(Board b, Square from, Piece p, List<Move> moves)
        {
            int[][] dirs = DirsFor(p);
            foreach (int[] d in dirs)
            {
                int nf = from.File + d[0];
                int nr = from.Rank + d[1];
                if (nf < 0 || nf > 7 || nr < 0 || nr > 7) continue;
                if (!Board.IsPlayable(nf, nr)) continue;
                if (b[nf, nr] != null) continue;

                Move m = new Move
                {
                    FromFile       = from.File,
                    FromRank       = from.Rank,
                    ToFile         = nf,
                    ToRank         = nr,
                    MoverColor     = p.Color,
                    Piece          = p.Type,
                    IsCapture      = false,
                    CapturedCount  = 0,
                    PathSerialized = string.Empty,
                    BecameKing     = p.Type == PieceType.Man && IsPromotionRank(nr, p.Color)
                };
                moves.Add(m);
            }
        }

        // DFS over all maximal jump sequences starting from `from`.
        // Mutates the board on the way down and undoes on the way up,
        // so each call shares one board copy and explores the search
        // space in O(branching ^ depth) without heap allocations per
        // node.
        private static void GenerateJumpMovesFrom(Board b, Square from, Piece p, List<Move> results)
        {
            Board work = b.Clone();
            List<Square> path = new List<Square> { from };
            JumpDFS(work, from, p, path, results, p.Color);
        }

        private static void JumpDFS(Board b, Square from, Piece moverNow,
                                    List<Square> path, List<Move> results, PieceColor moverColor)
        {
            int[][] dirs = DirsFor(moverNow);
            bool extendedAny = false;

            foreach (int[] d in dirs)
            {
                int mf = from.File + d[0],     mr = from.Rank + d[1];
                int lf = from.File + 2 * d[0], lr = from.Rank + 2 * d[1];
                if (lf < 0 || lf > 7 || lr < 0 || lr > 7) continue;
                if (!Board.IsPlayable(lf, lr)) continue;

                Piece mid = b[mf, mr];
                if (mid == null || mid.Color == moverColor) continue;
                if (b[lf, lr] != null) continue;

                // Apply this jump.
                Square landing = new Square(lf, lr);
                b[from]        = null;
                b[mf, mr]      = null;
                bool promotesNow = moverNow.Type == PieceType.Man
                                   && IsPromotionRank(lr, moverColor);
                Piece nowAt = promotesNow
                    ? new Piece(PieceType.King, moverColor)
                    : new Piece(moverNow.Type, moverColor);
                b[lf, lr] = nowAt;
                path.Add(landing);
                extendedAny = true;

                if (promotesNow)
                {
                    // American rules: promotion stops the jump sequence.
                    results.Add(BuildJumpMove(path, moverColor, moverNow.Type, true));
                }
                else
                {
                    int before = results.Count;
                    JumpDFS(b, landing, nowAt, path, results, moverColor);
                    // If the recursion added nothing, this hop is a terminal —
                    // record the path here.
                    if (results.Count == before)
                        results.Add(BuildJumpMove(path, moverColor, moverNow.Type, false));
                }

                // Undo.
                path.RemoveAt(path.Count - 1);
                b[lf, lr] = null;
                b[mf, mr] = mid;
                b[from]   = moverNow;
            }
            // If no jump direction was taken we simply return; the
            // caller is responsible for recording the terminal.
            _ = extendedAny;
        }

        private static Move BuildJumpMove(List<Square> path, PieceColor color,
                                          PieceType startingType, bool becameKing)
        {
            // path[0] is the start; path[1..] are landing squares.
            Square from = path[0];
            Square to   = path[path.Count - 1];

            List<Square> hops = new List<Square>(path.Count - 1);
            for (int i = 1; i < path.Count; i++) hops.Add(path[i]);

            return new Move
            {
                FromFile       = from.File,
                FromRank       = from.Rank,
                ToFile         = to.File,
                ToRank         = to.Rank,
                MoverColor     = color,
                Piece          = startingType,
                IsCapture      = true,
                CapturedCount  = path.Count - 1,
                PathSerialized = Move.SerializePath(hops),
                BecameKing     = becameKing
            };
        }

        // ----- Helpers -----

        private static int[][] DirsFor(Piece p)
        {
            if (p.Type == PieceType.King) return KingDirs;
            return p.Color == PieceColor.White ? WhiteManDirs : BlackManDirs;
        }

        public static bool IsPromotionRank(int rank, PieceColor color)
        {
            return color == PieceColor.White ? rank == 7 : rank == 0;
        }

        private static bool PathsEqual(IList<Square> a, IList<Square> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
                if (a[i] != b[i]) return false;
            return true;
        }

        private static string BuildNotation(Move m)
        {
            // "a3-b4" for slide, "a3xc5xe7" for jumps. King-promotion is
            // signalled by a trailing "=K".
            StringBuilder sb = new StringBuilder();
            char sep = m.IsCapture ? 'x' : '-';
            sb.Append(m.From.Algebraic);
            List<Square> hops = m.Path;
            for (int i = 1; i < hops.Count; i++)
            {
                sb.Append(sep);
                sb.Append(hops[i].Algebraic);
            }
            if (m.BecameKing) sb.Append("=K");
            return sb.ToString();
        }
    }
}
