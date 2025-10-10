using DiagnosticNP.Models;
using DiagnosticNP.Models.Vibrometer;
using DiagnosticNP.Services.Bluetooth;
using DiagnosticNP.Services.Repository;
using DiagnosticNP.Services.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace DiagnosticNP.ViewModels
{
    public class MeasurementViewModel : BaseViewModel, IDisposable
    {
        private readonly IRepository _repository;
        private readonly EquipmentNode _equipmentNode;
        private IVPenControl _vibrometerController;
        private OperationToken _operationToken;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isDisposed;

        private double _velocity;
        private double _temperature;
        private double _acceleration;
        private double _kurtosis;
        private DateTime _measurementTime;
        private string _vibrometerStatus;
        private bool _isReadingData;
        private bool _isPollingVibrometer;
        private string _vibrometerDevice;
        private DateTime _lastAdvertising;

        public MeasurementViewModel(EquipmentNode equipmentNode, IRepository repository)
        {
            _equipmentNode = equipmentNode;
            _repository = repository;
            _measurementTime = DateTime.Now;
            _operationToken = new OperationToken();

            InitializeCommands();
            InitializeVibrometerController();
            InitializeVibrometerScanning();
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

        public DateTime LastAdvertising
        {
            get => _lastAdvertising;
            set => SetProperty(ref _lastAdvertising, value);
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
            StopPollingCommand = new Command(() => StopPolling());
        }

        private void InitializeVibrometerController()
        {
            _vibrometerController = VPenControlManager.GetController();
        }

        private void InitializeVibrometerScanning()
        {
            try
            {
                if (!BluetoothController.LeScanner.IsRunning)
                    BluetoothController.LeScanner.Start();

                BluetoothController.LeScanner.NewData += OnVibrometerDataReceived;
                VibrometerStatus = "Сканирование виброметра запущено";
            }
            catch (Exception ex)
            {
                VibrometerStatus = $"Ошибка инициализации виброметра: {ex.Message}";
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
                // Не обрабатываем advertising данные во время активного опроса
                if (IsPollingVibrometer) return;

                _vibrometerDevice = e.Data.Address;
                var data = e.Data.Data.BytesToStruct<ViPenAdvertising>();

                Velocity = Math.Round(data.Velocity * 0.01, 2);
                Acceleration = Math.Round(data.Acceleration * 0.01, 2);
                Kurtosis = Math.Round(data.Kurtosis * 0.01, 2);
                Temperature = Math.Round(data.Temperature * 0.01, 2);
                LastAdvertising = DateTime.Now;

                VibrometerStatus = $"Данные получены: {_vibrometerDevice}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Vibrometer data processing error: {ex.Message}");
            }
        }

        private async Task<bool> ConnectToVibrometer()
        {
            if (string.IsNullOrEmpty(_vibrometerDevice))
            {
                VibrometerStatus = "Виброметр не обнаружен";
                return false;
            }

            IsReadingData = true;
            VibrometerStatus = "Подключение к виброметру...";

            try
            {
                _cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                var connected = await _vibrometerController.ConnectAsync(_vibrometerDevice, _operationToken);

                if (connected)
                {
                    VibrometerStatus = "Виброметр подключен";
                    return true;
                }
                else
                {
                    VibrometerStatus = "Ошибка подключения";
                    return false;
                }
            }
            catch (Exception ex)
            {
                VibrometerStatus = $"Ошибка подключения: {ex.Message}";
                return false;
            }
            finally
            {
                IsReadingData = false;
            }
        }

        private async Task DisconnectFromVibrometer()
        {
            try
            {
                await _vibrometerController.Stop(_vibrometerDevice, _operationToken);
                await _vibrometerController.Disconnect();
                VibrometerStatus = "Виброметр отключен";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disconnecting vibrometer: {ex.Message}");
            }
        }

        private async Task ReadFromVibrometer()
        {
            if (string.IsNullOrEmpty(_vibrometerDevice))
            {
                await Application.Current.MainPage.DisplayAlert("Внимание",
                    "Виброметр не обнаружен", "OK");
                return;
            }

            IsReadingData = true;
            VibrometerStatus = "Чтение данных с виброметра...";

            try
            {
                _cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                // Автоматически подключаемся если не подключены
                if (!_vibrometerController.IsConnected)
                {
                    if (!await ConnectToVibrometer())
                    {
                        await Application.Current.MainPage.DisplayAlert("Ошибка",
                            "Не удалось подключиться к виброметру", "OK");
                        return;
                    }
                }

                var userData = await _vibrometerController.ReadUserData(_vibrometerDevice, _operationToken);

                if (userData.Values != null && userData.Values.Length >= 4)
                {
                    Velocity = Math.Round(userData.Values[0] * 0.01, 2);
                    Acceleration = Math.Round(userData.Values[1] * 0.01, 2);
                    Kurtosis = Math.Round(userData.Values[2] * 0.01, 2);
                    Temperature = Math.Round(userData.Values[3] * 0.01, 2);
                    MeasurementTime = DateTime.Now;

                    VibrometerStatus = "Данные успешно получены";
                }
                else
                {
                    VibrometerStatus = "Ошибка: некорректные данные";
                    await Application.Current.MainPage.DisplayAlert("Ошибка",
                        "Получены некорректные данные с виброметра", "OK");
                }
            }
            catch (Exception ex)
            {
                VibrometerStatus = $"Ошибка чтения: {ex.Message}";
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    $"Ошибка при чтении данных с виброметра: {ex.Message}", "OK");
            }
            finally
            {
                IsReadingData = false;
            }
        }

        private async Task StartPolling()
        {
            if (IsPollingVibrometer) return;

            if (string.IsNullOrEmpty(_vibrometerDevice))
            {
                await Application.Current.MainPage.DisplayAlert("Внимание",
                    "Виброметр не обнаружен", "OK");
                return;
            }

            IsPollingVibrometer = true;
            VibrometerStatus = "Запуск опроса виброметра...";

            try
            {
                // Останавливаем автоматическое сканирование
                BluetoothController.LeScanner.Stop();

                // Автоматически подключаемся
                if (!await ConnectToVibrometer())
                {
                    await Application.Current.MainPage.DisplayAlert("Ошибка",
                        "Не удалось подключиться к виброметру", "OK");
                    IsPollingVibrometer = false;
                    return;
                }

                await _vibrometerController.Start(_vibrometerDevice, _operationToken);

                VibrometerStatus = "Опрос виброметра запущен";

                // Запускаем цикл опроса
                _ = Task.Run(async () => await PollVibrometerLoop());
            }
            catch (Exception ex)
            {
                VibrometerStatus = $"Ошибка запуска опроса: {ex.Message}";
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    $"Ошибка при запуске опроса виброметра: {ex.Message}", "OK");
                IsPollingVibrometer = false;
            }
        }

        private async void StopPolling()
        {
            if (!IsPollingVibrometer) return;

            IsPollingVibrometer = false;
            VibrometerStatus = "Остановка опроса виброметра...";

            try
            {
                await _vibrometerController.Stop(_vibrometerDevice, _operationToken);
                await DisconnectFromVibrometer();
                VibrometerStatus = "Опрос виброметра остановлен";

                // Восстанавливаем сканирование
                if (!BluetoothController.LeScanner.IsRunning)
                {
                    BluetoothController.LeScanner.Start();
                }
            }
            catch (Exception ex)
            {
                VibrometerStatus = $"Ошибка остановки опроса: {ex.Message}";
            }
        }

        private async Task PollVibrometerLoop()
        {
            int consecutiveErrors = 0;
            const int maxConsecutiveErrors = 3;

            while (IsPollingVibrometer)
            {
                try
                {
                    if (!_vibrometerController.IsConnected)
                    {
                        consecutiveErrors++;
                        if (consecutiveErrors > maxConsecutiveErrors)
                        {
                            Device.BeginInvokeOnMainThread(() =>
                            {
                                VibrometerStatus = "Соединение потеряно";
                                IsPollingVibrometer = false;
                            });
                            break;
                        }

                        // Попытка переподключения
                        if (await ConnectToVibrometer())
                        {
                            consecutiveErrors = 0;
                            await _vibrometerController.Start(_vibrometerDevice, _operationToken);
                        }
                        else
                        {
                            await Task.Delay(1000);
                        }
                        continue;
                    }

                    var userData = await _vibrometerController.ReadUserData(_vibrometerDevice, _operationToken);

                    if (userData.Values != null && userData.Values.Length >= 4)
                    {
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            Velocity = Math.Round(userData.Values[0] * 0.01, 2);
                            Acceleration = Math.Round(userData.Values[1] * 0.01, 2);
                            Kurtosis = Math.Round(userData.Values[2] * 0.01, 2);
                            Temperature = Math.Round(userData.Values[3] * 0.01, 2);
                            MeasurementTime = DateTime.Now;
                            LastAdvertising = DateTime.Now;
                        });

                        consecutiveErrors = 0;
                    }

                    await Task.Delay(500); // Пауза между опросами
                }
                catch (Exception ex)
                {
                    consecutiveErrors++;
                    System.Diagnostics.Debug.WriteLine($"Polling error: {ex.Message}");

                    if (consecutiveErrors > maxConsecutiveErrors)
                    {
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            VibrometerStatus = "Ошибка опроса виброметра";
                            IsPollingVibrometer = false;
                        });
                        break;
                    }

                    await Task.Delay(1000);
                }
            }

            // Автоматически отключаемся при выходе из цикла
            try
            {
                await DisconnectFromVibrometer();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disconnecting after polling: {ex.Message}");
            }

            // Восстанавливаем сканирование при выходе из цикла
            if (!BluetoothController.LeScanner.IsRunning)
            {
                BluetoothController.LeScanner.Start();
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
                    Velocity = Velocity,
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
            if (Velocity < 0)
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
                    // Останавливаем опрос
                    IsPollingVibrometer = false;

                    _cancellationTokenSource?.Cancel();
                    _cancellationTokenSource?.Dispose();

                    // Отписываемся от событий сканера
                    BluetoothController.LeScanner.NewData -= OnVibrometerDataReceived;

                    // Автоматически отключаем виброметр
                    if (_vibrometerController != null)
                    {
                        try
                        {
                            _vibrometerController.Stop(_vibrometerDevice, _operationToken).Wait(TimeSpan.FromSeconds(3));
                            _vibrometerController.Disconnect().Wait(TimeSpan.FromSeconds(3));
                        }
                        catch
                        {
                            // Игнорируем ошибки при отключении
                        }

                        _vibrometerController.Dispose();
                    }

                    // Восстанавливаем сканирование если было остановлено
                    if (!BluetoothController.LeScanner.IsRunning)
                    {
                        BluetoothController.LeScanner.Start();
                    }

                    //_operationToken?.Dispose();
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