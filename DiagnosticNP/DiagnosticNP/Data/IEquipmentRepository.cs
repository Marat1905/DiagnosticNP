using System.Collections.Generic;
using System.Threading.Tasks;
using DiagnosticNP.Models;

namespace DiagnosticNP.Data
{
    public interface IEquipmentRepository
    {
        Task<bool> SaveEquipmentStructureAsync(List<EquipmentNode> nodes);
        Task<List<EquipmentNode>> GetEquipmentStructureAsync();
        Task<bool> ClearEquipmentStructureAsync();
        Task<EquipmentNode> FindNodeByPathAsync(string path);
    }
}