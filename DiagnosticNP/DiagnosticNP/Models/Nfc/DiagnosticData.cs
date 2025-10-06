using System;
using System.ComponentModel;

namespace DiagnosticNP.Models.Nfc
{
    public class DiagnosticData : INotifyPropertyChanged
    {
        private string _nfcData;
        private DateTime _scanTime;

        public string NFCData
        {
            get => _nfcData;
            set
            {
                _nfcData = value;
                OnPropertyChanged(nameof(NFCData));
            }
        }

        public DateTime ScanTime
        {
            get => _scanTime;
            set
            {
                _scanTime = value;
                OnPropertyChanged(nameof(ScanTime));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}