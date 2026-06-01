using System.Windows.Controls;
using CheckersClient.UserControls;
using CheckersClient.ViewModels;

namespace CheckersClient.Views
{
    public partial class GameView : UserControl
    {
        public GameView()
        {
            InitializeComponent();
        }

        // Forwards the UserControl's SquareClicked delegate-event to
        // the view-model. The VM is the one that knows the rules of
        // selection / move-submission.
        private async void OnBoardSquareClicked(object sender, SquareClickedEventArgs e)
        {
            var vm = DataContext as GameViewModel;
            if (vm == null) return;
            await vm.OnSquareClickedAsync(e.File, e.Rank);
        }
    }
}
