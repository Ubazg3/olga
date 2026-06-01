using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using CheckersClient.Helpers;
using Model;

namespace CheckersClient.ViewModels
{
    // Live game screen. Owns the BoardModel, polls the server for
    // opponent moves, and turns user clicks into MakeMove calls.
    public class GameViewModel : ViewModelBase
    {
        private readonly AppViewModel _app;
        private readonly DispatcherTimer _pollTimer;
        private List<List<Square>> _legalPathsForSelected = new List<List<Square>>();

        public GameViewModel(AppViewModel app, GameStateDto initialState)
        {
            _app = app;

            Board = new BoardModel();
            Moves = new ObservableCollection<Move>();
            MoveRows = new ObservableCollection<MoveRow>();
            ApplyState(initialState, isInitial: true);
            Board.Flipped = MyColor == PieceColor.Black;

            ResignCommand           = new RelayCommand(async _ => await DoResignAsync(), _ => CanInteract);
            OfferDrawCommand        = new RelayCommand(async _ => await OfferDrawAsync(),
                                                       _ => CanInteract && !DrawOfferIsMine && !OpponentOfferedDraw);
            AcceptDrawCommand       = new RelayCommand(async _ => await RespondToDrawAsync(true),
                                                       _ => OpponentOfferedDraw);
            DeclineDrawCommand      = new RelayCommand(async _ => await RespondToDrawAsync(false),
                                                       _ => OpponentOfferedDraw);

            // Poll the server every 500 ms — fast enough to feel
            // immediate when the opponent moves, slow enough that 100
            // concurrent games still wouldn't crush the server.
            _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _pollTimer.Tick += async (s, e) => await PollAsync();
            _pollTimer.Start();

            _ = LoadHistoryAsync();
        }

        // ----- bindable properties -----

        public BoardModel Board { get; }
        public ObservableCollection<Move> Moves { get; }
        // Display projection over Moves — each entry numbered 1..N so
        // the side panel doesn't show duplicate "1. … / 1. …" rows.
        public ObservableCollection<MoveRow> MoveRows { get; }

        private GameStateDto _state;
        public GameStateDto State
        {
            get { return _state; }
            set
            {
                if (SetProperty(ref _state, value))
                {
                    // Computed properties that read off State — kick
                    // their bindings so the UI re-evaluates them.
                    OnPropertyChanged(nameof(DrawOfferIsMine));
                    OnPropertyChanged(nameof(OpponentOfferedDraw));
                }
            }
        }

        // The local player's colour (derived once from the initial state).
        public PieceColor MyColor { get; private set; }

        public bool IsMyTurn
        {
            get
            {
                if (State == null) return false;
                if (State.Status != GameStatus.InProgress) return false;
                // Whose move depends on the board's side-to-move, which
                // we don't have directly here — derive from MoveCount
                // parity instead: on even count it's white's move.
                bool whitesTurn = (State.MoveCount % 2) == 0;
                return whitesTurn ? MyColor == PieceColor.White : MyColor == PieceColor.Black;
            }
        }

        public bool CanInteract => State != null && State.Status == GameStatus.InProgress;

        public string TurnLabel
        {
            get
            {
                if (State == null) return string.Empty;
                if (State.Status != GameStatus.InProgress)
                    return State.Status.ToString();
                return IsMyTurn ? "Your turn" : "Opponent's turn";
            }
        }

        public ICommand ResignCommand      { get; }
        public ICommand OfferDrawCommand   { get; }
        public ICommand AcceptDrawCommand  { get; }
        public ICommand DeclineDrawCommand { get; }

        // Whose offer is currently on the table (or no offer if false).
        public bool DrawOfferIsMine    => State?.DrawOfferFromUserId == _app.Client.CurrentUser?.Id;
        public bool OpponentOfferedDraw =>
            State?.DrawOfferFromUserId.HasValue == true
            && State.DrawOfferFromUserId.Value != _app.Client.CurrentUser?.Id;

        // ----- click handler called from the view code-behind -----

        public async Task OnSquareClickedAsync(int file, int rank)
        {
            if (!CanInteract || !IsMyTurn) return;

            BoardCell clicked = Board.Get(file, rank);

            // If a piece is already selected and the click is on a
            // legal target, submit the move.
            if (_legalPathsForSelected.Count > 0)
            {
                List<Square> path = FindMatchingPath(file, rank);
                if (path != null)
                {
                    await SubmitMoveAsync(path);
                    return;
                }
            }

            // Otherwise treat this as a (re-)selection.
            await SelectAsync(clicked);
        }

        // ----- internal -----

        private async Task SelectAsync(BoardCell cell)
        {
            Board.ClearHighlights();
            _legalPathsForSelected = new List<List<Square>>();

            if (cell.PieceCode == '.') return;

            // Only your own pieces are selectable.
            bool isWhitePiece = cell.PieceCode == 'w' || cell.PieceCode == 'W';
            bool mine = (MyColor == PieceColor.White) == isWhitePiece;
            if (!mine) return;

            cell.IsSelected = true;
            try
            {
                _legalPathsForSelected =
                    await _app.Client.GetLegalMovesForSquareAsync(cell.File, cell.Rank);
            }
            catch
            {
                _legalPathsForSelected = new List<List<Square>>();
            }

            foreach (List<Square> path in _legalPathsForSelected)
            {
                if (path.Count < 2) continue;
                Square dest = path[path.Count - 1];
                Board.Get(dest.File, dest.Rank).IsLegalTarget = true;
            }
        }

        private List<Square> FindMatchingPath(int destFile, int destRank)
        {
            foreach (List<Square> path in _legalPathsForSelected)
            {
                if (path.Count < 2) continue;
                Square last = path[path.Count - 1];
                if (last.File == destFile && last.Rank == destRank)
                    return path;
            }
            return null;
        }

        private async Task SubmitMoveAsync(List<Square> path)
        {
            try
            {
                MoveResult res = await _app.Client.MakeMoveAsync(path);
                if (res == null || !res.Accepted)
                {
                    // The illegal-move case is rare here because we
                    // pulled the legal paths from the server moments
                    // ago, but we still surface any failure.
                    Board.ClearHighlights();
                    _legalPathsForSelected.Clear();
                    return;
                }
                // Mirror the move locally so the UI updates instantly,
                // before the next poll round-trips.
                Board.ClearHighlights();
                _legalPathsForSelected.Clear();

                if (res.PlayedMove != null)
                {
                    Board.MarkLastMove(res.PlayedMove.Path);
                    Moves.Add(res.PlayedMove);
                    MoveRows.Add(new MoveRow(MoveRows.Count + 1, res.PlayedMove));
                }
                State = new GameStateDto
                {
                    GameId           = State.GameId,
                    WhitePlayerId    = State.WhitePlayerId,
                    BlackPlayerId    = State.BlackPlayerId,
                    WhiteUsername    = State.WhiteUsername,
                    BlackUsername    = State.BlackUsername,
                    Board            = res.FEN,
                    Status           = res.NewStatus,
                    EndReason        = res.EndReason,
                    MoveCount        = State.MoveCount + 1,
                    LastMoveNotation = res.PlayedMove?.Notation,
                    WhitePiecesLeft  = State.WhitePiecesLeft,
                    BlackPiecesLeft  = State.BlackPiecesLeft
                };
                Board.ApplyState(res.FEN);
                OnPropertyChanged(nameof(IsMyTurn));
                OnPropertyChanged(nameof(TurnLabel));

                if (res.NewStatus != GameStatus.InProgress)
                    HandleGameEnd(State);
            }
            catch
            {
                // Server unreachable — wait for the next poll tick.
            }
        }

        private async Task OfferDrawAsync()
        {
            try { await _app.Client.OfferDrawAsync(); }
            catch { /* network blip — next poll fixes it */ }
            // Force a state refresh so the UI updates immediately
            // instead of waiting for the next polling tick.
            await PollAsync();
        }

        private async Task RespondToDrawAsync(bool accept)
        {
            try
            {
                MoveResult res = await _app.Client.RespondToDrawAsync(accept);
                if (accept && res != null && res.NewStatus != GameStatus.InProgress)
                {
                    State.Status    = res.NewStatus;
                    State.EndReason = res.EndReason;
                    HandleGameEnd(State);
                    return;
                }
            }
            catch { /* let polling reconcile */ }
            await PollAsync();
        }

        private async Task DoResignAsync()
        {
            try
            {
                MoveResult res = await _app.Client.ResignAsync();
                if (res != null && State != null)
                {
                    State = new GameStateDto
                    {
                        GameId        = State.GameId,
                        WhitePlayerId = State.WhitePlayerId,
                        BlackPlayerId = State.BlackPlayerId,
                        WhiteUsername = State.WhiteUsername,
                        BlackUsername = State.BlackUsername,
                        Board         = res.FEN,
                        Status        = res.NewStatus,
                        EndReason     = res.EndReason,
                        MoveCount     = State.MoveCount,
                    };
                    HandleGameEnd(State);
                }
            }
            catch { /* best effort */ }
        }

        // ----- polling -----

        private async Task PollAsync()
        {
            try
            {
                GameStateDto fresh = await _app.Client.GetCurrentGameAsync();
                if (fresh == null)
                {
                    // The game is gone (maybe ended a moment ago).
                    return;
                }
                if (fresh.MoveCount != State.MoveCount
                    || fresh.Status != State.Status)
                {
                    ApplyState(fresh, isInitial: false);
                    if (fresh.Status != GameStatus.InProgress)
                        HandleGameEnd(fresh);
                }
            }
            catch
            {
                // Ignore transient errors — try again next tick.
            }
        }

        private async Task LoadHistoryAsync()
        {
            if (State == null) return;
            try
            {
                List<Move> history = await _app.Client.GetMoveHistoryAsync(State.GameId);
                Moves.Clear();
                MoveRows.Clear();
                int idx = 1;
                foreach (Move m in history)
                {
                    Moves.Add(m);
                    MoveRows.Add(new MoveRow(idx++, m));
                }
            }
            catch { /* not critical */ }
        }

        private void ApplyState(GameStateDto s, bool isInitial)
        {
            State = s;
            Board.ApplyState(s.Board);

            if (isInitial)
            {
                // Determine our colour the first time we get a state.
                MyColor = s.WhitePlayerId == _app.Client.CurrentUser.Id
                    ? PieceColor.White : PieceColor.Black;
            }

            // Refresh move list from the server in the background.
            _ = LoadHistoryAsync();
            OnPropertyChanged(nameof(IsMyTurn));
            OnPropertyChanged(nameof(TurnLabel));
        }

        private void HandleGameEnd(GameStateDto finalState)
        {
            _pollTimer.Stop();
            _app.GoToGameOver(finalState);
        }
    }

    // Lightweight projection of a Move for the side panel — bundles a
    // sequential 1..N display index next to the underlying move so the
    // bound DataTemplate doesn't have to fight with the server-side
    // MoveNumber (which counts pairs, not individual ply).
    public class MoveRow
    {
        public int  Index { get; }
        public Move Move  { get; }
        public string Notation => Move?.Notation ?? "";
        public MoveRow(int index, Move move) { Index = index; Move = move; }
    }
}
