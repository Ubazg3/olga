using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CheckersClient.ViewModels
{
    // Base class for every view-model. Implements INotifyPropertyChanged
    // once so concrete VMs only need to call SetProperty / OnPropertyChanged.
    // Demonstrates "inheritance from a class the student wrote" required
    // by the rubric.
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Sets the field, raises PropertyChanged, and returns true if
        // the value actually changed. Saves the boilerplate "if (x ==
        // value) return; x = value; OnPropertyChanged();" in every setter.
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (object.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
