using SQLite;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DiagnosticNP.Models
{
    [Table("EquipmentNode")]
    public class EquipmentNode : INotifyPropertyChanged
    {
        private string _id;
        private string _name;
        private string _fullPath;
        private string _parentId;
        private bool _isExpanded;
        private bool _isSelected;

        [PrimaryKey]
        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string FullPath
        {
            get => _fullPath;
            set => SetProperty(ref _fullPath, value);
        }

        public string ParentId
        {
            get => _parentId;
            set => SetProperty(ref _parentId, value);
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
        public ObservableRangeCollection<EquipmentNode> Children { get; set; } = new ObservableRangeCollection<EquipmentNode>();

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
    }
}