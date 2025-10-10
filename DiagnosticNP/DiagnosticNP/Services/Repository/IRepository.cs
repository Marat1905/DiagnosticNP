using DiagnosticNP.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiagnosticNP.Services.Repository
{
    public interface IRepository
    {
        Task InitializeDatabaseAsync();

        // Equipment Nodes
        Task SaveEquipmentNodesAsync(List<EquipmentNode> nodes);
        Task<List<EquipmentNode>> GetEquipmentNodesAsync();
        Task ClearEquipmentNodesAsync();

        // Measurements
        Task SaveMeasurementAsync(Measurement measurement);
        Task<List<Measurement>> GetMeasurementsAsync(bool includeUploaded = false);
        Task<List<Measurement>> GetPendingUploadMeasurementsAsync();
        Task MarkAsUploadedAsync(int measurementId);
        Task DeleteUploadedMeasurementsAsync();
        Task ClearAllMeasurementsAsync();
    }
}