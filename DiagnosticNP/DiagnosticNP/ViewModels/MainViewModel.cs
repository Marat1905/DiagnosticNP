using DiagnosticNP.Data;
using DiagnosticNP.Models;
using DiagnosticNP.Models.Vibrometer;
using DiagnosticNP.Services;
using DiagnosticNP.Services.Bluetooth;
using DiagnosticNP.Services.Nfc;
using DiagnosticNP.Services.Utils;
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
    public class MainViewModel : BaseViewModel
    {
        private readonly IApiService _apiService;
        private readonly IEquipmentRepository _equipmentRepository;
        private readonly IMeasurementRepository _measurementRepository;
        private readonly INfcService _nfcService;

        private EquipmentNode _selectedNode;
        private string _nfcFilter;
        private bool _isLoading;
        private string _vibrometerDevice;

        // Данные виброметра
        private DateTime _lastAdvertising;
        private double _velocity;
        private double _acceleration;
        private double _kurtosis;
        private double _temperature;
        private bool _isPollingVibrometer;

        public MainViewModel()
        {
            _apiService = new MockApiService();
            _equipmentRepository = new EquipmentRepository();
            _measurementRepository = new MeasurementRepository();
            _nfcService = new NfcService();

            EquipmentNodes = new ObservableCollection<EquipmentNode>();
            FilteredNodes = new ObservableCollection<EquipmentNode>();

            InitializeCommands();
            LoadEquipmentStructure();
            InitializeNfc();
            InitializeVibrometer();
        }

        public ObservableCollection<EquipmentNode> EquipmentNodes { get; }
        public ObservableCollection<EquipmentNode> FilteredNodes { get; }

        public EquipmentNode SelectedNode
        {
            get => _selectedNode;
            set
            {
                SetProperty(ref _selectedNode, value);
                OnPropertyChanged(nameof(IsDirectionSelected));
                if (value?.Type == NodeType.Direction)
                {
                    ShowMeasurementPage(value);
                }
            }
        }

        public string NfcFilter
        {
            get => _nfcFilter;
            set
            {
                SetProperty(ref _nfcFilter, value);
                ApplyNfcFilter();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsDirectionSelected => SelectedNode?.Type == NodeType.Direction;

        // Свойства виброметра
        public DateTime LastAdvertising
        {
            get => _lastAdvertising;
            set => SetProperty(ref _lastAdvertising, value);
        }

        public double Velocity
        {
            get => _velocity;
            set => SetProperty(ref _velocity, value);
        }

        public double Acceleration
        {
            get => _acceleration;
            set => SetProperty(ref _acceleration, value);
        }

        public double Kurtosis
        {
            get => _kurtosis;
            set => SetProperty(ref _kurtosis, value);
        }

        public double Temperature
        {
            get => _temperature;
            set => SetProperty(ref _temperature, value);
        }

        public bool IsPollingVibrometer
        {
            get => _isPollingVibrometer;
            set
            {
                SetProperty(ref _isPollingVibrometer, value);
                OnPropertyChanged(nameof(IsNotPollingVibrometer));
            }
        }

        public bool IsNotPollingVibrometer => !IsPollingVibrometer;

        public ICommand LoadControlPointsCommand { get; private set; }
        public ICommand UploadDataCommand { get; private set; }
        public ICommand ClearDataCommand { get; private set; }

        private void InitializeCommands()
        {
            LoadControlPointsCommand = new Command(async () => await LoadControlPointsAsync());
            UploadDataCommand = new Command(async () => await UploadDataAsync());
            ClearDataCommand = new Command(async () => await ClearDataAsync());
        }

        private async void LoadEquipmentStructure()
        {
            var nodes = await _equipmentRepository.GetEquipmentStructureAsync();
            if (nodes.Any())
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    EquipmentNodes.Clear();
                    foreach (var node in nodes)
                    {
                        EquipmentNodes.Add(node);
                    }
                    ApplyNfcFilter();
                });
            }
        }

        private async Task LoadControlPointsAsync()
        {
            IsLoading = true;
            try
            {
                var controlPoints = await _apiService.DownloadControlPointsAsync();
                if (controlPoints != null && controlPoints.Any())
                {
                    await _equipmentRepository.SaveEquipmentStructureAsync(controlPoints);

                    Device.BeginInvokeOnMainThread(() =>
                    {
                        EquipmentNodes.Clear();
                        foreach (var node in controlPoints)
                        {
                            EquipmentNodes.Add(node);
                        }
                        ApplyNfcFilter();
                    });

                    await Application.Current.MainPage.DisplayAlert("Успех",
                        "Контрольные точки успешно загружены", "OK");
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Ошибка",
                        "Не удалось загрузить контрольные точки", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    $"Ошибка загрузки: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task UploadDataAsync()
        {
            IsLoading = true;
            try
            {
                var unsyncedMeasurements = await _measurementRepository.GetUnsyncedMeasurementsAsync();
                if (unsyncedMeasurements.Any())
                {
                    var success = await _apiService.UploadMeasurementsAsync(unsyncedMeasurements);
                    if (success)
                    {
                        foreach (var measurement in unsyncedMeasurements)
                        {
                            await _measurementRepository.MarkAsSyncedAsync(measurement.Id);
                        }

                        await Application.Current.MainPage.DisplayAlert("Успех",
                            $"Данные успешно выгружены ({unsyncedMeasurements.Count} замеров)", "OK");
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert("Ошибка",
                            "Не удалось выгрузить данные", "OK");
                    }
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Информация",
                        "Нет данных для выгрузки", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    $"Ошибка выгрузки: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ClearDataAsync()
        {
            var result = await Application.Current.MainPage.DisplayAlert("Подтверждение",
                "Очистить все данные замеров?", "Да", "Нет");

            if (result)
            {
                await _measurementRepository.ClearMeasurementsAsync();
                await Application.Current.MainPage.DisplayAlert("Успех",
                    "Данные очищены", "OK");
            }
        }

        private void ShowMeasurementPage(EquipmentNode node)
        {
            if (node?.Type == NodeType.Direction)
            {
                var measurementVM = new MeasurementViewModel(node, _measurementRepository, _vibrometerDevice);
                var measurementPage = new MeasurementPage { BindingContext = measurementVM };
                Application.Current.MainPage.Navigation.PushAsync(measurementPage);
            }
        }

        private void InitializeNfc()
        {
            _nfcService.TagScanned += OnNfcTagScanned;
            _nfcService.StartListening();
        }

        private void InitializeVibrometer()
        {
            // Запуск сканирования BLE устройств
            if (!BluetoothController.LeScanner.IsRunning)
                BluetoothController.LeScanner.Restart();

            BluetoothController.LeScanner.NewData += OnVibrometerDataReceived;
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

                // Не обрабатываем advertising данные во время активного опроса
                if (IsPollingVibrometer) return;

                var data = e.Data.Data.BytesToStruct<ViPenAdvertising>();

                Velocity = Math.Round(data.Velocity * 0.01, 2);
                Acceleration = Math.Round(data.Acceleration * 0.01, 2);
                Kurtosis = Math.Round(data.Kurtosis * 0.01, 2);
                Temperature = Math.Round(data.Temperature * 0.01, 2);
                LastAdvertising = DateTime.Now;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка обработки данных виброметра: {ex.Message}");
            }
        }

        private void OnNfcTagScanned(object sender, string nfcData)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                NfcFilter = nfcData;
                ApplyNfcFilter();
            });
        }

        private void ApplyNfcFilter()
        {
            FilteredNodes.Clear();

            if (string.IsNullOrWhiteSpace(NfcFilter))
            {
                foreach (var node in EquipmentNodes)
                {
                    FilteredNodes.Add(node);
                }
            }
            else
            {
                var filtered = FilterNodes(EquipmentNodes, NfcFilter);
                foreach (var node in filtered)
                {
                    FilteredNodes.Add(node);
                }
            }
        }

        private List<EquipmentNode> FilterNodes(IEnumerable<EquipmentNode> nodes, string filter)
        {
            var result = new List<EquipmentNode>();

            foreach (var node in nodes)
            {
                if (node?.FullPath == null)
                    continue;

                if (node.FullPath.ToLower().Contains(filter.ToLower()))
                {
                    result.Add(node);
                }
                else if (node.Children?.Any() == true)
                {
                    var childResults = FilterNodes(node.Children, filter);
                    if (childResults.Any())
                    {
                        var expandedNode = node;
                        expandedNode.IsExpanded = true;
                        result.Add(expandedNode);
                        result.AddRange(childResults);
                    }
                }
            }

            return result;
        }

        public void OnDisappearing()
        {
            _nfcService?.StopListening();
            BluetoothController.LeScanner.NewData -= OnVibrometerDataReceived;
        }
    }
}