using DiagnosticNP.Models.Vibrometer;
using DiagnosticNP.Services.Bluetooth;
using DiagnosticNP.Services.Utils;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DiagnosticNP.Services.Vibrometer
{
    public class VibrometerService : IVibrometerService, IDisposable
    {
        private readonly IVPenControl _controller;
        private string _currentDeviceAddress;
        private bool _isInitialized;
        private bool _isDisposed;

        // Раздельные семафоры для разных типов операций
        private readonly SemaphoreSlim _stateLock = new SemaphoreSlim(1, 1); // Для изменения состояния
        private readonly SemaphoreSlim _pollingLock = new SemaphoreSlim(1, 1); // Для операций опроса

        // Состояние
        private volatile bool _isPolling;
        private volatile bool _isBusy;
        private VibrometerData _currentData = new VibrometerData();

        public VibrometerService()
        {
            _controller = VPenControlManager.GetController();
        }

        public event EventHandler<VibrometerDataReceivedEventArgs> DataReceived;
        public event EventHandler<string> ErrorOccurred;
        public event EventHandler<string> StatusChanged;
        public event EventHandler<bool> PollingStateChanged;

        public bool IsPolling => _isPolling;
        public bool IsConnected => _controller?.IsConnected ?? false;
        public bool IsBusy => _isBusy;
        public VibrometerData CurrentData => _currentData;

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            try
            {
                await _stateLock.WaitAsync();
                SetBusy(true);

                // Запускаем сканирование BLE устройств
                if (!BluetoothController.LeScanner.IsRunning)
                    BluetoothController.LeScanner.Restart();

                BluetoothController.LeScanner.NewData += OnAdvertisingDataReceived;
                _isInitialized = true;

                OnStatusChanged("Виброметр инициализирован");
            }
            catch (Exception ex)
            {
                OnError($"Ошибка инициализации виброметра: {ex.Message}");
                throw;
            }
            finally
            {
                SetBusy(false);
                _stateLock.Release();
            }
        }

        public async Task<bool> ConnectAsync()
        {
            if (string.IsNullOrEmpty(_currentDeviceAddress))
            {
                OnError("Адрес устройства виброметра не найден");
                return false;
            }

            try
            {
                // Не используем блокировку здесь, чтобы избежать взаимоблокировки
                SetBusy(true);
                OnStatusChanged("Подключение к виброметру...");

                var token = new OperationToken();
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                {
                    var connectTask = _controller.ConnectAsync(_currentDeviceAddress, token);
                    var completedTask = await Task.WhenAny(connectTask, Task.Delay(-1, cts.Token));

                    if (completedTask == connectTask && connectTask.Result)
                    {
                        OnStatusChanged("Подключение к виброметру установлено");
                        return true;
                    }
                    else
                    {
                        OnError("Не удалось подключиться к виброметру (таймаут)");
                        return false;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                OnError("Таймаут подключения к виброметру");
                return false;
            }
            catch (Exception ex)
            {
                OnError($"Ошибка подключения к виброметру: {ex.Message}");
                return false;
            }
            finally
            {
                SetBusy(false);
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                SetBusy(true);

                if (IsPolling)
                    await StopPollingAsync();

                await _controller.Disconnect();
                OnStatusChanged("Отключено от виброметра");
            }
            catch (Exception ex)
            {
                OnError($"Ошибка отключения от виброметра: {ex.Message}");
            }
            finally
            {
                SetBusy(false);
            }
        }

        public async Task StartPollingAsync()
        {
            if (_isPolling) return;

            try
            {
                // Используем отдельный семафор для операций опроса
                if (!await _pollingLock.WaitAsync(TimeSpan.FromSeconds(1)))
                {
                    OnError("Не удалось начать опрос: операция занята");
                    return;
                }

                SetBusy(true);

                if (string.IsNullOrEmpty(_currentDeviceAddress))
                {
                    OnError("Виброметр не найден");
                    return;
                }

                // Подключаемся без блокировки
                if (!await ConnectAsync())
                    return;

                // Останавливаем автоматическое сканирование для снижения помех
                BluetoothController.LeScanner.Stop();
                await Task.Delay(500);

                _isPolling = true;
                PollingStateChanged?.Invoke(this, true);
                OnStatusChanged("Запуск опроса виброметра...");

                // Запускаем фоновый опрос в отдельной задаче
                _ = Task.Run(() => PollingLoop().ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        OnError($"Ошибка в цикле опроса: {t.Exception?.Message}");
                    }
                }));
            }
            catch (Exception ex)
            {
                OnError($"Ошибка запуска опроса: {ex.Message}");
            }
            finally
            {
                SetBusy(false);
                _pollingLock.Release();
            }
        }

        public async Task StopPollingAsync()
        {
            if (!_isPolling) return;

            try
            {
                if (!await _pollingLock.WaitAsync(TimeSpan.FromSeconds(2)))
                {
                    OnError("Не удалось остановить опрос: операция занята");
                    return;
                }

                _isPolling = false;
                PollingStateChanged?.Invoke(this, false);

                // Восстанавливаем сканирование
                await Task.Delay(1000);
                if (!BluetoothController.LeScanner.IsRunning)
                    BluetoothController.LeScanner.Start();

                OnStatusChanged("Опрос виброметра остановлен");
            }
            catch (Exception ex)
            {
                OnError($"Ошибка при остановке опроса: {ex.Message}");
            }
            finally
            {
                _pollingLock.Release();
            }
        }

        private async Task PollingLoop()
        {
            using (var token = new OperationToken())
            {
                int successfulPolls = 0;
                int consecutiveErrors = 0;
                const int maxConsecutiveErrors = 3;

                Debug.WriteLine("=== ЗАПУСК ОПРОСА ВИБРОМЕТРА ===");

                try
                {
                    // Запускаем измерение
                    await _controller.Start(_currentDeviceAddress, token);

                    while (_isPolling) // Используем volatile поле без блокировки
                    {
                        try
                        {
                            // Проверяем соединение
                            if (!_controller.IsConnected)
                            {
                                consecutiveErrors++;
                                if (consecutiveErrors > maxConsecutiveErrors)
                                    break;

                                await Task.Delay(300);
                                if (await _controller.ConnectAsync(_currentDeviceAddress, token))
                                {
                                    consecutiveErrors = 0;
                                    await _controller.Start(_currentDeviceAddress, token);
                                }
                                continue;
                            }

                            // Чтение данных
                            var data = await _controller.ReadUserData(_currentDeviceAddress, token);
                            var vibrometerData = new VibrometerData
                            {
                                Timestamp = DateTime.Now,
                                Velocity = Math.Round(data.Values[0] * 0.01, 2),
                                Acceleration = Math.Round(data.Values[1] * 0.01, 2),
                                Kurtosis = Math.Round(data.Values[2] * 0.01, 2),
                                Temperature = Math.Round(data.Values[3] * 0.01, 2),
                                DeviceAddress = _currentDeviceAddress,
                                Source = DataSource.Polling
                            };

                            UpdateCurrentData(vibrometerData);
                            successfulPolls++;
                            consecutiveErrors = 0;

                            await Task.Delay(800);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Ошибка в цикле опроса: {ex.Message}");
                            consecutiveErrors++;

                            if (consecutiveErrors > maxConsecutiveErrors)
                                break;

                            await Task.Delay(500);
                        }
                    }
                }
                finally
                {
                    // Корректное завершение
                    try
                    {
                        if (_controller.IsConnected)
                        {
                            await _controller.Stop(_currentDeviceAddress, token);
                            await Task.Delay(200);
                            await _controller.Disconnect();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Ошибка при остановке: {ex.Message}");
                    }

                    Debug.WriteLine($"ОПРОС ЗАВЕРШЕН. Успешных опросов: {successfulPolls}");

                    // Сбрасываем состояние опроса
                    if (_isPolling)
                    {
                        _isPolling = false;
                        PollingStateChanged?.Invoke(this, false);
                    }
                }
            }
        }

        private void OnAdvertisingDataReceived(object sender, BleDataEventArgs e)
        {
            if (_isPolling) return; // Не обрабатываем advertising во время опроса

            try
            {
                _currentDeviceAddress = e.Data.Address;
                var data = e.Data.Data.BytesToStruct<ViPenAdvertising>();

                var vibrometerData = new VibrometerData
                {
                    Timestamp = DateTime.Now,
                    Velocity = Math.Round(data.Velocity * 0.01, 2),
                    Acceleration = Math.Round(data.Acceleration * 0.01, 2),
                    Kurtosis = Math.Round(data.Kurtosis * 0.01, 2),
                    Temperature = Math.Round(data.Temperature * 0.01, 2),
                    DeviceAddress = e.Data.Address,
                    Source = DataSource.Advertising
                };

                UpdateCurrentData(vibrometerData);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка обработки advertising данных: {ex.Message}");
            }
        }

        private void UpdateCurrentData(VibrometerData data)
        {
            _currentData = data;
            DataReceived?.Invoke(this, new VibrometerDataReceivedEventArgs(data));
        }

        private void SetBusy(bool busy)
        {
            _isBusy = busy;
        }

        private void OnError(string message)
        {
            ErrorOccurred?.Invoke(this, message);
            Debug.WriteLine($"VibrometerService Error: {message}");
        }

        private void OnStatusChanged(string message)
        {
            StatusChanged?.Invoke(this, message);
            Debug.WriteLine($"VibrometerService Status: {message}");
        }

        public async Task CleanupAsync()
        {
            try
            {
                BluetoothController.LeScanner.NewData -= OnAdvertisingDataReceived;

                if (_isPolling)
                    await StopPollingAsync();

                BluetoothController.LeScanner.Stop();
            }
            catch (Exception ex)
            {
                OnError($"Ошибка при очистке ресурсов: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                try
                {
                    CleanupAsync().Wait(3000);
                }
                catch { }
                _controller?.Dispose();
                _stateLock?.Dispose();
                _pollingLock?.Dispose();
            }
        }
    }
}