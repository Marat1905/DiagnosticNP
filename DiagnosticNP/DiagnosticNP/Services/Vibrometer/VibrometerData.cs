// VibrometerData.cs
using System;

namespace DiagnosticNP.Services.Vibrometer
{
    public class VibrometerData
    {
        public DateTime Timestamp { get; set; }
        public double Velocity { get; set; }
        public double Acceleration { get; set; }
        public double Kurtosis { get; set; }
        public double Temperature { get; set; }
        public string DeviceAddress { get; set; }
        public DataSource Source { get; set; }
    }

    public enum DataSource
    {
        Advertising,
        Polling
    }
}