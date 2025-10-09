using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SQLite;

namespace DiagnosticNP.Models
{
    [Table("EquipmentNodes")]
    public class EquipmentNode : INotifyPropertyChanged
    {
        private string _name;
        private bool _isExpanded;
        private bool _isSelected;

        [PrimaryKey]
        public string Id { get; set; }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public string FullPath { get; set; }
        public string ParentId { get; set; }
        public NodeType Type { get; set; }

        // Убираем Children из хранения в БД
        [Ignore]
        public List<EquipmentNode> Children { get; set; } = new List<EquipmentNode>();

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                OnPropertyChanged();
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum NodeType
    {
        Equipment,
        Component,
        MeasurementPoint,
        Direction
    }
}