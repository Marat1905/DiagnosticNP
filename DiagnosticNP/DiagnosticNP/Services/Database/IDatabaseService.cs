using DiagnosticNP.Models.Equipment;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiagnosticNP.Services.Database
{
    public interface IDatabaseService
    {
        SQLite.SQLiteConnection GetConnection();
        Task InitializeDatabaseAsync();
    }

    public interface IEquipmentRepository
    {
        Task<List<EquipmentNode>> GetAllNodesAsync();
        Task<EquipmentNode> GetNodeByIdAsync(string id);
        Task<List<EquipmentNode>> GetNodesByParentIdAsync(string parentId);
        Task SaveNodeAsync(EquipmentNode node);
        Task SaveNodesAsync(List<EquipmentNode> nodes);
        Task DeleteAllNodesAsync();
        Task<List<EquipmentNode>> GetNodesByNfcFilterAsync(string nfcFilter);

        Task SaveMeasurementAsync(MeasurementData measurement);
        Task<List<MeasurementData>> GetMeasurementsByNodeIdAsync(string nodeId);
        Task<List<MeasurementData>> GetUnsyncedMeasurementsAsync();
        Task MarkMeasurementsAsSyncedAsync();
        Task DeleteAllMeasurementsAsync();
    }
}