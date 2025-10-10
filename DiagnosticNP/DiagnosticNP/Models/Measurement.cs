using SQLite;
using System;

namespace DiagnosticNP.Models
{
    [Table("Measurements")]
    public class Measurement
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string EquipmentNodeId { get; set; }
        public string MeasurementType { get; set; } // Горизонтальная, Вертикальная, Осевая
        public double Velocity { get; set; } // мм/с
        public double Temperature { get; set; } // °C
        public double Acceleration { get; set; } // м/с²
        public double Kurtosis { get; set; }
        public DateTime MeasurementTime { get; set; }
        public bool IsUploaded { get; set; }
        public string CreatedBy { get; set; }

        [Ignore]
        public string EquipmentPath { get; set; }

        [Ignore]
        public string DisplayMeasurementTime => MeasurementTime.ToString("dd.MM.yyyy HH:mm");
    }
}