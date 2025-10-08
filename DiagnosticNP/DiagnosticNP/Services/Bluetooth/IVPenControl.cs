using DiagnosticNP.Models.Vibrometer;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace DiagnosticNP.Services.Bluetooth
{
    public interface IVPenControl : IDisposable
    {
        Task<bool> ConnectAsync(string connection, OperationToken token);
        Task<bool> StartMeasurementAsync(string connection, OperationToken token);
        Task<VPenData> Download(string connection, OperationToken token);
        Task<stUser_DataViPen> ReadUserData(string connection, OperationToken token);
        Task Start(string connection, OperationToken token);
        Task Stop(string connection, OperationToken token);
        Task Disconnect();
        bool IsConnected { get; }
    }

    public static class VPenControlManager
    {
        public static IVPenControl GetController()
        {
            return DependencyService.Get<IVPenControl>(DependencyFetchTarget.NewInstance);
        }
    }
}