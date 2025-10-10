// IVibrometerService.cs
using System;
using System.Threading.Tasks;

namespace DiagnosticNP.Services.Vibrometer
{
    public interface IVibrometerService : IDisposable
    {
        // События для уведомлений
        event EventHandler<VibrometerDataReceivedEventArgs> DataReceived;
        event EventHandler<string> ErrorOccurred;
        event EventHandler<string> StatusChanged;
        event EventHandler<bool> PollingStateChanged;

        // Свойства состояния
        bool IsPolling { get; }
        bool IsConnected { get; }
        bool IsBusy { get; }
        VibrometerData CurrentData { get; }

        // Основные методы
        Task InitializeAsync();
        Task<bool> ConnectAsync();
        Task DisconnectAsync();
        Task StartPollingAsync();
        Task StopPollingAsync();

        // Управление жизненным циклом
        Task CleanupAsync();
    }

    public class VibrometerDataReceivedEventArgs : EventArgs
    {
        public VibrometerData Data { get; }

        public VibrometerDataReceivedEventArgs(VibrometerData data)
        {
            Data = data;
        }
    }
}