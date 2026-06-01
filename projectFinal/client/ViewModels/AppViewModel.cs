using System;
using System.Threading.Tasks;
using CheckersClient.Services;
using Model;

namespace CheckersClient.ViewModels
{
    // Root view-model. Owns the CheckersServiceClient and decides which
    // page is currently visible. Each child VM gets a "navigate to X"
    // callback so they don't need to know about each other.
    public class AppViewModel : ViewModelBase
    {
        public CheckersServiceClient Client { get; }

        private object _currentView;
        public object CurrentView
        {
            get { return _currentView; }
            private set { SetProperty(ref _currentView, value); }
        }

        public AppViewModel(CheckersServiceClient client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
        }

        // ----- Navigation -----

        public void GoToLogin()
        {
            CurrentView = new LoginViewModel(this);
        }

        public void GoToLobby()
        {
            CurrentView = new LobbyViewModel(this);
        }

        public void GoToGame(GameStateDto state)
        {
            CurrentView = new GameViewModel(this, state);
        }

        public void GoToGameOver(GameStateDto finalState)
        {
            CurrentView = new GameOverViewModel(this, finalState);
        }

        public void GoToAdmin()
        {
            CurrentView = new AdminViewModel(this);
        }

        public void GoToProfile(int userId)
        {
            CurrentView = new ProfileViewModel(this, userId);
        }

        public void GoToReplay(int gameId, string whiteName = null, string blackName = null)
        {
            CurrentView = new ReplayViewModel(this, gameId, whiteName, blackName);
        }

        // Called from App.OnExit — best-effort logout so the server can
        // free our matchmaking slot and active game.
        public void OnAppExit()
        {
            try { Client.LogoutAsync().Wait(TimeSpan.FromSeconds(2)); }
            catch { /* ignore */ }
        }
    }
}
