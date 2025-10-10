using DiagnosticNP.Models;
using DiagnosticNP.Models.Vibrometer;
using DiagnosticNP.Services;
using DiagnosticNP.Services.Api;
using DiagnosticNP.Services.Bluetooth;
using DiagnosticNP.Services.Nfc;
using DiagnosticNP.Services.Repository;
using DiagnosticNP.Services.Utils;
using DiagnosticNP.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace DiagnosticNP.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly IApiService _apiService;
        private readonly IRepository _repository;
        private readonly INfcService _nfcService;

        private EquipmentNode _selectedNode;
        private string _searchText;
        private bool _isLoading;
        private string _statusMessage;
        private string _vibrometerDevice;
        private bool _isNfcInitialized;
        private CancellationTokenSource _nfcProcessingCancellation;

        // Свойства для управления TreeView через Behavior
        private bool _expandAll;
        private bool _collapseAll;
        private object _nodeToExpand;

        public MainViewModel()
        {
            _apiService = new MockApiService();
            _repository = new Repository();
            _nfcService = new NfcService();

            InitializeCommands();
            InitializeNfc();
            InitializeVibrometer();
            LoadEquipmentStructure();
        }

        public ObservableCollection<EquipmentNode> EquipmentNodes { get; } = new ObservableCollection<EquipmentNode>();

        public EquipmentNode SelectedNode
        {
            get => _selectedNode;
            set => SetProperty(ref _selectedNode, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                FilterTreeView(value);
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // Свойства для Behavior
        public bool ExpandAll
        {
            get => _expandAll;
            set => SetProperty(ref _expandAll, value);
        }

        public bool CollapseAll
        {
            get => _collapseAll;
            set => SetProperty(ref _collapseAll, value);
        }

        public object NodeToExpand
        {
            get => _nodeToExpand;
            set => SetProperty(ref _nodeToExpand, value);
        }

        public ICommand LoadEquipmentCommand { get; private set; }
        public ICommand UploadMeasurementsCommand { get; private set; }
        public ICommand TakeMeasurementCommand { get; private set; }
        public ICommand ClearMeasurementsCommand { get; private set; }
        public ICommand ExpandAllCommand { get; private set; }
        public ICommand CollapseAllCommand { get; private set; }

        private void InitializeCommands()
        {
            LoadEquipmentCommand = new Command(async () => await LoadEquipmentStructureFromApi());
            UploadMeasurementsCommand = new Command(async () => await UploadMeasurements());
            TakeMeasurementCommand = new Command(async () => await TakeMeasurement());
            ClearMeasurementsCommand = new Command(async () => await ClearMeasurements());
            ExpandAllCommand = new Command(() => ExpandAll = true);
            CollapseAllCommand = new Command(() => CollapseAll = true);
        }

        private async void LoadEquipmentStructure()
        {
            try
            {
                var nodes = await _repository.GetEquipmentNodesAsync();
                if (nodes?.Any() == true)
                {
                    BuildTreeView(nodes);
                    StatusMessage = $"Загружено из БД: {nodes.Count} узлов";
                }
                else
                {
                    StatusMessage = "Локальная структура не найдена";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки: {ex.Message}";
            }
        }

        private async Task LoadEquipmentStructureFromApi()
        {
            if (IsLoading) return;

            IsLoading = true;
            StatusMessage = "Загрузка структуры оборудования с сервера...";

            try
            {
                var nodes = await _apiService.GetEquipmentStructureAsync();

                if (nodes?.Any() == true)
                {
                    await _repository.SaveEquipmentNodesAsync(nodes);
                    BuildTreeView(nodes);
                    StatusMessage = $"Структура загружена. Узлов: {nodes.Count}";

                    await Application.Current.MainPage.DisplayAlert("Успех",
                        "Структура оборудования успешно загружена с сервера", "OK");
                }
                else
                {
                    StatusMessage = "Не удалось загрузить структуру оборудования";
                    await Application.Current.MainPage.DisplayAlert("Ошибка",
                        "Не удалось загрузить структуру оборудования с сервера", "OK");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки: {ex.Message}";
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    $"Ошибка при загрузке структуры оборудования: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void BuildTreeView(List<EquipmentNode> nodes)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    EquipmentNodes.Clear();

                    var rootNodes = nodes.Where(n => string.IsNullOrEmpty(n.ParentId)).ToList();

                    foreach (var node in rootNodes)
                    {
                        EquipmentNodes.Add(node);
                        BuildTreeRecursive(node, nodes);
                    }

                    OnPropertyChanged(nameof(EquipmentNodes));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"TreeView build error: {ex.Message}");
                }
            });
        }

        private void BuildTreeRecursive(EquipmentNode parentNode, List<EquipmentNode> allNodes)
        {
            var children = allNodes.Where(n => n.ParentId == parentNode.Id).ToList();

            foreach (var child in children)
            {
                parentNode.Children.Add(child);
                BuildTreeRecursive(child, allNodes);
            }
        }

        private void FilterTreeView(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return;
            }

            var searchLower = searchText.ToLowerInvariant().Trim();
            var matchingNodes = FindMatchingNodes(searchLower);

            if (matchingNodes.Any())
            {
                var firstMatch = matchingNodes.First();
                SelectedNode = firstMatch;
                NodeToExpand = firstMatch;
            }
        }

        private List<EquipmentNode> FindMatchingNodes(string searchText)
        {
            var matches = new List<EquipmentNode>();
            SearchNodes(EquipmentNodes, searchText, matches);
            return matches;
        }

        private void SearchNodes(IEnumerable<EquipmentNode> nodes, string searchText, List<EquipmentNode> matches)
        {
            foreach (var node in nodes)
            {
                var nodeName = node.Name?.ToLowerInvariant() ?? "";
                var nodePath = node.FullPath?.ToLowerInvariant() ?? "";

                if (nodeName.Contains(searchText) || nodePath.Contains(searchText))
                {
                    matches.Add(node);
                }

                SearchNodes(node.Children, searchText, matches);
            }
        }

        private async Task TakeMeasurement()
        {
            if (SelectedNode == null)
            {
                await Application.Current.MainPage.DisplayAlert("Внимание",
                    "Выберите точку замера в дереве оборудования", "OK");
                return;
            }

            if (!IsMeasurementPoint(SelectedNode))
            {
                await Application.Current.MainPage.DisplayAlert("Внимание",
                    "Выберите конкретную точку замера (Горизонтальная, Вертикальная, Осевая)", "OK");
                return;
            }

            var measurementVm = new MeasurementViewModel(SelectedNode, _repository, _vibrometerDevice);
            await Application.Current.MainPage.Navigation.PushAsync(new MeasurementPage(measurementVm));
        }

        private bool IsMeasurementPoint(EquipmentNode node)
        {
            return node.Name == "Горизонтальная" ||
                   node.Name == "Вертикальная" ||
                   node.Name == "Осевая";
        }

        private async Task UploadMeasurements()
        {
            IsLoading = true;
            StatusMessage = "Подготовка данных для выгрузки...";

            try
            {
                var pendingMeasurements = await _repository.GetPendingUploadMeasurementsAsync();

                if (!pendingMeasurements.Any())
                {
                    StatusMessage = "Нет данных для выгрузки";
                    await Application.Current.MainPage.DisplayAlert("Информация",
                        "Нет данных для выгрузки", "OK");
                    return;
                }

                StatusMessage = $"Выгрузка {pendingMeasurements.Count} замеров...";

                var success = await _apiService.UploadMeasurementsAsync(pendingMeasurements);

                if (success)
                {
                    foreach (var measurement in pendingMeasurements)
                    {
                        await _repository.MarkAsUploadedAsync(measurement.Id);
                    }

                    StatusMessage = $"Успешно выгружено {pendingMeasurements.Count} замеров";
                    await Application.Current.MainPage.DisplayAlert("Успех",
                        $"Данные успешно выгружены на сервер", "OK");
                }
                else
                {
                    StatusMessage = "Ошибка выгрузки данных";
                    await Application.Current.MainPage.DisplayAlert("Ошибка",
                        "Не удалось выгрузить данные на сервер", "OK");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка выгрузки: {ex.Message}";
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    $"Ошибка при выгрузке данных: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ClearMeasurements()
        {
            var result = await Application.Current.MainPage.DisplayAlert("Подтверждение",
                "Вы уверены, что хотите очистить все замеры?", "Да", "Нет");

            if (result)
            {
                try
                {
                    await _repository.ClearAllMeasurementsAsync();
                    StatusMessage = "Все замеры очищены";
                    await Application.Current.MainPage.DisplayAlert("Успех",
                        "Все замеры успешно очищены", "OK");
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Ошибка очистки: {ex.Message}";
                    await Application.Current.MainPage.DisplayAlert("Ошибка",
                        $"Ошибка при очистке замеров: {ex.Message}", "OK");
                }
            }
        }

        private async void InitializeNfc()
        {
            try
            {
                var isAvailable = await _nfcService.IsAvailableAsync();
                if (!isAvailable)
                {
                    StatusMessage = "NFC не поддерживается";
                    return;
                }

                var isEnabled = await _nfcService.IsEnabledAsync();
                if (!isEnabled)
                {
                    StatusMessage = "NFC отключен";
                    return;
                }

                _nfcService.TagScanned += OnTagScanned;
                _nfcService.StartListening();
                _isNfcInitialized = true;
                StatusMessage = "NFC сканирование активно";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка NFC: {ex.Message}";
            }
        }

        private async void OnTagScanned(object sender, string nfcData)
        {
            if (_nfcProcessingCancellation != null)
            {
                _nfcProcessingCancellation.Cancel();
                _nfcProcessingCancellation.Dispose();
            }

            _nfcProcessingCancellation = new CancellationTokenSource();

            try
            {
                await Task.Delay(100, _nfcProcessingCancellation.Token);

                Device.BeginInvokeOnMainThread(async () =>
                {
                    StatusMessage = $"NFC: {nfcData}";
                    await ProcessNfcTag(nfcData);
                });
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NFC processing error: {ex.Message}");
            }
        }

        private async Task ProcessNfcTag(string nfcData)
        {
            if (string.IsNullOrWhiteSpace(nfcData))
                return;

            var normalizedPath = NormalizePath(nfcData);
            var matchingNodes = FindMatchingNodesByPath(normalizedPath);

            if (matchingNodes.Any())
            {
                // Для всех найденных узлов разворачиваем путь
                foreach (var node in matchingNodes)
                {
                    NodeToExpand = node;
                    // Даем время на анимацию между узлами
                    await Task.Delay(300);
                }

                // Выбираем первый узел
                var firstMatch = matchingNodes.First();
                SelectedNode = firstMatch;

                await Application.Current.MainPage.DisplayAlert("NFC",
                    $"Найдено {matchingNodes.Count} точек. Раскрыт путь к: {firstMatch.FullPath}", "OK");
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("NFC",
                    $"Точка не найдена: {nfcData}", "OK");
            }
        }

        private string NormalizePath(string path)
        {
            return path.Trim()
                .Replace("/", "\\")
                .ToLowerInvariant()
                .Replace(" ", "")
                .Replace("\t", "");
        }

        private List<EquipmentNode> FindMatchingNodesByPath(string searchPath)
        {
            var matches = new List<EquipmentNode>();
            SearchNodesByPath(EquipmentNodes, searchPath, matches);
            return matches;
        }

        private void SearchNodesByPath(IEnumerable<EquipmentNode> nodes, string searchPath, List<EquipmentNode> matches)
        {
            foreach (var node in nodes)
            {
                var nodePath = NormalizePath(node.FullPath ?? "");

                if (nodePath.Contains(searchPath) || searchPath.Contains(nodePath))
                {
                    matches.Add(node);
                }

                SearchNodesByPath(node.Children, searchPath, matches);
            }
        }

        private void InitializeVibrometer()
        {
            try
            {
                if (!BluetoothController.LeScanner.IsRunning)
                    BluetoothController.LeScanner.Start();

                BluetoothController.LeScanner.NewData += OnVibrometerDataReceived;
                StatusMessage = "Сканирование виброметра запущено";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка инициализации виброметра: {ex.Message}";
            }
        }

        private void OnVibrometerDataReceived(object sender, BleDataEventArgs e)
        {
            Device.BeginInvokeOnMainThread(() => ProcessVibrometerData(e));
        }

        private void ProcessVibrometerData(BleDataEventArgs e)
        {
            try
            {
                _vibrometerDevice = e.Data.Address;
                var data = e.Data.Data.BytesToStruct<ViPenAdvertising>();

                StatusMessage = $"Виброметр обнаружен: {_vibrometerDevice}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Vibrometer data processing error: {ex.Message}");
            }
        }

        public void OnDisappearing()
        {
            try
            {
                BluetoothController.LeScanner.NewData -= OnVibrometerDataReceived;

                if (_isNfcInitialized)
                {
                    _nfcService.TagScanned -= OnTagScanned;
                    _nfcService.StopListening();
                }

                _nfcProcessingCancellation?.Cancel();
                _nfcProcessingCancellation?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cleanup error: {ex.Message}");
            }
        }
    }
}