using System;

namespace DiagnosticNP.Services.Vibrometer
{
    public static class VibrometerServiceFactory
    {
        public static IVibrometerService CreateService()
        {
            return new VibrometerService();
        }
    }
}