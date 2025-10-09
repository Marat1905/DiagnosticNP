using System.Collections.Generic;
using System.Threading.Tasks;
using DiagnosticNP.Models;

namespace DiagnosticNP.Services
{
    public interface IApiService
    {
        Task<List<EquipmentNode>> DownloadControlPointsAsync();
        Task<bool> UploadMeasurementsAsync(List<MeasurementData> measurements);
    }
}