using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using DiagnosticNP.Models.Vibrometer;
using DiagnosticNP.Services.Bluetooth;
using Java.Util;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using DiagnosticNP.Services.Utils;

[assembly: Dependency(typeof(DiagnosticNP.Droid.Bluetooth.VPenControl))]
namespace DiagnosticNP.Droid.Bluetooth
{
    public class VPenControl : IVPenControl
    {
        private string _currentDeviceAddress;
        private bool _isDisposing = false;
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);
        private bool _isConnecting = false;

        private static BluetoothAdapter GetAdapter()
        {
            BluetoothAdapter adapter = BluetoothAdapter.DefaultAdapter;

            if (adapter == null)
                throw new Exception("No Bluetooth adapter found.");

            if (!adapter.IsEnabled)
            {
                adapter.Enable();

                for (int i = 0; i < 64; i++)
                {
                    if (adapter.State == State.On)
                        break;
                    else
                        Thread.Sleep(10);
                }
            }

            if (!adapter.IsEnabled)
                throw new Exception("Bluetooth adapter is disabled.");
            return adapter;
        }

        private static BluetoothDevice FindBluetoothDevice(string deviceAddress)
        {
            var adapter = GetAdapter();

            var bytes = deviceAddress.Trim().Split(':').Select(s => Convert.ToByte(s, 16)).ToArray();

            BluetoothDevice device = adapter.GetRemoteDevice(bytes);
            return device;
        }

        public bool IsConnected => Gatt != null && Gatt.IsGattConnected;

        public GATTConetionTools Gatt { get; private set; }

        public bool LongFrame { get; private set; }

        public async Task<bool> ConnectAsync(string connection, OperationToken token)
        {
            if (_isConnecting)
                return false;

            _isConnecting = true;
            await _connectionLock.WaitAsync();

            try
            {
                if (_isDisposing) return false;

                // Если уже подключены к тому же устройству, возвращаем true
                if (IsConnected && _currentDeviceAddress == connection)
                    return true;

                await QuickDisconnect();

                if (Gatt == null)
                    Gatt = new GATTConetionTools();

                var device = await Task.Run(() => FindBluetoothDevice(connection));

                System.Diagnostics.Debug.WriteLine("Подключение к виброметру...");

                // Подключение с повторными попытками
                bool connectSuccess = false;
                for (int i = 0; i < 2; i++)
                {
                    try
                    {
                        if (await Gatt.ConnectGattAsync(device, TimeSpan.FromSeconds(8), false))
                        {
                            connectSuccess = true;
                            break;
                        }
                    }
                    catch
                    {
                        if (i == 1) throw;
                        await Task.Delay(500);
                    }
                }

                if (!connectSuccess)
                {
                    System.Diagnostics.Debug.WriteLine("Не удалось установить соединение GATT");
                    return false;
                }

                // Обнаружение сервисов
                if (!await Gatt.DiscoverServicesAsync(TimeSpan.FromSeconds(5)))
                {
                    System.Diagnostics.Debug.WriteLine("Не удалось обнаружить сервисы");
                    await QuickDisconnect();
                    return false;
                }

                // Проверяем, что нужный сервис доступен
                var service = Gatt.Gatt.GetService(SERVICE);
                if (service == null)
                {
                    System.Diagnostics.Debug.WriteLine("Требуемый сервис не найден");
                    await QuickDisconnect();
                    return false;
                }

                // Опционально: изменение MTU
                try
                {
                    LongFrame = false;
                    if ((await Gatt.ChangeMtuAsync(TimeSpan.FromSeconds(3), 512)) > 100)
                        LongFrame = true;
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine("MTU change failed, continuing with default");
                }

                if (IsConnected)
                {
                    try
                    {
                        Gatt.Gatt.RequestConnectionPriority(GattConnectionPriority.High);
                    }
                    catch (Exception priEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Connection priority change failed: {priEx.Message}");
                    }
                }

                _currentDeviceAddress = connection;
                System.Diagnostics.Debug.WriteLine("Подключение успешно установлено");
                return IsConnected;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка подключения: {ex.Message}");
                await QuickDisconnect();
                return false;
            }
            finally
            {
                _isConnecting = false;
                _connectionLock.Release();
            }
        }

        private async Task QuickDisconnect()
        {
            try
            {
                if (Gatt != null)
                {
                    await Gatt.DisconnectGattAsync(TimeSpan.FromSeconds(2));
                    Gatt.Gatt?.Close();
                    await Task.Delay(100);
                    Gatt.Dispose();
                    Gatt = null;
                }
                _currentDeviceAddress = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка быстрого отключения: {ex.Message}");
                Gatt?.Dispose();
                Gatt = null;
                _currentDeviceAddress = null;
            }
        }

        public async Task Disconnect()
        {
            await _connectionLock.WaitAsync();
            try
            {
                await QuickDisconnect();
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        public void Dispose()
        {
            _isDisposing = true;
            try
            {
                _connectionLock?.Wait(TimeSpan.FromSeconds(3));
                QuickDisconnect().Wait(TimeSpan.FromSeconds(3));
                Gatt?.Dispose();
            }
            catch { }
            finally
            {
                _connectionLock?.Dispose();
                Gatt = null;
                _currentDeviceAddress = null;
            }
        }

        private static readonly UUID SERVICE = UUID.FromString("378B4074-C2B8-45FF-894C-418739E60000");

        private static readonly UUID C_READING = UUID.FromString("3890BE9F-3A5E-459D-B799-102365770001");
        private static readonly UUID C_CTRL = UUID.FromString("3890BE9F-3A5E-459D-B799-102365770002");

        private static readonly UUID C_WAV_CTRL = UUID.FromString("3890BE9F-3A5E-459D-B799-102365770003");
        private static readonly UUID C_WAV_READ = UUID.FromString("3890BE9F-3A5E-459D-B799-102365770004");

        private const ushort ViPen_Command_Start = 0x0001;
        private const ushort ViPen_Command_Stop = 0x0002;
        private const ushort ViPen_Command_Off = 0x0003;
        private const ushort ViPen_Command_Idle = 0x0004;

        private const ushort ViPen_State_Stoped = (0 << 0);
        private const ushort ViPen_State_Started = (1 << 0);
        private const ushort ViPen_State_NoData = (0 << 1);
        private const ushort ViPen_State_Data = (1 << 1);

        private const byte ViPen_Get_Data_Vel = 0x10;
        private const byte ViPen_Get_Data_Acc = 0x11;

        public async Task<bool> StartMeasurementAsync(string connection, OperationToken token)
        {
            await _connectionLock.WaitAsync();
            try
            {
                if (!IsConnected)
                    throw new Exception("GATT is not connected!");

                var service = Gatt.Gatt.GetService(SERVICE);
                if (service == null)
                    throw new Exception("Service discover error!");

                var cControl = service.GetCharacteristic(C_CTRL);
                if (cControl == null)
                    throw new Exception("Cant find characteristics!");

                try
                {
                    if (!await Gatt.Write(BitConverter.GetBytes(ViPen_Command_Start), cControl, TimeSpan.FromSeconds(2), false))
                        throw new Exception("Characteristics set error!");

                    var watch = new Stopwatch();
                    watch.Start();

                    while (watch.ElapsedMilliseconds < 5000)
                    {
                        var data = await Gatt.Read(cControl, TimeSpan.FromSeconds(1), false);
                        var status = BitConverter.ToUInt16(data, 0);
                        if ((status & ViPen_State_Data) > 0)
                            return true;
                    }

                    return false;
                }
                finally
                {
                    if (!await Gatt.Write(BitConverter.GetBytes(ViPen_Command_Stop), cControl, TimeSpan.FromSeconds(2), false))
                        throw new Exception("Characteristics set error!");
                }
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        public async Task Start(string connection, OperationToken token)
        {
            await _connectionLock.WaitAsync();
            try
            {
                if (!IsConnected)
                    throw new Exception("GATT is not connected!");

                var service = Gatt.Gatt.GetService(SERVICE);
                if (service == null)
                    throw new Exception("Service discover error!");

                var cControl = service.GetCharacteristic(C_CTRL);
                if (cControl == null)
                    throw new Exception("Cant find characteristics!");

                // Проверяем состояние перед отправкой команды
                try
                {
                    var currentState = await Gatt.Read(cControl, TimeSpan.FromSeconds(1), false);
                    var status = BitConverter.ToUInt16(currentState, 0);

                    // Если устройство уже в нужном состоянии, не отправляем команду повторно
                    if ((status & ViPen_State_Started) > 0)
                        return;
                }
                catch
                {
                    // Игнорируем ошибки чтения состояния, продолжаем отправку команды
                }

                // Отправляем команду START с повторными попытками
                for (int i = 0; i < 2; i++)
                {
                    try
                    {
                        if (await Gatt.Write(BitConverter.GetBytes(ViPen_Command_Start), cControl, TimeSpan.FromSeconds(2), false))
                        {
                            System.Diagnostics.Debug.WriteLine("Команда START отправлена успешно");
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка отправки START (попытка {i + 1}): {ex.Message}");
                        if (i == 1) throw;
                        await Task.Delay(200);
                    }
                }
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        public async Task Stop(string connection, OperationToken token)
        {
            await _connectionLock.WaitAsync();
            try
            {
                if (!IsConnected)
                    return; // Уже отключено

                var service = Gatt.Gatt.GetService(SERVICE);
                if (service == null)
                    return;

                var cControl = service.GetCharacteristic(C_CTRL);
                if (cControl == null)
                    return;

                // Отправляем команду STOP с повторными попытками
                for (int i = 0; i < 2; i++)
                {
                    try
                    {
                        if (await Gatt.Write(BitConverter.GetBytes(ViPen_Command_Stop), cControl, TimeSpan.FromSeconds(1), false))
                        {
                            System.Diagnostics.Debug.WriteLine("Команда STOP отправлена успешно");
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка отправки STOP (попытка {i + 1}): {ex.Message}");
                        if (i == 1) throw;
                        await Task.Delay(200);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в методе Stop: {ex.Message}");
                // Не бросаем исключение наружу, чтобы не прерывать процесс остановки
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        public async Task<stUser_DataViPen> ReadUserData(string connection, OperationToken token)
        {
            await _connectionLock.WaitAsync();
            try
            {
                if (!IsConnected)
                    throw new Exception("GATT is not connected!");

                var service = Gatt.Gatt.GetService(SERVICE);
                if (service == null)
                    throw new Exception("Service discover error!");

                var cReading = service.GetCharacteristic(C_READING);
                if (cReading == null)
                    throw new Exception("Cant find characteristics!");

                // Быстрое чтение с коротким таймаутом
                var result = await Gatt.Read(cReading, TimeSpan.FromSeconds(2), false);
                return result.BytesToStruct<stUser_DataViPen>();
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        public async Task<VPenData> Download(string connection, OperationToken token)
        {
            await _connectionLock.WaitAsync();
            try
            {
                if (!IsConnected)
                    throw new Exception("GATT is not connected!");

                var service = Gatt.Gatt.GetService(SERVICE);
                if (service == null)
                    throw new Exception("Service discover error!");

                var cControl = service.GetCharacteristic(C_WAV_CTRL);
                if (cControl == null)
                    throw new Exception("Cant find characteristics!");

                var cRead = service.GetCharacteristic(C_WAV_READ);
                if (cRead == null)
                    throw new Exception("Cant find characteristics!");

                try
                {
                    const int NUMBER_OF_PARTS = 23;
                    const int BLOCKSIZE = 150;

                    if (!await Gatt.SubscribeAsync(cRead, true, TimeSpan.FromSeconds(5), true))
                        throw new Exception("Subscription error!");

                    var result = new VPenData();
                    var request = new byte[]
                         {
                                ViPen_Get_Data_Vel,
                                0
                         };

                    var writeTask = Gatt.Write(request, cControl, TimeSpan.FromSeconds(2), false);
                    var readTask = Gatt.ReadBlockWithNotificationAsync(C_WAV_READ, TimeSpan.FromSeconds(30), token, BLOCKSIZE * NUMBER_OF_PARTS);
                    await Task.WhenAll(writeTask, readTask);
                    if (!writeTask.Result)
                        throw new Exception("Characteristics set error!");

                    if (readTask.Result == null)
                        throw new Exception("Blocks read error!");

                    var all = readTask.Result;
                    result.Header = all.BytesToStruct<stVPenData>();
                    return result;
                }
                finally
                {
                    await Gatt.SubscribeAsync(cRead, false, TimeSpan.FromSeconds(5), true);
                }
            }
            finally
            {
                _connectionLock.Release();
            }
        }
    }
}