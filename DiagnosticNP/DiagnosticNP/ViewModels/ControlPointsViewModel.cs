using DiagnosticNP.Models;
using DiagnosticNP.Services;
using DiagnosticNP.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace DiagnosticNP.ViewModels
{
    public class ControlPointsViewModel : BaseViewModel
    {
        private readonly IDatabaseService _databaseService;
        private string _nfcFilter;
        private string _filterInfo;
        private List<ControlPointNode> _allNodes;
        private bool _isToggling = false;

        public ControlPointsViewModel(IDatabaseService databaseService, string nfcFilter = null)
        {
            _databaseService = databaseService;
            _nfcFilter = nfcFilter;
            _filterInfo = string.IsNullOrEmpty(nfcFilter) ? "Нет активного фильтра" : $"Фильтр: {nfcFilter}";

            DisplayNodes = new ObservableCollection<ControlPointNode>();
            _allNodes = new List<ControlPointNode>();
            InitializeCommands();
        }

        public ObservableCollection<ControlPointNode> DisplayNodes { get; }

        public string FilterInfo
        {
            get => _filterInfo;
            set => SetProperty(ref _filterInfo, value);
        }

        public ICommand AddMeasurementCommand { get; private set; }
        public ICommand ResetFilterCommand { get; private set; }
        public ICommand ToggleNodeCommand { get; private set; }
        public ICommand LoadDataCommand { get; private set; }

        private void InitializeCommands()
        {
            AddMeasurementCommand = new Command<ControlPointNode>(async (node) => await AddMeasurement(node));
            ResetFilterCommand = new Command(ResetFilter);
            ToggleNodeCommand = new Command<ControlPointNode>((node) => ToggleNode(node));
            LoadDataCommand = new Command(() => LoadData());
        }

        public async void LoadData()
        {
            if (IsBusy) return;

            IsBusy = true;
            StatusMessage = "Загрузка данных...";
            StatusColor = Color.FromHex("#3498DB");

            try
            {
                var controlPoints = await _databaseService.GetControlPointsAsync();

                // Используем улучшенный поиск
                List<ControlPoint> filteredPoints;
                if (string.IsNullOrEmpty(_nfcFilter))
                {
                    filteredPoints = controlPoints;
                }
                else
                {
                    filteredPoints = EnhancedNfcSearch(controlPoints, _nfcFilter);

                    // Если улучшенный поиск не дал результатов, пробуем простой
                    if (!filteredPoints.Any())
                    {
                        filteredPoints = FilterByNfcData(controlPoints, _nfcFilter);
                    }
                }

                // Построение дерева
                _allNodes = BuildTree(filteredPoints);

                // Обновление отображаемых узлов
                UpdateDisplayNodes();

                StatusMessage = $"Найдено {filteredPoints.Count} точек из {controlPoints.Count}";
                StatusColor = filteredPoints.Any() ? Color.FromHex("#27AE60") : Color.FromHex("#E67E22");

                if (!string.IsNullOrEmpty(_nfcFilter) && !filteredPoints.Any())
                {
                    StatusMessage = $"По запросу '{_nfcFilter}' ничего не найдено";
                    StatusColor = Color.FromHex("#E67E22");
                }
            }
            catch (System.Exception ex)
            {
                StatusMessage = $"Ошибка загрузки: {ex.Message}";
                StatusColor = Color.FromHex("#E74C3C");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private List<ControlPoint> FilterByNfcData(List<ControlPoint> points, string nfcData)
        {
            if (string.IsNullOrEmpty(nfcData))
                return points;

            var normalizedNfcData = nfcData.ToLowerInvariant()
                .Replace(":", "")
                .Replace(".", "")
                .Replace(",", "")
                .Trim();

            var keywords = normalizedNfcData.Split(new[] { ' ', '\t', '\n', '\r' },
                StringSplitOptions.RemoveEmptyEntries);

            if (keywords.Length == 0)
                return points;

            return points.Where(p =>
            {
                if (p?.FullPath == null) return false;

                var normalizedPath = p.FullPath.ToLowerInvariant();

                return keywords.All(keyword => normalizedPath.Contains(keyword)) ||
                       normalizedPath.Contains(normalizedNfcData) ||
                       IsPartialMatch(normalizedPath, normalizedNfcData);
            }).ToList();
        }

        private bool IsPartialMatch(string path, string nfcData)
        {
            var pathComponents = path.Split('/');
            var nfcComponents = nfcData.Split(' ');

            var matches = 0;
            foreach (var nfcComponent in nfcComponents)
            {
                if (pathComponents.Any(component =>
                    component.Contains(nfcComponent) ||
                    nfcComponent.Contains(component)))
                {
                    matches++;
                }
            }

            return matches >= nfcComponents.Length / 2;
        }

        private List<ControlPoint> EnhancedNfcSearch(List<ControlPoint> points, string nfcData)
        {
            if (string.IsNullOrEmpty(nfcData))
                return points;

            var results = new List<ControlPoint>();
            var normalizedNfc = nfcData.ToLowerInvariant();

            var exactMatches = points.Where(p =>
                p.FullPath?.ToLowerInvariant().Contains(normalizedNfc) == true).ToList();
            results.AddRange(exactMatches);

            var components = normalizedNfc.Split(new[] { ' ', ':', ',', ';' },
                StringSplitOptions.RemoveEmptyEntries);

            foreach (var component in components)
            {
                var componentMatches = points.Where(p =>
                    p.Name?.ToLowerInvariant().Contains(component) == true ||
                    p.FullPath?.ToLowerInvariant().Contains(component) == true).ToList();

                results.AddRange(componentMatches.Where(m => !results.Contains(m)));
            }

            var variations = GetSearchVariations(normalizedNfc);
            foreach (var variation in variations)
            {
                var variationMatches = points.Where(p =>
                    p.FullPath?.ToLowerInvariant().Contains(variation) == true).ToList();

                results.AddRange(variationMatches.Where(m => !results.Contains(m)));
            }

            return results.Distinct().ToList();
        }

        private List<string> GetSearchVariations(string searchTerm)
        {
            var variations = new List<string>();

            if (searchTerm.Contains("сушильная"))
            {
                variations.Add("сушильная группа");
                variations.Add("сушильная");
            }

            if (searchTerm.Contains("верхняя"))
            {
                variations.Add("верхняя сетка");
                variations.Add("верхняя");
            }

            if (searchTerm.Contains("цилиндр"))
            {
                variations.Add("сушильный цилиндр");
                variations.Add("цилиндр");
            }

            if (searchTerm.Contains("лицевой"))
            {
                variations.Add("лицевой подшипник");
                variations.Add("подшипник");
            }

            return variations;
        }

        private List<ControlPointNode> BuildTree(List<ControlPoint> points)
        {
            if (points == null || !points.Any())
                return new List<ControlPointNode>();

            // Сортируем точки по уровню и имени для правильного порядка
            var sortedPoints = points
                .OrderBy(p => p.Level)
                .ThenBy(p => p.Name)
                .ToList();

            var nodes = new List<ControlPointNode>();
            var rootPoints = sortedPoints.Where(p => p.Level == 0).ToList();

            foreach (var root in rootPoints)
            {
                var node = new ControlPointNode(root);
                BuildNodeChildren(node, sortedPoints);
                nodes.Add(node);
            }

            return nodes;
        }

        private void BuildNodeChildren(ControlPointNode parent, List<ControlPoint> allPoints)
        {
            var children = allPoints
                .Where(p => p.ParentId == parent.ControlPoint.Id)
                .OrderBy(p => p.Name)
                .ToList();

            foreach (var child in children)
            {
                var childNode = new ControlPointNode(child);
                BuildNodeChildren(childNode, allPoints);
                parent.Children.Add(childNode);
            }
        }

        private void UpdateDisplayNodes()
        {
            DisplayNodes.Clear();
            foreach (var node in _allNodes)
            {
                DisplayNodes.Add(node);
                if (node.IsExpanded && node.HasChildren)
                {
                    AddChildrenToDisplay(node);
                }
            }
        }

        private void AddChildrenToDisplay(ControlPointNode parent)
        {
            var parentIndex = DisplayNodes.IndexOf(parent);
            if (parentIndex == -1) return;

            var insertIndex = parentIndex + 1;

            // Добавляем детей в правильном порядке
            foreach (var child in parent.Children.OrderBy(c => c.Name))
            {
                // Проверяем, не добавлен ли уже этот узел
                if (!DisplayNodes.Contains(child))
                {
                    DisplayNodes.Insert(insertIndex, child);
                    insertIndex++;
                }

                // Рекурсивно добавляем раскрытых детей
                if (child.IsExpanded && child.HasChildren)
                {
                    AddChildrenToDisplay(child);
                    // Обновляем индекс после добавления вложенных элементов
                    insertIndex = DisplayNodes.IndexOf(child) + child.GetVisibleChildrenCount() + 1;
                }
            }
        }

        private void RemoveChildrenFromDisplay(ControlPointNode parent)
        {
            var childrenToRemove = new List<ControlPointNode>();
            CollectChildrenForRemoval(parent, childrenToRemove);

            // Удаляем в обратном порядке чтобы не нарушать индексы
            for (int i = childrenToRemove.Count - 1; i >= 0; i--)
            {
                var child = childrenToRemove[i];
                if (DisplayNodes.Contains(child))
                {
                    DisplayNodes.Remove(child);
                }
            }
        }

        private void CollectChildrenForRemoval(ControlPointNode parent, List<ControlPointNode> childrenToRemove)
        {
            foreach (var child in parent.Children)
            {
                childrenToRemove.Add(child);
                if (child.IsExpanded)
                {
                    CollectChildrenForRemoval(child, childrenToRemove);
                    // Сбрасываем состояние раскрытия у удаляемых детей
                    child.IsExpanded = false;
                }
            }
        }

        private void ToggleNode(ControlPointNode node)
        {
            if (node == null || !node.HasChildren || _isToggling)
                return;

            _isToggling = true;

            try
            {
                var wasExpanded = node.IsExpanded;
                node.IsExpanded = !node.IsExpanded;

                if (node.IsExpanded)
                {
                    // Добавляем детей
                    AddChildrenToDisplay(node);
                }
                else
                {
                    // Удаляем детей
                    RemoveChildrenFromDisplay(node);
                }

                // Принудительно обновляем интерфейс
                Device.BeginInvokeOnMainThread(() =>
                {
                    // Обновляем коллекцию для принудительного обновления UI
                    var tempList = new List<ControlPointNode>(DisplayNodes);
                    DisplayNodes.Clear();
                    foreach (var item in tempList)
                    {
                        DisplayNodes.Add(item);
                    }
                });
            }
            finally
            {
                _isToggling = false;
            }
        }

        private async Task AddMeasurement(ControlPointNode node)
        {
            if (node?.ControlPoint == null || !node.ControlPoint.IsMeasurementPoint)
                return;

            var measurementVM = new MeasurementViewModel(_databaseService, node.ControlPoint);
            var page = new MeasurementPage { BindingContext = measurementVM };

            await Application.Current.MainPage.Navigation.PushAsync(page);
        }

        private void ResetFilter()
        {
            _nfcFilter = null;
            FilterInfo = "Нет активного фильтра";
            LoadData();
        }
    }

    public class ControlPointNode : BaseViewModel
    {
        private bool _isExpanded;

        public ControlPoint ControlPoint { get; }
        public List<ControlPointNode> Children { get; }

        public string Name => ControlPoint?.Name ?? "";
        public string Path => ControlPoint?.FullPath ?? "";
        public int Level => ControlPoint?.Level ?? 0;
        public bool IsMeasurementPoint => ControlPoint?.IsMeasurementPoint ?? false;
        public bool HasChildren => Children?.Count > 0;

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged(nameof(IsExpanded));

                    // Принудительно обновляем отображение иконки
                    OnPropertyChanged(nameof(HasChildren));
                }
            }
        }

        public ControlPointNode(ControlPoint controlPoint)
        {
            ControlPoint = controlPoint;
            Children = new List<ControlPointNode>();
            // Раскрываем только корневые узлы по умолчанию
            IsExpanded = Level == 0;
        }

        public int GetVisibleChildrenCount()
        {
            var count = 0;
            foreach (var child in Children)
            {
                count++; // Сам ребенок
                if (child.IsExpanded)
                {
                    count += child.GetVisibleChildrenCount();
                }
            }
            return count;
        }

        // Переопределяем Equals и GetHashCode для правильной работы коллекций
        public override bool Equals(object obj)
        {
            return obj is ControlPointNode node &&
                   ControlPoint?.Id == node.ControlPoint?.Id;
        }

        public override int GetHashCode()
        {
            return ControlPoint?.Id.GetHashCode() ?? 0;
        }
    }
}