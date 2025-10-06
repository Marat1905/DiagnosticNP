using Plugin.NFC;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace DiagnosticNP.Services.Nfc
{
    public interface INfcService
    {
        Task<bool> IsAvailableAsync();
        Task<bool> IsEnabledAsync();
        Task<string> ReadTagAsync();
        void StartListening();
        void StopListening();
        event EventHandler<string> TagScanned;
    }

    public class NfcService : INfcService
    {
        private bool _isListening;

        public event EventHandler<string> TagScanned;

        public async Task<bool> IsAvailableAsync()
        {
            return await Task.Run(() => CrossNFC.IsSupported);
        }

        public async Task<bool> IsEnabledAsync()
        {
            if (!CrossNFC.IsSupported)
                return false;

            try
            {
                return CrossNFC.Current.IsAvailable;
            }
            catch
            {
                return false;
            }
        }

        public void StartListening()
        {
            if (!CrossNFC.IsSupported || _isListening)
                return;

            try
            {
                CrossNFC.Current.OnMessageReceived += OnMessageReceived;
                CrossNFC.Current.StartListening();
                _isListening = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка запуска прослушивания: {ex.Message}");
            }
        }

        public void StopListening()
        {
            if (!_isListening)
                return;

            try
            {
                CrossNFC.Current.StopListening();
                CrossNFC.Current.OnMessageReceived -= OnMessageReceived;
                _isListening = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка остановки прослушивания: {ex.Message}");
            }
        }

        public async Task<string> ReadTagAsync()
        {
            if (!CrossNFC.IsSupported)
                return string.Empty;

            try
            {
                var tcs = new TaskCompletionSource<string>();
                NdefMessageReceivedEventHandler handler = null;

                handler = (tagInfo) =>
                {
                    CrossNFC.Current.OnMessageReceived -= handler;
                    var tagData = ProcessTag(tagInfo);
                    tcs.TrySetResult(tagData);
                };

                CrossNFC.Current.OnMessageReceived += handler;
                CrossNFC.Current.StartListening();

                // Ждем 30 секунд для чтения тега
                var timeoutTask = Task.Delay(30000);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

                CrossNFC.Current.StopListening();
                CrossNFC.Current.OnMessageReceived -= handler;

                if (completedTask == tcs.Task)
                    return await tcs.Task;
                else
                    return string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка чтения тега: {ex.Message}");
                return string.Empty;
            }
        }

        private void OnMessageReceived(ITagInfo tagInfo)
        {
            if (tagInfo == null)
                return;

            var tagData = ProcessTag(tagInfo);
            if (!string.IsNullOrEmpty(tagData))
            {
                TagScanned?.Invoke(this, tagData);
            }
        }

        private string ProcessTag(ITagInfo tagInfo)
        {
            try
            {
                if (tagInfo?.Records != null && tagInfo.Records.Length > 0)
                {
                    foreach (var record in tagInfo.Records)
                    {
                        // Получаем текст из записи
                        if (!string.IsNullOrEmpty(record.Message))
                        {
                            return record.Message;
                        }
                    }
                    return "Тег прочитан, но данные не распознаны";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка обработки тега: {ex.Message}");
            }
            return string.Empty;
        }

        public void Dispose()
        {
            StopListening();
        }
    }
}