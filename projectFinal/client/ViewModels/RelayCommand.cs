using System;
using System.Windows.Input;

namespace CheckersClient.ViewModels
{
    // Generic ICommand backed by two delegates. Every Button / MenuItem
    // in the UI binds to a RelayCommand on its view-model.
    //
    // RelayCommand inherits no behaviour itself, but it is consumed by
    // the views via WPF's built-in Command/CommandParameter system,
    // which fulfils the "advanced event handling with delegates"
    // extension item from the rubric.
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute    = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public RelayCommand(Action execute, Func<bool> canExecute = null)
            : this(_ => execute(), canExecute == null ? (Predicate<object>)null : _ => canExecute())
        {
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        // Forwards CommandManager so WPF re-queries CanExecute when the
        // user interacts with anything (focus changes, keystrokes, etc.).
        public event EventHandler CanExecuteChanged
        {
            add    { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
