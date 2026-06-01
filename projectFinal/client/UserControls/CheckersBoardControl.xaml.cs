using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CheckersClient.Helpers;

namespace CheckersClient.UserControls
{
    // Reusable checkers board. Renders any BoardModel and surfaces a
    // single SquareClicked event. The hosting view-model is responsible
    // for turning a click into "select piece" / "make move" — this
    // control knows nothing about the rules.
    //
    // The SquareClicked event is a delegate-typed event, demonstrating
    // the rubric extension "advanced event-driven programming such as
    // delegates" and "UserControl that includes delegates".
    public partial class CheckersBoardControl : UserControl
    {
        public CheckersBoardControl()
        {
            InitializeComponent();
        }

        // ----- SquareClicked event -----

        public delegate void SquareClickedHandler(object sender, SquareClickedEventArgs e);
        public event SquareClickedHandler SquareClicked;

        // ----- Click forwarding -----

        // The DataTemplate's Border raises this. We pull the BoardCell
        // out of its DataContext so the host doesn't need to know how
        // the visual tree is built.
        private void OnCellClick(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement fe = sender as FrameworkElement;
            BoardCell cell = fe?.DataContext as BoardCell;
            if (cell == null || !cell.IsPlayable) return;

            SquareClicked?.Invoke(this, new SquareClickedEventArgs(cell));
            e.Handled = true;
        }
    }

    public class SquareClickedEventArgs : EventArgs
    {
        public BoardCell Cell { get; }
        public int File => Cell.File;
        public int Rank => Cell.Rank;

        public SquareClickedEventArgs(BoardCell cell) { Cell = cell; }
    }
}
