using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Model;

namespace CheckersClient.Helpers
{
    // One observable cell. Bound directly by CheckersBoardControl.xaml.
    public class BoardCell : INotifyPropertyChanged
    {
        public int File { get; }
        public int Rank { get; }
        public bool IsPlayable { get; }   // dark squares only

        private char _pieceCode;
        public char PieceCode
        {
            get { return _pieceCode; }
            set { if (_pieceCode != value) { _pieceCode = value; Raise(); } }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set { if (_isSelected != value) { _isSelected = value; Raise(); } }
        }

        // True for any square the selected piece can legally land on.
        private bool _isLegalTarget;
        public bool IsLegalTarget
        {
            get { return _isLegalTarget; }
            set { if (_isLegalTarget != value) { _isLegalTarget = value; Raise(); } }
        }

        // True for the source / destination squares of the most recent
        // move — used to draw a soft highlight.
        private bool _isLastMove;
        public bool IsLastMove
        {
            get { return _isLastMove; }
            set { if (_isLastMove != value) { _isLastMove = value; Raise(); } }
        }

        public BoardCell(int file, int rank)
        {
            File = file;
            Rank = rank;
            IsPlayable = ((file + rank) & 1) == 0;
            _pieceCode = '.';
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Raise([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    // 64-cell board the UserControl binds to. Has helpers to update from
    // a serialized board string and to flip orientation for the black
    // player.
    public class BoardModel
    {
        public ObservableCollection<BoardCell> Cells { get; }
        private readonly BoardCell[,] _grid = new BoardCell[8, 8];
        private bool _flipped;

        public BoardModel()
        {
            Cells = new ObservableCollection<BoardCell>();
            Rebuild();
        }

        public bool Flipped
        {
            get { return _flipped; }
            set
            {
                if (_flipped == value) return;
                _flipped = value;
                Rebuild();
            }
        }

        public BoardCell Get(int file, int rank) { return _grid[file, rank]; }

        // Rebuilds the visible Cells collection in display order (top
        // row first). White-on-bottom unless Flipped is true.
        private void Rebuild()
        {
            // Make sure the per-square objects exist before we order them.
            for (int f = 0; f < 8; f++)
                for (int r = 0; r < 8; r++)
                    if (_grid[f, r] == null)
                        _grid[f, r] = new BoardCell(f, r);

            Cells.Clear();
            if (!_flipped)
            {
                for (int r = 7; r >= 0; r--)
                    for (int f = 0; f < 8; f++)
                        Cells.Add(_grid[f, r]);
            }
            else
            {
                for (int r = 0; r < 8; r++)
                    for (int f = 7; f >= 0; f--)
                        Cells.Add(_grid[f, r]);
            }
        }

        // Updates each cell's PieceCode from a Board.Serialize() string.
        public void ApplyState(string serialized)
        {
            if (string.IsNullOrWhiteSpace(serialized)) return;
            string[] parts = serialized.Split(' ');
            if (parts.Length < 1 || parts[0].Length != 32) return;

            int idx = 0;
            for (int r = 0; r < 8; r++)
                for (int f = 0; f < 8; f++)
                {
                    if (((f + r) & 1) != 0) continue;     // light square
                    _grid[f, r].PieceCode = parts[0][idx++];
                }
        }

        public void ClearHighlights()
        {
            for (int f = 0; f < 8; f++)
                for (int r = 0; r < 8; r++)
                {
                    BoardCell c = _grid[f, r];
                    c.IsSelected     = false;
                    c.IsLegalTarget  = false;
                }
        }

        public void ClearLastMove()
        {
            for (int f = 0; f < 8; f++)
                for (int r = 0; r < 8; r++)
                    _grid[f, r].IsLastMove = false;
        }

        // Marks the start and end squares of the last move so the user
        // sees what just happened.
        public void MarkLastMove(IList<Square> path)
        {
            ClearLastMove();
            if (path == null || path.Count == 0) return;
            Square first = path[0];
            Square last  = path[path.Count - 1];
            _grid[first.File, first.Rank].IsLastMove = true;
            _grid[last.File,  last.Rank ].IsLastMove = true;
        }
    }
}
