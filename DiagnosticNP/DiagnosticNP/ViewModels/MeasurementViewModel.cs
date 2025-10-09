using DiagnosticNP.Data;
using DiagnosticNP.Models;
using DiagnosticNP.Models.Vibrometer;
using DiagnosticNP.Services.Bluetooth;
using DiagnosticNP.Services.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace DiagnosticNP.ViewModels
{
    public class MeasurementViewModel : BaseViewModel
    {
        private readonly EquipmentNode _directionNode;
        private readonly IMeasurementRepository _measurementRepository;
        private readonly IVPenControl _vipenController;

        private double _velocity;
        private double _temperature;
        private double _acceleration;
        private double _kurtosis;
        private bool _isPolling;
        private string _connectionStatus;
        private MeasurementData _latestMeasurement;
        private CancellationTokenSource _pollingCancellationToken;
        private string _deviceAddress;

        public MeasurementViewModel(EquipmentNode directionNode, IMeasurementRepository measurementRepository, string deviceAddress = null)
        {
            _directionNode = directionNode;
            _measurementRepository = measurementRepository;
            _deviceAddress = deviceAddress;
            _vipenController = VPenControlManager.GetController();

            InitializeCommands();
            LoadLatestMeasurement();
            UpdateConnectionStatus();
        }

        public string DirectionName => _directionNode?.Name ?? "Неизвестно";
        public string FullPath => _directionNode?.FullPath ?? "Неизвестно";

        public double Velocity
        {
            get => _velocity;
            set => SetProperty(ref _velocity, value);
        }

        public double Temperature
        {
            get => _temperature;
            set => SetProperty(ref _temperature, value);
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

        public bool IsPolling
        {
            get => _isPolling;
            set => SetProperty(ref _isPolling, value);
        }

        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => SetProperty(ref _connectionStatus, value);
        }

        public MeasurementData LatestMeasurement
        {
            get => _latestMeasurement;
            set => SetProperty(ref _latestMeasurement, value);
        }

        public bool CanUseVibrometer => !string.IsNullOrEmpty(_deviceAddress);

        public ICommand SaveMeasurementCommand { get; private set; }
        public ICommand StartPollingCommand { get; private set; }
        public ICommand StopPollingCommand { get; private set; }
        public ICommand ClearFormCommand { get; private set; }

        private void InitializeCommands()
        {
            SaveMeasurementCommand = new Command(async () => await SaveMeasurementAsync());
            StartPollingCommand = new Command(async () => await StartPollingAsync());
            StopPollingCommand = new Command(async () => await StopPollingAsync());
            ClearFormCommand = new Command(ClearForm);
        }

        private async void LoadLatestMeasurement()
        {
            if (_directionNode != null)
            {
                LatestMeasurement = await _measurementRepository.GetLatestMeasurementAsync(
                    _directionNode.Id, _directionNode.Name);

                if (LatestMeasurement != null)
                {
                    Velocity = LatestMeasurement.Velocity;
                    Temperature = LatestMeasurement.Temperature;
                    Acceleration = LatestMeasurement.Acceleration;
                    Kurtosis = LatestMeasurement.Kurtosis;
                }
            }
        }

        private void UpdateConnectionStatus()
        {
            ConnectionStatus = _vipenController.IsConnected ? "Подключено" : "Не подключено";
        }

        public async Task SaveMeasurementAsync()
        {
            try
            {
                if (_directionNode == null)
                {
                    await Application.Current.MainPage.DisplayAlert("Ошибка",
                        "Не выбрана точка измерения", "OK");
                    return;
                }

                var measurement = new MeasurementData
                {
                    NodeId = _directionNode.Id,
                    NodePath = _directionNode.FullPath,
                    Direction = _directionNode.Name,
                    Velocity = Velocity,
                    Temperature = Temperature,
                    Acceleration = Acceleration,
                    Kurtosis = Kurtosis,
                    MeasurementTime = DateTime.Now,
                    IsManualEntry = !IsPolling,
                    IsSynced = false,
                    DeviceId = IsPolling ? "ViPen" : "Manual"
                };

                var result = await _measurementRepository.SaveMeasurementAsync(measurement);
                if (result > 0)
                {
                    await Application.Current.MainPage.DisplayAlert("Успех",
                        "Данные сохранены", "OK");
                    LatestMeasurement = measurement;
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Ошибка",
                        "Не удалось сохранить данные", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    $"Ошибка сохранения: {ex.Message}", "OK");
            }
        }

        public async Task StartPollingAsync()
        {
            if (string.IsNullOrEmpty(_deviceAddress))
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    "Виброметр не найден. Убедитесь, что устройство включено.", "OK");
                return;
            }

            IsPolling = true;
            ConnectionStatus = "Подключение...";

            try
            {
                var token = new OperationToken();

                // Автоматическое подключение к виброметру
                System.Diagnostics.Debug.WriteLine("Автоподключение к виброметру...");
                var connectSuccess = await _vipenController.ConnectAsync(_deviceAddress, token);

                if (!connectSuccess)
                {
                    IsPolling = false;
                    ConnectionStatus = "Ошибка подключения";
                    await Application.Current.MainPage.DisplayAlert("Ошибка",
                        "Не удалось подключиться к виброметру", "OK");
                    return;
                }

                ConnectionStatus = "Подключено";

                // Запускаем измерение на виброметре
                await _vipenController.Start(_deviceAddress, token);

                // Запускаем фоновый опрос
                _pollingCancellationToken = new CancellationTokenSource();
                Device.StartTimer(TimeSpan.FromMilliseconds(1000), () =>
                {
                    if (!IsPolling || _pollingCancellationToken.Token.IsCancellationRequested)
                        return false;

                    Task.Run(async () => await PollVibrometerData());
                    return true;
                });

                await Application.Current.MainPage.DisplayAlert("Успех",
                    "Опрос виброметра запущен", "OK");
            }
            catch (Exception ex)
            {
                IsPolling = false;
                ConnectionStatus = "Ошибка подключения";
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    $"Ошибка запуска опроса: {ex.Message}", "OK");
            }
        }

        public async Task StopPollingAsync()
        {
            try
            {
                IsPolling = false;
                _pollingCancellationToken?.Cancel();

                if (_vipenController.IsConnected)
                {
                    var token = new OperationToken();
                    await _vipenController.Stop(_deviceAddress, token);
                    await _vipenController.Disconnect();
                }

                ConnectionStatus = "Не подключено";

                await Application.Current.MainPage.DisplayAlert("Успех",
                    "Опрос виброметра остановлен", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    $"Ошибка остановки опроса: {ex.Message}", "OK");
            }
        }

        private async Task PollVibrometerData()
        {
            if (!_vipenController.IsConnected || !IsPolling)
                return;

            try
            {
                var token = new OperationToken();
                var data = await _vipenController.ReadUserData(_deviceAddress, token);

                // ОТЛАДОЧНАЯ ИНФОРМАЦИЯ - выведем сырые данные
                System.Diagnostics.Debug.WriteLine($"Raw data from ViPen: V={data.Values[0]}, A={data.Values[1]}, K={data.Values[2]}, T={data.Values[3]}");

                // Обновляем UI в основном потоке с ПРАВИЛЬНЫМ преобразованием данных
                // Согласно исходному проекту, данные нужно преобразовывать как в MainViewModel
                Device.BeginInvokeOnMainThread(() =>
                {
                    // ПРАВИЛЬНОЕ преобразование как в исходном проекте
                    Velocity = Math.Round(data.Values[0] * 0.0001, 2);        // мм/с
                    Acceleration = Math.Round(data.Values[1] * 0.0001, 2);    // м/с²
                    Kurtosis = Math.Round(data.Values[2] * 0.01, 2);        // безразмерная величина
                    Temperature = Math.Round(data.Values[3] * 0.01, 2);     // °C
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка опроса виброметра: {ex.Message}");

                // При ошибке опроса автоматически останавливаем опрос
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await StopPollingAsync();
                    await Application.Current.MainPage.DisplayAlert("Ошибка",
                        "Ошибка опроса виброметра. Опрос остановлен.", "OK");
                });
            }
        }

        private void ClearForm()
        {
            Velocity = 0;
            Temperature = 0;
            Acceleration = 0;
            Kurtosis = 0;
        }

        public async void OnDisappearing()
        {
            try
            {
                // Останавливаем опрос при закрытии страницы
                if (IsPolling)
                {
                    await StopPollingAsync();
                }

                _vipenController?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при очистке ресурсов: {ex.Message}");
            }
        }
    }
}