using DiagnosticNP.Models;
using DiagnosticNP.Models.Nfc;
using DiagnosticNP.Models.Vibrometer;
using DiagnosticNP.Services;
using DiagnosticNP.Services.Bluetooth;
using DiagnosticNP.Services.Nfc;
using DiagnosticNP.Services.Utils;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace DiagnosticNP.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly INfcService _nfcService;
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

        public MainViewModel()
        {
            _nfcService = new NfcService();
            _diagnosticData = new DiagnosticData();
            _nfcService.TagScanned += OnTagScanned;

            InitializeCommands();
            InitializeNfc();
            InitializeVibrometer();
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

        // Команды
        public ICommand StartListeningCommand { get; private set; }
        public ICommand StopListeningCommand { get; private set; }
        public ICommand ReadTagCommand { get; private set; }
        public ICommand StartVibrometerCommand { get; private set; }
        public ICommand StopVibrometerCommand { get; private set; }
        public ICommand PollVibrometerCommand { get; private set; }
        public ICommand ConnectVibrometerCommand { get; private set; }
        public ICommand DisconnectVibrometerCommand { get; private set; }

        private void InitializeCommands()
        {
            // NFC команды
            StartListeningCommand = new Command(async () => await StartListening());
            StopListeningCommand = new Command(StopListening);
            ReadTagCommand = new Command(async () => await ReadTag());

            // Команды виброметра
            StartVibrometerCommand = new Command(async () => await StartVibrometerMeasurement());
            StopVibrometerCommand = new Command(StopVibrometer);
            PollVibrometerCommand = new Command(async () => await PollVibrometer());
            ConnectVibrometerCommand = new Command(async () => await ConnectVibrometer());
            DisconnectVibrometerCommand = new Command(async () => await DisconnectVibrometer());
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
                // Не обрабатываем advertising данные во время активного опроса
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
                    if (await controller.ConnectAsync(_vibrometerDevice, token))
                    {
                        await Application.Current.MainPage.DisplayAlert("Успех",
                            "Подключение к виброметру установлено", "OK");
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert("Ошибка",
                            "Не удалось подключиться к виброметру", "OK");
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
            }
        }

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
            try
            {
                if (string.IsNullOrEmpty(_vibrometerDevice))
                {
                    await Application.Current.MainPage.DisplayAlert("Ошибка",
                        "Виброметр не найден. Убедитесь, что устройство включено и доступно.", "OK");
                    return;
                }

                IsPollingVibrometer = true;

                using (var controller = VPenControlManager.GetController())
                {
                    var token = new OperationToken();

                    // Останавливаем автоматическое сканирование перед ручным опросом
                    BluetoothController.LeScanner.Stop();

                    // Подключаемся к устройству
                    if (!await controller.ConnectAsync(_vibrometerDevice, token))
                    {
                        await Application.Current.MainPage.DisplayAlert("Ошибка",
                            "Не удалось подключиться к виброметру", "OK");
                        IsPollingVibrometer = false;
                        return;
                    }

                    // Отправляем команду START на устройство
                    await controller.Start(_vibrometerDevice, token);

                    // Циклический опрос данных
                    while (IsPollingVibrometer && controller.IsConnected)
                    {
                        try
                        {
                            var data = await controller.ReadUserData(_vibrometerDevice, token);

                            Device.BeginInvokeOnMainThread(() =>
                            {
                                Velocity = Math.Round(data.Values[0] * 0.01, 2);
                                Acceleration = Math.Round(data.Values[1] * 0.01, 2);
                                Kurtosis = Math.Round(data.Values[2] * 0.01, 2);
                                Temperature = Math.Round(data.Values[3] * 0.01, 2);
                                LastAdvertising = DateTime.Now;
                            });

                            await Task.Delay(1000); // Опрос каждую секунду
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Ошибка опроса виброметра: {ex.Message}");
                            // При серьезной ошибке выходим из цикла
                            if (ex is TimeoutException || ex.Message.Contains("Service discover error"))
                                break;
                            // Для других ошибок ждем 1 секунду и продолжаем
                            await Task.Delay(1000);
                        }
                    }

                    // Корректное завершение - отправляем команду STOP
                    if (controller.IsConnected)
                    {
                        await controller.Stop(_vibrometerDevice, token);
                        await controller.Disconnect();
                    }
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    $"Ошибка опроса виброметра: {ex.Message}", "OK");
            }
            finally
            {
                IsPollingVibrometer = false;
                // Возобновляем автоматическое сканирование
                if (!BluetoothController.LeScanner.IsRunning)
                    BluetoothController.LeScanner.Start();
            }
        }

        private void StopVibrometer()
        {
            IsPollingVibrometer = false;
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

                await Application.Current.MainPage.DisplayAlert("Успех",
                    "Сканирование NFC запущено. Поднесите метку к устройству.", "OK");
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

                Application.Current.MainPage.DisplayAlert("Успех",
                    "Сканирование NFC остановлено", "OK");
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

        private void OnTagScanned(object sender, string nfcData)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                DiagnosticData.NFCData = nfcData;
                DiagnosticData.ScanTime = DateTime.Now;

                Application.Current.MainPage.DisplayAlert("Успех",
                    $"Метка просканирована: {nfcData}", "OK");
            });
        }

        public async void OnDisappearing()
        {
            try
            {
                BluetoothController.LeScanner.NewData -= OnVibrometerDataReceived;
                _nfcService?.StopListening();

                // Асинхронная остановка виброметра
                if (IsPollingVibrometer)
                {
                    IsPollingVibrometer = false;
                    await Task.Delay(1000); // Даем время на завершение
                }

                // Принудительная очистка BLE
                BluetoothController.LeScanner.Stop();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при очистке ресурсов: {ex.Message}");
            }
        }
    }
}