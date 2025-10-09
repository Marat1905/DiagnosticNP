using System;
using System.ComponentModel;
using SQLite;

namespace DiagnosticNP.Models
{
    public class Measurement : INotifyPropertyChanged
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int ControlPointId { get; set; }
        public string ControlPointPath { get; set; }
        public DateTime MeasurementTime { get; set; }

        private double _velocity;
        public double Velocity
        {
            get => _velocity;
            set
            {
                _velocity = value;
                OnPropertyChanged(nameof(Velocity));
            }
        }

        private double _temperature;
        public double Temperature
        {
            get => _temperature;
            set
            {
                _temperature = value;
                OnPropertyChanged(nameof(Temperature));
            }
        }

        private double _acceleration;
        public double Acceleration
        {
            get => _acceleration;
            set
            {
                _acceleration = value;
                OnPropertyChanged(nameof(Acceleration));
            }
        }

        private double _kurtosis;
        public double Kurtosis
        {
            get => _kurtosis;
            set
            {
                _kurtosis = value;
                OnPropertyChanged(nameof(Kurtosis));
            }
        }

        public string MeasurementType { get; set; } // Horizontal, Vertical, Axial
        public bool IsAutoMeasurement { get; set; }
        public string DeviceAddress { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}