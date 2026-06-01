using System;
using System.Collections.Generic;
using Model;

namespace BusinessLogic
{
    // One running checkers game in memory. Holds the live Board, both
    // players, the move list, and a lock so concurrent WCF callers
    // can't race each other on the same game.
    public class GameSession
    {
        public int  GameId { get; set; }
        public User WhitePlayer { get; private set; }
        public User BlackPlayer { get; private set; }
        public Board Board { get; private set; }

        public List<Move> MoveHistory { get; private set; }

        public GameStatus    Status    { get; private set; }
        public GameEndReason EndReason { get; private set; }
        public int?          WinnerId  { get; private set; }

        public DateTime StartedAt { get; private set; }
        public DateTime? EndedAt  { get; private set; }

        // True for games where one participant is the in-process Bot
        // user. After each human move the GameManager schedules a bot
        // reply on these.
        public bool IsBotGame { get; set; }
        public int  BotUserId { get; set; }   // userId of the bot side

        // Player-id of whoever currently has a draw offer on the table.
        // null when no draw is pending. Cleared either when the offer
        // is accepted (game ends as Draw) or declined.
        public int? PendingDrawFromUserId { get; private set; }

        // Per-game lock — move execution is atomic with respect to it.
        private readonly object _lock = new object();
        public object Lock { get { return _lock; } }

        public GameSession(User white, User black)
        {
            WhitePlayer = white;
            BlackPlayer = black;
            Board       = Board.StartingPosition();
            MoveHistory = new List<Move>();
            Status      = GameStatus.InProgress;
            EndReason   = GameEndReason.None;
            StartedAt   = DateTime.Now;
        }

        public PieceColor ColorOf(int userId)
        {
            if (userId == WhitePlayer.Id) return PieceColor.White;
            if (userId == BlackPlayer.Id) return PieceColor.Black;
            throw new InvalidOperationException("User " + userId + " is not in this game.");
        }

        public bool IsPlayer(int userId)
        {
            return userId == WhitePlayer.Id || userId == BlackPlayer.Id;
        }

        public int OpponentOf(int userId)
        {
            return userId == WhitePlayer.Id ? BlackPlayer.Id : WhitePlayer.Id;
        }

        public void RecordMove(Move m)
        {
            m.GameId     = GameId;
            m.MoveNumber = (MoveHistory.Count / 2) + 1;
            MoveHistory.Add(m);
        }

        public void SetEnded(GameStatus status, GameEndReason reason, int? winnerId)
        {
            Status    = status;
            EndReason = reason;
            WinnerId  = winnerId;
            EndedAt   = DateTime.Now;
            PendingDrawFromUserId = null;
        }

        // ----- Draw-offer state machine -----

        public void OfferDraw(int userId)
        {
            if (Status != GameStatus.InProgress) return;
            if (!IsPlayer(userId))               return;
            // Re-offering does nothing; offering against an existing
            // offer from the opponent is treated as accept by the VM.
            PendingDrawFromUserId = userId;
        }

        // Returns true if the offer was accepted (game now Draw).
        public bool TryAcceptDraw(int byUserId)
        {
            if (Status != GameStatus.InProgress) return false;
            if (!PendingDrawFromUserId.HasValue) return false;
            if (PendingDrawFromUserId.Value == byUserId) return false; // can't accept own offer
            if (!IsPlayer(byUserId))             return false;
            SetEnded(GameStatus.Draw, GameEndReason.DrawAgreement, null);
            return true;
        }

        public void DeclineDraw()
        {
            PendingDrawFromUserId = null;
        }

        public GameStateDto ToDto()
        {
            string lastNot = MoveHistory.Count == 0
                ? string.Empty
                : MoveHistory[MoveHistory.Count - 1].Notation;

            return new GameStateDto
            {
                GameId           = GameId,
                WhitePlayerId    = WhitePlayer.Id,
                BlackPlayerId    = BlackPlayer.Id,
                WhiteUsername    = WhitePlayer.Username,
                BlackUsername    = BlackPlayer.Username,
                Board            = Board.Serialize(),
                Status           = Status,
                EndReason        = EndReason,
                WinnerId         = WinnerId,
                MoveCount        = MoveHistory.Count,
                LastMoveNotation = lastNot,
                WhitePiecesLeft  = Board.CountPieces(PieceColor.White),
                BlackPiecesLeft  = Board.CountPieces(PieceColor.Black),
                DrawOfferFromUserId = PendingDrawFromUserId
            };
        }
    }
}
