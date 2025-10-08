using System;

namespace DiagnosticNP.Services.Bluetooth
{
    public interface ILeScanner : IDisposable
    {
        void Start();

        void Stop();

        bool IsRunning { get; }

        void Restart();

        event BleDataEventHandler NewData;
    }

    public class BleData
    {
        public BleData(byte[] data, string address)
        {
            this.Data = data;
            this.Address = address;
        }

        public byte[] Data { get; }
        public string Address { get; }
    }

    public class BleDataEventArgs : EventArgs
    {
        public BleDataEventArgs(BleData data)
        {
            this.Data = data;
        }

        public BleData Data { get; private set; }
    }

    public delegate void BleDataEventHandler(object sender, BleDataEventArgs e);

}
