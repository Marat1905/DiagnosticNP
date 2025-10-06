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

        public ControlPointsViewModel(IDatabaseService databaseService, string nfcFilter = null)
        {
            _databaseService = databaseService;
            _nfcFilter = nfcFilter;
            _filterInfo = string.IsNullOrEmpty(nfcFilter) ? "Нет активного фильтра" : $"Фильтр: {nfcFilter}";

            DisplayNodes = new ObservableCollection<ControlPointNode>();
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

        private void InitializeCommands()
        {
            AddMeasurementCommand = new Command<ControlPointNode>(async (node) => await AddMeasurement(node));
            ResetFilterCommand = new Command(ResetFilter);
            ToggleNodeCommand = new Command<ControlPointNode>((node) => ToggleNode(node));
        }

        public async void LoadData()
        {
            if (IsBusy) return;

            IsBusy = true;
            StatusMessage = "Загрузка данных...";

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
                var rootNodes = BuildTree(filteredPoints);

                // Обновление отображаемых узлов
                UpdateDisplayNodes(rootNodes);

                StatusMessage = $"Найдено {filteredPoints.Count} точек из {controlPoints.Count}";
                StatusColor = filteredPoints.Any() ? Color.Green : Color.Orange;

                if (!string.IsNullOrEmpty(_nfcFilter) && !filteredPoints.Any())
                {
                    StatusMessage = $"По запросу '{_nfcFilter}' ничего не найдено";
                    StatusColor = Color.Orange;
                }
            }
            catch (System.Exception ex)
            {
                StatusMessage = $"Ошибка загрузки: {ex.Message}";
                StatusColor = Color.Red;
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

            // Нормализуем NFC данные: убираем лишние пробелы, приводим к нижнему регистру
            var normalizedNfcData = nfcData.ToLowerInvariant()
                .Replace(":", "")
                .Replace(".", "")
                .Replace(",", "")
                .Trim();

            // Разбиваем на ключевые слова
            var keywords = normalizedNfcData.Split(new[] { ' ', '\t', '\n', '\r' },
                StringSplitOptions.RemoveEmptyEntries);

            if (keywords.Length == 0)
                return points;

            return points.Where(p =>
            {
                if (p?.FullPath == null) return false;

                // Нормализуем полный путь точки
                var normalizedPath = p.FullPath.ToLowerInvariant();

                // Проверяем, содержатся ли все ключевые слова в пути
                // Или путь содержит основную часть NFC данных
                return keywords.All(keyword => normalizedPath.Contains(keyword)) ||
                       normalizedPath.Contains(normalizedNfcData) ||
                       IsPartialMatch(normalizedPath, normalizedNfcData);
            }).ToList();
        }

        private bool IsPartialMatch(string path, string nfcData)
        {
            // Разбиваем путь на компоненты
            var pathComponents = path.Split('/');
            var nfcComponents = nfcData.Split(' ');

            // Проверяем совпадение основных компонентов
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

            // Считаем совпадением если больше половины компонентов совпало
            return matches >= nfcComponents.Length / 2;
        }

        private List<ControlPointNode> BuildTree(List<ControlPoint> points)
        {
            var nodes = new List<ControlPointNode>();

            // Сначала собираем все точки, которые подходят под фильтр
            var filteredPoints = points.ToList();

            // Затем добавляем их родителей для построения полного дерева
            var pointsWithParents = new List<ControlPoint>(filteredPoints);

            foreach (var point in filteredPoints)
            {
                AddParentChain(point, points, pointsWithParents);
            }

            // Строим дерево из полного набора точек
            var rootPoints = pointsWithParents.Where(p => p.Level == 0).ToList();

            foreach (var root in rootPoints)
            {
                var node = new ControlPointNode(root);
                BuildNodeChildren(node, pointsWithParents);
                nodes.Add(node);
            }

            return nodes;
        }

        private void AddParentChain(ControlPoint point, List<ControlPoint> allPoints, List<ControlPoint> result)
        {
            if (point.ParentId.HasValue)
            {
                var parent = allPoints.FirstOrDefault(p => p.Id == point.ParentId.Value);
                if (parent != null && !result.Contains(parent))
                {
                    result.Add(parent);
                    AddParentChain(parent, allPoints, result);
                }
            }
        }

        private List<ControlPoint> EnhancedNfcSearch(List<ControlPoint> points, string nfcData)
        {
            if (string.IsNullOrEmpty(nfcData))
                return points;

            var results = new List<ControlPoint>();
            var normalizedNfc = nfcData.ToLowerInvariant();

            // 1. Поиск точного совпадения в полном пути
            var exactMatches = points.Where(p =>
                p.FullPath?.ToLowerInvariant().Contains(normalizedNfc) == true).ToList();
            results.AddRange(exactMatches);

            // 2. Поиск по отдельным компонентам
            var components = normalizedNfc.Split(new[] { ' ', ':', ',', ';' },
                StringSplitOptions.RemoveEmptyEntries);

            foreach (var component in components)
            {
                var componentMatches = points.Where(p =>
                    p.Name?.ToLowerInvariant().Contains(component) == true ||
                    p.FullPath?.ToLowerInvariant().Contains(component) == true).ToList();

                results.AddRange(componentMatches.Where(m => !results.Contains(m)));
            }

            // 3. Поиск по синонимам и вариациям
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

            // Добавляем различные вариации поискового запроса
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

        private void BuildNodeChildren(ControlPointNode parent, List<ControlPoint> allPoints)
        {
            var children = allPoints.Where(p => p.ParentId == parent.ControlPoint.Id).ToList();

            foreach (var child in children)
            {
                var childNode = new ControlPointNode(child);
                BuildNodeChildren(childNode, allPoints);
                parent.Children.Add(childNode);
            }
        }

        private void UpdateDisplayNodes(List<ControlPointNode> rootNodes)
        {
            DisplayNodes.Clear();
            foreach (var node in rootNodes)
            {
                DisplayNodes.Add(node);
                if (node.IsExpanded)
                {
                    AddChildrenToDisplay(node);
                }
            }
        }

        private void AddChildrenToDisplay(ControlPointNode parent)
        {
            var index = DisplayNodes.IndexOf(parent) + 1;
            foreach (var child in parent.Children)
            {
                DisplayNodes.Insert(index, child);
                index++;
                if (child.IsExpanded)
                {
                    AddChildrenToDisplay(child);
                }
            }
        }

        private void RemoveChildrenFromDisplay(ControlPointNode parent)
        {
            foreach (var child in parent.Children)
            {
                DisplayNodes.Remove(child);
                if (child.IsExpanded)
                {
                    RemoveChildrenFromDisplay(child);
                }
            }
        }

        private void ToggleNode(ControlPointNode node)
        {
            if (node == null || !node.HasChildren) return;

            node.IsExpanded = !node.IsExpanded;

            if (node.IsExpanded)
            {
                AddChildrenToDisplay(node);
            }
            else
            {
                RemoveChildrenFromDisplay(node);
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
            set => SetProperty(ref _isExpanded, value);
        }

        public ControlPointNode(ControlPoint controlPoint)
        {
            ControlPoint = controlPoint;
            Children = new List<ControlPointNode>();
            // По умолчанию раскрываем первые два уровня для удобства
            IsExpanded = Level < 2;
        }
    }
}