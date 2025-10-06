using DiagnosticNP.Models;
using DiagnosticNP.Services;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace DiagnosticNP.ViewModels
{
    public class MeasurementViewModel : BaseViewModel
    {
        private readonly IDatabaseService _databaseService;
        private readonly ControlPoint _controlPoint;

        private double _vibrationSpeed;
        private double _temperature;
        private double _acceleration;
        private double _kurtosis;
        private DateTime _measurementTime;

        public MeasurementViewModel(IDatabaseService databaseService, ControlPoint controlPoint)
        {
            _databaseService = databaseService;
            _controlPoint = controlPoint;
            _measurementTime = DateTime.Now;

            InitializeCommands();
        }

        public ControlPoint ControlPoint => _controlPoint;

        public double VibrationSpeed
        {
            get => _vibrationSpeed;
            set => SetProperty(ref _vibrationSpeed, value);
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

        public ICommand SaveMeasurementCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }

        private void InitializeCommands()
        {
            SaveMeasurementCommand = new Command(async () => await SaveMeasurement());
            CancelCommand = new Command(async () => await Cancel());
        }

        private async Task SaveMeasurement()
        {
            if (IsBusy) return;

            IsBusy = true;
            StatusMessage = "Сохранение замера...";

            try
            {
                var measurement = new MeasurementData
                {
                    ControlPointId = _controlPoint.Id,
                    VibrationSpeed = VibrationSpeed,
                    Temperature = Temperature,
                    Acceleration = Acceleration,
                    Kurtosis = Kurtosis,
                    MeasurementTime = MeasurementTime,
                    CreatedAt = DateTime.Now
                };

                await _databaseService.SaveMeasurementAsync(measurement);

                StatusMessage = "Замер успешно сохранен";
                StatusColor = Color.Green;

                // Возвращаемся назад через 1 секунду
                await Task.Delay(1000);
                await Application.Current.MainPage.Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка сохранения: {ex.Message}";
                StatusColor = Color.Red;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task Cancel()
        {
            await Application.Current.MainPage.Navigation.PopAsync();
        }
    }
}