using DiagnosticNP.Models;
using DiagnosticNP.Models.Equipment;
using DiagnosticNP.Models.Nfc;
using DiagnosticNP.Models.Vibrometer;
using DiagnosticNP.Services;
using DiagnosticNP.Services.Api;
using DiagnosticNP.Services.Bluetooth;
using DiagnosticNP.Services.Database;
using DiagnosticNP.Services.Nfc;
using DiagnosticNP.Services.Utils;
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
        private readonly INfcService _nfcService;
        private readonly IEquipmentApiService _apiService;
        private readonly IEquipmentRepository _repository;
        private DiagnosticData _diagnosticData;
        private bool _isListening;

        // Данные виброметра
        private DateTime _lastAdvertising;
        private double _velocity;
        private double _acceleration;
        private double _kurtosis;
        private double _temperature;
        private string _vibrometerDevice;
        private bool _isPollingVibrometer;
        private bool _isBusy;

        // Данные оборудования
        private ObservableCollection<EquipmentNode> _equipmentTree;
        private ObservableCollection<EquipmentNode> _filteredEquipmentTree;
        private EquipmentNode _selectedNode;
        private string _nfcFilter;
        private bool _showFilteredView;

        public MainViewModel()
        {
            _nfcService = new NfcService();
            _apiService = new MockEquipmentApiService();
            _repository = new EquipmentRepository();
            _diagnosticData = new DiagnosticData();
            _nfcService.TagScanned += OnTagScanned;

            EquipmentTree = new ObservableCollection<EquipmentNode>();
            FilteredEquipmentTree = new ObservableCollection<EquipmentNode>();

            InitializeCommands();
            InitializeNfc();
            InitializeVibrometer();
            LoadEquipmentStructure();
        }

        public async Task InitializeAsync()
        {
            try
            {
                // Проверяем, есть ли данные в БД
                var existingNodes = await _repository.GetAllNodesAsync();
                if (existingNodes.Any())
                {
                    System.Diagnostics.Debug.WriteLine("Найдены существующие данные в БД, строим дерево...");
                    await BuildEquipmentTree();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Данные в БД отсутствуют");
                }

                // Автоматически запускаем NFC
                await StartListening();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации: {ex.Message}");
            }
        }

        public DiagnosticData DiagnosticData
        {
            get => _diagnosticData;
            set => SetProperty(ref _diagnosticData, value);
        }

        public bool IsListening
        {
            get => _isListening;
            set => SetProperty(ref _isListening, value);
        }

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

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                SetProperty(ref _isBusy, value);
                OnPropertyChanged(nameof(IsReady));
            }
        }

        public bool IsReady => !_isBusy;

        public ObservableCollection<EquipmentNode> EquipmentTree
        {
            get => _equipmentTree;
            set => SetProperty(ref _equipmentTree, value);
        }

        public ObservableCollection<EquipmentNode> FilteredEquipmentTree
        {
            get => _filteredEquipmentTree;
            set => SetProperty(ref _filteredEquipmentTree, value);
        }

        public EquipmentNode SelectedNode
        {
            get => _selectedNode;
            set
            {
                SetProperty(ref _selectedNode, value);
                OnNodeSelected(value);
            }
        }

        public string NfcFilter
        {
            get => _nfcFilter;
            set => SetProperty(ref _nfcFilter, value);
        }

        public bool ShowFilteredView
        {
            get => _showFilteredView;
            set => SetProperty(ref _showFilteredView, value);
        }

        public ICommand StartListeningCommand { get; private set; }
        public ICommand StopListeningCommand { get; private set; }
        public ICommand ReadTagCommand { get; private set; }
        public ICommand StartVibrometerCommand { get; private set; }
        public ICommand StopVibrometerCommand { get; private set; }
        public ICommand PollVibrometerCommand { get; private set; }
        public ICommand ConnectVibrometerCommand { get; private set; }
        public ICommand DisconnectVibrometerCommand { get; private set; }
        public ICommand LoadEquipmentCommand { get; private set; }
        public ICommand UploadMeasurementsCommand { get; private set; }
        public ICommand ClearMeasurementsCommand { get; private set; }
        public ICommand TakeMeasurementCommand { get; private set; }
        public ICommand ManualMeasurementCommand { get; private set; }
        public ICommand ShowFullTreeCommand { get; private set; }
        public ICommand ShowFilteredTreeCommand { get; private set; }

        private void InitializeCommands()
        {
            StartListeningCommand = new Command(async () => await StartListening());
            StopListeningCommand = new Command(StopListening);
            ReadTagCommand = new Command(async () => await ReadTag());

            StartVibrometerCommand = new Command(async () => await StartVibrometerMeasurement());
            StopVibrometerCommand = new Command(StopVibrometer);
            PollVibrometerCommand = new Command(async () => await PollVibrometer());
            ConnectVibrometerCommand = new Command(async () => await ConnectVibrometer());
            DisconnectVibrometerCommand = new Command(async () => await DisconnectVibrometer());

            LoadEquipmentCommand = new Command(async () => await LoadEquipmentStructure());
            UploadMeasurementsCommand = new Command(async () => await UploadMeasurements());
            ClearMeasurementsCommand = new Command(async () => await ClearMeasurements());
            TakeMeasurementCommand = new Command(async () => await TakeMeasurement());
            ManualMeasurementCommand = new Command(async () => await ManualMeasurement());
            ShowFullTreeCommand = new Command(() => ShowFilteredView = false);
            ShowFilteredTreeCommand = new Command(() => ShowFilteredView = true);
        }

        private void InitializeVibrometer()
        {
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
                if (IsPollingVibrometer) return;

                _vibrometerDevice = e.Data.Address;
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

        private async Task ConnectVibrometer()
        {
            if (_isConnectingInProgress) return;

            _isConnectingInProgress = true;

            try
            {
                if (string.IsNullOrEmpty(_vibrometerDevice))
                {
                    await Application.Current.MainPage.DisplayAlert("Ошибка",
                        "Виброметр не найден. Убедитесь, что устройство включено и доступно.", "OK");
                    return;
                }

                IsBusy = true;

                using (var controller = VPenControlManager.GetController())
                {
                    var token = new OperationToken();

                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                    {
                        try
                        {
                            var connectTask = controller.ConnectAsync(_vibrometerDevice, token);
                            var completedTask = await Task.WhenAny(connectTask, Task.Delay(-1, cts.Token));

                            if (completedTask == connectTask && connectTask.Result)
                            {
                                await Application.Current.MainPage.DisplayAlert("Успех",
                                    "Подключение к виброметру установлено", "OK");
                            }
                            else
                            {
                                await Application.Current.MainPage.DisplayAlert("Ошибка",
                                    "Не удалось подключиться к виброметру (таймаут)", "OK");
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            await Application.Current.MainPage.DisplayAlert("Ошибка",
                                "Таймаут подключения к виброметру", "OK");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    $"Ошибка подключения к виброметру: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
                _isConnectingInProgress = false;
            }
        }

        private bool _isConnectingInProgress = false;

        private async Task DisconnectVibrometer()
        {
            try
            {
                using (var controller = VPenControlManager.GetController())
                {
                    await controller.Disconnect();
                    await Application.Current.MainPage.DisplayAlert("Успех",
                        "Отключено от виброметра", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    $"Ошибка отключения от виброметра: {ex.Message}", "OK");
            }
        }

        private async Task StartVibrometerMeasurement()
        {
            try
            {
                if (string.IsNullOrEmpty(_vibrometerDevice))
                {
                    await Application.Current.MainPage.DisplayAlert("Ошибка",
                        "Сначала подключитесь к виброметру", "OK");
                    return;
                }

                IsBusy = true;

                using (var controller = VPenControlManager.GetController())
                {
                    var token = new OperationToken();
                    if (await controller.StartMeasurementAsync(_vibrometerDevice, token))
                    {
                        await Application.Current.MainPage.DisplayAlert("Успех",
                            "Измерение виброметром запущено", "OK");
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert("Ошибка",
                            "Не удалось запустить измерение", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    $"Ошибка запуска измерения: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task PollVibrometer()
        {
            System.Diagnostics.Debug.WriteLine("=== ЗАПУСК ОПРОСА ВИБРОМЕТРА ===");

            try
            {
                if (string.IsNullOrEmpty(_vibrometerDevice))
                {
                    await Application.Current.MainPage.DisplayAlert("Ошибка",
                        "Виброметр не найден", "OK");
                    return;
                }

                IsPollingVibrometer = true;
                int successfulPolls = 0;
                int consecutiveErrors = 0;
                const int maxConsecutiveErrors = 3;

                BluetoothController.LeScanner.Stop();
                await Task.Delay(500);

                using (var controller = VPenControlManager.GetController())
                {
                    var token = new OperationToken();

                    System.Diagnostics.Debug.WriteLine("Подключение к устройству...");
                    if (!await controller.ConnectAsync(_vibrometerDevice, token))
                    {
                        await Application.Current.MainPage.DisplayAlert("Ошибка",
                            "Не удалось подключиться к виброметру", "OK");
                        IsPollingVibrometer = false;
                        return;
                    }

                    try
                    {
                        System.Diagnostics.Debug.WriteLine("Отправка команды START...");
                        await controller.Start(_vibrometerDevice, token);
                        System.Diagnostics.Debug.WriteLine("Команда START отправлена");
                    }
                    catch (Exception startEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка отправки START: {startEx.Message}");
                    }

                    while (IsPollingVibrometer)
                    {
                        try
                        {
                            if (!controller.IsConnected)
                            {
                                System.Diagnostics.Debug.WriteLine("Соединение потеряно, попытка восстановления...");
                                consecutiveErrors++;

                                if (consecutiveErrors > maxConsecutiveErrors)
                                {
                                    System.Diagnostics.Debug.WriteLine("Слишком много ошибок, останавливаю опрос");
                                    break;
                                }

                                await Task.Delay(300);
                                if (await controller.ConnectAsync(_vibrometerDevice, token))
                                {
                                    System.Diagnostics.Debug.WriteLine("Соединение восстановлено");
                                    consecutiveErrors = 0;

                                    try
                                    {
                                        await controller.Start(_vibrometerDevice, token);
                                    }
                                    catch { }
                                }
                                continue;
                            }

                            var data = await controller.ReadUserData(_vibrometerDevice, token);

                            Device.BeginInvokeOnMainThread(() =>
                            {
                                Velocity = Math.Round(data.Values[0] * 0.01, 2);
                                Acceleration = Math.Round(data.Values[1] * 0.01, 2);
                                Kurtosis = Math.Round(data.Values[2] * 0.01, 2);
                                Temperature = Math.Round(data.Values[3] * 0.01, 2);
                                LastAdvertising = DateTime.Now;
                            });

                            successfulPolls++;
                            consecutiveErrors = 0;

                            await Task.Delay(800);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Ошибка в цикле опроса: {ex.Message}");
                            consecutiveErrors++;

                            if (consecutiveErrors > maxConsecutiveErrors)
                            {
                                System.Diagnostics.Debug.WriteLine("Слишком много ошибок подряд, останавливаю опрос");
                                break;
                            }

                            await Task.Delay(500);
                        }
                    }

                    System.Diagnostics.Debug.WriteLine("Завершение опроса...");
                    try
                    {
                        if (controller.IsConnected)
                        {
                            await controller.Stop(_vibrometerDevice, token);
                            await Task.Delay(200);
                            await controller.Disconnect();
                        }
                    }
                    catch (Exception stopEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка при остановке: {stopEx.Message}");
                    }

                    System.Diagnostics.Debug.WriteLine($"ОПРОС ЗАВЕРШЕН. Успешных опросов: {successfulPolls}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"КРИТИЧЕСКАЯ ОШИБКА ОПРОСА: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    $"Ошибка опроса виброметра: {ex.Message}", "OK");
            }
            finally
            {
                IsPollingVibrometer = false;

                await Task.Delay(1000);
                if (!BluetoothController.LeScanner.IsRunning)
                {
                    BluetoothController.LeScanner.Start();
                }

                System.Diagnostics.Debug.WriteLine("=== ОПРОС ОСТАНОВЛЕН ===");
            }
        }

        private void StopVibrometer()
        {
            IsPollingVibrometer = false;
        }

        private async Task LoadEquipmentStructure()
        {
            try
            {
                IsBusy = true;

                var nodes = await _apiService.GetEquipmentStructureAsync();

                await _repository.DeleteAllNodesAsync();

                await _repository.SaveNodesAsync(nodes);

                await BuildEquipmentTree();

                await Application.Current.MainPage.DisplayAlert("Успех",
                    "Структура оборудования загружена успешно", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    $"Ошибка загрузки структуры: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // В начало класса добавьте
        private bool _isTreeViewInitialized = false;

        // Обновите метод BuildEquipmentTree
        private async Task BuildEquipmentTree()
        {
            try
            {
                var allNodes = await _repository.GetAllNodesAsync();
                var rootNodes = allNodes.Where(n => string.IsNullOrEmpty(n.ParentId)).ToList();

                EquipmentTree.Clear();

                foreach (var node in rootNodes)
                {
                    await BuildTreeRecursive(node, allNodes, 0);
                    EquipmentTree.Add(node);
                }

                _isTreeViewInitialized = true;

                // Принудительно обновляем UI
                OnPropertyChanged(nameof(EquipmentTree));
                System.Diagnostics.Debug.WriteLine($"Дерево построено. Корневых узлов: {EquipmentTree.Count}");

                // Убедимся, что сообщение скрывается
                OnPropertyChanged(nameof(EquipmentTree.Count));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка построения дерева: {ex.Message}");
            }
        }

        // Обновите метод BuildTreeRecursive
        // В методе BuildTreeRecursive добавьте установку уровня
        private async Task BuildTreeRecursive(EquipmentNode parent, List<EquipmentNode> allNodes, int level = 0)
        {
            try
            {
                var children = allNodes.Where(n => n.ParentId == parent.Id).ToList();
                parent.Children.Clear();
                parent.Level = level; // Устанавливаем уровень для отступов

                foreach (var child in children)
                {
                    await BuildTreeRecursive(child, allNodes, level + 1);
                    parent.Children.Add(child);
                }

                parent.HasChildren = parent.Children.Count > 0;
                // Автоматически раскрываем только первый уровень
                parent.IsExpanded = level == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка построения узла {parent.Name}: {ex.Message}");
            }
        }

        // Добавьте метод для отладки TreeView
        public void DebugTreeView()
        {
            System.Diagnostics.Debug.WriteLine("=== DEBUG TREEVIEW ===");
            System.Diagnostics.Debug.WriteLine($"EquipmentTree count: {EquipmentTree.Count}");
            System.Diagnostics.Debug.WriteLine($"FilteredEquipmentTree count: {FilteredEquipmentTree.Count}");
            System.Diagnostics.Debug.WriteLine($"ShowFilteredView: {ShowFilteredView}");

            if (EquipmentTree.Count > 0)
            {
                foreach (var node in EquipmentTree)
                {
                    PrintNode(node, 0);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("EquipmentTree is EMPTY");
            }
        }

        private void PrintNode(EquipmentNode node, int level)
        {
            var indent = new string(' ', level * 2);
            System.Diagnostics.Debug.WriteLine($"{indent}- {node.Name} (Children: {node.Children?.Count ?? 0})");

            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    PrintNode(child, level + 1);
                }
            }
        }

        private async void OnNodeSelected(EquipmentNode node)
        {
            if (node?.NodeType == NodeType.Direction)
            {
                await ShowMeasurementDialog(node);
            }
        }

        private async Task ShowMeasurementDialog(EquipmentNode node)
        {
            var action = await Application.Current.MainPage.DisplayActionSheet(
                $"Измерение для {node.Name}",
                "Отмена", null,
                "Ручной ввод", "Автоматическое измерение");

            if (action == "Ручной ввод")
            {
                await ManualMeasurementForNode(node);
            }
            else if (action == "Автоматическое измерение")
            {
                await TakeMeasurementForNode(node);
            }
        }

        private async Task ManualMeasurementForNode(EquipmentNode node)
        {
            var velocity = await Application.Current.MainPage.DisplayPromptAsync(
                "Ручной ввод", "Скорость вибрации (мм/с):", "OK", "Отмена", keyboard: Keyboard.Numeric);

            var acceleration = await Application.Current.MainPage.DisplayPromptAsync(
                "Ручной ввод", "Ускорение (м/с²):", "OK", "Отмена", keyboard: Keyboard.Numeric);

            var temperature = await Application.Current.MainPage.DisplayPromptAsync(
                "Ручной ввод", "Температура (°C):", "OK", "Отмена", keyboard: Keyboard.Numeric);

            var kurtosis = await Application.Current.MainPage.DisplayPromptAsync(
                "Ручной ввод", "Куртозис:", "OK", "Отмена", keyboard: Keyboard.Numeric);

            if (double.TryParse(velocity, out double vel) &&
                double.TryParse(acceleration, out double acc) &&
                double.TryParse(temperature, out double temp) &&
                double.TryParse(kurtosis, out double kurt))
            {
                var measurement = new MeasurementData
                {
                    NodeId = node.Id,
                    MeasurementTime = DateTime.Now,
                    Velocity = vel,
                    Acceleration = acc,
                    Temperature = temp,
                    Kurtosis = kurt,
                    Direction = node.Name,
                    IsManualEntry = true,
                    IsSynced = false
                };

                await _repository.SaveMeasurementAsync(measurement);
                await Application.Current.MainPage.DisplayAlert("Успех", "Измерение сохранено", "OK");
            }
        }

        private async Task TakeMeasurementForNode(EquipmentNode node)
        {
            if (string.IsNullOrEmpty(_vibrometerDevice))
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    "Сначала подключите виброметр", "OK");
                return;
            }

            try
            {
                IsBusy = true;

                using (var controller = VPenControlManager.GetController())
                {
                    var token = new OperationToken();

                    if (!controller.IsConnected)
                    {
                        if (!await controller.ConnectAsync(_vibrometerDevice, token))
                        {
                            await Application.Current.MainPage.DisplayAlert("Ошибка",
                                "Не удалось подключиться к виброметру", "OK");
                            return;
                        }
                    }

                    var data = await controller.ReadUserData(_vibrometerDevice, token);

                    var measurement = new MeasurementData
                    {
                        NodeId = node.Id,
                        MeasurementTime = DateTime.Now,
                        Velocity = Math.Round(data.Values[0] * 0.01, 2),
                        Acceleration = Math.Round(data.Values[1] * 0.01, 2),
                        Temperature = Math.Round(data.Values[3] * 0.01, 2),
                        Kurtosis = Math.Round(data.Values[2] * 0.01, 2),
                        Direction = node.Name,
                        IsManualEntry = false,
                        IsSynced = false
                    };

                    await _repository.SaveMeasurementAsync(measurement);

                    Velocity = measurement.Velocity;
                    Acceleration = measurement.Acceleration;
                    Temperature = measurement.Temperature;
                    Kurtosis = measurement.Kurtosis;
                    LastAdvertising = measurement.MeasurementTime;

                    await Application.Current.MainPage.DisplayAlert("Успех",
                        "Автоматическое измерение завершено", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    $"Ошибка автоматического измерения: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task UploadMeasurements()
        {
            try
            {
                IsBusy = true;

                var unsyncedMeasurements = await _repository.GetUnsyncedMeasurementsAsync();

                if (!unsyncedMeasurements.Any())
                {
                    await Application.Current.MainPage.DisplayAlert("Информация",
                        "Нет данных для выгрузки", "OK");
                    return;
                }

                var success = await _apiService.UploadMeasurementsAsync(unsyncedMeasurements);

                if (success)
                {
                    await _repository.MarkMeasurementsAsSyncedAsync();
                    await Application.Current.MainPage.DisplayAlert("Успех",
                        $"Выгружено {unsyncedMeasurements.Count} измерений", "OK");
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Ошибка",
                        "Ошибка выгрузки данных", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    $"Ошибка выгрузки: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ClearMeasurements()
        {
            var confirm = await Application.Current.MainPage.DisplayAlert(
                "Подтверждение",
                "Удалить все данные измерений?",
                "Да", "Нет");

            if (confirm)
            {
                await _repository.DeleteAllMeasurementsAsync();
                await Application.Current.MainPage.DisplayAlert("Успех",
                    "Данные измерений очищены", "OK");
            }
        }

        private async Task TakeMeasurement()
        {
            if (SelectedNode?.NodeType == NodeType.Direction)
            {
                await TakeMeasurementForNode(SelectedNode);
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Информация",
                    "Выберите направление измерения", "OK");
            }
        }

        private async Task ManualMeasurement()
        {
            if (SelectedNode?.NodeType == NodeType.Direction)
            {
                await ManualMeasurementForNode(SelectedNode);
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Информация",
                    "Выберите направление измерения", "OK");
            }
        }

        private async void InitializeNfc()
        {
            try
            {
                var isAvailable = await _nfcService.IsAvailableAsync();
                if (!isAvailable)
                {
                    await Application.Current.MainPage.DisplayAlert("Информация",
                        "NFC не поддерживается на этом устройстве", "OK");
                }
                else
                {
                    // Автоматически запускаем прослушивание NFC
                    await StartListening();
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    $"Ошибка инициализации NFC: {ex.Message}", "OK");
            }
        }

        private async Task StartListening()
        {
            try
            {
                if (!await _nfcService.IsEnabledAsync())
                {
                    await Application.Current.MainPage.DisplayAlert("Ошибка",
                        "NFC отключен. Включите NFC в настройках устройства.", "OK");
                    return;
                }

                _nfcService.StartListening();
                IsListening = true;

                System.Diagnostics.Debug.WriteLine("NFC прослушивание запущено");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    $"Ошибка запуска сканирования: {ex.Message}", "OK");
            }
        }

        private void StopListening()
        {
            try
            {
                _nfcService.StopListening();
                IsListening = false;

                System.Diagnostics.Debug.WriteLine("NFC прослушивание остановлено");
            }
            catch (Exception ex)
            {
                Application.Current.MainPage.DisplayAlert("Ошибка",
                    $"Ошибка остановки сканирования: {ex.Message}", "OK");
            }
        }

        private async Task ReadTag()
        {
            try
            {
                if (!await _nfcService.IsEnabledAsync())
                {
                    await Application.Current.MainPage.DisplayAlert("Ошибка",
                        "NFC отключен", "OK");
                    return;
                }

                var tagData = await _nfcService.ReadTagAsync();
                if (!string.IsNullOrEmpty(tagData))
                {
                    DiagnosticData.NFCData = tagData;
                    DiagnosticData.ScanTime = DateTime.Now;

                    await Application.Current.MainPage.DisplayAlert("Успех",
                        $"Метка прочитана: {tagData}", "OK");
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Информация",
                        "Не удалось прочитать метку или метка пуста", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    $"Ошибка чтения метки: {ex.Message}", "OK");
            }
        }

        private async void OnTagScanned(object sender, string nfcData)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                DiagnosticData.NFCData = nfcData;
                DiagnosticData.ScanTime = DateTime.Now;

                NfcFilter = nfcData;
                await FilterEquipmentByNfc(nfcData);

                await Application.Current.MainPage.DisplayAlert("Успех",
                    $"Метка просканирована: {nfcData}", "OK");
            });
        }

        private async Task FilterEquipmentByNfc(string nfcFilter)
        {
            try
            {
                var filteredNodes = await _repository.GetNodesByNfcFilterAsync(nfcFilter);

                FilteredEquipmentTree.Clear();
                foreach (var node in filteredNodes)
                {
                    await BuildNodePath(node);
                    FilteredEquipmentTree.Add(node);
                }

                ShowFilteredView = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка фильтрации по NFC: {ex.Message}");
            }
        }

        private async Task BuildNodePath(EquipmentNode node)
        {
            var path = new List<string>();
            var currentNode = node;

            while (currentNode != null)
            {
                path.Insert(0, currentNode.Name);
                currentNode = await _repository.GetNodeByIdAsync(currentNode.ParentId);
            }

            node.FullPath = string.Join(" → ", path);
        }

        public async void OnDisappearing()
        {
            try
            {
                BluetoothController.LeScanner.NewData -= OnVibrometerDataReceived;
                _nfcService?.StopListening();

                if (IsPollingVibrometer)
                {
                    IsPollingVibrometer = false;
                    await Task.Delay(1000);
                }

                BluetoothController.LeScanner.Stop();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при очистке ресурсов: {ex.Message}");
            }
        }

        private async Task<bool> QuickReconnect(IVPenControl controller, string deviceAddress, OperationToken token)
        {
            try
            {
                await controller.Disconnect();
                await Task.Delay(300);
                return await controller.ConnectAsync(deviceAddress, token);
            }
            catch
            {
                return false;
            }
        }
    }
}