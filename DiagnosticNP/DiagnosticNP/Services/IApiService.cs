using DiagnosticNP.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiagnosticNP.Services
{
    public interface IApiService
    {
        Task<List<ControlPoint>> GetControlPointsAsync();
        Task<bool> UploadMeasurementsAsync(List<Measurement> measurements);
    }
}