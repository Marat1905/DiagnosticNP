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
        private readonly MainViewModel _mainViewModel;
        private readonly IVPenControl _vPenControl;
        private Measurement _measurement;
        private bool _isAutoMeasuring;

        public ControlPoint ControlPoint { get; }

        public Measurement Measurement
        {
            get => _measurement;
            set => SetProperty(ref _measurement, value);
        }

        public bool IsAutoMeasuring
        {
            get => _isAutoMeasuring;
            set => SetProperty(ref _isAutoMeasuring, value);
        }

        public ICommand SaveMeasurementCommand { get; private set; }
        public ICommand AutoMeasureCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand ManualInputCommand { get; private set; }

        public MeasurementViewModel(ControlPoint controlPoint, MainViewModel mainViewModel)
        {
            ControlPoint = controlPoint ?? throw new ArgumentNullException(nameof(controlPoint));
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            _vPenControl = VPenControlManager.GetController();

            InitializeMeasurement();
            InitializeCommands();
        }

        private void InitializeMeasurement()
        {
            Measurement = new Measurement
            {
                ControlPointId = ControlPoint.Id,
                ControlPointPath = ControlPoint.FullPath,
                MeasurementTime = DateTime.Now,
                MeasurementType = ControlPoint.MeasurementType,
                IsAutoMeasurement = false
            };
        }

        private void InitializeCommands()
        {
            SaveMeasurementCommand = new Command(async () => await SaveMeasurement(),
                () => !IsBusy && IsMeasurementValid());
            AutoMeasureCommand = new Command(async () => await AutoMeasure(),
                () => !IsBusy && !IsAutoMeasuring);
            CancelCommand = new Command(async () => await Cancel());
            ManualInputCommand = new Command(() => EnableManualInput());

            // Обновляем доступность команд при изменении свойств
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(IsBusy) || e.PropertyName == nameof(IsAutoMeasuring))
                {
                    (SaveMeasurementCommand as Command)?.ChangeCanExecute();
                    (AutoMeasureCommand as Command)?.ChangeCanExecute();
                }
            };
        }

        private bool IsMeasurementValid()
        {
            return Measurement != null &&
                   !string.IsNullOrEmpty(Measurement.MeasurementType) &&
                   Measurement.MeasurementTime != DateTime.MinValue;
        }

        private void EnableManualInput()
        {
            Measurement.IsAutoMeasurement = false;
            Measurement.MeasurementTime = DateTime.Now;
        }

        private async Task SaveMeasurement()
        {
            if (IsBusy || !IsMeasurementValid()) return;

            IsBusy = true;
            try
            {
                // Валидация данных
                if (Measurement.Velocity < 0 || Measurement.Temperature < -50 || Measurement.Temperature > 150)
                {
                    await Application.Current.MainPage.DisplayAlert("Ошибка",
                        "Проверьте корректность введенных данных", "OK");
                    return;
                }

                // Сохраняем в основной ViewModel
                _mainViewModel.AddMeasurement(Measurement);

                await Application.Current.MainPage.DisplayAlert("Успех",
                    "Замер сохранен локально", "OK");

                // Возвращаемся на предыдущую страницу
                await Application.Current.MainPage.Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения замера: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    $"Ошибка сохранения: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task AutoMeasure()
        {
            if (IsBusy || IsAutoMeasuring) return;

            IsBusy = true;
            IsAutoMeasuring = true;

            string deviceAddress = null; // Здесь должен быть реальный адрес устройства

            try
            {
                System.Diagnostics.Debug.WriteLine("=== ЗАПУСК АВТОМАТИЧЕСКОГО ЗАМЕРА ===");

                // Используем виброметр для автоматического замера
                var token = new OperationToken();

                // Подключаемся к виброметру
                System.Diagnostics.Debug.WriteLine("Подключение к виброметру...");
                if (!await _vPenControl.ConnectAsync(deviceAddress, token))
                {
                    await Application.Current.MainPage.DisplayAlert("Ошибка",
                        "Не удалось подключиться к виброметру. Проверьте подключение устройства.", "OK");
                    return;
                }

                // Запускаем измерение
                System.Diagnostics.Debug.WriteLine("Запуск измерения...");
                await _vPenControl.Start(deviceAddress, token);
                await Task.Delay(1000); // Даем время на стабилизацию

                // Получаем данные несколько раз для усреднения
                const int measurementCount = 3;
                double totalVelocity = 0, totalAcceleration = 0, totalKurtosis = 0, totalTemperature = 0;

                for (int i = 0; i < measurementCount; i++)
                {
                    try
                    {
                        var data = await _vPenControl.ReadUserData(deviceAddress, token);

                        totalVelocity += data.Values[0] * 0.01;
                        totalAcceleration += data.Values[1] * 0.01;
                        totalKurtosis += data.Values[2] * 0.01;
                        totalTemperature += data.Values[3] * 0.01;

                        // Обновляем прогресс
                        token.Progress = (i + 1) / (double)measurementCount;

                        await Task.Delay(500); // Пауза между замерами
                    }
                    catch (Exception readEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка чтения данных (попытка {i + 1}): {readEx.Message}");
                    }
                }

                // Усредняем значения
                Measurement.Velocity = Math.Round(totalVelocity / measurementCount, 2);
                Measurement.Acceleration = Math.Round(totalAcceleration / measurementCount, 2);
                Measurement.Kurtosis = Math.Round(totalKurtosis / measurementCount, 2);
                Measurement.Temperature = Math.Round(totalTemperature / measurementCount, 2);
                Measurement.MeasurementTime = DateTime.Now;
                Measurement.IsAutoMeasurement = true;
                Measurement.DeviceAddress = deviceAddress;

                // Останавливаем измерение
                await _vPenControl.Stop(deviceAddress, token);

                System.Diagnostics.Debug.WriteLine("=== АВТОМАТИЧЕСКИЙ ЗАМЕР ЗАВЕРШЕН ===");
                System.Diagnostics.Debug.WriteLine($"Результаты: V={Measurement.Velocity}, A={Measurement.Acceleration}, K={Measurement.Kurtosis}, T={Measurement.Temperature}");

                await Application.Current.MainPage.DisplayAlert("Успех",
                    "Автоматический замер выполнен успешно", "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"КРИТИЧЕСКАЯ ОШИБКА АВТОМАТИЧЕСКОГО ЗАМЕРА: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    $"Ошибка автоматического замера: {ex.Message}", "OK");

                // В случае ошибки переключаем на ручной ввод
                EnableManualInput();
            }
            finally
            {
                try
                {
                    // Гарантированно отключаемся от устройства
                    await _vPenControl.Disconnect();
                }
                catch (Exception disconnectEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка отключения: {disconnectEx.Message}");
                }

                IsBusy = false;
                IsAutoMeasuring = false;
            }
        }

        private async Task Cancel()
        {
            if (IsAutoMeasuring)
            {
                var confirm = await Application.Current.MainPage.DisplayAlert("Подтверждение",
                    "Прервать автоматический замер?", "Да", "Нет");

                if (!confirm) return;

                // Останавливаем измерение
                try
                {
                    await _vPenControl.Stop("адрес_устройства", new OperationToken());
                    await _vPenControl.Disconnect();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при отмене: {ex.Message}");
                }

                IsAutoMeasuring = false;
            }

            await Application.Current.MainPage.Navigation.PopAsync();
        }

        public void OnDisappearing()
        {
            try
            {
                if (IsAutoMeasuring)
                {
                    // Экстренное отключение при закрытии страницы
                    _vPenControl?.Disconnect();
                    _vPenControl?.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при очистке ресурсов MeasurementViewModel: {ex.Message}");
            }
        }
    }
}