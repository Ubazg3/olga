using System.Windows.Input;
using Model;

namespace CheckersClient.ViewModels
{
    // Modal-style result page shown right after a game ends. Tells the
    // player whether they won, lost or drew, and summarises the game.
    public class GameOverViewModel : ViewModelBase
    {
        private readonly AppViewModel _app;

        public GameOverViewModel(AppViewModel app, GameStateDto finalState)
        {
            _app = app;
            FinalState = finalState;

            int myId = _app.Client.CurrentUser.Id;
            bool iAmWhite = finalState.WhitePlayerId == myId;

            switch (finalState.Status)
            {
                case GameStatus.WhiteWins:
                    IsWin = iAmWhite;
                    IsLoss = !iAmWhite;
                    break;
                case GameStatus.BlackWins:
                    IsWin = !iAmWhite;
                    IsLoss = iAmWhite;
                    break;
                case GameStatus.Draw:
                    IsDraw = true;
                    break;
            }

            BackToLobbyCommand  = new RelayCommand(_ => _app.GoToLobby());
            PlayAgainCommand    = new RelayCommand(_ => _app.GoToLobby());
        }

        public GameStateDto FinalState { get; }

        public bool IsWin  { get; }
        public bool IsLoss { get; }
        public bool IsDraw { get; }

        public string Title
        {
            get
            {
                if (IsWin)  return "Victory!";
                if (IsLoss) return "Defeat";
                if (IsDraw) return "Draw";
                return "Game over";
            }
        }

        public string Subtitle
        {
            get
            {
                if (IsWin)  return "Well played — you outmaneuvered them.";
                if (IsLoss) return "Tough one. Set 'em up again?";
                if (IsDraw) return "A balanced game.";
                return string.Empty;
            }
        }

        public ICommand BackToLobbyCommand { get; }
        public ICommand PlayAgainCommand   { get; }
    }
}
