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
        private bool _isVibrometerConnected;
        private string _vibrometerStatus;
        private bool _isReadingData;

        public MeasurementViewModel(EquipmentNode equipmentNode, IRepository repository, string vibrometerDeviceAddress)
        {
            _equipmentNode = equipmentNode;
            _repository = repository;
            VibrometerDeviceAddress = vibrometerDeviceAddress;
            _measurementTime = DateTime.Now;
            _operationToken = new OperationToken();

            InitializeCommands();
            InitializeVibrometerController();

            // Автоматическое подключение к виброметру при создании
            if (!string.IsNullOrEmpty(VibrometerDeviceAddress))
            {
                Task.Run(async () => await ConnectVibrometer());
            }
        }

        public string EquipmentPath => _equipmentNode?.FullPath ?? "Неизвестно";
        public string MeasurementType => _equipmentNode?.Name ?? "Неизвестно";
        public string VibrometerDeviceAddress { get; }

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

        public bool IsVibrometerConnected
        {
            get => _isVibrometerConnected;
            set => SetProperty(ref _isVibrometerConnected, value);
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

        public ICommand SaveMeasurementCommand { get; private set; }
        public ICommand ReadFromVibrometerCommand { get; private set; }
        public ICommand UseCurrentTimeCommand { get; private set; }

        private void InitializeCommands()
        {
            SaveMeasurementCommand = new Command(async () => await SaveMeasurement());
            ReadFromVibrometerCommand = new Command(async () => await ReadFromVibrometer());
            UseCurrentTimeCommand = new Command(() => MeasurementTime = DateTime.Now);
        }

        private void InitializeVibrometerController()
        {
            _vibrometerController = VPenControlManager.GetController();
        }

        private async Task ConnectVibrometer()
        {
            if (IsVibrometerConnected || string.IsNullOrEmpty(VibrometerDeviceAddress))
                return;

            IsReadingData = true;
            VibrometerStatus = "Подключение к виброметру...";

            try
            {
                _cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                var connected = await _vibrometerController.ConnectAsync(VibrometerDeviceAddress, _operationToken);

                if (connected)
                {
                    IsVibrometerConnected = true;
                    VibrometerStatus = "Виброметр подключен";

                    // Автоматически читаем данные после подключения
                    await ReadFromVibrometer();
                }
                else
                {
                    VibrometerStatus = "Ошибка подключения";
                    await Application.Current.MainPage.DisplayAlert("Ошибка",
                        "Не удалось подключиться к виброметру", "OK");
                }
            }
            catch (Exception ex)
            {
                VibrometerStatus = $"Ошибка подключения: {ex.Message}";
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    $"Ошибка при подключении к виброметру: {ex.Message}", "OK");
            }
            finally
            {
                IsReadingData = false;
            }
        }

        private async Task ReadFromVibrometer()
        {
            if (!IsVibrometerConnected)
            {
                await Application.Current.MainPage.DisplayAlert("Внимание",
                    "Виброметр не подключен", "OK");
                return;
            }

            IsReadingData = true;
            VibrometerStatus = "Чтение данных с виброметра...";

            try
            {
                _cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                var userData = await _vibrometerController.ReadUserData(VibrometerDeviceAddress, _operationToken);

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
                    _cancellationTokenSource?.Cancel();
                    _cancellationTokenSource?.Dispose();

                    if (_vibrometerController != null)
                    {
                        if (IsVibrometerConnected)
                        {
                            try
                            {
                                _vibrometerController.Stop(VibrometerDeviceAddress, _operationToken).Wait(TimeSpan.FromSeconds(3));
                                _vibrometerController.Disconnect().Wait(TimeSpan.FromSeconds(3));
                            }
                            catch
                            {
                            }
                        }

                        _vibrometerController.Dispose();
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