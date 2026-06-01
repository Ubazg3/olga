using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using Model;

namespace CheckersClient.ViewModels
{
    // Home screen after login. Shows profile, leaderboard, online users,
    // recent games, and the "Find a Game" button that drops the user
    // into the matchmaking queue. While queued, a DispatcherTimer polls
    // the server for a started game and navigates to GameView when
    // matched.
    public class LobbyViewModel : ViewModelBase
    {
        private readonly AppViewModel _app;
        private readonly DispatcherTimer _matchmakingTimer;
        private readonly DispatcherTimer _refreshTimer;

        public LobbyViewModel(AppViewModel app)
        {
            _app = app;
            CurrentUser = _app.Client.CurrentUser;

            FindGameCommand = new RelayCommand(async _ => await ToggleQueueAsync(), _ => !IsBusy);
            PlayBotCommand  = new RelayCommand(async _ => await PlayBotAsync(),    _ => !IsBusy && !IsInQueue);
            LogoutCommand   = new RelayCommand(async _ => await DoLogoutAsync(),    _ => !IsBusy);
            OpenAdminCommand = new RelayCommand(_ => _app.GoToAdmin(),
                                                _ => CurrentUser != null && CurrentUser.Role == UserRole.Admin);
            RefreshCommand  = new RelayCommand(async _ => await RefreshAsync(), _ => !IsBusy);

            _matchmakingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(800)
            };
            _matchmakingTimer.Tick += async (s, e) => await PollForGameAsync();

            // 5 s feels responsive without flooding the server when
            // the lobby is left open. There's no Refresh button now —
            // this is the only way the lobby data stays current.
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _refreshTimer.Tick += async (s, e) => await RefreshAsync();
            _refreshTimer.Start();

            // First load.
            _ = RefreshAsync();
        }

        // ----- bindable properties -----

        public ObservableCollection<User>        TopPlayers     { get; } = new ObservableCollection<User>();
        public ObservableCollection<User>        OnlinePlayers  { get; } = new ObservableCollection<User>();
        public ObservableCollection<GameRecord>  RecentGames    { get; } = new ObservableCollection<GameRecord>();

        public User CurrentUser { get; }

        public bool IsAdmin => CurrentUser != null && CurrentUser.Role == UserRole.Admin;

        private bool _isInQueue;
        public  bool IsInQueue { get { return _isInQueue; } set { SetProperty(ref _isInQueue, value); OnPropertyChanged(nameof(FindGameLabel)); } }

        public string FindGameLabel => IsInQueue ? "Cancel search" : "Find a game";

        private bool _isBusy;
        public  bool IsBusy { get { return _isBusy; } set { SetProperty(ref _isBusy, value); } }

        private string _statusMessage;
        public  string StatusMessage { get { return _statusMessage; } set { SetProperty(ref _statusMessage, value); } }

        public ICommand FindGameCommand  { get; }
        public ICommand PlayBotCommand   { get; }
        public ICommand LogoutCommand    { get; }
        public ICommand OpenAdminCommand { get; }
        public ICommand RefreshCommand   { get; }

        // ----- operations -----

        private async Task ToggleQueueAsync()
        {
            IsBusy = true;
            try
            {
                if (IsInQueue)
                {
                    await _app.Client.LeaveQueueAsync();
                    IsInQueue = false;
                    _matchmakingTimer.Stop();
                    StatusMessage = "Search canceled.";
                    return;
                }

                StatusMessage = "Searching for an opponent…";
                string reply = await _app.Client.JoinQueueAsync();
                IsInQueue = true;
                _matchmakingTimer.Start();

                // The server may have paired us instantly with someone
                // already waiting — react on the next poll tick either way.
                StatusMessage = reply ?? "Queued.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Could not contact the server: " + ex.Message;
                IsInQueue = false;
                _matchmakingTimer.Stop();
            }
            finally { IsBusy = false; }
        }

        private async Task PlayBotAsync()
        {
            IsBusy = true;
            try
            {
                StatusMessage = "Spinning up the bot opponent…";
                string reply = await _app.Client.JoinBotGameAsync();
                StatusMessage = reply ?? "Started.";
                // The matchmaking timer already polls for an active
                // game. Start it here too so the lobby flips into the
                // game view without an explicit "Play" trigger.
                _matchmakingTimer.Start();
            }
            catch (Exception ex)
            {
                StatusMessage = "Could not contact the server: " + ex.Message;
            }
            finally { IsBusy = false; }
        }

        private async Task PollForGameAsync()
        {
            try
            {
                GameStateDto state = await _app.Client.GetCurrentGameAsync();
                if (state != null && state.Status == GameStatus.InProgress)
                {
                    _matchmakingTimer.Stop();
                    _refreshTimer.Stop();
                    IsInQueue = false;
                    _app.GoToGame(state);
                }
            }
            catch
            {
                // Transient error, just try again on the next tick.
            }
        }

        private async Task RefreshAsync()
        {
            try
            {
                IsBusy = true;
                var top    = _app.Client.GetTopPlayersAsync(10);
                var online = _app.Client.GetOnlinePlayersAsync();
                var hist   = _app.Client.GetMyGameHistoryAsync();
                await Task.WhenAll(top, online, hist);

                TopPlayers.Clear();
                foreach (var u in top.Result) TopPlayers.Add(u);

                OnlinePlayers.Clear();
                foreach (var u in online.Result) OnlinePlayers.Add(u);

                RecentGames.Clear();
                int max = Math.Min(hist.Result.Count, 10);
                for (int i = 0; i < max; i++) RecentGames.Add(hist.Result[i]);
            }
            catch
            {
                // Server may be momentarily unavailable; the timer will retry.
            }
            finally { IsBusy = false; }
        }

        private async Task DoLogoutAsync()
        {
            _matchmakingTimer.Stop();
            _refreshTimer.Stop();
            await _app.Client.LogoutAsync();
            _app.GoToLogin();
        }

        // Called from the view code-behind when the user clicks any
        // PlayerCardControl on the lobby.
        public void OpenProfile(int userId)
        {
            _matchmakingTimer.Stop();
            _refreshTimer.Stop();
            _app.GoToProfile(userId);
        }
    }
}
