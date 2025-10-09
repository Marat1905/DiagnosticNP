using System.Collections.Generic;
using System.Threading.Tasks;
using DiagnosticNP.Models;

namespace DiagnosticNP.Data
{
    public interface IMeasurementRepository
    {
        Task<int> SaveMeasurementAsync(MeasurementData measurement);
        Task<List<MeasurementData>> GetMeasurementsByNodeAsync(string nodeId);
        Task<List<MeasurementData>> GetUnsyncedMeasurementsAsync();
        Task<bool> MarkAsSyncedAsync(int measurementId);
        Task<bool> ClearMeasurementsAsync();
        Task<bool> DeleteMeasurementAsync(int measurementId);
        Task<MeasurementData> GetLatestMeasurementAsync(string nodeId, string direction);
    }
}