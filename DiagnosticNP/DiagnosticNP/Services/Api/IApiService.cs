using DiagnosticNP.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiagnosticNP.Services.Api
{
    public interface IApiService
    {
        Task<List<EquipmentNode>> GetEquipmentStructureAsync();
        Task<bool> UploadMeasurementsAsync(List<Measurement> measurements);
    }
}