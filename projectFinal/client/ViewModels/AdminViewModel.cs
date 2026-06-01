using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using Model;

namespace CheckersClient.ViewModels
{
    // Admin-only screen. Lists every user, lets the admin ban / unban,
    // and shows the most recent games across the whole system. Visible
    // only when the logged-in user has UserRole.Admin — the lobby
    // entry point is gated on the same check.
    public class AdminViewModel : ViewModelBase
    {
        private readonly AppViewModel _app;
        private readonly DispatcherTimer _refreshTimer;

        public AdminViewModel(AppViewModel app)
        {
            _app = app;
            BackCommand      = new RelayCommand(_ => StopAndGoBack());
            ToggleBanCommand = new RelayCommand(async u => await ToggleBanAsync(u as User), u => u is User);

            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _refreshTimer.Tick += async (s, e) => await RefreshAsync();
            _refreshTimer.Start();

            _ = RefreshAsync();
        }

        public ObservableCollection<User>       Users       { get; } = new ObservableCollection<User>();
        public ObservableCollection<GameRecord> RecentGames { get; } = new ObservableCollection<GameRecord>();

        private bool _isBusy;
        public  bool IsBusy { get { return _isBusy; } set { SetProperty(ref _isBusy, value); } }

        private string _statusMessage;
        public  string StatusMessage { get { return _statusMessage; } set { SetProperty(ref _statusMessage, value); } }

        public ICommand BackCommand      { get; }
        public ICommand ToggleBanCommand { get; }

        private void StopAndGoBack()
        {
            _refreshTimer.Stop();
            _app.GoToLobby();
        }

        private async Task RefreshAsync()
        {
            // No spinner — the lists just repopulate quietly. Setting
            // IsBusy here would flicker on every poll tick.
            try
            {
                var users  = _app.Client.AdminGetAllUsersAsync();
                var games  = _app.Client.AdminGetRecentGamesAsync(20);
                await Task.WhenAll(users, games);

                ReplaceCollection(Users, users.Result);
                ReplaceCollection(RecentGames, games.Result);
            }
            catch (System.Exception ex)
            {
                // Surface only persistent errors; transient ones are
                // self-healing on the next tick.
                StatusMessage = "Refresh error: " + ex.Message;
            }
        }

        // Helper that swaps the contents of an ObservableCollection
        // without leaving it empty mid-update — avoids ItemsControl
        // flicker on each poll tick.
        private static void ReplaceCollection<T>(ObservableCollection<T> dest, System.Collections.Generic.IList<T> src)
        {
            dest.Clear();
            foreach (T item in src) dest.Add(item);
        }

        private async Task ToggleBanAsync(User user)
        {
            if (user == null) return;
            try
            {
                bool ok = await _app.Client.AdminBanUserAsync(user.Id, !user.IsBanned);
                if (ok)
                {
                    user.IsBanned = !user.IsBanned;
                    StatusMessage = $"{user.Username}: {(user.IsBanned ? "banned" : "unbanned")}.";
                    // Force the list to repaint by re-adding (User does
                    // not implement INotifyPropertyChanged).
                    int idx = Users.IndexOf(user);
                    if (idx >= 0) { Users.RemoveAt(idx); Users.Insert(idx, user); }
                }
            }
            catch (System.Exception ex) { StatusMessage = "Error: " + ex.Message; }
        }
    }
}
