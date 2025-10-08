using System.Collections.Generic;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;
using SQLite;

namespace DiagnosticNP.Models.Equipment
{
    public class EquipmentNode : INotifyPropertyChanged
    {
        [PrimaryKey]
        public string Id { get; set; }

        public string ParentId { get; set; }

        public string Name { get; set; }

        public string FullPath { get; set; }

        public NodeType NodeType { get; set; }

        public string NfcFilter { get; set; }

        public bool HasChildren { get; set; }

        public int Level { get; set; }

        private ObservableCollection<EquipmentNode> _children = new ObservableCollection<EquipmentNode>();
        [Ignore]
        public ObservableCollection<EquipmentNode> Children
        {
            get => _children;
            set
            {
                _children = value;
                OnPropertyChanged(nameof(Children));
                HasChildren = _children?.Count > 0;
            }
        }

        private bool _isExpanded;
        [Ignore]
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                OnPropertyChanged(nameof(IsExpanded));
            }
        }

        private bool _isSelected;
        [Ignore]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return Name;
        }

        // Метод для построения полного пути
        public void BuildFullPath(List<EquipmentNode> allNodes)
        {
            var path = new List<string>();
            var currentNode = this;

            while (currentNode != null)
            {
                path.Insert(0, currentNode.Name);
                currentNode = allNodes.FirstOrDefault(n => n.Id == currentNode.ParentId);
            }

            FullPath = string.Join(" \\ ", path);
        }

        // Метод для поиска узла по пути
        public EquipmentNode FindNodeByPath(string[] pathParts, int currentIndex = 0)
        {
            if (currentIndex >= pathParts.Length)
                return null;

            var currentPart = pathParts[currentIndex].Trim();

            // Если текущий узел соответствует текущей части пути
            if (Name.Equals(currentPart, System.StringComparison.OrdinalIgnoreCase))
            {
                if (currentIndex == pathParts.Length - 1)
                {
                    // Это последняя часть пути - нашли целевой узел
                    return this;
                }
                else
                {
                    // Ищем следующую часть пути среди детей
                    foreach (var child in Children)
                    {
                        var found = child.FindNodeByPath(pathParts, currentIndex + 1);
                        if (found != null)
                            return found;
                    }
                }
            }

            // Если текущий узел не соответствует, ищем среди детей
            foreach (var child in Children)
            {
                var found = child.FindNodeByPath(pathParts, currentIndex);
                if (found != null)
                    return found;
            }

            return null;
        }

        // Метод для получения пути к этому узлу
        public List<EquipmentNode> GetPathToNode(List<EquipmentNode> allNodes)
        {
            var path = new List<EquipmentNode>();
            var currentNode = this;

            while (currentNode != null)
            {
                path.Insert(0, currentNode);
                currentNode = allNodes.FirstOrDefault(n => n.Id == currentNode.ParentId);
            }

            return path;
        }
    }

    public enum NodeType
    {
        Equipment,
        Section,
        Component,
        MeasurementPoint,
        Direction
    }
}