using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace DiagnosticNP.Models
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

    [Table("ControlPoints")]
    public class ControlPoint
    {
        [PrimaryKey]
        public int Id { get; set; }
        public string Name { get; set; }
        public int Level { get; set; }
        public int? ParentId { get; set; }
        public string ParentPath { get; set; }
        public string FullPath { get; set; }
        public bool IsMeasurementPoint { get; set; }

        // Новое свойство для улучшенного поиска
        [Ignore]
        public string SearchKeywords { get; set; }
    }

    // Добавляем метод для инициализации поисковых ключевых слов
    public static class ControlPointExtensions
    {
        public static void InitializeSearchKeywords(this ControlPoint point)
        {
            if (point == null) return;

            var keywords = new List<string>();

            // Добавляем основные компоненты имени
            if (!string.IsNullOrEmpty(point.Name))
            {
                keywords.Add(point.Name.ToLowerInvariant());
                keywords.AddRange(point.Name.ToLowerInvariant().Split(' '));
            }

            // Добавляем компоненты полного пути
            if (!string.IsNullOrEmpty(point.FullPath))
            {
                keywords.Add(point.FullPath.ToLowerInvariant());
                var pathComponents = point.FullPath.Split('/');
                keywords.AddRange(pathComponents.Select(p => p.ToLowerInvariant()));
            }

            // Добавляем синонимы
            if (point.Name?.ToLowerInvariant().Contains("сушильная") == true)
            {
                keywords.Add("сушильная группа");
                keywords.Add("сушка");
            }

            if (point.Name?.ToLowerInvariant().Contains("цилиндр") == true)
            {
                keywords.Add("сушильный цилиндр");
                keywords.Add("барабан");
            }

            if (point.Name?.ToLowerInvariant().Contains("подшипник") == true)
            {
                keywords.Add("подшипник");
                keywords.Add("bearing");
            }

            point.SearchKeywords = string.Join(" ", keywords.Distinct());
        }
    }

    [Table("Measurements")]
    public class MeasurementData
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int ControlPointId { get; set; }
        public double VibrationSpeed { get; set; } // мм/с
        public double Temperature { get; set; } // °C
        public double Acceleration { get; set; } // м/с²
        public double Kurtosis { get; set; }
        public DateTime MeasurementTime { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}