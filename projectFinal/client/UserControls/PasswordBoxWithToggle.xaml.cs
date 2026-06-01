using System.Windows;
using System.Windows.Controls;

namespace CheckersClient.UserControls
{
    // Hybrid PasswordBox / TextBox with an eye toggle. Existing
    // call-sites read .Password right before invoking a command,
    // matching the pattern the login & profile screens already use.
    public partial class PasswordBoxWithToggle : UserControl
    {
        // Tracks whether the plain-text TextBox is the currently
        // visible surface. We keep both controls' values in sync at
        // all times so reading .Password is always cheap.
        private bool _isVisible;
        private bool _suppressSync;

        public PasswordBoxWithToggle()
        {
            InitializeComponent();
        }

        // The current secret value. Reads from whichever surface is
        // currently authoritative (PasswordBox by default).
        public string Password
        {
            get
            {
                return _isVisible ? PART_Plain.Text : PART_Password.Password;
            }
            set
            {
                _suppressSync = true;
                PART_Password.Password = value ?? string.Empty;
                PART_Plain.Text       = value ?? string.Empty;
                _suppressSync = false;
            }
        }

        // Lets callers move focus to the field programmatically
        // (e.g. clear-error → re-focus).
        public void FocusInput()
        {
            if (_isVisible) PART_Plain.Focus();
            else            PART_Password.Focus();
        }

        // Clear the field, e.g. after a successful submit.
        public void Clear()
        {
            _suppressSync = true;
            PART_Password.Clear();
            PART_Plain.Clear();
            _suppressSync = false;
        }

        // ----- internal sync handlers -----

        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_suppressSync) return;
            _suppressSync = true;
            PART_Plain.Text = PART_Password.Password;
            _suppressSync = false;
        }

        private void OnPlainChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressSync) return;
            _suppressSync = true;
            PART_Password.Password = PART_Plain.Text;
            _suppressSync = false;
        }

        private void OnToggle(object sender, RoutedEventArgs e)
        {
            _isVisible = !_isVisible;
            if (_isVisible)
            {
                PART_Plain.Visibility       = Visibility.Visible;
                PART_Password.Visibility    = Visibility.Collapsed;
                PART_IconOpen.Visibility    = Visibility.Collapsed;
                PART_IconClosed.Visibility  = Visibility.Visible;
                PART_Toggle.ToolTip         = "Hide password";
                PART_Plain.CaretIndex       = PART_Plain.Text.Length;
                PART_Plain.Focus();
            }
            else
            {
                PART_Plain.Visibility       = Visibility.Collapsed;
                PART_Password.Visibility    = Visibility.Visible;
                PART_IconOpen.Visibility    = Visibility.Visible;
                PART_IconClosed.Visibility  = Visibility.Collapsed;
                PART_Toggle.ToolTip         = "Show password";
                PART_Password.Focus();
            }
        }

        // Light up the outer border when either surface has focus —
        // matches StyledTextBox / StyledPasswordBox behaviour.
        private void OnAnyFocus(object sender, RoutedEventArgs e)
        {
            OuterBorder.BorderBrush =
                (System.Windows.Media.Brush)FindResource("AccentBrush");
        }

        private void OnAnyBlur(object sender, RoutedEventArgs e)
        {
            OuterBorder.BorderBrush =
                (System.Windows.Media.Brush)FindResource("DividerBrush");
        }
    }
}
