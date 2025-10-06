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
            try
            {
                await Disconnect();
                Dispose();

                if (Gatt == null)
                    Gatt = new GATTConetionTools();
                var device = await Task.Run(() => FindBluetoothDevice(connection));
                for (int i = 0; i < 4; i++)
                {
                    try
                    {
                        if (await Gatt.ConnectGattAsync(device, TimeSpan.FromSeconds(10), false))
                            break;
                    }
                    catch { continue; }
                }
                LongFrame = false;
                if ((await Gatt.ChangeMtuAsync(TimeSpan.FromSeconds(5), 512)) > 100)
                    LongFrame = true;

                if (!await Gatt.DiscoverServicesAsync(TimeSpan.FromSeconds(10)))
                    throw new Exception("Cant discover services!");

                if (IsConnected)
                    Gatt.Gatt.RequestConnectionPriority(GattConnectionPriority.High);

                return IsConnected;
            }
            catch
            {
                return false;
            }
        }

        public async Task Disconnect()
        {
            try
            {
                await Gatt?.DisconnectGattAsync(TimeSpan.FromSeconds(5));
                Gatt?.Gatt?.Close();
            }
            catch { return; }
        }

        public void Dispose()
        {
            try
            {
                Gatt?.Dispose();
            }
            catch { return; }
            finally
            {
                Gatt = null;
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

        public async Task Start(string connection, OperationToken token)
        {
            if (!IsConnected)
                throw new Exception("GATT is not connected!");

            var service = Gatt.Gatt.GetService(SERVICE);
            if (service == null)
                throw new Exception("Service discover error!");

            var cControl = service.GetCharacteristic(C_CTRL);
            if (cControl == null)
                throw new Exception("Cant find characteristics!");

            if (!await Gatt.Write(BitConverter.GetBytes(ViPen_Command_Start), cControl, TimeSpan.FromSeconds(2), false))
                throw new Exception("Characteristics set error!");
        }

        public async Task Stop(string connection, OperationToken token)
        {
            if (!IsConnected)
                throw new Exception("GATT is not connected!");

            var service = Gatt.Gatt.GetService(SERVICE);
            if (service == null)
                throw new Exception("Service discover error!");

            var cControl = service.GetCharacteristic(C_CTRL);
            if (cControl == null)
                throw new Exception("Cant find characteristics!");

            if (!await Gatt.Write(BitConverter.GetBytes(ViPen_Command_Stop), cControl, TimeSpan.FromSeconds(2), false))
                throw new Exception("Characteristics set error!");
        }

        public async Task<stUser_DataViPen> ReadUserData(string connection, OperationToken token)
        {
            if (!IsConnected)
                throw new Exception("GATT is not connected!");

            var service = Gatt.Gatt.GetService(SERVICE);
            if (service == null)
                throw new Exception("Service discover error!");

            var cReading = service.GetCharacteristic(C_READING);
            if (cReading == null)
                throw new Exception("Cant find characteristics!");

            var result = await Gatt.Read(cReading, TimeSpan.FromSeconds(3));

            return result.BytesToStruct<stUser_DataViPen>();
        }

        public async Task<VPenData> Download(string connection, OperationToken token)
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
    }
}