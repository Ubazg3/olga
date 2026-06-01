using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Model;
using ViewDB;

namespace BusinessLogic
{
    public class JoinResult
    {
        public bool        Started { get; set; }
        public GameSession Session { get; set; }
        public string      Message { get; set; }
    }

    // Server-side hub for the checkers application. Owns:
    //   • Queue<int>                   — matchmaking FIFO of waiting userIds
    //   • Dictionary<int,GameSession>  — active games by gameId
    //   • Dictionary<int,int>          — userId → gameId
    //   • Dictionary<int,User>         — currently online users
    public class GameManager
    {
        private readonly Queue<int>                     _matchmakingQueue = new Queue<int>();
        private readonly Dictionary<int, GameSession>   _activeGames      = new Dictionary<int, GameSession>();
        private readonly Dictionary<int, int>           _userToGame       = new Dictionary<int, int>();
        private readonly Dictionary<int, User>          _onlineUsers      = new Dictionary<int, User>();
        // userId → time of the last authenticated call from this user.
        // Drives the stale-player sweep so a client that closes hard
        // (process killed, network dropped, X button while a Logout
        // request is still in flight) doesn't leave the opponent stuck.
        private readonly Dictionary<int, DateTime>      _lastSeen         = new Dictionary<int, DateTime>();
        private readonly object _stateLock = new object();

        // Timer that periodically looks for players in active games who
        // have stopped polling for too long, and ends those games as
        // disconnections.
        private readonly System.Timers.Timer _staleSweepTimer;
        private const int StaleSeconds = 30;        // grace before we kill the game

        public GameManager()
        {
            _staleSweepTimer = new System.Timers.Timer(5_000);
            _staleSweepTimer.Elapsed += async (s, e) => await SweepStalePlayersAsync();
            _staleSweepTimer.AutoReset = true;
            _staleSweepTimer.Start();
        }

        private readonly UsersDB _usersDB = new UsersDB();
        private readonly GamesDB _gamesDB = new GamesDB();
        private readonly MovesDB _movesDB = new MovesDB();

        public GameSession FindByGameId(int gameId)
        {
            lock (_stateLock)
                return _activeGames.TryGetValue(gameId, out GameSession s) ? s : null;
        }

        public GameSession FindByUserId(int userId)
        {
            lock (_stateLock)
            {
                if (_userToGame.TryGetValue(userId, out int gameId)
                    && _activeGames.TryGetValue(gameId, out GameSession s))
                    return s;
                return null;
            }
        }

        public void RegisterOnline(User user)
        {
            lock (_stateLock) _onlineUsers[user.Id] = user;
        }

        public void UnregisterOnline(int userId)
        {
            lock (_stateLock) _onlineUsers.Remove(userId);
        }

        public List<User> GetOnlineUsers()
        {
            lock (_stateLock) return new List<User>(_onlineUsers.Values);
        }

        // Refresh the user's last-seen timestamp. Called by
        // CheckersService on every authenticated request so the sweep
        // can tell who's still actively connected.
        public void TouchLastSeen(int userId)
        {
            lock (_stateLock) _lastSeen[userId] = DateTime.UtcNow;
        }

        // Scans every active game (skipping bot games — the bot is the
        // server itself, can't disconnect). Players whose last-seen is
        // older than StaleSeconds are treated as having quit; their
        // ongoing game ends with EndReason.Disconnection, which
        // FinalizeGameAsync turns into the rating penalty.
        private async Task SweepStalePlayersAsync()
        {
            DateTime cutoff = DateTime.UtcNow.AddSeconds(-StaleSeconds);
            List<int> toDisconnect = new List<int>();

            lock (_stateLock)
            {
                foreach (KeyValuePair<int, GameSession> kv in _activeGames)
                {
                    GameSession s = kv.Value;
                    if (s.IsBotGame) continue;
                    if (s.Status != GameStatus.InProgress) continue;
                    int[] both = { s.WhitePlayer.Id, s.BlackPlayer.Id };
                    foreach (int uid in both)
                    {
                        // No last-seen entry at all → never been
                        // observed, but they're in a game. That can
                        // only happen if matchmaking placed them and
                        // they immediately died. Give them a one-tick
                        // grace by seeding the entry now instead of
                        // disconnecting.
                        if (!_lastSeen.ContainsKey(uid))
                        {
                            _lastSeen[uid] = DateTime.UtcNow;
                            continue;
                        }
                        if (_lastSeen[uid] < cutoff && !toDisconnect.Contains(uid))
                            toDisconnect.Add(uid);
                    }
                }
            }

            // Async work happens outside the lock.
            foreach (int uid in toDisconnect)
            {
                try { await HandleDisconnectAsync(uid); }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "Stale-sweep disconnect failed for " + uid + ": " + ex.Message);
                }
            }
        }

        public int GetActiveGameCount()
        {
            lock (_stateLock) return _activeGames.Count;
        }

        // Adds the user to the matchmaking queue. If another player is
        // already waiting, dequeues them, persists a new GameRecord,
        // creates a GameSession, and notifies both players.
        public async Task<JoinResult> JoinMatchmakingAsync(User user)
        {
            int? opponentId = null;
            lock (_stateLock)
            {
                if (_userToGame.ContainsKey(user.Id))
                    return new JoinResult { Started = false, Message = "Already in a game." };

                while (_matchmakingQueue.Count > 0)
                {
                    int candidate = _matchmakingQueue.Dequeue();
                    if (candidate != user.Id && _onlineUsers.ContainsKey(candidate))
                    {
                        opponentId = candidate;
                        break;
                    }
                }
                if (opponentId == null)
                {
                    _matchmakingQueue.Enqueue(user.Id);
                    return new JoinResult { Started = false, Message = "Queued. Waiting for opponent…" };
                }
            }

            User opponent;
            lock (_stateLock) opponent = _onlineUsers[opponentId.Value];

            User white, black;
            if (DateTime.Now.Ticks % 2 == 0)
            {
                white = user;     black = opponent;
            }
            else
            {
                white = opponent; black = user;
            }

            GameSession session = new GameSession(white, black);

            GameRecord record = new GameRecord
            {
                WhitePlayerId = white.Id,
                BlackPlayerId = black.Id,
                StartedAt     = session.StartedAt,
                Status        = GameStatus.InProgress,
                FinalBoard    = session.Board.Serialize()
            };
            int gameId = await _gamesDB.AddAsync(record);
            session.GameId = gameId;

            lock (_stateLock)
            {
                _activeGames[gameId]  = session;
                _userToGame[white.Id] = gameId;
                _userToGame[black.Id] = gameId;
            }

            return new JoinResult { Started = true, Session = session };
        }

        // Special-case "matchmaking" path that pairs the human against
        // the in-process Bot user. Doesn't go through the queue — the
        // game starts immediately. Human always plays White so they
        // move first.
        public async Task<JoinResult> JoinBotGameAsync(User human)
        {
            User bot = await _usersDB.GetByUsernameAsync(BotUsername);
            if (bot == null)
                return new JoinResult { Started = false, Message = "Bot user is not configured." };

            lock (_stateLock)
            {
                if (_userToGame.ContainsKey(human.Id))
                    return new JoinResult { Started = false, Message = "You are already in a game." };
            }

            GameSession session = new GameSession(human, bot)
            {
                IsBotGame = true,
                BotUserId = bot.Id
            };

            GameRecord record = new GameRecord
            {
                WhitePlayerId = human.Id,
                BlackPlayerId = bot.Id,
                StartedAt     = session.StartedAt,
                Status        = GameStatus.InProgress,
                FinalBoard    = session.Board.Serialize()
            };
            int gameId = await _gamesDB.AddAsync(record);
            session.GameId = gameId;

            lock (_stateLock)
            {
                _activeGames[gameId]   = session;
                _userToGame[human.Id]  = gameId;
                _userToGame[bot.Id]    = gameId;
            }
            return new JoinResult { Started = true, Session = session };
        }

        // Username of the seeded bot account. The host calls
        // EnsureBotUserAsync at startup to create it if missing.
        public const string BotUsername = "Bot";

        public async Task EnsureBotUserAsync()
        {
            User bot = await _usersDB.GetByUsernameAsync(BotUsername);
            if (bot != null) return;
            bot = new User
            {
                Username     = BotUsername,
                // Random password — the bot account isn't login-able.
                PasswordHash = PasswordHasher.Hash(System.Guid.NewGuid().ToString()),
                Email        = string.Empty,
                Role         = UserRole.Player,
                Rating       = 1000,
                CreatedAt    = DateTime.Now
            };
            await _usersDB.AddAsync(bot);
        }

        public void LeaveMatchmaking(int userId)
        {
            lock (_stateLock)
            {
                Queue<int> filtered = new Queue<int>();
                while (_matchmakingQueue.Count > 0)
                {
                    int id = _matchmakingQueue.Dequeue();
                    if (id != userId) filtered.Enqueue(id);
                }
                while (filtered.Count > 0) _matchmakingQueue.Enqueue(filtered.Dequeue());
            }
        }

        // Validates and applies a move. The path is the full sequence
        // of squares: [from, to] for a slide; [from, hop1, hop2, …] for
        // a (multi-)jump. Persists the move row, ends the game when
        // appropriate, and updates player stats.
        public async Task<MoveResult> SubmitMoveAsync(int userId, IList<Square> path)
        {
            GameSession session = FindByUserId(userId);
            if (session == null) return MoveResult.Fail("You are not in an active game.");

            Move executed;
            bool ended;
            GameStatus endStatus = GameStatus.InProgress;
            GameEndReason endReason = GameEndReason.None;
            int? winnerId = null;

            lock (session.Lock)
            {
                if (session.Status != GameStatus.InProgress)
                    return MoveResult.Fail("Game has already ended.");

                PieceColor mover = session.ColorOf(userId);
                if (mover != session.Board.SideToMove)
                    return MoveResult.Fail("It is not your turn.");

                executed = CheckersEngine.TryExecuteMove(session.Board, path);
                if (executed == null) return MoveResult.Fail("Illegal move.");

                session.RecordMove(executed);

                // Game-end checks: opponent has no pieces or no legal moves.
                PieceColor opponentColor = session.Board.SideToMove;
                int opponentPieces = session.Board.CountPieces(opponentColor);
                if (opponentPieces == 0)
                {
                    endStatus = mover == PieceColor.White ? GameStatus.WhiteWins : GameStatus.BlackWins;
                    endReason = GameEndReason.NoPieces;
                    winnerId  = userId;
                }
                else if (!CheckersEngine.HasAnyLegalMove(session.Board))
                {
                    endStatus = mover == PieceColor.White ? GameStatus.WhiteWins : GameStatus.BlackWins;
                    endReason = GameEndReason.NoLegalMoves;
                    winnerId  = userId;
                }
                ended = endStatus != GameStatus.InProgress;
                if (ended) session.SetEnded(endStatus, endReason, winnerId);
            }

            await _movesDB.AddAsync(executed);
            if (ended) await FinalizeGameAsync(session);

            // If this is a bot game and it's now the bot's turn,
            // schedule a reply on a background thread so the human's
            // call returns immediately. The 600 ms delay gives a
            // natural "thinking" pause before the bot's move appears
            // on the next poll tick.
            if (session.IsBotGame
                && session.Status == GameStatus.InProgress
                && userId != session.BotUserId)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(600);
                    await PlayBotMoveAsync(session);
                });
            }

            return MoveResult.Ok(executed, session.Status, session.EndReason, session.Board.Serialize());
        }

        // Reads the legal moves from the bot side, picks one via
        // BotEngine, and submits it through the same SubmitMoveAsync
        // path the human uses — so all the same persistence, end-game
        // detection, and stat-update logic runs identically.
        private async Task PlayBotMoveAsync(GameSession session)
        {
            try
            {
                List<Square> path;
                lock (session.Lock)
                {
                    if (session.Status != GameStatus.InProgress) return;
                    if (session.Board.SideToMove != session.ColorOf(session.BotUserId)) return;
                    path = BotEngine.PickMove(session.Board);
                }
                if (path == null) return; // bot has no moves — human will win on next move
                await SubmitMoveAsync(session.BotUserId, path);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Bot move failed: " + ex.Message);
            }
        }

        // Records a pending draw offer on the active game. Idempotent —
        // re-calling for the same player is a no-op.
        public bool OfferDraw(int userId)
        {
            GameSession session = FindByUserId(userId);
            if (session == null) return false;
            lock (session.Lock) { session.OfferDraw(userId); }
            return true;
        }

        // Accept (game ends as Draw) or decline (offer cleared).
        public async Task<MoveResult> RespondToDrawAsync(int userId, bool accept)
        {
            GameSession session = FindByUserId(userId);
            if (session == null) return MoveResult.Fail("You are not in an active game.");

            bool ended = false;
            lock (session.Lock)
            {
                if (accept)
                {
                    if (!session.TryAcceptDraw(userId))
                        return MoveResult.Fail("No draw offer to accept.");
                    ended = true;
                }
                else
                {
                    session.DeclineDraw();
                }
            }
            if (ended) await FinalizeGameAsync(session);

            return MoveResult.Ok(null, session.Status, session.EndReason, session.Board.Serialize());
        }

        public async Task<MoveResult> ResignAsync(int userId)
        {
            GameSession session = FindByUserId(userId);
            if (session == null) return MoveResult.Fail("You are not in an active game.");

            lock (session.Lock)
            {
                if (session.Status != GameStatus.InProgress)
                    return MoveResult.Fail("Game has already ended.");
                int winner = session.OpponentOf(userId);
                GameStatus s = winner == session.WhitePlayer.Id ? GameStatus.WhiteWins : GameStatus.BlackWins;
                session.SetEnded(s, GameEndReason.Resignation, winner);
            }
            await FinalizeGameAsync(session);
            return MoveResult.Ok(null, session.Status, session.EndReason, session.Board.Serialize());
        }

        public async Task HandleDisconnectAsync(int userId)
        {
            GameSession session = FindByUserId(userId);
            UnregisterOnline(userId);
            LeaveMatchmaking(userId);
            if (session == null || session.Status != GameStatus.InProgress) return;

            lock (session.Lock)
            {
                int winner = session.OpponentOf(userId);
                GameStatus s = winner == session.WhitePlayer.Id ? GameStatus.WhiteWins : GameStatus.BlackWins;
                session.SetEnded(s, GameEndReason.Disconnection, winner);
            }
            await FinalizeGameAsync(session);
        }

        // ----- helpers -----

        private async Task FinalizeGameAsync(GameSession session)
        {
            await _gamesDB.UpdateOnEndAsync(
                session.GameId,
                session.Status,
                session.EndReason,
                session.WinnerId,
                session.Board.Serialize(),
                session.MoveHistory.Count,
                DateTime.Now);

            // Update both players' Win/Loss/Draw counts and Elo rating.
            User w = await _usersDB.GetByIdAsync(session.WhitePlayer.Id);
            User b = await _usersDB.GetByIdAsync(session.BlackPlayer.Id);
            if (w == null || b == null) return;

            double scoreW;
            switch (session.Status)
            {
                case GameStatus.WhiteWins: scoreW = 1.0; break;
                case GameStatus.BlackWins: scoreW = 0.0; break;
                default:                    scoreW = 0.5; break;
            }
            double expectedW = 1.0 / (1 + Math.Pow(10, (b.Rating - w.Rating) / 400.0));
            const int K = 24;
            int newW = (int)Math.Round(w.Rating + K * (scoreW - expectedW));
            int newB = (int)Math.Round(b.Rating + K * ((1.0 - scoreW) - (1.0 - expectedW)));

            // Disconnection penalty: whoever rage-quit takes a flat 20
            // LP hit on top of the standard Elo loss. The opponent's
            // rating is unaffected by the bonus — they get the normal
            // win delta only.
            if (session.EndReason == GameEndReason.Disconnection)
            {
                const int DisconnectPenalty = 20;
                if (session.Status == GameStatus.WhiteWins)
                    newB -= DisconnectPenalty;            // black quit
                else if (session.Status == GameStatus.BlackWins)
                    newW -= DisconnectPenalty;            // white quit
                // Floor at 100 so a long string of disconnects can't
                // drive a rating into the negative.
                if (newW < 100) newW = 100;
                if (newB < 100) newB = 100;
            }

            int wWins = w.Wins, wLoss = w.Losses, wDraw = w.Draws;
            int bWins = b.Wins, bLoss = b.Losses, bDraw = b.Draws;
            switch (session.Status)
            {
                case GameStatus.WhiteWins: wWins++; bLoss++; break;
                case GameStatus.BlackWins: bWins++; wLoss++; break;
                case GameStatus.Draw:      wDraw++; bDraw++; break;
            }

            await _usersDB.UpdateStatsAsync(w.Id, wWins, wLoss, wDraw, newW);
            await _usersDB.UpdateStatsAsync(b.Id, bWins, bLoss, bDraw, newB);

            lock (_stateLock)
            {
                _userToGame.Remove(session.WhitePlayer.Id);
                _userToGame.Remove(session.BlackPlayer.Id);
                _activeGames.Remove(session.GameId);
            }
        }
    }
}
