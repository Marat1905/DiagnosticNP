using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using DiagnosticNP.Models.Vibrometer;
using Java.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DiagnosticNP.Droid.Bluetooth
{
    public class GATTConetionTools : BluetoothGattCallback
    {
        public bool IsGattDisconnected { get; private set; }
        public bool IsGattConnected { get; private set; }
        public bool IsServicesDiscovered { get; private set; }
        public BluetoothGatt Gatt { get; private set; }
        public bool IsMtuChanged { get; private set; }
        public bool IsDescriptorChanged { get; private set; }
        private readonly object _readLock = new object();

        public override void OnConnectionStateChange(BluetoothGatt gatt, [GeneratedEnum] GattStatus status, [GeneratedEnum] ProfileState newState)
        {
            if (newState == ProfileState.Connected)
            {
                IsGattConnected = true;
            }
            else if (newState == ProfileState.Disconnected)
            {
                IsGattConnected = false;
                IsGattDisconnected = true;
            }

            base.OnConnectionStateChange(gatt, status, newState);
        }

        public override void OnServicesDiscovered(BluetoothGatt gatt, [GeneratedEnum] GattStatus status)
        {
            if (status == GattStatus.Success)
                IsServicesDiscovered = true;
            base.OnServicesDiscovered(gatt, status);
        }

        public BluetoothDevice Device { get; private set; }

        public async Task<bool> ConnectGattAsync(BluetoothDevice device, TimeSpan timeout, bool autoConnect = true)
        {
            try
            {
                Device = device;
                if (Gatt != null && IsGattConnected)
                    return true;

                return await Task.Run(() =>
                {
                    IsGattConnected = false;

                    if (Build.VERSION.SdkInt >= BuildVersionCodes.LollipopMr1)
                    {
                        Gatt = device.ConnectGatt(Application.Context, autoConnect, this, BluetoothTransports.Le);
                        if (!autoConnect)
                            Gatt.Connect();
                        WaitFor(timeout, () => IsGattConnected);
                    }
                    else
                    {
                        Gatt = device.ConnectGatt(Application.Context, false, this);
                        Gatt.Connect();
                        WaitFor(timeout, () => IsGattConnected);
                    }


                    if (!IsGattConnected)
                    {
                        Gatt.Disconnect();
                        Gatt.Dispose();
                        Gatt = null;
                    }

                    return IsGattConnected;
                });
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
                return false;
            }
        }

        public async Task<bool> DisconnectGattAsync(TimeSpan timeout)
        {
            IsGattConnected = false;
            if (Gatt == null)
                return true;

            IsGattDisconnected = false;

            return await Task.Run(() =>
            {
                Gatt.Disconnect();

                WaitFor(timeout, () => IsGattDisconnected);

                return IsGattConnected;
            });
        }

        public async Task<int> ChangeMtuAsync(TimeSpan timeout, int mtu = 512)
        {
            try
            {
                if (this.Gatt == null) throw new Exception("GATT is not connected!");
                IsMtuChanged = false;
                return await Task.Run(() =>
                {
                    if (Gatt.RequestMtu(mtu))
                    {
                        WaitFor(timeout, () => IsMtuChanged);
                    }

                    if (!IsMtuChanged)
                        throw new Exception("Mtu request error!");

                    return mtu;
                });
            }
            catch
            {
                return -1;
            }
        }

        public async Task<bool> DiscoverServicesAsync(TimeSpan timeout)
        {
            if (this.Gatt == null) throw new Exception("GATT is not connected!");
            IsServicesDiscovered = false;

            return await Task.Run(() =>
            {
                Gatt.DiscoverServices();
                WaitFor(timeout, () => IsServicesDiscovered);
                return IsServicesDiscovered;
            });
        }

        public UUID ReadUUID { get; private set; }
        public MemoryStream ReadStream { get; private set; }

        public async Task<byte[]> ReadBlockWithNotificationAsync(UUID uuid, TimeSpan timeout, OperationToken token, int minSize)
        {
            if (this.Gatt == null) throw new Exception("GATT is not connected!");
            ReadUUID = uuid;
            ReadStream = new MemoryStream();
            try
            {
                await Task.Run(() =>
                {
                    var w = new Stopwatch();
                    w.Start();
                    long position = 0;
                    while (position < minSize)
                    {
                        if (token != null && token.IsAborted)
                            return;

                        lock (this._readLock)
                        {
                            if (ReadStream.Position != position)
                            {
                                w.Restart();
                                position = ReadStream.Position;
                            }
                        }

                        if (w.Elapsed > timeout)
                            throw new TimeoutException();

                        if (token != null)
                        {
                            token.Progress = position / (double)minSize;
                        }

                        Thread.Sleep(1);
                    }
                });

                if (ReadStream.Position >= minSize)
                    return ReadStream.ToArray();
                else
                    return null;
            }
            finally
            {
                ReadStream.Dispose();
                ReadStream = null;
            }
        }

        private bool CommonReadConfirmed { get; set; }
        private UUID CommonReadUUID { get; set; }

        public async Task<byte[]> Read(BluetoothGattCharacteristic characteristic, TimeSpan timeout, bool throwError = true)
        {
            CommonReadConfirmed = false;
            CommonReadUUID = characteristic.Uuid;

            if (!Gatt.ReadCharacteristic(characteristic) && throwError)
                throw new Exception("Characteristics read error!");

            var watch = new Stopwatch();
            watch.Start();
            while (watch.Elapsed < timeout)
            {
                if (CommonReadConfirmed)
                    return characteristic.GetValue();
                await Task.Delay(10);
            }

            throw new TimeoutException();
        }

        public override void OnCharacteristicRead(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, [GeneratedEnum] GattStatus status)
        {
            try
            {
                if (characteristic.Uuid.CompareTo(CommonReadUUID) == 0)
                {
                    CommonReadConfirmed = true;
                }
                base.OnCharacteristicRead(gatt, characteristic, status);
            }
            catch { return; }
        }

        private bool WriteConfirmed { get; set; }
        private UUID WriteUUID { get; set; }

        public async Task<bool> Write(byte[] value, BluetoothGattCharacteristic characteristic, TimeSpan timeout, bool throwError = true)
        {
            WriteUUID = characteristic.Uuid;
            if (!characteristic.SetValue(value) && throwError)
                throw new Exception("Characteristics set error!");
            WriteConfirmed = false;
            if (!Gatt.WriteCharacteristic(characteristic) && throwError)
                throw new Exception("Characteristics write error!");
            var watch = new Stopwatch();
            watch.Start();
            while (watch.Elapsed < timeout)
            {
                if (WriteConfirmed) return true;
                await Task.Delay(10);
            }

            throw new TimeoutException();
        }

        public override void OnCharacteristicWrite(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, [GeneratedEnum] GattStatus status)
        {
            try
            {
                if (characteristic.Uuid.CompareTo(WriteUUID) == 0)
                {
                    WriteConfirmed = true;
                }
                base.OnCharacteristicWrite(gatt, characteristic, status);
            }
            catch { return; }
        }

        public override void OnCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic)
        {
            try
            {
                if (ReadUUID == null) return;
                if (characteristic.Uuid.CompareTo(ReadUUID) == 0)
                {
                    var v = characteristic.GetValue();
                    if (v != null && v.Length > 0)
                    {
                        lock (this._readLock)
                        {
                            if (this.ReadStream == null) return;
                            this.ReadStream.Write(v, 0, v.Length);
                        }
                    }
                }

                base.OnCharacteristicChanged(gatt, characteristic);
            }
            catch { return; }
        }

        public override void OnDescriptorWrite(BluetoothGatt gatt, BluetoothGattDescriptor descriptor, [GeneratedEnum] GattStatus status)
        {
            IsDescriptorChanged = true;
            base.OnDescriptorWrite(gatt, descriptor, status);
        }

        public async Task<bool> SubscribeAsync(BluetoothGattCharacteristic characteristic, bool enable, TimeSpan timeout, bool indication)
        {
            if (this.Gatt == null) throw new Exception("GATT is not connected!");
            IsDescriptorChanged = false;

            return await Task.Run(() =>
            {
                this.Gatt.SetCharacteristicNotification(characteristic, enable);
                UUID CLIENT_CHARACTERISTIC_CONFIG = UUID.FromString("00002902-0000-1000-8000-00805f9b34fb");
                BluetoothGattDescriptor descriptor = characteristic.GetDescriptor(CLIENT_CHARACTERISTIC_CONFIG);
                if (descriptor != null)
                {
                    byte[] ENABLE_NOTIFICATION_VALUE = { 0x01, 0x00 };
                    if (indication)
                        ENABLE_NOTIFICATION_VALUE[0] = 2;
                    if (!descriptor.SetValue(enable ? ENABLE_NOTIFICATION_VALUE : new byte[] { 0x00, 0x00 }))
                        throw new Exception("Cant subscribe");
                    this.Gatt.WriteDescriptor(descriptor);
                    WaitFor(timeout, () => IsDescriptorChanged);
                }
                return IsDescriptorChanged;
            });
        }

        public override void OnMtuChanged(BluetoothGatt gatt, int mtu, [GeneratedEnum] GattStatus status)
        {
            if (status == GattStatus.Success)
                IsMtuChanged = true;
            base.OnMtuChanged(gatt, mtu, status);
        }

        private static bool WaitFor(TimeSpan interval, Func<bool> exit)
        {
            for (int i = 0; i < interval.TotalMilliseconds; i++)
            {
                if (exit.Invoke())
                    return true;
                Thread.Sleep(1);
            }

            return false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    Gatt?.Dispose();
                    ReadStream?.Dispose();
                }
                catch { return; }
                finally
                {
                    this.Device = null;
                    this.Gatt = null;
                    ReadUUID = null;
                    WriteUUID = null;
                    ReadStream = null;
                    CommonReadUUID = null;
                }
            }

            base.Dispose(disposing);
        }
    }
}