using System;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CheckersClient.Helpers;
using Model;

namespace CheckersClient.ViewModels
{
    // Profile screen. Same view used for both "look at someone else"
    // and "edit my own profile" — we toggle into edit mode only when
    // the user is viewing their own profile.
    public class ProfileViewModel : ViewModelBase
    {
        private readonly AppViewModel _app;
        private readonly int _targetUserId;

        public ProfileViewModel(AppViewModel app, int targetUserId)
        {
            _app          = app;
            _targetUserId = targetUserId;

            BackCommand           = new RelayCommand(_ => _app.GoToLobby());
            OpenReplayCommand     = new RelayCommand(o =>
            {
                if (o is GameRow row && row.GameId > 0)
                    _app.GoToReplay(row.GameId, row.WhiteName, row.BlackName);
            });
            BeginEditCommand      = new RelayCommand(_ => StartEdit(), _ => IsMine && !IsEditing);
            CancelEditCommand     = new RelayCommand(_ => CancelEdit(), _ => IsEditing);
            SaveProfileCommand    = new RelayCommand(async _ => await SaveAsync(), _ => IsEditing && !IsBusy);
            ChoosePictureCommand  = new RelayCommand(_ => PickPicture(),  _ => IsEditing);
            RemovePictureCommand  = new RelayCommand(_ => RemovePicture(),
                _ => IsEditing && EditPictureBytes != null && EditPictureBytes.Length > 0);

            _ = LoadAsync();
        }

        // ----- bindable state -----

        public ObservableCollection<GameRow> RecentGames { get; } = new ObservableCollection<GameRow>();

        private User _user;
        public  User User { get { return _user; } set { SetProperty(ref _user, value); } }

        public bool IsMine =>
            User != null && _app.Client.CurrentUser != null
            && User.Id == _app.Client.CurrentUser.Id;

        public bool IsAdmin => User != null && User.Role == UserRole.Admin;

        // Win-rate as a percentage string ("56%" / "—" if no games).
        public string WinRate
        {
            get
            {
                if (User == null) return "—";
                int total = User.Wins + User.Losses + User.Draws;
                if (total == 0) return "—";
                int pct = (int)Math.Round(100.0 * User.Wins / total);
                return pct + "%";
            }
        }

        public string MemberSinceText
        {
            get
            {
                if (User == null || User.CreatedAt == DateTime.MinValue) return "";
                return "Member since " + User.CreatedAt.ToString("dd MMM yyyy");
            }
        }

        // ----- edit-mode state -----

        private bool _isEditing;
        public  bool IsEditing { get { return _isEditing; } private set { SetProperty(ref _isEditing, value); } }

        private bool _isBusy;
        public  bool IsBusy { get { return _isBusy; } private set { SetProperty(ref _isBusy, value); } }

        private string _editEmail = "";
        public  string EditEmail { get { return _editEmail; } set { SetProperty(ref _editEmail, value); } }

        // Picture buffer the avatar binds to. While viewing, this just
        // mirrors User.ProfilePicture; while editing, it tracks the
        // user's pending choice (choose/remove) before they hit Save.
        private byte[] _editPictureBytes;
        public  byte[] EditPictureBytes
        {
            get { return _editPictureBytes; }
            set { SetProperty(ref _editPictureBytes, value); }
        }
        private bool _pictureChanged;

        // PasswordBox.Password isn't bindable; the view pushes them in
        // before invoking the Save command (same pattern as login).
        public string CurrentPassword { get; set; } = "";
        public string NewPassword     { get; set; } = "";
        public string ConfirmPassword { get; set; } = "";

        private string _statusMessage;
        public  string StatusMessage { get { return _statusMessage; } set { SetProperty(ref _statusMessage, value); } }

        private bool _isError;
        public  bool IsError { get { return _isError; } set { SetProperty(ref _isError, value); } }

        // ----- commands -----

        public ICommand BackCommand           { get; }
        public ICommand BeginEditCommand      { get; }
        public ICommand CancelEditCommand     { get; }
        public ICommand SaveProfileCommand    { get; }
        public ICommand ChoosePictureCommand  { get; }
        public ICommand RemovePictureCommand  { get; }
        public ICommand OpenReplayCommand     { get; }

        // ----- ops -----

        private async Task LoadAsync()
        {
            try
            {
                IsBusy = true;
                UserProfileDto dto = await _app.Client.GetUserProfileAsync(_targetUserId);
                if (dto == null) { Show("User not found.", true); return; }

                User = dto.User;
                EditEmail        = User.Email ?? string.Empty;
                EditPictureBytes = User.ProfilePicture;
                _pictureChanged  = false;

                RecentGames.Clear();
                int myId = User.Id;
                foreach (GameRecord g in dto.RecentGames)
                {
                    int oppId = g.WhitePlayerId == myId ? g.BlackPlayerId : g.WhitePlayerId;
                    string oppName = NameOf(dto.Usernames, oppId);
                    string whiteName = NameOf(dto.Usernames, g.WhitePlayerId);
                    string blackName = NameOf(dto.Usernames, g.BlackPlayerId);
                    RecentGames.Add(new GameRow(g, myId, oppName, whiteName, blackName));
                }
                OnPropertyChanged(nameof(WinRate));
                OnPropertyChanged(nameof(MemberSinceText));
                OnPropertyChanged(nameof(IsMine));
                OnPropertyChanged(nameof(IsAdmin));
            }
            catch (Exception ex) { Show(ex.Message, true); }
            finally { IsBusy = false; }
        }

        private void StartEdit()
        {
            IsEditing       = true;
            CurrentPassword = NewPassword = ConfirmPassword = "";
            StatusMessage   = "";
        }

        private void CancelEdit()
        {
            IsEditing = false;
            EditEmail        = User?.Email ?? string.Empty;
            EditPictureBytes = User?.ProfilePicture;
            _pictureChanged  = false;
            CurrentPassword  = NewPassword = ConfirmPassword = "";
            StatusMessage    = "";
        }

        private void PickPicture()
        {
            string path = ImageProcessor.PickImageFile();
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                EditPictureBytes = ImageProcessor.LoadAndProcess(path);
                _pictureChanged  = true;
                Show("Image ready — click Save to upload.", false);
            }
            catch (Exception ex)
            {
                Show("Could not read that image: " + ex.Message, true);
            }
        }

        private void RemovePicture()
        {
            EditPictureBytes = null;
            _pictureChanged  = true;
        }

        private async Task SaveAsync()
        {
            IsBusy = true;
            try
            {
                bool wantsPasswordChange =
                    !string.IsNullOrEmpty(CurrentPassword) ||
                    !string.IsNullOrEmpty(NewPassword) ||
                    !string.IsNullOrEmpty(ConfirmPassword);

                if (wantsPasswordChange)
                {
                    if (NewPassword != ConfirmPassword)
                    { Show("New passwords don't match.", true); return; }
                    if (string.IsNullOrEmpty(CurrentPassword))
                    { Show("Enter your current password.", true); return; }
                    try
                    {
                        await _app.Client.ChangeMyPasswordAsync(CurrentPassword, NewPassword);
                    }
                    catch (FaultException fx)
                    { Show(fx.Message, true); return; }
                }

                // Email is always sent — server stores whatever we send,
                // including empty. Cheap call, not worth conditioning.
                await _app.Client.UpdateMyEmailAsync(EditEmail ?? string.Empty);
                if (User != null) User.Email = EditEmail;

                if (_pictureChanged)
                {
                    await _app.Client.UpdateMyProfilePictureAsync(EditPictureBytes);
                    if (User != null) User.ProfilePicture = EditPictureBytes;
                    _pictureChanged = false;
                }

                IsEditing = false;
                CurrentPassword = NewPassword = ConfirmPassword = "";
                Show("Profile saved.", false);
            }
            catch (Exception ex) { Show("Could not save: " + ex.Message, true); }
            finally { IsBusy = false; }
        }

        private void Show(string msg, bool error)
        {
            StatusMessage = msg;
            IsError = error;
        }

        // Username lookup with a fallback so unresolved players don't
        // crash the UI — they just show "#42" until the next refresh.
        private static string NameOf(System.Collections.Generic.Dictionary<int, string> map, int id)
        {
            if (map != null && map.TryGetValue(id, out var name)) return name;
            return "#" + id;
        }
    }

    // Row in the recent-games list. Annotates each game with whether
    // the profile owner won/lost/drew it and the opponent's username.
    public class GameRow
    {
        public int      GameId     { get; }
        public DateTime When       { get; }
        public string   Opponent   { get; }
        public string   WhiteName  { get; }
        public string   BlackName  { get; }
        public string   Outcome    { get; }
        public string   OutcomeColor { get; }     // "Success" / "Danger" / "Info"
        public int      MoveCount  { get; }

        public GameRow(GameRecord g, int myUserId, string opponentName,
                       string whiteName, string blackName)
        {
            GameId      = g.Id;
            When        = g.StartedAt;
            Opponent    = opponentName;
            WhiteName   = whiteName;
            BlackName   = blackName;
            MoveCount   = g.MoveCount;

            if (g.Status == GameStatus.Draw)
            { Outcome = "Draw"; OutcomeColor = "Info"; }
            else if (g.WinnerId.HasValue && g.WinnerId.Value == myUserId)
            { Outcome = "Win"; OutcomeColor = "Success"; }
            else if (g.WinnerId.HasValue)
            { Outcome = "Loss"; OutcomeColor = "Danger"; }
            else
            { Outcome = g.Status.ToString(); OutcomeColor = "Muted"; }
        }
    }
}
