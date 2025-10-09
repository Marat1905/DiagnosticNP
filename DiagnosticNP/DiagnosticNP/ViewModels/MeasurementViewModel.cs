using DiagnosticNP.Data;
using DiagnosticNP.Models;
using DiagnosticNP.Models.Vibrometer;
using DiagnosticNP.Services.Bluetooth;
using DiagnosticNP.Services.Utils;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace DiagnosticNP.ViewModels
{
    public class MeasurementViewModel : BaseViewModel
    {
        private readonly EquipmentNode _directionNode;
        private readonly IMeasurementRepository _measurementRepository;

        private double _velocity;
        private double _temperature;
        private double _acceleration;
        private double _kurtosis;
        private bool _isMeasuring;
        private MeasurementData _latestMeasurement;

        public MeasurementViewModel(EquipmentNode directionNode, IMeasurementRepository measurementRepository)
        {
            _directionNode = directionNode;
            _measurementRepository = measurementRepository;

            InitializeCommands();
            LoadLatestMeasurement();
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

        public bool IsMeasuring
        {
            get => _isMeasuring;
            set => SetProperty(ref _isMeasuring, value);
        }

        public MeasurementData LatestMeasurement
        {
            get => _latestMeasurement;
            set => SetProperty(ref _latestMeasurement, value);
        }

        public ICommand SaveManualMeasurementCommand { get; private set; }
        public ICommand StartVibrometerMeasurementCommand { get; private set; }
        public ICommand StopVibrometerMeasurementCommand { get; private set; }
        public ICommand ClearFormCommand { get; private set; }

        private void InitializeCommands()
        {
            SaveManualMeasurementCommand = new Command(async () => await SaveManualMeasurementAsync());
            StartVibrometerMeasurementCommand = new Command(async () => await StartVibrometerMeasurementAsync());
            StopVibrometerMeasurementCommand = new Command(async () => await StopVibrometerMeasurementAsync());
            ClearFormCommand = new Command(ClearForm);
        }

        private async void LoadLatestMeasurement()
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

        private async Task SaveManualMeasurementAsync()
        {
            try
            {
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
                    IsManualEntry = true,
                    IsSynced = false
                };

                var result = await _measurementRepository.SaveMeasurementAsync(measurement);
                if (result > 0)
                {
                    await Application.Current.MainPage.DisplayAlert("Успех",
                        "Данные сохранены", "OK");
                    LatestMeasurement = measurement;
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    $"Ошибка сохранения: {ex.Message}", "OK");
            }
        }

        private async Task StartVibrometerMeasurementAsync()
        {
            IsMeasuring = true;

            try
            {
                // Используем существующую реализацию Bluetooth для виброметра
                using (var controller = VPenControlManager.GetController())
                {
                    var token = new OperationToken();

                    // Здесь можно добавить логику для автоматического сбора данных с виброметра
                    // и обновления полей Velocity, Temperature, Acceleration, Kurtosis

                    // Временная заглушка - имитация данных с виброметра
                    await SimulateVibrometerData(controller, token);
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    $"Ошибка измерения: {ex.Message}", "OK");
            }
            finally
            {
                IsMeasuring = false;
            }
        }

        private async Task StopVibrometerMeasurementAsync()
        {
            IsMeasuring = false;
            await SaveManualMeasurementAsync(); // Сохраняем данные после остановки
        }

        private async Task SimulateVibrometerData(IVPenControl controller, OperationToken token)
        {
            // Имитация сбора данных с виброметра
            var random = new Random();

            for (int i = 0; i < 10 && IsMeasuring; i++)
            {
                Velocity = Math.Round(random.NextDouble() * 10, 2);
                Temperature = Math.Round(20 + random.NextDouble() * 10, 2);
                Acceleration = Math.Round(random.NextDouble() * 5, 2);
                Kurtosis = Math.Round(random.NextDouble() * 3, 2);

                await Task.Delay(500);
            }
        }

        private void ClearForm()
        {
            Velocity = 0;
            Temperature = 0;
            Acceleration = 0;
            Kurtosis = 0;
        }
    }
}