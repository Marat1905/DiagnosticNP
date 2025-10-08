using System;
using System.ComponentModel;
using SQLite;

namespace DiagnosticNP.Models.Equipment
{
    public class MeasurementData : INotifyPropertyChanged
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string NodeId { get; set; }

        public DateTime MeasurementTime { get; set; }

        public double Velocity { get; set; }

        public double Acceleration { get; set; }

        public double Kurtosis { get; set; }

        public double Temperature { get; set; }

        public string Direction { get; set; }

        public bool IsManualEntry { get; set; }

        public bool IsSynced { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}