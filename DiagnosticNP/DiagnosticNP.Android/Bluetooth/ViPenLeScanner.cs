using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Runtime;
using DiagnosticNP.Services.Bluetooth;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

[assembly: Dependency(typeof(DiagnosticNP.Droid.Bluetooth.ViPenLeScanner))]

namespace DiagnosticNP.Droid.Bluetooth
{
    public class ViPenLeScanner : ScanCallback, ILeScanner
    {
        private readonly object _lock = new object();
        public const int MANUFACTURER = 13;
        public const string VIPEN = "ViPen";
        private BluetoothAdapter _adapter;

        public void Start()
        {
            lock (this._lock)
            {
                try
                {
                    this.IsRunning = false;
                    BluetoothManager manager = ForceEnableBluettoth();

                    this._adapter = manager.Adapter;

                    StartScan();
                    this.IsRunning = true;
                }
                catch
                {
                    throw new Exception();
                }
            }
        }

        private static BluetoothManager ForceEnableBluettoth()
        {
            var appContext = Android.App.Application.Context;
            var manager = (BluetoothManager)appContext.GetSystemService("bluetooth");

            if (!manager.Adapter.IsEnabled)
            {
                ForceEnabled = false;
                manager.Adapter.Enable();
                ForceEnabled = true;

                for (int i = 0; i < 64; i++)
                {
                    if (manager.Adapter.State == State.On)
                        break;
                    else
                        Thread.Sleep(100);
                }
            }

            return manager;
        }

        public void DisableBluetooth()
        {
            try
            {
                var appContext = Android.App.Application.Context;
                var manager = (BluetoothManager)appContext.GetSystemService("bluetooth");

                if (manager.Adapter.IsEnabled)
                {
                    manager.Adapter.Disable();
                }
            }
            catch { return; }
        }

        private void StartScan()
        {
            var builder = new ScanSettings.Builder();

            builder.SetScanMode(Android.Bluetooth.LE.ScanMode.LowLatency);
            var settings = builder.Build();

            this._adapter.BluetoothLeScanner.StartScan(new List<ScanFilter>(), settings, this);
        }

        public bool IsRunning { get; private set; }

        public void Stop()
        {
            lock (this._lock)
            {
                try
                {
                    this.IsRunning = false;
                    this._adapter.BluetoothLeScanner.StopScan(this);
                    this._adapter = null;
                }
                catch { return; }
            }
        }

        public static bool ForceEnabled { get; private set; }

        public event BleDataEventHandler NewData;

        protected virtual void OnNewData(BleData data)
        {
            NewData?.Invoke(this, new BleDataEventArgs(data));
        }

        protected virtual void OnNewDataAsync(object state)
        {
            var data = state as BleData;
            if (data == null) return;

            OnNewData(data);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    Stop();
                }
            }
            catch { return; }
            finally
            {
                base.Dispose(disposing);
            }
        }

        private int _errorCount = 0;
        private bool _isRestarting = false;

        public override async void OnScanFailed([GeneratedEnum] ScanFailure errorCode)
        {
            try
            {
                if (this._isRestarting)
                    return;

                this._errorCount++;
                if (this._errorCount > 50) return;
                await Task.Run(() => DisableAndRestart());
                base.OnScanFailed(errorCode);
            }
            catch { return; }
        }

        private void DisableAndRestart()
        {
            try
            {
                this._isRestarting = true;

                if (this._errorCount > 0)
                {
                    Stop();
                    DisableBluetooth();
                    Thread.Sleep(500);
                    Start();
                }
            }
            catch { return; }
            finally
            {
                this._isRestarting = false;
            }
        }

        public override void OnScanResult([GeneratedEnum] ScanCallbackType callbackType, ScanResult result)
        {
            try
            {
                this._errorCount = 0;
                var bytes = result.ScanRecord.GetManufacturerSpecificData(MANUFACTURER);

                if (bytes != null && result.Device.Name == VIPEN)
                {
                    Task.Factory.StartNew(OnNewDataAsync, new BleData(bytes, result.Device.Address));
                }
            }
            catch
            {
                return;
            }
            finally
            {
                base.OnScanResult(callbackType, result);
            }
        }

        public void Restart()
        {
            try
            {
                Stop();
                Start();
            }
            catch { return; }
        }
    }
}