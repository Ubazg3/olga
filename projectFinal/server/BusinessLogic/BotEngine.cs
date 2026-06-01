using System;
using System.Collections.Generic;
using Model;

namespace BusinessLogic
{
    // Tiny "not very smart" checkers bot. Uses the same legal-move
    // generator the human players go through, then picks one with a
    // small heuristic:
    //
    //   1. Forced-capture rules already constrain `legal` — when
    //      captures exist they're the only options, so we don't need
    //      to prefer them explicitly.
    //   2. Among ties, prefer moves that crown a man (BecameKing).
    //   3. Otherwise, pick at random so games don't replay identically.
    //
    // Plenty of room to add minimax later; this engine is the floor,
    // intentionally easy enough that a human can win.
    public static class BotEngine
    {
        private static readonly Random _rng = new Random();

        // Returns the path the bot wants to take, or null if there is
        // no legal move (in which case the human just won — game-end
        // detection will fire on the next move attempt).
        public static List<Square> PickMove(Board board)
        {
            List<Move> legal = CheckersEngine.GenerateLegalMoves(board);
            if (legal.Count == 0) return null;

            // Tier 1: prefer moves that promote a man this turn.
            List<Move> promotions = legal.FindAll(m => m.BecameKing);
            List<Move> pool = promotions.Count > 0 ? promotions : legal;

            Move pick = pool[_rng.Next(pool.Count)];
            return pick.Path;
        }
    }
}
