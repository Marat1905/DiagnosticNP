using DiagnosticNP.Models;
using DiagnosticNP.Services;
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

        public MainViewModel()
        {
            _nfcService = new NfcService();
            _diagnosticData = new DiagnosticData();
            _nfcService.TagScanned += OnTagScanned;

            InitializeCommands();
            InitializeNfc();
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

        public ICommand StartListeningCommand { get; private set; }
        public ICommand StopListeningCommand { get; private set; }
        public ICommand ReadTagCommand { get; private set; }

        private void InitializeCommands()
        {
            StartListeningCommand = new Command(async () => await StartListening());
            StopListeningCommand = new Command(StopListening);
            ReadTagCommand = new Command(async () => await ReadTag());
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

        //public void Dispose()
        //{
        //    _nfcService?.Dispose();
        //}
    }
}