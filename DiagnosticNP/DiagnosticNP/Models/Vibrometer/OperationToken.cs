using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DiagnosticNP.Models.Vibrometer
{
    public class OperationToken : INotifyPropertyChanged
    {
        public bool IsAborted { get; set; } = false;

        private double _progress;
        public double Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}