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
    // Replay viewer for finished (or in-progress) games. Loads the
    // server-side move history and steps the BoardModel through every
    // recorded BoardAfter snapshot. Reuses CheckersBoardControl in the
    // view by binding to the same BoardModel — but the host (ReplayView)
    // does not subscribe to SquareClicked, so the board is read-only.
    public class ReplayViewModel : ViewModelBase
    {
        private readonly AppViewModel _app;
        private readonly int _gameId;
        private readonly DispatcherTimer _playTimer;

        public ReplayViewModel(AppViewModel app, int gameId,
                               string whiteName = null, string blackName = null)
        {
            _app          = app;
            _gameId       = gameId;
            WhiteUsername = whiteName ?? "White";
            BlackUsername = blackName ?? "Black";

            Board    = new BoardModel();
            MoveRows = new ObservableCollection<MoveRow>();

            BackCommand    = new RelayCommand(_ => _app.GoToLobby());
            FirstCommand   = new RelayCommand(_ => { StopPlay(); JumpTo(-1); });
            PrevCommand    = new RelayCommand(_ => { StopPlay(); JumpTo(_currentIdx - 1); });
            NextCommand    = new RelayCommand(_ => { StopPlay(); JumpTo(_currentIdx + 1); });
            LastCommand    = new RelayCommand(_ => { StopPlay(); JumpTo(_moves.Count - 1); });
            PlayPauseCommand = new RelayCommand(_ => TogglePlay());
            JumpCommand    = new RelayCommand(o =>
            {
                if (o is MoveRow row) { StopPlay(); JumpTo(row.Index - 1); }
            });

            _playTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
            _playTimer.Tick += (s, e) =>
            {
                if (_currentIdx >= _moves.Count - 1) { StopPlay(); return; }
                JumpTo(_currentIdx + 1);
            };

            _ = LoadAsync();
        }

        // ----- bindable state -----

        public BoardModel Board { get; }
        public ObservableCollection<MoveRow> MoveRows { get; }

        public string WhiteUsername { get; }
        public string BlackUsername { get; }

        private int _currentIdx = -1;     // -1 = start position
        public  int CurrentIdx { get { return _currentIdx; } private set { SetProperty(ref _currentIdx, value); } }

        public string PositionLabel
        {
            get
            {
                if (_currentIdx < 0)              return "Start position";
                if (_moves == null || _moves.Count == 0) return "";
                Move m = _moves[_currentIdx];
                return $"{_currentIdx + 1}. {m.Notation}";
            }
        }

        private bool _isPlaying;
        public  bool IsPlaying { get { return _isPlaying; } private set { SetProperty(ref _isPlaying, value); OnPropertyChanged(nameof(PlayLabel)); } }
        public  string PlayLabel => IsPlaying ? "❚❚ Pause" : "▶ Play";

        private string _statusMessage;
        public  string StatusMessage { get { return _statusMessage; } private set { SetProperty(ref _statusMessage, value); } }

        // ----- commands -----

        public ICommand BackCommand    { get; }
        public ICommand FirstCommand   { get; }
        public ICommand PrevCommand    { get; }
        public ICommand NextCommand    { get; }
        public ICommand LastCommand    { get; }
        public ICommand PlayPauseCommand { get; }
        public ICommand JumpCommand    { get; }

        // ----- internals -----

        private List<Move> _moves = new List<Move>();

        private async Task LoadAsync()
        {
            try
            {
                StatusMessage = "Loading…";
                _moves = await _app.Client.GetMoveHistoryAsync(_gameId);

                MoveRows.Clear();
                for (int i = 0; i < _moves.Count; i++)
                    MoveRows.Add(new MoveRow(i + 1, _moves[i]));

                JumpTo(-1);
                StatusMessage = $"Game #{_gameId} · {_moves.Count} moves";
            }
            catch (Exception ex)
            {
                StatusMessage = "Could not load this game: " + ex.Message;
            }
        }

        private void JumpTo(int idx)
        {
            int clamped = Math.Max(-1, Math.Min(_moves.Count - 1, idx));
            CurrentIdx = clamped;

            if (clamped < 0)
            {
                Board.ApplyState(StartingBoardSerialised());
                Board.ClearLastMove();
            }
            else
            {
                Move m = _moves[clamped];
                Board.ApplyState(m.BoardAfter);
                Board.ClearLastMove();
                Board.MarkLastMove(new System.Collections.Generic.List<Square>
                {
                    new Square(m.FromFile, m.FromRank),
                    new Square(m.ToFile,   m.ToRank)
                });
            }
            OnPropertyChanged(nameof(PositionLabel));
        }

        private void TogglePlay()
        {
            if (IsPlaying) StopPlay();
            else           StartPlay();
        }

        private void StartPlay()
        {
            if (_moves.Count == 0) return;
            if (_currentIdx >= _moves.Count - 1) JumpTo(-1);
            IsPlaying = true;
            _playTimer.Start();
        }

        private void StopPlay()
        {
            if (!IsPlaying) return;
            _playTimer.Stop();
            IsPlaying = false;
        }

        // Same starting layout the engine uses (12 white pieces on
        // ranks 0-2, 12 black on 5-7). Encoded as 32 dark-square chars
        // followed by " w 0".
        private static string StartingBoardSerialised()
        {
            var sb = new System.Text.StringBuilder(32);
            for (int r = 0; r < 8; r++)
                for (int f = 0; f < 8; f++)
                {
                    if (((f + r) & 1) != 0) continue;
                    if (r <= 2)      sb.Append('w');
                    else if (r >= 5) sb.Append('b');
                    else             sb.Append('.');
                }
            sb.Append(" w 0");
            return sb.ToString();
        }
    }
}
