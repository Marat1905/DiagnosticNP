using DiagnosticNP.Models;
using DiagnosticNP.Services.Repository;
using DiagnosticNP.Services.Vibrometer;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace DiagnosticNP.ViewModels
{
    public class MeasurementViewModel : BaseViewModel, IDisposable
    {
        private readonly IRepository _repository;
        private readonly EquipmentNode _equipmentNode;
        private readonly IVibrometerService _vibrometerService;
        private bool _isDisposed;

        private double _velocity;
        private double _temperature;
        private double _acceleration;
        private double _kurtosis;
        private DateTime _measurementTime;
        private string _vibrometerStatus;
        private bool _isReadingData;
        private bool _isPollingVibrometer;
        private DateTime _lastDataUpdate;

        public MeasurementViewModel(EquipmentNode equipmentNode, IRepository repository)
        {
            _equipmentNode = equipmentNode;
            _repository = repository;
            _measurementTime = DateTime.Now;

            // Используем фабрику для создания сервиса
            _vibrometerService = VibrometerServiceFactory.CreateService();

            InitializeCommands();
            InitializeVibrometerService();
        }

        public string EquipmentPath => _equipmentNode?.FullPath ?? "Неизвестно";
        public string MeasurementType => _equipmentNode?.Name ?? "Неизвестно";

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

        public DateTime MeasurementTime
        {
            get => _measurementTime;
            set => SetProperty(ref _measurementTime, value);
        }

        public string VibrometerStatus
        {
            get => _vibrometerStatus;
            set => SetProperty(ref _vibrometerStatus, value);
        }

        public bool IsReadingData
        {
            get => _isReadingData;
            set => SetProperty(ref _isReadingData, value);
        }

        public bool IsPollingVibrometer
        {
            get => _isPollingVibrometer;
            set => SetProperty(ref _isPollingVibrometer, value);
        }

        public DateTime LastDataUpdate
        {
            get => _lastDataUpdate;
            set => SetProperty(ref _lastDataUpdate, value);
        }

        public ICommand SaveMeasurementCommand { get; private set; }
        public ICommand ReadFromVibrometerCommand { get; private set; }
        public ICommand UseCurrentTimeCommand { get; private set; }
        public ICommand StartPollingCommand { get; private set; }
        public ICommand StopPollingCommand { get; private set; }

        private void InitializeCommands()
        {
            SaveMeasurementCommand = new Command(async () => await SaveMeasurement());
            ReadFromVibrometerCommand = new Command(async () => await ReadFromVibrometer());
            UseCurrentTimeCommand = new Command(() => MeasurementTime = DateTime.Now);
            StartPollingCommand = new Command(async () => await StartPolling());
            StopPollingCommand = new Command(async () => await StopPolling());
        }

        private async void InitializeVibrometerService()
        {
            try
            {
                // Подписываемся на события сервиса
                _vibrometerService.DataReceived += OnVibrometerDataReceived;
                _vibrometerService.StatusChanged += OnVibrometerStatusChanged;
                _vibrometerService.ErrorOccurred += OnVibrometerErrorOccurred;
                _vibrometerService.PollingStateChanged += OnPollingStateChanged;

                // Инициализируем сервис
                await _vibrometerService.InitializeAsync();
                VibrometerStatus = "Сервис виброметра инициализирован";
            }
            catch (Exception ex)
            {
                VibrometerStatus = $"Ошибка инициализации сервиса виброметра: {ex.Message}";
            }
        }

        private void OnVibrometerDataReceived(object sender, VibrometerDataReceivedEventArgs e)
        {
            Device.BeginInvokeOnMainThread(() => UpdateFromVibrometerData(e.Data));
        }

        private void OnVibrometerStatusChanged(object sender, string status)
        {
            Device.BeginInvokeOnMainThread(() => VibrometerStatus = status);
        }

        private void OnVibrometerErrorOccurred(object sender, string error)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                VibrometerStatus = $"Ошибка: {error}";
                // Можно показать диалог с ошибкой, если нужно
                System.Diagnostics.Debug.WriteLine($"VibrometerService Error: {error}");
            });
        }

        private void OnPollingStateChanged(object sender, bool isPolling)
        {
            Device.BeginInvokeOnMainThread(() => IsPollingVibrometer = isPolling);
        }

        private void UpdateFromVibrometerData(VibrometerData data)
        {
            var t = data.Velocity;
            Velocity1 = data.Velocity;
            Acceleration = data.Acceleration;
            Kurtosis = data.Kurtosis;
            Temperature = data.Temperature;
            LastDataUpdate = data.Timestamp;

            // Автоматически обновляем время замера при получении новых данных
            MeasurementTime = DateTime.Now;
        }

        private async Task ReadFromVibrometer()
        {
            if (!_vibrometerService.IsConnected)
            {
                IsReadingData = true;
                VibrometerStatus = "Подключение к виброметру...";

                try
                {
                    var connected = await _vibrometerService.ConnectAsync();
                    if (!connected)
                    {
                        await Application.Current.MainPage.DisplayAlert("Ошибка",
                            "Не удалось подключиться к виброметру", "OK");
                        IsReadingData = false;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.DisplayAlert("Ошибка",
                        $"Ошибка подключения: {ex.Message}", "OK");
                    IsReadingData = false;
                    return;
                }
            }

            // Данные автоматически придут через событие DataReceived
            VibrometerStatus = "Ожидание данных от виброметра...";
        }

        private async Task StartPolling()
        {
            if (IsPollingVibrometer) return;

            try
            {
                IsReadingData = true;
                await _vibrometerService.StartPollingAsync();
                // Состояние опроса обновится через событие PollingStateChanged
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    $"Ошибка запуска опроса: {ex.Message}", "OK");
            }
            finally
            {
                IsReadingData = false;
            }
        }

        private async Task StopPolling()
        {
            if (!IsPollingVibrometer) return;

            try
            {
                IsReadingData = true;
                await _vibrometerService.StopPollingAsync();
                // Состояние опроса обновится через событие PollingStateChanged
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    $"Ошибка остановки опроса: {ex.Message}", "OK");
            }
            finally
            {
                IsReadingData = false;
            }
        }

        private async Task SaveMeasurement()
        {
            if (!ValidateMeasurement())
                return;

            try
            {
                var measurement = new Measurement
                {
                    EquipmentNodeId = _equipmentNode.Id,
                    MeasurementType = MeasurementType,
                    Velocity = Velocity1,
                    Temperature = Temperature,
                    Acceleration = Acceleration,
                    Kurtosis = Kurtosis,
                    MeasurementTime = MeasurementTime,
                    IsUploaded = false,
                    CreatedBy = Environment.UserName
                };

                await _repository.SaveMeasurementAsync(measurement);

                await Application.Current.MainPage.DisplayAlert("Успех",
                    "Замер успешно сохранен", "OK");

                await Application.Current.MainPage.Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    $"Не удалось сохранить замер: {ex.Message}", "OK");
            }
        }

        private bool ValidateMeasurement()
        {
            if (Velocity1 < 0)
            {
                Application.Current.MainPage.DisplayAlert("Ошибка", "Скорость не может быть отрицательной", "OK");
                return false;
            }

            if (Temperature < -273)
            {
                Application.Current.MainPage.DisplayAlert("Ошибка", "Некорректная температура", "OK");
                return false;
            }

            if (MeasurementTime > DateTime.Now.AddMinutes(5))
            {
                Application.Current.MainPage.DisplayAlert("Ошибка", "Время замера не может быть в будущем", "OK");
                return false;
            }

            return true;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;

                try
                {
                    // Отписываемся от событий
                    if (_vibrometerService != null)
                    {
                        _vibrometerService.DataReceived -= OnVibrometerDataReceived;
                        _vibrometerService.StatusChanged -= OnVibrometerStatusChanged;
                        _vibrometerService.ErrorOccurred -= OnVibrometerErrorOccurred;
                        _vibrometerService.PollingStateChanged -= OnPollingStateChanged;

                        // Останавливаем опрос и освобождаем ресурсы
                        _vibrometerService.StopPollingAsync().Wait(TimeSpan.FromSeconds(3));
                        _vibrometerService.DisconnectAsync().Wait(TimeSpan.FromSeconds(3));
                        _vibrometerService.CleanupAsync().Wait(TimeSpan.FromSeconds(3));
                        _vibrometerService.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error disposing MeasurementViewModel: {ex.Message}");
                }
            }
        }

        ~MeasurementViewModel()
        {
            Dispose();
        }
    }
}