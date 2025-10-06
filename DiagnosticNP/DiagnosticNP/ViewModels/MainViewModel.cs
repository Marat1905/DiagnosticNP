using DiagnosticNP.Models;
using DiagnosticNP.Services;
using DiagnosticNP.Views;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace DiagnosticNP.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly INfcService _nfcService;
        private readonly IApiService _apiService;
        private readonly IDatabaseService _databaseService;

        private DiagnosticData _diagnosticData;
        private string _nfcStatusText;
        private Color _nfcStatusColor;

        public MainViewModel()
        {
            _nfcService = new NfcService();
            _apiService = new ApiService();
            _databaseService = new DatabaseService();

            _diagnosticData = new DiagnosticData();
            _nfcStatusText = "NFC не инициализирован";
            _nfcStatusColor = Color.Gray;

            InitializeCommands();
        }

        public DiagnosticData DiagnosticData
        {
            get => _diagnosticData;
            set => SetProperty(ref _diagnosticData, value);
        }

        public string NfcStatusText
        {
            get => _nfcStatusText;
            set => SetProperty(ref _nfcStatusText, value);
        }

        public Color NfcStatusColor
        {
            get => _nfcStatusColor;
            set => SetProperty(ref _nfcStatusColor, value);
        }

        public ICommand LoadControlPointsCommand { get; private set; }
        public ICommand ShowControlPointsTreeCommand { get; private set; }
        public ICommand UploadDataCommand { get; private set; }
        public ICommand ClearMeasurementsCommand { get; private set; }

        private void InitializeCommands()
        {
            LoadControlPointsCommand = new Command(async () => await LoadControlPoints());
            ShowControlPointsTreeCommand = new Command(async () => await ShowControlPointsTree());
            UploadDataCommand = new Command(async () => await UploadData());
            ClearMeasurementsCommand = new Command(async () => await ClearMeasurements());
        }

        public void OnAppearing()
        {
            InitializeNfc();
        }

        private async void InitializeNfc()
        {
            try
            {
                var isAvailable = await _nfcService.IsAvailableAsync();
                if (!isAvailable)
                {
                    NfcStatusText = "NFC не поддерживается";
                    NfcStatusColor = Color.Red;
                    return;
                }

                var isEnabled = await _nfcService.IsEnabledAsync();
                if (!isEnabled)
                {
                    NfcStatusText = "NFC отключен";
                    NfcStatusColor = Color.Orange;
                    return;
                }

                _nfcService.TagScanned += OnTagScanned;
                _nfcService.StartListening();

                NfcStatusText = "Сканирование NFC активно";
                NfcStatusColor = Color.Green;
            }
            catch (Exception ex)
            {
                NfcStatusText = $"Ошибка NFC: {ex.Message}";
                NfcStatusColor = Color.Red;
            }
        }

        private async Task LoadControlPoints()
        {
            if (IsBusy) return;

            IsBusy = true;
            StatusMessage = "Загрузка контрольных точек...";
            StatusColor = Color.Blue;

            try
            {
                var controlPoints = await _apiService.GetControlPointsAsync();

                if (controlPoints != null && controlPoints.Count > 0)
                {
                    await _databaseService.SaveControlPointsAsync(controlPoints);
                    StatusMessage = $"Загружено {controlPoints.Count} контрольных точек";
                    StatusColor = Color.Green;
                }
                else
                {
                    StatusMessage = "Не удалось загрузить контрольные точки";
                    StatusColor = Color.Red;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки: {ex.Message}";
                StatusColor = Color.Red;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ShowControlPointsTree()
        {
            var page = new ControlPointsPage
            {
                BindingContext = new ControlPointsViewModel(_databaseService, DiagnosticData.NFCData)
            };
            await Application.Current.MainPage.Navigation.PushAsync(page);
        }

        private async Task UploadData()
        {
            if (IsBusy) return;

            IsBusy = true;
            StatusMessage = "Выгрузка данных на сервер...";
            StatusColor = Color.Blue;

            try
            {
                var measurements = await _databaseService.GetAllMeasurementsAsync();
                var success = await _apiService.UploadMeasurementsAsync(measurements);

                if (success)
                {
                    StatusMessage = $"Успешно выгружено {measurements.Count} замеров";
                    StatusColor = Color.Green;
                }
                else
                {
                    StatusMessage = "Ошибка выгрузки данных";
                    StatusColor = Color.Red;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка выгрузки: {ex.Message}";
                StatusColor = Color.Red;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ClearMeasurements()
        {
            var result = await Application.Current.MainPage.DisplayAlert(
                "Подтверждение",
                "Вы уверены, что хотите очистить все данные замеров?",
                "Да", "Нет");

            if (result)
            {
                await _databaseService.ClearMeasurementsAsync();
                StatusMessage = "Данные замеров очищены";
                StatusColor = Color.Green;
            }
        }

        private void OnTagScanned(object sender, string nfcData)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                DiagnosticData.NFCData = nfcData;
                DiagnosticData.ScanTime = DateTime.Now;

                // Автоматически показываем дерево контрольных точек при сканировании NFC
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await ShowAlert("NFC Метка", $"Метка просканирована: {nfcData}", "OK");
                    await ShowControlPointsTree();
                });
            });
        }
    }
}