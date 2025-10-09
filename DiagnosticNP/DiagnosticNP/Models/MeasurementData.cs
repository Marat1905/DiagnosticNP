using System;
using SQLite;

namespace DiagnosticNP.Models
{
    [Table("Measurements")]
    public class MeasurementData
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string NodeId { get; set; }
        public string NodePath { get; set; }
        public string Direction { get; set; }

        public double Velocity { get; set; } // мм/с
        public double Temperature { get; set; } // °C
        public double Acceleration { get; set; } // м/с²
        public double Kurtosis { get; set; }

        public DateTime MeasurementTime { get; set; }
        public bool IsManualEntry { get; set; }
        public bool IsSynced { get; set; }
        public string DeviceId { get; set; }

        [Ignore]
        public bool HasData => Velocity > 0 || Temperature > 0 || Acceleration > 0 || Kurtosis > 0;
    }
}