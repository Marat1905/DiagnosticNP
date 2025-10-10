// OperationToken.cs
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DiagnosticNP.Models.Vibrometer
{
    public class OperationToken : INotifyPropertyChanged, IDisposable
    {
        private bool _disposed = false;
        private double _progress;

        public bool IsAborted { get; set; } = false;

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

        // Реализация IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Освобождаем управляемые ресурсы
                    // В данном случае PropertyChanged может содержать подписки,
                    // которые нужно очистить
                    PropertyChanged = null;
                }

                // Освобождаем неуправляемые ресурсы (если есть)

                _disposed = true;
            }
        }

        // Финализатор на случай, если Dispose не был вызван
        ~OperationToken()
        {
            Dispose(false);
        }
    }
}