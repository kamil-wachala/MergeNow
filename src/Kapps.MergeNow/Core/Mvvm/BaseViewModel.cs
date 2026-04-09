using MergeNow.Core.Mvvm.Commands;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MergeNow.Core.Mvvm
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetValue<T>(ref T storage, T value, [CallerMemberName] string name = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(name);
            return true;
        }

        protected void LinkToViewModel(IBaseCommand command)
        {
            if (command == null)
            {
                return;
            }

            PropertyChanged += (s, e) => command.RaiseCanExecuteChanged();
        }

        protected void LinkToViewModel<T>(ObservableCollection<T> collection)
        {
            if (collection == null)
            {
                return;
            }

            collection.CollectionChanged += (s, e) => RaisePropertyChanged();
        }

        protected void RaisePropertyChanged(string name = "")
        {
            OnPropertyChanged(name);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
