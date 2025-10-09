using SQLite;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DiagnosticNP.Models
{
    [Table("ControlPoints")]
    public class ControlPoint : INotifyPropertyChanged
    {
        private string _name;
        private bool _isExpanded;
        private bool _isSelected;
        private string _fullPath;
        private string _measurementType;
        private bool _hasMeasurements;

        [PrimaryKey] // УБРАН AutoIncrement - мы сами управляем ID
        public int Id { get; set; }

        public int? ParentId { get; set; }

        [MaxLength(500)]
        public string FullPath
        {
            get => _fullPath;
            set => SetProperty(ref _fullPath, value);
        }

        [MaxLength(200)]
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        [MaxLength(50)]
        public string MeasurementType
        {
            get => _measurementType;
            set => SetProperty(ref _measurementType, value);
        }

        public bool HasMeasurements
        {
            get => _hasMeasurements;
            set => SetProperty(ref _hasMeasurements, value);
        }

        [Ignore]
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        [Ignore]
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        [Ignore]
        public ObservableCollection<ControlPoint> Children { get; set; } = new ObservableCollection<ControlPoint>();

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public void AddChild(ControlPoint child)
        {
            if (child == null) return;

            Children.Add(child);
            OnPropertyChanged(nameof(Children));
        }

        public override string ToString()
        {
            return $"{Name} (Id: {Id}, ParentId: {ParentId}, Path: {FullPath})";
        }
    }
}