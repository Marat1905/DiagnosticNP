using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace DiagnosticNP.ViewModels
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        private bool _isBusy;
        private string _statusMessage;
        private Color _statusColor;

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                SetProperty(ref _statusMessage, value);
                OnPropertyChanged(nameof(HasStatusMessage));
            }
        }

        public Color StatusColor
        {
            get => _statusColor;
            set => SetProperty(ref _statusColor, value);
        }

        public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value,
            [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected async Task ShowAlert(string title, string message, string cancel = "OK")
        {
            await Application.Current.MainPage.DisplayAlert(title, message, cancel);
        }
    }
}